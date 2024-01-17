using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  using ERDB = External.DB;

  public class FamilyGeometryByCurve : Component
  {
    public override Guid ComponentGuid => new Guid("6FBB9200-D725-4A0D-9360-89ACBE5B4D9F");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => "C";

    public FamilyGeometryByCurve()
    : base("Component Family Curve", "FamCrv", string.Empty, "Revit", "Component")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddCurveParameter("Curve", "C", string.Empty, GH_ParamAccess.item);
      manager[manager.AddBooleanParameter("Symbolic", "S", "Model or Symbolic", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddBooleanParameter("Visible", "V", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddIntegerParameter("Visibility", "Vs", "Visibility/Graphics Overrides", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Category(), "Subcategory", "Sc", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.GraphicsStyleType>(), "GraphicsStyle", "G", string.Empty, GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddCurveParameter("Curve", "C", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var curve = default(Curve);
      if (!DA.GetData("Curve", ref curve))
        return;

      curve = curve.DuplicateCurve();

      var symbolic = default(bool);
      if (DA.GetData("Symbolic", ref symbolic))
        curve.TrySetUserString(ARDB.BuiltInParameter.MODEL_OR_SYMBOLIC.ToString(), symbolic, true);

      var visible = default(bool);
      if (DA.GetData("Visible", ref visible))
        curve.TrySetUserString(ARDB.BuiltInParameter.IS_VISIBLE_PARAM.ToString(), visible, true);

      var visibility = default(int);
      if (DA.GetData("Visibility", ref visibility))
        curve.TrySetUserString(ARDB.BuiltInParameter.GEOM_VISIBILITY_PARAM.ToString(), (ERDB.FamilyElementVisibility) visibility, ERDB.FamilyElementVisibility.DefaultModel);

      var subCategoryId = default(ARDB.ElementId);
      if (DA.GetData("Subcategory", ref subCategoryId))
        curve.TrySetUserString(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), subCategoryId);

      var graphicsStyleType = default(ARDB.GraphicsStyleType);
      if (DA.GetData("GraphicsStyle", ref graphicsStyleType))
        curve.TrySetUserString(ARDB.BuiltInParameter.FAMILY_CURVE_GSTYLE_PLUS_INVISIBLE.ToString(), graphicsStyleType, ARDB.GraphicsStyleType.Projection);

      DA.SetData("Curve", curve);
    }
  }
}
