using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using RhinoInside.Revit.GH.Exceptions;

#if REVIT_2024
  [ComponentVersion(introduced: "1.16"), ComponentRevitAPIVersion(min: "2024.0")]
  public class AddToposolid : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("1A85EE3C-F045-462C-B6DA-E03F36C41E77");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.tertiary);

    public AddToposolid() : base
    (
      name: "Add Toposolid",
      nickname: "Toposolid",
      description: "Given its outline curve, it adds a Toposolid element to the active Revit document",
      category: "Revit",
      subCategory: "Site"
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
          Description = "Toposolid boundary profile",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
       (
        new Parameters.HostObjectType
        {
          Name = "Type",
          NickName = "T",
          Description = "Toposolid type",
          Optional = true,
          //SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Toposolid
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
       (
        new Parameters.LevelConstraint
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the {_Toposolid_.ToLowerInvariant()}.{Environment.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Boundary'.",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Toposolid()
        {
          Name = _Toposolid_,
          NickName = _Toposolid_.Substring(0, 1),
          Description = $"Output {_Toposolid_}",
        }
      )
    };

    const string _Toposolid_ = "Toposolid";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.LEVEL_PARAM,
      ARDB.BuiltInParameter.TOPOSOLID_HEIGHTABOVELEVEL_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Toposolid>
      (
        doc.Value, _Toposolid_, toposolid =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;

          var boundaryElevation = Interval.Unset;
          {
            var tol = GeometryTolerance.Model;
            var normal = default(Vector3d);
            var maxArea = 0.0; var maxIndex = 0;
            for (int index = 0; index < boundary.Count; ++index)
            {
              var loop = boundary[index];
              if (loop is null) return null;
              if
              (
                loop.IsShort(tol.ShortCurveTolerance) ||
                !loop.IsClosed ||
                !loop.TryGetPlane(out var plane, tol.VertexTolerance) ||
                plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
              )
                throw new RuntimeArgumentException(nameof(boundary), "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary);

              boundaryElevation = Interval.FromUnion(boundaryElevation, new Interval(plane.OriginZ, plane.OriginZ));
              boundary[index] = loop.Simplify(CurveSimplifyOptions.All, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

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

            if (!boundaryElevation.IsValid) return null;
          }

          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.HostObjectType type, doc, ARDB.BuiltInCategory.OST_Toposolid)) return null;

          if (!(type.Value is ARDB.ToposolidType toposolidType))
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid toposolid type.");

          if (!Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation)) return null;

          // Solve optional Base
          ERDB.ElevationElementReference.SolveBase
          (
            doc.Value, GeometryEncoder.ToInternalLength(boundaryElevation.Mid),
            0.0, ref baseElevation
          );

          // Compute
          toposolid = Reconstruct(toposolid, doc.Value, boundary, toposolidType, baseElevation.Value);
          DA.SetData(_Toposolid_, toposolid);
          return toposolid;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.Toposolid toposolid,
      IList<Curve> boundaries,
      ARDB.ToposolidType type,
      ARDB.Level baseLevel,
      double baseOffset
    )
    {
      if (toposolid is null) return false;

      if (!(toposolid.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis)))
        return false;

      if (toposolid.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(toposolid.Document, new ARDB.ElementId[] { toposolid.Id }, type.Id))
        {
          if (toposolid.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            toposolid = toposolid.Document.GetElement(id) as ARDB.Toposolid;
        }
        else return false;
      }

      toposolid.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(baseLevel.Id);
      toposolid.get_Parameter(ARDB.BuiltInParameter.TOPOSOLID_HEIGHTABOVELEVEL_PARAM).Update(baseOffset);

      return true;
    }

    ARDB.Toposolid Create
    (
      ARDB.Document document,
      IList<Curve> boundary,
      ARDB.ToposolidType type,
      ARDB.Level baseLevel
    )
    {
      // We create a Level here to obtain a toposolid with a <not associated> `SketckPlane`
      var level = ARDB.Level.Create(document, 0.0);

      var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);
      var toposolid = ARDB.Toposolid.Create(document, curveLoops, type.Id, level.Id);

      toposolid.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(baseLevel.Id);
      document.Delete(level.Id);

      return toposolid;
    }

    ARDB.Toposolid Reconstruct
    (
      ARDB.Toposolid toposolid,
      ARDB.Document doc,
      IList<Curve> boundary,
      ARDB.ToposolidType type,
      ERDB.ElevationElementReference baseElevation
    )
    {
      if (!baseElevation.IsLevelConstraint(out var baseLevel, out var baseOffset)) return default;

      if (!Reuse(ref toposolid, boundary, type, baseLevel, baseOffset ?? 0.0))
      {
        toposolid = toposolid.ReplaceElement
        (
          Create(doc, boundary, type, baseLevel),
          ExcludeUniqueProperties
        );
      }

      return toposolid;
    }
  }
#endif
}
