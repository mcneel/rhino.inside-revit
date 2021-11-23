using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ElementTypes
{
  public class ElementTypeSimilar : Component
  {
    public override Guid ComponentGuid => new Guid("BA9C72C5-EC88-450B-B736-BE6D827FA2F3");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override string IconTag => "S";

    public ElementTypeSimilar() : base
    (
      name: "Similar Types",
      nickname: "SimTypes",
      description: "Obtains a set of types that are similar to Type",
      category: "Revit",
      subCategory: "Type"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "ElementType to query for its similar types", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Types", "T", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementType = default(ARDB.ElementType);
      if (!DA.GetData("Type", ref elementType) || elementType is null)
        return;

      DA.SetDataList("Types", elementType.GetSimilarTypes().Select(x => Types.ElementType.FromElementId(elementType.Document, x)));
    }
  }
}
