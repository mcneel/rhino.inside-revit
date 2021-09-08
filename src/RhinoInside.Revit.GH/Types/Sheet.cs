using System;
using System.Globalization;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Sheet")]
  public interface IGH_Sheet : IGH_Element { }

  [Kernel.Attributes.Name("Sheet")]
  public class Sheet : Element, IGH_Sheet
  {
    protected override Type ValueType => typeof(DB.ViewSheet);
    public static explicit operator DB.ViewSheet(Sheet value) => value?.Value;
    public new DB.ViewSheet Value => base.Value as DB.ViewSheet;

    string IGH_Goo.TypeName => "Revit Sheet";

    public Sheet() { }
    public Sheet(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public Sheet(DB.ViewSheet sheet) : base(sheet) { }

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
