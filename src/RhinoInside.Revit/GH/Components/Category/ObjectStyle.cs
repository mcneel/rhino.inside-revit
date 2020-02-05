using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CategoryObjectStyle : Component
  {
    public override Guid ComponentGuid => new Guid("1DD8AE78-F7DA-4F26-8353-4CCE6B925DC6");

    public CategoryObjectStyle()
    : base("Category.ObjectStyle", "ObjectStyle", string.Empty, "Revit", "Category")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category to query", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddIntegerParameter("LineWeight [projection]", "LWP", "Category line weight [projection]", GH_ParamAccess.item);
      manager.AddIntegerParameter("LineWeight [cut]", "LWC", "Category line weigth [cut]", GH_ParamAccess.item);
      manager.AddColourParameter("LineColor", "LC", "Category line color", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Element(), "LinePattern [projection]", "LPP", "Category line pattern [projection]", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Element(), "LinePattern [cut]", "LPC", "Category line pattern [cut]", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Material(), "Material", "M", "Category material", GH_ParamAccess.item);
      manager.AddBooleanParameter("Cuttable", "C", "Indicates if the category is cuttable or not", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Category category = null;
      if (!DA.GetData("Category", ref category))
        return;

      var doc = category?.Document();

      DA.SetData("LineWeight [projection]", category?.GetLineWeight(DB.GraphicsStyleType.Projection));
      DA.SetData("LineWeight [cut]", category?.GetLineWeight(DB.GraphicsStyleType.Cut));
      DA.SetData("LineColor", category?.LineColor.ToRhino());
      DA.SetData("LinePattern [projection]", doc?.GetElement(category.GetLinePatternId(DB.GraphicsStyleType.Projection)));
      DA.SetData("LinePattern [cut]", doc?.GetElement(category.GetLinePatternId(DB.GraphicsStyleType.Cut)));
      DA.SetData("Material", category?.Material);
      DA.SetData("Cuttable", category?.IsCuttable);
    }
  }
}
