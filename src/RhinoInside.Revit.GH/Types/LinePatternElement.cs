using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.UI.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class LinePatternElement : Element
  {
    public override string TypeName => "Revit Line Pattern";
    public override string TypeDescription => "Represents a Revit Line Pattern Element";
    public override bool IsValid => Document.IsValid() && Id.IsLinePatternId(Document);
    public override string DisplayName => Id.IntegerValue == -3000010 ? "Solid" : base.DisplayName;
    protected override Type ScriptVariableType => typeof(DB.LinePatternElement);

    public LinePatternElement() { }
    public LinePatternElement(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public LinePatternElement(DB.LinePatternElement value) : base(value) { }

    new public static LinePatternElement FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (id.IsLinePatternId(doc))
        return new LinePatternElement(doc, id);

      return null;
    }

    public override sealed bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      var parameterId = DB.ElementId.InvalidElementId;
      switch (source)
      {
        case DB.LinePatternElement element: SetValue(element.Document, element.Id); return true;
        case DB.ElementId id: parameterId = id; break;
        case int integer: parameterId = new DB.ElementId(integer); break;
      }

      if (parameterId.IsLinePatternId(Revit.ActiveDBDocument))
      {
        SetValue(Revit.ActiveDBDocument, parameterId);
        return true;
      }

      return base.CastFrom(source);
    }

    #region IGH_ElementId
    public override bool LoadElement()
    {
      if (Document is null)
      {
        Id = null;
        if (!Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc))
        {
          Document = null;
          return false;
        }

        Document = doc;
      }
      else if (IsElementLoaded)
        return true;

      if (Document is object && Document.TryGetLinePatternId(UniqueID, out var value))
      {
        Id = value;
        return true;
      }

      return false;
    }
    #endregion
  }
}
