using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using GH.Exceptions;

  public class AddRoof : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("198E152B-6636-4D90-9443-AE77B8B1475E");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddRoof() : base
    (
      name: "Add Roof",
      nickname: "Roof",
      description: "Given its outline curve, it adds a Roof element to the active Revit document",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(Parameters.Document.AddTarget(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_Curve
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Roof boundary profile",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
       (
        new Parameters.HostObjectType
        {
          Name = "Type",
          NickName = "T",
          Description = "Roof type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Roofs
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
       (
        new Parameters.Level
        {
          Name = "Level",
          NickName = "L",
          Description = "Roof base level",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Roof()
        {
          Name = _Roof_,
          NickName = _Roof_.Substring(0, 1),
          Description = $"Output {_Roof_}",
        }
      )
    };

    const string _Roof_ = "Roof";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.ROOF_BASE_LEVEL_PARAM,
      ARDB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.FootPrintRoof>
      (
        doc.Value, _Roof_, roof =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;

          var tol = GeometryTolerance.Model;
          var normal = default(Vector3d);
          var maxArea = 0.0; var maxIndex = 0;
          for (int index = 0; index < boundary.Count; ++index)
          {
            var loop = boundary[index];
            if (loop is null) return null;
            var plane = default(Plane);
            if
            (
              loop.IsShort(tol.ShortCurveTolerance) ||
              !loop.IsClosed ||
              !loop.TryGetPlane(out plane, tol.VertexTolerance) ||
              plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
            )
              throw new RuntimeArgumentException(nameof(boundary), "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary);

            boundary[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

            using (var properties = AreaMassProperties.Compute(loop, tol.VertexTolerance))
            {
              if (properties is null) return null;
              if (properties.Area > maxArea)
              {
                normal = plane.Normal;
                maxArea = properties.Area;
                maxIndex = index;

                var orientation = loop.ClosedCurveOrientation(Plane.WorldXY);
                if (orientation == CurveOrientation.CounterClockwise)
                  normal.Reverse();
              }
            }
          }

          var bbox = Rhino.Geometry.BoundingBox.Empty;
          foreach (var geometry in boundary)
            bbox.Union(geometry.GetBoundingBox(true));

          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.HostObjectType type, doc, ARDB.ElementTypeGroup.RoofType)) return null;

          if (!(type.Value is ARDB.RoofType roofType))
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid roof type.");

          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, bbox.IsValid ? bbox.Min.Z : double.NaN)) return null;
          if (!Params.TryGetData(DA, "Structural", out bool? structural)) return null;

          // Compute
          roof = Reconstruct(roof, doc.Value, boundary, roofType, level.Value, structural ?? true);

          if (roof is object)
          {
            var heightAboveLevel = bbox.Min.Z / Revit.ModelUnits - level.Value.GetElevation();
            roof.get_Parameter(ARDB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM)?.Update(heightAboveLevel);
          }

          DA.SetData(_Roof_, roof);
          return roof;
        }
      );
    }

    bool Reuse(ref ARDB.FootPrintRoof roof, IList<Curve> boundaries, ARDB.RoofType type, ARDB.Level level, bool structural)
    {
      if (roof is null) return false;

      if (!(roof.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis)))
        return false;

      if (roof.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(roof.Document, new ARDB.ElementId[] { roof.Id }, type.Id))
        {
          if (roof.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            roof = roof.Document.GetElement(id) as ARDB.FootPrintRoof;
        }
        else return false;
      }

      bool succeed = true;
      succeed &= roof.get_Parameter(ARDB.BuiltInParameter.ROOF_BASE_LEVEL_PARAM).Update(level.Id);

      return succeed;
    }

    ARDB.FootPrintRoof Create(ARDB.Document document, IList<Curve> boundary, ARDB.RoofType type, ARDB.Level level, bool structural)
    {
      using (var curveArray = boundary.ToCurveArray())
      {
        var footPrintToModelCurvesMapping = new ARDB.ModelCurveArray();
        return document.Create.NewFootPrintRoof(curveArray, level, type, out footPrintToModelCurvesMapping);
      }
    }

    ARDB.FootPrintRoof Reconstruct(ARDB.FootPrintRoof roof, ARDB.Document doc, IList<Curve> boundary, ARDB.RoofType type, ARDB.Level level, bool structural)
    {
      if (!Reuse(ref roof, boundary, type, level, structural))
      {
        roof = roof.ReplaceElement
        (
          Create(doc, boundary, type, level, structural),
          ExcludeUniqueProperties
        );
      }

      return roof;
    }
  }
}
