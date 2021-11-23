using System;
using System.Linq;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Elements
{
  public class ElementMaterials : Component
  {
    public override Guid ComponentGuid => new Guid("93C18DFD-FAAB-4CF1-A681-C11754C2495D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "M";

    public ElementMaterials()
    : base
    (
     name: "Element Materials",
     nickname: "Materials",
     description: "Query element used materials",
     category: "Revit",
     subCategory: "Element"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to query for its materials", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Material(), "Materials", "M", "Materials this Element is made of", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Material(), "Paint", "P", "Materials used to paint this Element", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.Element element = null;
      if (!DA.GetData("Element", ref element) || !element.IsValid)
        return;

      DA.SetDataList("Materials", element.Value.GetMaterialIds(false).Select(x => Types.Material.FromElementId(element.Document, x)));
      DA.SetDataList("Paint",     element.Value.GetMaterialIds(true ).Select(x => Types.Material.FromElementId(element.Document, x)));
    }
  }
}
