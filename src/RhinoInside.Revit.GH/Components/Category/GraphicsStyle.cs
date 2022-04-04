using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Categories
{
  public class CategoryGraphicsStyle : Component
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new Guid("46139967-74FC-4820-BA20-B1DC7F30ABDE");
    protected override string IconTag => "G";

    public CategoryGraphicsStyle() : base
    (
      name: "Category GraphicsStyle",
      nickname: "GraphicsStyle",
      description: string.Empty,
      category: "Revit",
      subCategory: "Object Styles"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category to query", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicsStyle(), "Projection", "P", string.Empty, GH_ParamAccess.item);
      manager.AddParameter(new Parameters.GraphicsStyle(), "Cut", "C", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      ARDB.Category category = null;
      if (!DA.GetData("Category", ref category))
        return;

      DA.SetData("Projection", category?.GetGraphicsStyle(ARDB.GraphicsStyleType.Projection));
      DA.SetData("Cut", category?.GetGraphicsStyle(ARDB.GraphicsStyleType.Cut));
    }
  }
}
