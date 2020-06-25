using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class FamilyInstance : GraphicalElementT<Types.GraphicalElement, DB.FamilyInstance>
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

    protected override Types.GraphicalElement PreferredCast(object data) => Types.GraphicalElement.FromValue(data as DB.FamilyInstance) as Types.GraphicalElement;
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

    protected override Types.Mullion PreferredCast(object data) => Types.Mullion.FromValue(data as DB.Mullion) as Types.Mullion;
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

    protected override Types.Panel PreferredCast(object data) => Types.Panel.FromValue(data as DB.Panel) as Types.Panel;
  }
}
