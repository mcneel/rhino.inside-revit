using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CategoryParameters : Component
  {
    public override Guid ComponentGuid => new Guid("189F0A94-D077-4B96-8A92-6D5334EF7157");
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.ParameterElement));

    public CategoryParameters() : base
    (
      "Category Parameters", "Parameters",
      "Gets a list of valid parameters for the specified category that can be used in a table view",
      "Revit", "Category"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "ParameterKeys", "K", "Parameter definitions list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var category = default(DB.Category);
      if (!DA.GetData("Category", ref category))
        return;

      if(category.Document() is DB.Document doc)
      {
        var parameterKeys = DB.TableView.GetAvailableParameters(doc, category.Id);
        DA.SetDataList("ParameterKeys", parameterKeys.Select(paramId => Types.ParameterKey.FromElementId(doc, paramId)));
      }
    }
  }
}
