using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.8")]
  public class AddDetailItem : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("FE258116-9184-41C9-8554-30BFFCC0E640");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddDetailItem() : base
    (
      name: "Add Detail Item",
      nickname: "DetItem",
      description: "Given its Location, it adds a detail item element to the active Revit document",
      category: "Revit",
      subCategory: "Annotation"
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
        new Param_Point()
        {
          Name = "Point",
          NickName = "P",
          Description = "Detail Component center.",
        }
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Rotation",
          NickName = "R",
          Description = "Detail Component rotation",
          Optional = true,
          AngleParameter = true,
        }, ParamRelevance.Secondary
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
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.FamilyInstance>
      (
        view.Document, _DetailItem_, detail =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.TryGetData(DA, "Rotation", out double? rotation)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out Types.FamilySymbol type, Types.Document.FromValue(view.Document), ARDB.BuiltInCategory.OST_DetailComponents)) return null;

          var viewPlane = new Plane(view.Origin.ToPoint3d(), view.RightDirection.ToVector3d(), view.UpDirection.ToVector3d());
          if (view.ViewType != ARDB.ViewType.ThreeD)
            point = viewPlane.ClosestPoint(point.Value);

          if (rotation.HasValue && Params.Input<Param_Number>("Rotation")?.UseDegrees == true)
            rotation = Rhino.RhinoMath.ToRadians(rotation.Value);

          // Compute
          detail = Reconstruct
          (
            detail,
            view,
            point.Value.ToXYZ(),
            rotation ?? 0.0,
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
      ARDB.FamilySymbol type
    )
    {
      if (detail is null) return false;

      if (detail.OwnerViewId != view.Id) return false;
      if (detail.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(detail.Document, new ARDB.ElementId[] { detail.Id }, type.Id))
        {
          if (detail.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            detail = detail.Document.GetElement(id) as ARDB.FamilyInstance;
        }
        else return false;
      }

      return false;
    }

    ARDB.FamilyInstance Create(ARDB.View view, ARDB.XYZ point, ARDB.FamilySymbol type)
    {
      return (view.Document.IsFamilyDocument ?
        view.Document.FamilyCreate.NewFamilyInstance
        (
          point,
          type,
          view
        ) :
        view.Document.Create.NewFamilyInstance
        (
          point,
          type,
          view
        ));
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance detail,
      ARDB.View view,
      ARDB.XYZ origin,
      double rotation,
      ARDB.FamilySymbol type
    )
    {
      if (!Reuse(ref detail, view, type))
      {
        detail = detail.ReplaceElement
        (
          Create(view, origin, type),
          ExcludeUniqueProperties
        );
      }

      detail.SetLocation(origin, rotation);
      return detail;
    }
  }
}
