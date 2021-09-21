using System;
using System.Globalization;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Sheet")]
  public interface IGH_Sheet : IGH_View { }

  [Kernel.Attributes.Name("Sheet")]
  public class ViewSheet : View, IGH_Sheet
  {
    protected override Type ValueType => typeof(DB.ViewSheet);
    public static explicit operator DB.ViewSheet(ViewSheet value) => value?.Value;
    public new DB.ViewSheet Value => base.Value as DB.ViewSheet;

    public ViewSheet() { }
    public ViewSheet(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public ViewSheet(DB.ViewSheet sheet) : base(sheet) { }

    public override string DisplayName
    {
      get
      {
        if (Value is DB.ViewSheet sheet)
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

    public string SheetIssueDate
    {
      get => Value?.GetParameterValue<string>(DB.BuiltInParameter.SHEET_ISSUE_DATE);
      set
      {
        if (value is object)
          Value?.UpdateParameterValue(DB.BuiltInParameter.SHEET_ISSUE_DATE, value);
      }
    }

    public bool? SheetScheduled
    {
      get => Value?.GetParameterValue<bool>(DB.BuiltInParameter.SHEET_SCHEDULED);
      set
      {
        if (value is object)
          Value?.UpdateParameterValue(DB.BuiltInParameter.SHEET_SCHEDULED, value);
      }
    }
    #endregion
  }
}
