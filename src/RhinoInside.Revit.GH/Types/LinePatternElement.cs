using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Line Pattern")]
  public class LinePatternElement : Element
  {
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

      if (source is ValueTuple<DB.Document, DB.ElementId> tuple)
      {
        (document, patternId) = tuple;
      }
      else if (source is IGH_Goo goo)
      {
        if (source is IGH_Element element)
        {
          document = element.Document;
          source = element.Id;
        }
        else source = goo.ScriptVariable();
      }

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

      return false;
    }

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
