using System;
using System.Globalization;
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
  }
}
