using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;
  using External.UI.Extensions;

  public class FamilyInstance : GraphicalElement<Types.IGH_FamilyInstance, ARDB.FamilyInstance>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("804BD6AC-8A4A-4D79-A734-330534B3C435");
    protected override string IconTag => "C";

    public FamilyInstance() : base
    (
      name: "Component",
      nickname: "Component",
      description: "Contains a collection of Revit component elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    protected override Types.IGH_FamilyInstance InstantiateT() => new Types.FamilyInstance();
  }

  public class FamilySymbol : ElementType<Types.IGH_FamilySymbol, ARDB.FamilySymbol>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("786D9097-DF9C-4513-9B5F-278667FBE999");

    public FamilySymbol() : base
    (
      name: "Component Type",
      nickname: "ComType",
      description: "Contains a collection of Revit component types",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    protected override Types.IGH_FamilySymbol InstantiateT() => new Types.FamilySymbol();

    public static new bool GetDataOrDefault<TOutput>
    (
      IGH_Component component,
      IGH_DataAccess DA,
      string name,
      out TOutput type,
      Types.Document document,
      ARDB.BuiltInCategory categoryId
    )
      where TOutput : class
    {
      type = default;

      try
      {
        if (!component.Params.TryGetData(DA, name, out type)) return false;
        if (type is null)
        {
          var data = Types.ElementType.FromElementId(document.Value, document.Value.GetDefaultFamilyTypeId(new ARDB.ElementId(categoryId)));
          if (data?.IsValid != true)
            throw new Exceptions.RuntimeArgumentException(name, $"No suitable {((ERDB.Schemas.CategoryId) categoryId).Label} type has been found.");

          if (data is Types.FamilySymbol symbol && !symbol.Value.IsActive)
            symbol.Value.Activate();

          type = data as TOutput;
          if (type is null)
            return data.CastTo(out type);
        }

        return true;
      }
      finally
      {
        // Validate type
        switch (type)
        {
          case ARDB.Element element:
          {
            if (!document.Value.IsEquivalent(element.Document))
              throw new Exceptions.RuntimeArgumentException(name, $"Failed to assign a {nameof(type)} from a diferent document.");

            if (element.Category.ToBuiltInCategory() != categoryId)
              throw new Exceptions.RuntimeArgumentException(name, $"Collected {nameof(type)} is not on category '{((ERDB.Schemas.CategoryId) categoryId).Label}'.");

            if (element is ARDB.FamilySymbol symbol && !symbol.IsActive)
              symbol.Activate();
          }
          break;

          case Types.Element goo:
          {
            if (!document.Value.IsEquivalent(goo.Document))
              throw new Exceptions.RuntimeArgumentException(name, $"Failed to assign a {nameof(type)} from a diferent document.");

            if (goo.Category.Id.ToBuiltInCategory() != categoryId)
              throw new Exceptions.RuntimeArgumentException(name, $"Collected {nameof(type)} is not on category '{((ERDB.Schemas.CategoryId) categoryId).Label}'.");

            if (goo is Types.FamilySymbol symbol && !symbol.Value.IsActive)
              symbol.Value.Activate();
          }
          break;
        }
      }
    }
  }

  public class Mullion : GraphicalElement<Types.Mullion, ARDB.Mullion>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("6D845CBD-1962-4912-80C1-F47FE99AD54A");

    public Mullion() : base
    (
      name: "Mullion",
      nickname: "Mullion",
      description: "Contains a collection of Revit curtain grid mullion elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Curve" }
    );
    #endregion
  }

  public class Panel : GraphicalElement<Types.Panel, ARDB.FamilyInstance>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("CEF5DD61-BC7D-4E66-AE94-E990B193ACDC");

    public Panel() : base
    (
      name: "Panel",
      nickname: "Panel",
      description: "Contains a collection of Revit curtain grid panel elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    public override bool AllowElement(ARDB.Element elem) => Types.Panel.IsValidElement(elem);

  }

}
