using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.GH.Types.Elements;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Documents.Params
{
  public class ParameterKey : Element
  {
    public override string TypeName => "Revit ParameterKey";
    public override string TypeDescription => "Represents a Revit parameter definition";
    override public object ScriptVariable() => null;
    protected override Type ScriptVariableType => typeof(DB.ParameterElement);

    #region IGH_ElementId
    public override bool LoadElement()
    {
      if (Document is null)
      {
        Value = null;
        if (!Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc))
        {
          Document = null;
          return false;
        }

        Document = doc;
      }
      else if (IsElementLoaded)
        return true;

      if (Document is object)
        return Document.TryGetParameterId(UniqueID, out m_value);

      return false;
    }
    #endregion

    public ParameterKey() { }
    public ParameterKey(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public ParameterKey(DB.ParameterElement element) : base(element) { }

    new public static ParameterKey FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (id.IsParameterId(doc))
        return new ParameterKey(doc, id);

      return null;
    }

    public override sealed bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      var parameterId = DB.ElementId.InvalidElementId;
      switch (source)
      {
        case DB.ParameterElement parameterElement: SetValue(parameterElement.Document, parameterElement.Id); return true;
        case DB.Parameter parameter: SetValue(parameter.Element.Document, parameter.Id); return true;
        case DB.ElementId id: parameterId = id; break;
        case int integer: parameterId = new DB.ElementId(integer); break;
      }

      if (parameterId.IsParameterId(Revit.ActiveDBDocument))
      {
        SetValue(Revit.ActiveDBDocument, parameterId);
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Guid)))
      {
        target = (Q) (object) (Document.GetElement(Value) as DB.SharedParameterElement)?.GuidValue;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    new class Proxy : Element.Proxy
    {
      public Proxy(ParameterKey o) : base(o) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => true;
      public override string FormatInstance()
      {
        int value = owner.Value?.IntegerValue ?? -1;
        if (Enum.IsDefined(typeof(DB.BuiltInParameter), value))
          return ((DB.BuiltInParameter) value).ToString();

        return value.ToString();
      }
      public override bool FromString(string str)
      {
        if (Enum.TryParse(str, out DB.BuiltInParameter builtInParameter))
        {
          owner.SetValue(owner.Document ?? Revit.ActiveUIDocument.Document, new DB.ElementId(builtInParameter));
          return true;
        }

        return false;
      }

      DB.BuiltInParameter builtInParameter => owner.Id.TryGetBuiltInParameter(out var bip) ? bip : DB.BuiltInParameter.INVALID;
      DB.ParameterElement parameter => IsBuiltIn ? null : owner.Document?.GetElement(owner.Id) as DB.ParameterElement;

      [System.ComponentModel.Description("The Guid that identifies this parameter as a shared parameter.")]
      public Guid Guid => (parameter as DB.SharedParameterElement)?.GuidValue ?? Guid.Empty;
      [System.ComponentModel.Description("API Object Type.")]
      public override Type ObjectType => IsBuiltIn ? typeof(DB.BuiltInParameter) : parameter?.GetType();

      [System.ComponentModel.Category("Other"), System.ComponentModel.Description("Internal parameter data storage type.")]
      public DB.StorageType StorageType => builtInParameter != DB.BuiltInParameter.INVALID ? Revit.ActiveDBDocument.get_TypeOfStorage(builtInParameter) : parameter?.GetDefinition().ParameterType.ToStorageType() ?? DB.StorageType.None;
      [System.ComponentModel.Category("Other"), System.ComponentModel.Description("Visible in UI.")]
      public bool Visible => IsBuiltIn ? Valid : parameter?.GetDefinition().Visible ?? false;
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);

    public override string DisplayName
    {
      get
      {
        try
        {
          if (Id is object && Id.TryGetBuiltInParameter(out var builtInParameter))
            return DB.LabelUtils.GetLabelFor(builtInParameter) ?? base.DisplayName;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        return base.DisplayName;
      }
    }
  }
}
