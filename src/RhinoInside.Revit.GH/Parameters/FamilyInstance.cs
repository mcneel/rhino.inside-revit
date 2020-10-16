using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class FamilyInstance : GraphicalElementT<Types.IGH_FamilyInstance, DB.FamilyInstance>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden; //GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("804BD6AC-8A4A-4D79-A734-330534B3C435");
    protected override string IconTag => "C";

    public FamilyInstance() : base
    (
      name: "Component",
      nickname: "Component",
      description: "Represents a Revit Component element.",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }
  }

  public class Mullion : GraphicalElementT<Types.Mullion, DB.Mullion>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("6D845CBD-1962-4912-80C1-F47FE99AD54A");

    public Mullion() : base
    (
      name: "Mullion",
      nickname: "Mullion",
      description: "Represents a Revit curtain grid mullion element.",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }
  }

  public class Panel : GraphicalElementT<Types.Panel, DB.Panel>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("CEF5DD61-BC7D-4E66-AE94-E990B193ACDC");

    public Panel() : base
    (
      name: "Panel",
      nickname: "Panel",
      description: "Represents a Revit curtain grid panel element.",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }
  }

  public class FamilySymbol : ElementTypeParameter<Types.IGH_FamilySymbol, DB.FamilySymbol>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("786D9097-DF9C-4513-9B5F-278667FBE999");

    public FamilySymbol() : base("Family Type", "FamType", "Represents a Revit document family type.", "Params", "Revit Primitives") { }

    protected override Types.IGH_FamilySymbol InstantiateT() => new Types.FamilySymbol();
  }
}
