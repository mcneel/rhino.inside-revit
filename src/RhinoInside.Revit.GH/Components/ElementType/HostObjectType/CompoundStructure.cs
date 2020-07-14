using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class HostObjectTypeCompoundStructure : Component
  {
    public override Guid ComponentGuid => new Guid("024619EF-58FF-47C1-8833-96BA2F2B677B");
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    protected override string IconTag => "CS";

    // Note: although categorized incorrectly by the Revit API,
    // Compound Structure is not an inherent property of the HostObject
    // Component is renames and organized under Element
    public HostObjectTypeCompoundStructure() : base
    (
      name: "Element Type Compound Structure",
      nickname: "TypCompStruct",
      description: "Get element type compound structure",
      category: "Revit",
      subCategory: "Element"
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
