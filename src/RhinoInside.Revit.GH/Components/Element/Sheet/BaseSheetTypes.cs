using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Sheets
{
  using External.DB.Extensions;

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

    protected ARDB.ViewSheet Reconstruct(ARDB.ViewSheet sheet, ARDB.Document doc, TSheetHandler handler)
    {
      if (!Reuse(sheet, handler))
      {
        sheet = sheet.ReplaceElement
        (
          Create(doc, handler),
          BaseSheetHandler.ExcludeUniqueProperties
        );
      }

      return sheet;
    }

    bool Reuse(ARDB.ViewSheet sheet, TSheetHandler handler)
    {
      if (!handler.CanUpdateSheet(sheet))
      {
        // let's change the sheet number so other sheets can be created with same id
        if (sheet is object)
          sheet.SheetNumber = sheet.UniqueId;

        return false;
      }
      else
      {
        handler.UpdateSheet(sheet);
        return true;
      }
    }

    ARDB.ViewSheet Create(ARDB.Document doc, TSheetHandler handler)
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

    public ARDB.ViewSheet Template { get; set; }

    public abstract ARDB.ViewSheet CreateSheet(ARDB.Document doc);

    public virtual bool CanUpdateSheet(ARDB.ViewSheet sheet) => sheet is object;

    public virtual void UpdateSheet(ARDB.ViewSheet sheet)
    {
      // we are not duplicating sheets. that is a very complex process
      // involving various view types, some can only exist on a single sheet
      // let's just copy the parameters from template instead
      if (Template is ARDB.ViewSheet template)
        sheet.CopyParametersFrom(template, ExcludeUniqueProperties);

      sheet.SheetNumber = Number;

      if (Name is object)
        sheet.Name = Name;

      if (SheetScheduled.HasValue)
        sheet.UpdateParameterValue(ARDB.BuiltInParameter.SHEET_SCHEDULED, SheetScheduled.Value);
    }

    public BaseSheetHandler(string number)
    {
      Number = number;
    }

    public static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.SHEET_NUMBER,
    };
  }

  public class SheetHandler : BaseSheetHandler
  {
    public SheetHandler(string number) : base(number) { }

    public override ARDB.ViewSheet CreateSheet(ARDB.Document doc) =>
      ARDB.ViewSheet.Create(doc, ARDB.ElementId.InvalidElementId);
  }

  public class PlaceholderSheetHandler : BaseSheetHandler
  {
    public PlaceholderSheetHandler(string number) : base(number) { }

    public override ARDB.ViewSheet CreateSheet(ARDB.Document doc) =>
      ARDB.ViewSheet.CreatePlaceholder(doc);
  }

  public class AssemblySheetHandler : BaseSheetHandler
  {
    public ARDB.AssemblyInstance Assembly { get; set; }

    public AssemblySheetHandler(string number) : base(number) { }

    public override ARDB.ViewSheet CreateSheet(ARDB.Document doc)
    {
      return Assembly is ARDB.AssemblyInstance assm ?
        ARDB.AssemblyViewUtils.CreateSheet(assm.Document, assm.Id, ARDB.ElementId.InvalidElementId) :
        null;
    }

    public override bool CanUpdateSheet(ARDB.ViewSheet sheet)
    {
      if (!base.CanUpdateSheet(sheet))
        return false;

      return sheet.AssemblyInstanceId == Assembly.Id;
    }
  }
}
