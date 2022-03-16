using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class AssemblyInstance : GraphicalElementT<Types.AssemblyInstance, ARDB.AssemblyInstance>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("d7958ccf-9847-4e27-996b-b7fdc0eb1086");
    public AssemblyInstance() : base
    (
      name: "Assembly",
      nickname: "A",
      description: "Contains a collection of Revit assemblies",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
}
