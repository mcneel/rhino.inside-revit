using System;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using RhinoInside.Revit.GH.ElementTracking;
using RhinoInside.Revit.External.DB.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Sheet
{
  public abstract class BaseSheetByNumber<TSheetHandler> : ElementTrackerComponent where TSheetHandler: BaseSheetHandler
  {
    public BaseSheetByNumber(string name, string nickname, string description) : base
    (
      name: name,
      nickname: nickname,
      description: description,
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected DB.ViewSheet Reconstruct(DB.ViewSheet sheet, DB.Document doc, TSheetHandler handler)
    {
      if (sheet is null || !Reuse(sheet, handler))
        sheet = sheet.ReplaceElement
        (
          Create(doc, handler),
          BaseSheetHandler.ExcludeUniqueProperties
        );

      return sheet;
    }

    bool Reuse(DB.ViewSheet sheet, TSheetHandler handler)
    {
      if (!handler.CanUpdateSheet(sheet))
      {
        // let's change the sheet number so other sheets can be created with same id
        sheet.SheetNumber = sheet.UniqueId;
        return false;
      }
      else
      {
        handler.UpdateSheet(sheet);
        return true;
      }
    }

    DB.ViewSheet Create(DB.Document doc, TSheetHandler handler)
    {
      var sheet = handler.CreateSheet(doc);
      handler.UpdateSheet(sheet);
      return sheet;
    }
  }

  public abstract class BaseSheetHandler
  {
    // required
    public string Number { get; }

    // Note (eirannejad Sep 10, 2021)
    // I decided for name to be a required field, although revit automatically names the sheets (e.g. "Unnamed" in English)
    // This matches the behaviour of Add View components since a name is required for a view and has to be unique
    // Sheets are a type of revit view but the naming is less stringent in the API and two sheets can have the same name
    // Having name optional, would cause a problem detecting if a sheet can be reused:
    //   If component creates a set of sheets without a name input, it would generate the sheets
    //   with revit's default name. If later a name input is connected, the component can correctly
    //   update the sheet names. But if the name input would be disconnected, the component has no way of
    //   knowing the previous or default name for the sheet and will leave the names as they are

    // use name that is set
    // OR, name from template
    // OR, default
    string _name = null;
    public string Name
    {
      get => _name ?? Template?.Name;
      set => _name = value;
    }

    public bool? SheetScheduled { get; set; }

    public DB.FamilySymbol TitleBlockType { get; set; }

    public DB.ViewSheet Template { get; set; }

    public abstract DB.ViewSheet CreateSheet(DB.Document doc);

    public virtual bool CanUpdateSheet(DB.ViewSheet sheet)
    {
      bool canUpdate = true;

      // if titleblock on existing does not match the titleblock provided (or not),
      // on the inputs, do not reuse so Revit places tblock by default at proper location
      // and whether sheet has titleblock or not matches the input
      var tblocks = new DB.FilteredElementCollector(sheet.Document, sheet.Id).
        WhereElementIsNotElementType().
        OfCategory(DB.BuiltInCategory.OST_TitleBlocks);

      var blocksCount = tblocks.GetElementCount();
      if (TitleBlockType is DB.ElementType tblockType)
      {
        if (blocksCount == 1)
        {
          var tblock = tblocks.FirstElement();
          if (!tblock.GetTypeId().Equals(tblockType.Id))
            canUpdate = false;
        }
        else canUpdate = false;
      }
      else if (blocksCount > 0)
        canUpdate = false;

      // placeholders are handled by another component
      if (sheet.IsPlaceholder)
        canUpdate = false;

      return canUpdate;
    }

    public virtual void UpdateSheet(DB.ViewSheet sheet)
    {
      // we are not duplicating sheets. that is a very complex process
      // involving various view types, some can only exist on a single sheet
      // let's just copy the parameters from template instead
      if (Template is DB.ViewSheet template)
        sheet.CopyParametersFrom(template, ExcludeUniqueProperties);

      sheet.SheetNumber = Number;

      if (Name is object)
        sheet.Name = Name;

      if (SheetScheduled.HasValue)
        sheet.UpdateParameterValue(DB.BuiltInParameter.SHEET_SCHEDULED, SheetScheduled.Value);
    }

    public BaseSheetHandler(string number)
    {
      Number = number;
    }

    public static readonly DB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      DB.BuiltInParameter.SHEET_NUMBER,
    };
  }

  public class SheetHandler : BaseSheetHandler
  {
    public SheetHandler(string number) : base(number) { }

    public override DB.ViewSheet CreateSheet(DB.Document doc)
    {
      // determine titleblock to use
      var tblockId = TitleBlockType?.Id ?? DB.ElementId.InvalidElementId;
      return DB.ViewSheet.Create(doc, tblockId);
    }
  }

  public class PlaceholderSheetHandler : BaseSheetHandler
  {
    public PlaceholderSheetHandler(string number) : base(number) { }

    public override DB.ViewSheet CreateSheet(DB.Document doc)
      => DB.ViewSheet.CreatePlaceholder(doc);

    public override bool CanUpdateSheet(DB.ViewSheet sheet) => sheet.IsPlaceholder;
  }

  public class AssemblySheetHandler : BaseSheetHandler
  {
    public DB.AssemblyInstance Assembly { get; set; }

    public AssemblySheetHandler(string number) : base(number) { }

    public override DB.ViewSheet CreateSheet(DB.Document doc)
    {
      // determine titleblock to use
      var tblockId = TitleBlockType?.Id ?? DB.ElementId.InvalidElementId;
      if (Assembly is DB.AssemblyInstance assm)
        return DB.AssemblyViewUtils.CreateSheet(assm.Document, assm.Id, tblockId);
      return null;
    }

    public override bool CanUpdateSheet(DB.ViewSheet sheet)
    {
      if (sheet.AssemblyInstanceId != Assembly.Id)
        return false;

      return base.CanUpdateSheet(sheet);
    }
  }
}

