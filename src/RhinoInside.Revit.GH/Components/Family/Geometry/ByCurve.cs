using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyGeometryByCurve : Component
  {
    public override Guid ComponentGuid => new Guid("6FBB9200-D725-4A0D-9360-89ACBE5B4D9F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override string IconTag => "C";

    public FamilyGeometryByCurve()
    : base("FamilyGeometry.ByCurve", "FamilyGeometry.ByCurve", string.Empty, "Revit", "Family")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddCurveParameter("Curve", "C", string.Empty, GH_ParamAccess.item);
      manager[manager.AddBooleanParameter("Visible", "V", string.Empty, GH_ParamAccess.item, true)].Optional = true;
      manager[manager.AddParameter(new Parameters.Category(), "Subcategory", "S", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.GraphicsStyleType>(), "GraphicsStyle", "G", string.Empty, GH_ParamAccess.item)].Optional = true; ;
      manager[manager.AddIntegerParameter("Visibility", "S", string.Empty, GH_ParamAccess.item, -1)].Optional = true;
      manager[manager.AddBooleanParameter("Symbolic", "S", string.Empty, GH_ParamAccess.item, false)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddCurveParameter("Curve", "C", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var curve = default(Rhino.Geometry.Curve);
      if (!DA.GetData("Curve", ref curve))
        return;

      curve = curve.DuplicateCurve();

      var visible = true;
      if (DA.GetData("Visible", ref visible))
        curve.SetUserString(DB.BuiltInParameter.IS_VISIBLE_PARAM.ToString(), visible ? null : "0");

      var subCategoryId = DB.ElementId.InvalidElementId;
      if (DA.GetData("Subcategory", ref subCategoryId))
        curve.SetUserString(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), subCategoryId.IsValid() ? subCategoryId.ToString() : null);

      var graphicsStyleType = DB.GraphicsStyleType.Projection;
      if (DA.GetData("GraphicsStyle", ref graphicsStyleType))
        curve.SetUserString(DB.BuiltInParameter.FAMILY_CURVE_GSTYLE_PLUS_INVISIBLE.ToString(), graphicsStyleType != DB.GraphicsStyleType.Projection ? graphicsStyleType.ToString() : null);

      var visibility = -1;
      if (DA.GetData("Visibility", ref visibility))
        curve.SetUserString(DB.BuiltInParameter.GEOM_VISIBILITY_PARAM.ToString(), visibility == -1 ? null : visibility.ToString());

      var symbolic = false;
      if (DA.GetData("Symbolic", ref symbolic))
        curve.SetUserString(DB.BuiltInParameter.MODEL_OR_SYMBOLIC.ToString(), symbolic ? "1" : null);

      DA.SetData("Curve", curve);
    }
  }
}
