using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Host
{
  public class HostObjectTypeCompoundStructure : Component
  {
    public override Guid ComponentGuid => new Guid("024619EF-58FF-47C1-8833-96BA2F2B677B");
    public override GH_Exposure Exposure => GH_Exposure.senary;
    protected override string IconTag => "CS";

    public HostObjectTypeCompoundStructure() : base
    (
      name: "Host Type Compound Structure",
      nickname: "CompStruct",
      description: "Get host object type compound structure",
      category: "Revit",
      subCategory: "Host"
    )
    { }


    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        new Parameters.HostObjectType(),
        name: "Type",
        nickname: "T",
        description: string.Empty,
        GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.DataObject<DB.CompoundStructure>(),
        name: "Compound Structure",
        nickname: "CS",
        description: "Compound Structure definition of given type",
        access: GH_ParamAccess.item
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var type = default(DB.HostObjAttributes);
      if (!DA.GetData("Type", ref type))
        return;

      DA.SetData("Compound Structure", new Types.DataObject<DB.CompoundStructure>(type.GetCompoundStructure(), type.Document));
    }
  }
}
