using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Exceptions;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.15")]
  public class AddBeamSystem : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("5E6EE9A3-3AA0-4186-9E5E-30081A56ABEE");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddBeamSystem() : base
    (
      name: "Add Beam System",
      nickname: "S-Beam System",
      description: "Given its profile curves, it adds a beam system to the active Revit document",
      category: "Revit",
      subCategory: "Structure"
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
        new Param_Curve()
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Structural framing boundary.",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.SketchPlane()
        {
          Name = "Work Plane",
          NickName = "WP",
          Description = "Work plane of beam system."
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Beam Type",
          NickName = "BT",
          Description = "Beam type.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Vector
        {
          Name = "Direction",
          NickName = "D",
          Description = "Structural framing system orientation.",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.BeamSystem()
        {
          Name = _BeamSystem_,
          NickName = _BeamSystem_.Substring(0, 1),
          Description = $"Output {_BeamSystem_}",
        }
      )
    };

    const string _BeamSystem_ = "Beam System";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
      ARDB.BuiltInParameter.INSTANCE_ELEVATION_PARAM,
      ARDB.BuiltInParameter.BEAM_SYSTEM_3D_PARAM,
      ARDB.BuiltInParameter.SKETCH_PLANE_PARAM
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.BeamSystem>
      (
        doc.Value, _BeamSystem_, beamSystem =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;
          if (!Params.GetData(DA, "Work Plane", out Types.SketchPlane sketchPlane)) return null;

          var is3D = false;
          var boundaryElevation = Interval.Unset;
          {
            var tol = GeometryTolerance.Model;
            for (int i = 0; i < boundary.Count; i++)
            {
              if (boundary[i] is null) return null;
              if
              (
                boundary[i].IsShort(tol.ShortCurveTolerance) ||
                !boundary[i].IsClosed
              )
              {
                boundaryElevation = Interval.Unset;
                break;
              }

              if (!boundary[i].TryGetPlane(out var plane, tol.VertexTolerance))
                is3D = true;

              if (!boundary[i].IsInPlane(sketchPlane.Location, tol.VertexTolerance))
                boundary[i] = Curve.ProjectToPlane(boundary[i], sketchPlane.Location);

              boundaryElevation = Interval.FromUnion(boundaryElevation, new Interval(sketchPlane.Location.OriginZ, sketchPlane.Location.OriginZ));
            }

            if (!boundaryElevation.IsValid || boundaryElevation.Length > tol.VertexTolerance)
              throw new Exceptions.RuntimeArgumentException("Boundary", "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary);

          }

          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Beam Type", out Types.FamilySymbol beamType, doc, ARDB.BuiltInCategory.OST_StructuralFraming)) return null;

          if (!Params.TryGetData(DA, "Direction", out Vector3d? dir)) return null;

          // Compute
          beamSystem = Reconstruct(beamSystem, doc.Value, boundary, sketchPlane.Value,
                                   dir.HasValue ? dir.Value : new Vector3d(1,0,0),
                                   is3D,  beamType.Value);

          DA.SetData(_BeamSystem_, beamSystem);
          return beamSystem;
        }
      );
    }

    bool Reuse
    (
      ARDB.BeamSystem beamSystem,
      IList<Curve> boundary,
      ARDB.SketchPlane sketchPlane,
      Vector3d direction,
      bool is3D,
      ARDB.FamilySymbol beamType
    )
    {
      if (beamSystem is null) return false;

      var currentSketchPlane = beamSystem.GetSketch().SketchPlane;
      using (var currentPlane = currentSketchPlane.GetPlane())
      using (var newPlane = sketchPlane.GetPlane())
      {
        if (currentPlane.Normal.IsAlmostEqualTo(newPlane.Normal))
        {
          if (!currentPlane.Origin.IsAlmostEqualTo(newPlane.Origin))
          {
            ARDB.ElementTransformUtils.MoveElement(currentSketchPlane.Document, currentSketchPlane.Id, (newPlane.Origin.Z - currentPlane.Origin.Z) * currentPlane.Normal);
            return true;
          }
        }
        else
          return false;
      }

      if (!(beamSystem.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundary, sketch.SketchPlane.GetPlane().ToPlane().Normal)))
        return false;

      beamSystem.get_Parameter(ARDB.BuiltInParameter.BEAM_SYSTEM_3D_PARAM).Update(is3D);

      if (!beamSystem.Direction.IsCodirectionalTo(GeometryEncoder.ToXYZ(direction))) return false;

      if (beamSystem.BeamType.Id != beamType.Id) beamSystem.BeamType = beamType;
      
      return true;
    }

    ARDB.BeamSystem Create(ARDB.Document doc, List<ARDB.Curve> profile, ARDB.SketchPlane sketchPlane, ARDB.XYZ direction, bool is3D, ARDB.FamilySymbol beamType)
    {
      ARDB.BeamSystem element = ARDB.BeamSystem.Create(doc, profile, sketchPlane, direction, is3D);
      element.BeamType = beamType;

      // We turn analytical model off by default
      element.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
      return element;
    }

    ARDB.BeamSystem Reconstruct
    (
      ARDB.BeamSystem beamSystem,
      ARDB.Document doc,
      IList<Curve> boundary,
      ARDB.SketchPlane sketchPlane,
      Vector3d dir,
      bool is3D,
      ARDB.FamilySymbol beamType
    )
    {
      if (!Reuse(beamSystem, boundary, sketchPlane, dir, is3D, beamType))
      {
        beamSystem = beamSystem.ReplaceElement
        (
          Create(doc, boundary.SelectMany(x => GeometryEncoder.ToCurveMany(x)).ToList(), sketchPlane, GeometryEncoder.ToXYZ(dir), is3D, beamType),
          ExcludeUniqueProperties
        );
        beamSystem.Document.Regenerate();
      }

      return beamSystem;
    }
  }
}

