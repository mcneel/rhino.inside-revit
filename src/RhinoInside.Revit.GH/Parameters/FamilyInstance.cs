using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class FamilyInstance : GraphicalElementT<Types.IGH_FamilyInstance, ARDB.FamilyInstance>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden; //GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("804BD6AC-8A4A-4D79-A734-330534B3C435");
    protected override string IconTag => "C";

    public FamilyInstance() : base
    (
      name: "Component",
      nickname: "Component",
      description: "Contains a collection of Revit component elements",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }

    protected override Types.IGH_FamilyInstance InstantiateT() => new Types.FamilyInstance();
  }

  public class Mullion : GraphicalElementT<Types.Mullion, ARDB.Mullion>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("6D845CBD-1962-4912-80C1-F47FE99AD54A");

    public Mullion() : base
    (
      name: "Mullion",
      nickname: "Mullion",
      description: "Contains a collection of Revit curtain grid mullion elements",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Curve" }
    );
    #endregion
  }

  public class Panel : GraphicalElementT<Types.Panel, ARDB.FamilyInstance>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("CEF5DD61-BC7D-4E66-AE94-E990B193ACDC");

    public Panel() : base
    (
      name: "Panel",
      nickname: "Panel",
      description: "Contains a collection of Revit curtain grid panel elements",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }

    public override bool AllowElement(ARDB.Element elem) => Types.Panel.IsValidElement(elem);

  }

  public class FamilySymbol : ElementType<Types.IGH_FamilySymbol, ARDB.FamilySymbol>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("786D9097-DF9C-4513-9B5F-278667FBE999");

    public FamilySymbol() : base("Family Type", "FamType", "Contains a collection of Revit family types", "Params", "Revit Primitives") { }

    protected override Types.IGH_FamilySymbol InstantiateT() => new Types.FamilySymbol();
  }
}
