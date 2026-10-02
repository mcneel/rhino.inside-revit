using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using RhinoInside.Revit.GH.Exceptions;

  [ComponentVersion(introduced: "1.3"), ComponentRevitAPIVersion(min: "2022.0")]
  public class AddCeiling : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("A39BBDF2-78F2-4501-BB6E-F9CC3E83516E");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddCeiling() : base
    (
      name: "Add Ceiling",
      nickname: "Ceiling",
      description: "Given its outline curve, it adds a Ceiling element to the active Revit document",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
       (
        new Param_Curve
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Ceiling boundary profile",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
       (
        new Parameters.HostObjectType
        {
          Name = "Type",
          NickName = "T",
          Description = "Ceiling type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Ceilings
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
       (
        new Parameters.Level
        {
          Name = "Level",
          NickName = "L",
          Description = "Ceiling base level",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Ceiling()
        {
          Name = _Ceiling_,
          NickName = _Ceiling_.Substring(0, 1),
          Description = $"Output {_Ceiling_}",
        }
      )
    };

    const string _Ceiling_ = "Ceiling";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.LEVEL_PARAM,
      ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2022
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Ceiling>
      (
        doc.Value, _Ceiling_, ceiling =>
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

          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.HostObjectType type, doc, ARDB.ElementTypeGroup.CeilingType)) return null;

          if (!(type.Value is ARDB.CeilingType ceilingType))
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid ceiling type.");

          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, bbox.IsValid ? bbox.Min.Z : double.NaN)) return null;

          // Compute
          ceiling = Reconstruct(ceiling, doc.Value, boundary, ceilingType, level.Value);

          if (ceiling is object)
          {
            var heightAboveLevel = bbox.Min.Z / Revit.ModelUnits - level.Value.GetElevation();
            ceiling.get_Parameter(ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM)?.Update(heightAboveLevel);
          }

          DA.SetData(_Ceiling_, ceiling);
          return ceiling;
        }
      );
#endif
    }

#if REVIT_2022
    bool Reuse(ref ARDB.Ceiling ceiling, IList<Curve> boundaries, ARDB.CeilingType type, ARDB.Level level)
    {
      if (ceiling is null) return false;

      if (!(ceiling.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis)))
        return false;

      if (ceiling.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(ceiling.Document, new ARDB.ElementId[] { ceiling.Id }, type.Id))
        {
          if (ceiling.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            ceiling = ceiling.Document.GetElement(id) as ARDB.Ceiling;
        }
        else return false;
      }

      bool succeed = true;
      succeed &= ceiling.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(level.Id);

      return succeed;
    }

    ARDB.Ceiling Create(ARDB.Document document, IList<Curve> boundary, ARDB.CeilingType type, ARDB.Level level)
    {
      var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);
      return ARDB.Ceiling.Create(document, curveLoops, type.Id, level.Id);
    }

    ARDB.Ceiling Reconstruct(ARDB.Ceiling ceiling, ARDB.Document doc, IList<Curve> boundary, ARDB.CeilingType type, ARDB.Level level)
    {
      if (!Reuse(ref ceiling, boundary, type, level))
      {
        ceiling = ceiling.ReplaceElement
        (
          Create(doc, boundary, type, level),
          ExcludeUniqueProperties
        );
      }

      return ceiling;
    }
#endif
  }
}
