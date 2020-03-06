using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CategoryGraphicsStyle : Component
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new Guid("46139967-74FC-4820-BA20-B1DC7F30ABDE");
    protected override string IconTag => "G";

    public CategoryGraphicsStyle()
    : base("Category.GraphicsStyle", "Category.GraphicsStyle", string.Empty, "Revit", "Category")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category to query", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicsStyle(), "Projection", "P", string.Empty, GH_ParamAccess.list);
      manager.AddParameter(new Parameters.GraphicsStyle(), "Cut", "C", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Category category = null;
      if (!DA.GetData("Category", ref category))
        return;

      DA.SetData("Projection", category?.GetGraphicsStyle(DB.GraphicsStyleType.Projection));
      DA.SetData("Cut", category?.GetGraphicsStyle(DB.GraphicsStyleType.Cut));
    }
  }
}
