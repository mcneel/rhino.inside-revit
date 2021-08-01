using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyTypes : Component
  {
    public override Guid ComponentGuid => new Guid("742836D7-01C4-485A-BFA8-6CDA3F121F7B");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => "T";

    public FamilyTypes() : base
    (
      name: "Family Types",
      nickname: "Types",
      description: "Obtains a set of types that are owned by Family",
      category: "Revit",
      subCategory: "Family"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Family(), "Family", "F", "Family to query for its types", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Types", "T", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Family family = null;
      if (!DA.GetData("Family", ref family))
        return;

      DA.SetDataList("Types", family?.GetFamilySymbolIds().Select(x => Types.ElementType.FromElementId(family.Document, x)));
    }
  }
}
