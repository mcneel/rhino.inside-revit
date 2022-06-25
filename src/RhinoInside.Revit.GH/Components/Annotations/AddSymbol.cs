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
  public class AddSymbol : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("2BEB60BA-F32C-469D-829F-5EC2B80492D9");
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    protected override string IconTag => string.Empty;

    public AddSymbol() : base
    (
      name: "Add Symbol",
      nickname: "Symbol",
      description: "Given its Location, it adds a symbol element to the active Revit document",
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
          Description = "View to add a specific symbol."
        }
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Point",
          NickName = "P",
          Description = "Symbol center.",
        }
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Rotation",
          NickName = "R",
          Description = "Symbol rotation.",
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
          Description = "Symbol type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_GenericAnnotation
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
          Name = _Symbol_,
          NickName = _Symbol_.Substring(0, 1),
          Description = $"Output {_Symbol_}",
        }
      )
    };

    const string _Symbol_ = "Symbol";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.AnnotationSymbol>
      (
        view.Document, _Symbol_, detail =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.TryGetData(DA, "Rotation", out double? rotation)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out Types.FamilySymbol type, Types.Document.FromValue(view.Document), ARDB.BuiltInCategory.OST_GenericAnnotation)) return null;

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
            type.Value as ARDB.AnnotationSymbolType
          );

          DA.SetData(_Symbol_, detail);
          return detail;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.AnnotationSymbol detail,
      ARDB.View view,
      ARDB.AnnotationSymbolType type
    )
    {
      if (detail is null) return false;

      if (detail.OwnerViewId != view.Id) return false;
      if (detail.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(detail.Document, new ARDB.ElementId[] { detail.Id }, type.Id))
        {
          if (detail.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            detail = detail.Document.GetElement(id) as ARDB.AnnotationSymbol;
        }
        else return false;
      }

      return false;
    }

    ARDB.AnnotationSymbol Create(ARDB.View view, ARDB.XYZ point, ARDB.AnnotationSymbolType type)
    {
      return
      (
        view.Document.IsFamilyDocument ?
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
        )
      ) as ARDB.AnnotationSymbol;
    }

    ARDB.AnnotationSymbol Reconstruct
    (
      ARDB.AnnotationSymbol symbol,
      ARDB.View view,
      ARDB.XYZ origin,
      double rotation,
      ARDB.AnnotationSymbolType type
    )
    {
      if (!Reuse(ref symbol, view, type))
      {
        symbol = symbol.ReplaceElement
        (
          Create(view, origin, type),
          ExcludeUniqueProperties
        );
      }

      symbol.SetLocation(origin, rotation);
      return symbol;
    }
  }
}
