using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Categories
{
  using External.DB.Extensions;

  public class CategoryParameters : Component
  {
    public override Guid ComponentGuid => new Guid("189F0A94-D077-4B96-8A92-6D5334EF7157");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    public CategoryParameters() : base
    (
      name: "Category Parameters",
      nickname: "Parameters",
      description: "Gets a list of valid parameters for the specified category that can be used in a table view",
      category: "Revit",
      subCategory: "Object Styles"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "Parameters", "P", "Parameter definitions list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var category = default(Types.Category);
      if (!DA.GetData("Category", ref category))
        return;

      if(category.Document is ARDB.Document doc)
      {
        var parameterKeys = ARDB.TableView.GetAvailableParameters(doc, category.Id);
        DA.SetDataList("Parameters", parameterKeys.Select(paramId => Types.ParameterKey.FromElementId(doc, paramId)));
      }
    }
  }
}
