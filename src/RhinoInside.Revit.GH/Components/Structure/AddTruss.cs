using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.14")]
  public class AddTruss : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("C0B04DC7-9AD5-4E49-9043-17CB06076132");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddTruss() : base
    (
      name: "Add Truss",
      nickname: "S-Truss",
      description: "Given its location curve, it adds a truss to the active Revit document",
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
          Name = "Curve",
          NickName = "C",
          Description = "Truss location.",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Parameters.SketchPlane()
        {
          Name = "Work Plane",
          NickName = "WP",
          Description = "Work Plane of truss.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Truss type.",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Truss()
        {
          Name = _Truss_,
          NickName = _Truss_.Substring(0, 1),
          Description = $"Output {_Truss_}",
        }
      )
    };

    const string _Truss_ = "Truss";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
      ARDB.BuiltInParameter.INSTANCE_ELEVATION_PARAM,
      ARDB.BuiltInParameter.BEAM_SYSTEM_3D_PARAM
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Structure.Truss>
      (
        doc.Value, _Truss_, truss =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;
          if (!Params.GetData(DA, "Work Plane", out Types.SketchPlane sketchPlane)) return null;

          var tol = GeometryTolerance.Model;
          var plane = sketchPlane.Location;

          if (curve.IsShort(tol.ShortCurveTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (curve.IsClosed(tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (!curve.TryGetPlane(out var p, tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be planar and parallel to view plane.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if ((curve = Curve.ProjectToPlane(curve, plane)) is null)
            throw new Exceptions.RuntimeArgumentException("Curve", "Failed to project Curve into 'Work Plane'", curve);

          if (curve.GetNextDiscontinuity(Continuity.C1_continuous, curve.Domain.Min, curve.Domain.Max, Math.Cos(tol.AngleTolerance), Rhino.RhinoMath.SqrtEpsilon, out var _))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be C1 continuous.\nTolerance is {Rhino.RhinoMath.ToDegrees(tol.AngleTolerance):N1}°", curve);

          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Truss", out Types.FamilySymbol type, doc, ARDB.BuiltInCategory.OST_Truss)) return null;

          // Compute
          truss = Reconstruct(truss, doc.Value, curve, sketchPlane.Value, type.Value);

          DA.SetData(_Truss_, truss);
          return truss;
        }
      );
    }

    bool Reuse
    (
      ARDB.Structure.Truss truss,
      Curve curve,
      ARDB.SketchPlane sketchPlane,
      ARDB.FamilySymbol type
    )
    {
      if (truss is null) return false;
      if (truss.TrussType.Id != type.Id) truss.TrussType = type as ARDB.Structure.TrussType;

      using (var loc = truss.Location as ARDB.LocationCurve)
      {
        if (!loc.Curve.IsSameKindAs(curve.ToCurve()))
          return false;

        if (!loc.Curve.AlmostEquals(curve.ToCurve(), truss.Document.Application.VertexTolerance))
          loc.Curve = curve.ToCurve();
      }

      return true;
    }

    ARDB.Structure.Truss Create(ARDB.Document doc, ARDB.Curve curve, ARDB.SketchPlane sketchPlane, ARDB.FamilySymbol type)
    {
      ARDB.Structure.Truss element = ARDB.Structure.Truss.Create(doc, type.Id, sketchPlane.Id, curve);

      // We turn analytical model off by default
      element.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
      return element;
    }

    ARDB.Structure.Truss Reconstruct
    (
      ARDB.Structure.Truss truss,
      ARDB.Document doc,
      Curve curve,
      ARDB.SketchPlane sketchPlane,
      ARDB.FamilySymbol type
    )
    {
      if (!Reuse(truss, curve, sketchPlane, type))
      {
        truss = truss.ReplaceElement
        (
          Create(doc, GeometryEncoder.ToCurve(curve), sketchPlane, type),
          ExcludeUniqueProperties
        );
        truss.Document.Regenerate();
      }

      return truss;
    }
  }
}

