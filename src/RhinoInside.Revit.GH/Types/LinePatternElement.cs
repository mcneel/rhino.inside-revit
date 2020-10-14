using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.UI.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class LinePatternElement : Element
  {
    public override string TypeName => "Revit Line Pattern";
    public override string TypeDescription => "Represents a Revit Line Pattern Element";
    public override string DisplayName => base.DisplayName;
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
      if (base.CastFrom(source))
        return true;

      var document = Revit.ActiveDBDocument;
      var patternId = DB.ElementId.InvalidElementId;

      if (source is IGH_ElementId elementId)
      {
        document = elementId.Document;
        source = elementId.Id;
      }
      else if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case int integer: patternId = new DB.ElementId(integer); break;
        case DB.ElementId id: patternId = id; break;
      }

      if (patternId.TryGetBuiltInLinePattern(out var _))
      {
        SetValue(document, patternId);
        return true;
      }

      return base.CastFrom(source);
    }

    #region IGH_ElementId
    public override bool LoadElement()
    {
      if (IsReferencedElement && !IsElementLoaded)
      {
        Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc);
        Document = doc;

        Document.TryGetLinePatternId(UniqueID, out var id);
        Id = id;
      }

      return IsElementLoaded;
    }
    #endregion

    #region Properties
    public override string Name
    {
      get
      {
        if (Id is object && Id.TryGetBuiltInLinePattern(out var builtInLinePattern))
          return $"<{builtInLinePattern}>";

        return base.Name;
      }
      set
      {
        if (value is object && value != Name)
        {
          if (Id.IsBuiltInId())
            throw new InvalidOperationException($"BuiltIn fill pattern '{Name}' does not support assignment of a user-specified name.");

          base.Name = value;
        }
      }
    }
    #endregion
  }
}
