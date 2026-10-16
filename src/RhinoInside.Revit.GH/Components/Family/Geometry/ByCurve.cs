using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Families
{
  using ERDB = External.DB;

  public class FamilyGeometryByCurve : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("6FBB9200-D725-4A0D-9360-89ACBE5B4D9F");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => "C";

    public FamilyGeometryByCurve()
    : base("Component Family Curve", "FamCrv", string.Empty, "Revit", "Component")
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Param_Curve>("Curve", "C"),
      ParamDefinition.Create<Param_Boolean>("Visible", "V", string.Empty, defaultValue:true, optional:true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.FamilyElementVisibility>>("Visibility", "Vs", "Visibility/Graphics Overrides", defaultValue:ERDB.FamilyElementVisibilityDefaults.Model, optional:true),
      ParamDefinition.Create<Parameters.Category>("Subcategory", "Sc", optional:true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.GraphicsStyleType>>("Graphics Style", "Gs", string.Empty, defaultValue: ARDB.GraphicsStyleType.Projection, optional:true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Param_Curve>("Curve", "C"),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var curve = default(Curve);
      if (!DA.GetData("Curve", ref curve))
        return;

      curve = curve.DuplicateCurve();

      var visible = default(bool);
      if (DA.GetData("Visible", ref visible))
        curve.TrySetUserString(ARDB.BuiltInParameter.IS_VISIBLE_PARAM.ToString(), visible, true);

      var visibility = default(int);
      if (DA.GetData("Visibility", ref visibility))
        curve.TrySetUserString(ARDB.BuiltInParameter.GEOM_VISIBILITY_PARAM.ToString(), (ERDB.FamilyElementVisibility) visibility, ERDB.FamilyElementVisibilityDefaults.Model);

      var subCategoryId = default(ARDB.ElementId);
      if (DA.GetData("Subcategory", ref subCategoryId))
        curve.TrySetUserString(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), subCategoryId);

      var graphicsStyleType = default(ARDB.GraphicsStyleType);
      if (DA.GetData("Graphics Style", ref graphicsStyleType))
        curve.TrySetUserString(ARDB.BuiltInParameter.FAMILY_CURVE_GSTYLE_PLUS_INVISIBLE.ToString(), graphicsStyleType, ARDB.GraphicsStyleType.Projection);

      DA.SetData("Curve", curve);
    }
  }
}
