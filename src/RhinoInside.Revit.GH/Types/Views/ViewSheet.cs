using System;
using System.Globalization;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Sheet")]
  public class ViewSheet : View
  {
    protected override Type ValueType => typeof(ARDB.ViewSheet);
    public static explicit operator ARDB.ViewSheet(ViewSheet value) => value?.Value;
    public new ARDB.ViewSheet Value => base.Value as ARDB.ViewSheet;

    public ViewSheet() { }
    public ViewSheet(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ViewSheet(ARDB.ViewSheet sheet) : base(sheet) { }

    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.ViewSheet sheet)
        {
          FormattableString formatable = $"{sheet.SheetNumber} - {sheet.Name}";
          return formatable.ToString(CultureInfo.CurrentUICulture);
        }

        return base.DisplayName;
      }
    }

    #region Identity
    public bool? IsPlaceholder => Value?.IsPlaceholder;

    public string SheetNumber
    {
      get => Value?.SheetNumber;
      set
      {
        if (value is object && Value?.SheetNumber != value)
          Value.SheetNumber = value;
      }
    }

    public string SheetName
    {
      get => Value?.Name;
      set
      {
        if (value is object && Value?.Name != value)
          Value.Name = value;
      }
    }

    public string SheetIssueDate
    {
      get => Value?.GetParameterValue<string>(ARDB.BuiltInParameter.SHEET_ISSUE_DATE);
      set
      {
        if (value is object)
          Value?.UpdateParameterValue(ARDB.BuiltInParameter.SHEET_ISSUE_DATE, value);
      }
    }

    public bool? SheetScheduled
    {
      get => Value?.GetParameterValue<bool>(ARDB.BuiltInParameter.SHEET_SCHEDULED);
      set
      {
        if (value is object)
          Value?.UpdateParameterValue(ARDB.BuiltInParameter.SHEET_SCHEDULED, value);
      }
    }
    #endregion
  }
}
