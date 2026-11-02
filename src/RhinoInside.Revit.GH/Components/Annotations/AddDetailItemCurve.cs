using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.37")]
  public class AddDetailItemCurve : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("8B9F858F-CE8F-4023-B48C-794B6AFF910F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddDetailItemCurve() : base
    (
      name: "Add Detail Item (Curve)",
      nickname: "C-Item",
      description: "Given its Curve, it adds a line based detail item element to the active Revit document",
      category: "Revit",
      subCategory: "Annotate"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific detail component"
        }
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Curve",
          NickName = "C",
          Description = "Detail Component line.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Detail Component type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_DetailComponents
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = _DetailItem_,
          NickName = "DI",
          Description = $"Output {_DetailItem_}",
        }
      )
    };

    const string _DetailItem_ = "Detail Item";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view)) return;

      ReconstructElement<ARDB.FamilyInstance>
      (
        view.Document, _DetailItem_, detail =>
        {
          // Input
          if (!view.Value.IsAnnotationView()) throw new Exceptions.RuntimeArgumentException("View", $"View '{view.Nomen}' does not support detail items creation", view);
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out Types.FamilySymbol type, Types.Document.FromValue(view.Document), ARDB.BuiltInCategory.OST_DetailComponents)) return null;

          type.AssertPlacementType(ARDB.FamilyPlacementType.CurveBasedDetail);

          var viewPlane = view.DetailPlane;
          var tol = GeometryTolerance.Model;

          if (curve.IsClosed(tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (!curve.IsLinear(tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be a line like curve.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          var line = new Line(viewPlane.ClosestPoint(curve.PointAtStart), viewPlane.ClosestPoint(curve.PointAtEnd));
          if (line.Length < tol.ShortCurveTolerance)
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short once projected into the view plane.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          // Compute
          detail = Reconstruct
          (
            detail,
            view.Value,
            line.ToLine(),
            type.Value
          );

          DA.SetData(_DetailItem_, detail);
          return detail;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.FamilyInstance detail,
      ARDB.View view,
      ARDB.Line line,
      ARDB.FamilySymbol type
    )
    {
      if (detail is null) return false;

      if (detail.OwnerViewId != view.Id) return false;
      if (!(detail.Location is ARDB.LocationCurve)) return false;
      if (detail.GetTypeId() != type.Id)
      {
        if (!ARDB.Element.IsValidType(detail.Document, new ARDB.ElementId[] { detail.Id }, type.Id))
          return false;

        if (detail.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
          detail = detail.Document.GetElement(id) as ARDB.FamilyInstance;
      }

      if (detail?.Location is ARDB.LocationCurve locationCurve)
      {
        if (!locationCurve.Curve.AlmostEquals(line, GeometryTolerance.Internal.VertexTolerance))
          locationCurve.Curve = line;

        return true;
      }

      return false;
    }

    ARDB.FamilyInstance Create(ARDB.View view, ARDB.Line line, ARDB.FamilySymbol type)
    {
      using (var create = view.Document.Create())
        return create.NewFamilyInstance(line, type, view);
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance detail,
      ARDB.View view,
      ARDB.Line line,
      ARDB.FamilySymbol type
    )
    {
      if (!Reuse(ref detail, view, line, type))
      {
        detail = detail.ReplaceElement
        (
          Create(view, line, type),
          ExcludeUniqueProperties
        );

        detail.Document.Regenerate();
      }

      return detail;
    }
  }
}
