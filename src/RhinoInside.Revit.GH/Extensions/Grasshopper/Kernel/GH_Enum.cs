using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_Enumerate
  {
    bool IsEmpty { get; }
    string Text { get; }
  }

  public interface IGH_Flags
  {
    bool HasFlag(IGH_Flags flag);
    void SetFlag(IGH_Flags flag, bool value);
  }

  public static class GH_Enumerate
  {
    public static IReadOnlyCollection<T> GetValues<T>() where T : new()
    {
      var enumType = typeof(T);
      if (!typeof(IGH_Enumerate).IsAssignableFrom(typeof(T)))
        throw new ArgumentException($"{enumType} does not implement interface {typeof(IGH_Enumerate)}", nameof(T));

      var _EnumValues_ = enumType.GetProperty("EnumValues", BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static, null, typeof(IReadOnlyCollection<T>), Type.EmptyTypes, null);
      if (_EnumValues_ != null)
        return (IReadOnlyCollection<T>) _EnumValues_?.GetValue(null);

      if (typeof(GH_Enum).IsAssignableFrom(typeof(T)))
      {
        var map = GH_Enum.GetNamedValues(typeof(T));
        var values = map.Keys.Select(x => { var value = new T(); (value as GH_Enum).Value = x; return value; });
        return values.ToArray();
      }

      return default;
    }

    internal static IGH_ActiveObject CreatePickerObject<T>()
    {
      var enumType = typeof(T);
      if (!typeof(IGH_Enumerate).IsAssignableFrom(typeof(T)))
        throw new ArgumentException($"{enumType.Name} does not implement interface {typeof(IGH_Enumerate).FullName}", nameof(T));

      if
      (
        enumType.GetProperty("PickerObjectType", BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static, null, typeof(Type), Type.EmptyTypes, null) is PropertyInfo _PickerObjectType_ &&
        _PickerObjectType_.GetValue(null) is Type pickerType
      )
        return Activator.CreateInstance(pickerType) as IGH_ActiveObject;

      return default;
    }
  }

  [EditorBrowsable(EditorBrowsableState.Never)]
  public abstract class GH_Enum : GH_Integer,
    IGH_Goo,
    IGH_Enumerate,
    IGH_ItemDescription,
    IFormattable,
    IComparable,
    IComparable<GH_Enum>,
    IEquatable<GH_Enum>
  {
    protected GH_Enum() { }
    protected GH_Enum(int value) : base(value) { }

    /// <summary>
    /// Gets the validity of this instance. Enums are valid if are defined.
    /// </summary>
    public override bool IsValid => Enum.IsDefined(UnderlyingEnumType, Value) ||
      UnderlyingEnumType.GetCustomAttribute<FlagsAttribute>() is object;

    /// <summary>
    /// Checks if this Enumerate value is the Empty value. Override this property to define an Empty value.
    /// </summary>
    public virtual bool IsEmpty => false;

    public abstract Type UnderlyingEnumType { get; }
    public override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    static Dictionary<Type, (Type Param, Type Goo)> LookForEnums(Assembly assembly)
    {
      var result = new Dictionary<Type, (Type Param, Type Goo)>();

      var exportedTypes = assembly.ExportedTypes.Where(x => typeof(IGH_Enumerate).IsAssignableFrom(x));
      foreach (var type in exportedTypes)
      {
        if (type.IsAbstract)
          continue;

        bool typeFound = false;
        var gooType = type;
        while (gooType != typeof(object))
        {
          if (gooType.IsConstructedGenericType /*&& gooType.GetGenericTypeDefinition() == typeof(GH_Enum<>)*/)
          {
            var valueType = gooType.GetGenericArguments()[0];
            foreach (var param in assembly.ExportedTypes.Where(x => x.GetInterfaces().Contains(typeof(IGH_Param))))
            {
              if (!param.IsClass)
                continue;

              var paramType = param;
              while (paramType != typeof(GH_ActiveObject))
              {
                if (paramType.IsConstructedGenericType && paramType.GetGenericTypeDefinition() == typeof(Parameters.Param_Enum<>))
                {
                  if (paramType.GetGenericArguments()[0] == type)
                  {
                    result.Add(valueType, (param, type));
                    typeFound = true;
                    break;
                  }
                }

                paramType = paramType.BaseType;
              }

              if (typeFound)
                break;
            }

            if (!typeFound)
            {
              result.Add(valueType, (typeof(Parameters.Param_Enum<>).MakeGenericType(type), type));
              typeFound = true;
            }
          }

          if (typeFound)
            break;

          gooType = gooType.BaseType;
        }
      }

      // Register all the ParamsTypes as params in Grasshopper
      foreach (var entry in result)
      {
        if (entry.Value.Param.IsGenericType)
        {
          var proxy = Activator.CreateInstance(entry.Value.Param) as IGH_ObjectProxy;
          if (!Instances.ComponentServer.IsObjectCached(proxy.Guid))
            Instances.ComponentServer.AddProxy(proxy);
        }
      }

      return result;
    }

    static readonly Dictionary<Type, (Type Param, Type Goo)> EnumTypes = LookForEnums(Assembly.GetExecutingAssembly());
    public static bool TryGetParamTypes(Type type, out (Type Param, Type Goo) paramTypes) =>
      EnumTypes.TryGetValue(type, out paramTypes);

    string IGH_Goo.ToString() => $"{TypeName} : {Text} : {Value}";
    public virtual string Text
    {
      get
      {
        if (!IsValid) return "<invalid>";
        if (IsEmpty) return "<empty>";
        return ToString(default, CultureInfo.CurrentUICulture);
      }
    }

    public static ReadOnlyDictionary<int, string> GetNamedValues(Type enumType)
    {
      if (!enumType.IsSubclassOf(typeof(GH_Enum)))
        throw new ArgumentException($"{nameof(enumType)} must be a subclass of {typeof(GH_Enum).FullName}", nameof(enumType));

      var _NamedValues_ = enumType.GetProperty("NamedValues", BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static, null, typeof(ReadOnlyDictionary<int, string>), Type.EmptyTypes, null);
      return _NamedValues_.GetValue(null) as ReadOnlyDictionary<int, string>;
    }

    public static GH_Enum FromString(Type enumType, string name)
    {
      if (enumType is null)
        throw new ArgumentNullException(nameof(enumType));

      if (!enumType.IsSubclassOf(typeof(GH_Enum)))
        throw new ArgumentException($"{nameof(enumType)} must be a subclass of {typeof(GH_Enum).FullName}", nameof(enumType));

      if (name is null)
      {
        return null;
      }
      else if (name == string.Empty)
      {
        var enumerate = Activator.CreateInstance(enumType) as GH_Enum;
        if(!enumerate.IsEmpty)
          throw new ArgumentException(nameof(name));

        return enumerate;
      }
      else
      {
        var enumerate = Activator.CreateInstance(enumType) as GH_Enum;
        enumerate.Value = (int) Enum.Parse(enumerate.UnderlyingEnumType, name);
        return enumerate;
      }
    }

    public static GH_Enum FromInt32(Type enumType, int value)
    {
      if (enumType is null)
        throw new ArgumentNullException(nameof(enumType));

      if (!enumType.IsSubclassOf(typeof(GH_Enum)))
        throw new ArgumentException($"{nameof(enumType)} must be a subclass of {typeof(GH_Enum).FullName}", nameof(enumType));

      var enumerate = Activator.CreateInstance(enumType) as GH_Enum;
      enumerate.Value = value;
      if(enumerate.IsValid)
        return enumerate;

      throw new ArgumentException(nameof(value));
    }

    public static GH_Enum FromDouble(Type enumType, double value)
    {
      if (enumType is null)
        throw new ArgumentNullException(nameof(enumType));

      if (!enumType.IsSubclassOf(typeof(GH_Enum)))
        throw new ArgumentException($"{nameof(enumType)} must be a subclass of {typeof(GH_Enum).FullName}", nameof(enumType));

      var enumerate = Activator.CreateInstance(enumType) as GH_Enum;
      if (double.IsNaN(value))
      {
        if (enumerate.IsEmpty)
          return enumerate;
      }
      else
      {
        try
        {
          enumerate.Value = System.Convert.ToInt32(value);
          if (enumerate.IsValid)
            return enumerate;
        }
        catch (OverflowException) { }
      }

      throw new ArgumentException(nameof(value));
    }

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case GH_Integer integer: source = integer.Value; break;
        case GH_Number number:   source = number.Value;  break;
        case GH_String text:     source = text.Value;    break;
      }

      try
      {
        var enumerate = default(GH_Enum);
        switch (source)
        {
          case int intValue:        enumerate = FromInt32(GetType(), intValue);     break;
          case double doubleValue:  enumerate = FromDouble(GetType(), doubleValue);  break;
          case string stringValue:  enumerate = FromString(GetType(), stringValue);  break;
          default:
            return base.CastFrom(source);
        }

        Value = enumerate.Value;
        return true;
      }
      catch (ArgumentException)
      {
        return false;
      }
    }

    class Proxy : IGH_GooProxy
    {
      readonly GH_Enum owner;
      IGH_Goo IGH_GooProxy.ProxyOwner => owner;
      bool IGH_GooProxy.IsParsable => true;
      string IGH_GooProxy.UserString { get; set; }

      public Proxy(GH_Enum o) { owner = o; (this as IGH_GooProxy).UserString = FormatInstance(); }
      public void Construct() { }
      public string FormatInstance() => Enum.Format(owner.UnderlyingEnumType, owner.Value, "G");
      public bool FromString(string str) => Enum.TryParse(str, out owner.m_value);
      public string MutateString(string str) => str.Trim();
      public override string ToString() => Text;

      public bool Valid => owner.IsValid;
      public Type Type => owner.UnderlyingEnumType;
      public string Text => owner.Text;
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);

    #region IGH_ItemDescription
    Bitmap IGH_ItemDescription.GetImage(Size size) => default;
    string IGH_ItemDescription.Name => Text;
    string IGH_ItemDescription.NickName => Value.ToString("D");
    string IGH_ItemDescription.Description => TypeDescription;
    #endregion

    #region System.Object
    public override int GetHashCode() => Value.GetHashCode();
    #endregion

    #region IFormattable
    public sealed override string ToString() => ToString(default, CultureInfo.CurrentCulture);
    public string ToString(string format, IFormatProvider formatProvider)
    {
      if (string.IsNullOrEmpty(format)) format = "G";

      if (format.ToUpper() == "G" && formatProvider is CultureInfo ci)
      {
        if (!ci.Equals(CultureInfo.InvariantCulture))
        {
          if (IsEmpty) return string.Empty;
          else if (IsValid)
          {
            if (UnderlyingEnumType.GetCustomAttribute<FlagsAttribute>() is object)
            {
              // TODO: Translate result using GetNammedValues.
              return ScriptVariable().ToString();
            }
            else
            {
              try { return GetNamedValues(GetType())[Value]; }
              catch (KeyNotFoundException) { return $"#{Value:D}"; }
            }
          }

          return default;
        }
      }

      switch (ScriptVariable())
      {
        case IFormattable formattable:
          return formattable.ToString(format, formatProvider);

        case object script:
          return script.ToString();
      }

      return default;
    }

    public static bool TryParse<T>(string value, out T result)
      where T : GH_Enum => TryParse(value, CultureInfo.CurrentCulture, out result);
    public static bool TryParse<T>(string value, IFormatProvider formatProvider, out T result)
      where T : GH_Enum
    {
      if (value is null)
      {
        result = default;
        return false;
      }
      else if (value == string.Empty)
      {
        result = Activator.CreateInstance(typeof(T)) as T;
        if (!result.IsEmpty)
          throw new ArgumentException(nameof(value));

        return true;
      }
      else
      {
        if (formatProvider is CultureInfo ci && !ci.Equals(CultureInfo.InvariantCulture))
        {
          if (value[0] == '#')
          {
            result = Activator.CreateInstance(typeof(T)) as T;
            result.Value = (int) Enum.Parse(result.UnderlyingEnumType, value.Substring(1));
            return true;
          }
          else
          {
            var NamedValues = GetNamedValues(typeof(T));
            var inverse = NamedValues.ToDictionary(x => x.Value, x => x.Key);
            if (!inverse.TryGetValue(value, out var val))
              throw new ArgumentException($"'{value}' is not one of the named constants defined for the type {typeof(T)}", nameof(value));

            result = Activator.CreateInstance(typeof(T)) as T;
            result.Value = val;
            return true;
          }
        }
        else
        {
          result = Activator.CreateInstance(typeof(T)) as T;
          result.Value = (int) Enum.Parse(result.UnderlyingEnumType, value);
          return true;
        }
      }
    }
    #endregion

    #region IComparable
    int IComparable.CompareTo(object obj) => ((IComparable<GH_Enum>)this).CompareTo(obj as GH_Enum);

    int IComparable<GH_Enum>.CompareTo(GH_Enum other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if(GetType() != other.GetType())
        throw new ArgumentException($"{nameof(other)} is not a {GetType().Name}", nameof(other));

      return Value - other.Value;
    }
    #endregion

    #region IEquatable
    public override bool Equals(object obj) => obj is GH_Enum other && Equals(other);
    public bool Equals(GH_Enum other) => other.GetType() == GetType() && Value.Equals(other.Value);
    #endregion
  }

  public abstract class GH_Enum<T> : GH_Enum
    where T : struct, Enum
  {
    public GH_Enum() { }
    public GH_Enum(T value) => m_value = (int) (object) value;
    public new T Value { get => (T) (object) base.Value; set => base.Value = (int) (object) value; }

    public override string TypeName
    {
      get
      {
        var name = GetType().GetTypeInfo().GetCustomAttribute(typeof(Kernel.Attributes.NameAttribute)) as RhinoInside.Revit.GH.Kernel.Attributes.NameAttribute;
        return name?.Name ?? typeof(T).Name;
      }
    }
    public override string TypeDescription
    {
      get
      {
        var description = GetType().GetTypeInfo().GetCustomAttribute(typeof(Kernel.Attributes.DescriptionAttribute)) as System.ComponentModel.DescriptionAttribute;
        return description?.Description ?? $"A {TypeName} value";
      }
    }
    public sealed override Type UnderlyingEnumType => typeof(T);

    public override bool CastFrom(object source)
    {
      if (source is T value)
      {
        var enumerate = Activator.CreateInstance(GetType()) as GH_Enum<T>;
        enumerate.Value = value;
        if (!enumerate.IsValid)
          return false;

        Value = enumerate.Value;
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(T)))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
      {
        target = (Q) (object) new GH_Integer(base.Value);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
      {
        target = (Q) (object) new GH_Number(base.Value);
        return true;
      }

      return base.CastTo(ref target);
    }

    public override object ScriptVariable() => Value;

    static GH_Enum<T>[] enumValues;
    public static GH_Enum<T>[] EnumValues 
    {
      get
      {
        if (enumValues is null)
        {
          var names = Enum.GetNames(typeof(T));
          var values = Enum.GetValues(typeof(T)) as T[];
          var goos = values.Select(x => { var value = Activator.CreateInstance<GH_Enum<T>>(); value.Value = x; return value; });
          var set = new SortedSet<GH_Enum<T>>(goos);

          enumValues = set.ToArray();
        }

        return enumValues;
      }
    }

    static ReadOnlyDictionary<int, string> namedValues;
    public static ReadOnlyDictionary<int, string> NamedValues
    {
      get
      {
        if (namedValues is null)
        {
          var names = Enum.GetNames(typeof(T));
          var values = Enum.GetValues(typeof(T)) as int[];
          var dictionary = new Dictionary<int, string>(values.Length);

          int index = 0;
          foreach (var value in values)
          {
            try { dictionary.Add(value, names[index++]); }
            catch (ArgumentException) { /* ignore duplicates */ }
          }

          namedValues = new ReadOnlyDictionary<int, string>(dictionary);
        }

        return namedValues;
      }
    }
  }

  public abstract class GH_Flags<T> : GH_Enum<T>, IGH_Flags
  where T : struct, Enum
  {
    static GH_Flags()
    {
      if (typeof(T).GetCustomAttribute<FlagsAttribute>() is null)
        throw new InvalidOperationException($"Type {typeof(T)} does not have {nameof(FlagsAttribute)}.");
    }

    public GH_Flags() { }
    public GH_Flags(T value) : base(value) { }

    public bool HasFlag(IGH_Flags flag)
    {
      if (!(flag is GH_Flags<T> fT))
        throw new System.InvalidCastException();

      return Value.HasFlag(fT.Value);
    }

    public void SetFlag(IGH_Flags flag, bool value)
    {
      if (!(flag is GH_Flags<T> fT))
        throw new System.InvalidCastException();

      var self = (int) (object) Value;
      var other = (int) (object) fT.Value;

      var result = value ? self | other : self & ~other;
      Value = (T) (object) result;
    }
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  using Kernel.Attributes;

  public class Param_Enum<T> : GH_PersistentParam<T>, IGH_ObjectProxy
    where T : class, IGH_Goo, Types.IGH_Enumerate, new()
  {
    protected Param_Enum(string name, string abbreviation, string description, string category, string subcategory) :
      base(name, abbreviation, description, category, subcategory)
    { }

    static readonly Guid GenericDataParamComponentGuid = new Guid("{8EC86459-BF01-4409-BAEE-174D0D2B13D0}");
    protected override Bitmap Icon => (Bitmap) Properties.Resources.ResourceManager.GetObject(typeof(T).Name) ??                    // try type name first
                                      (Bitmap) Properties.Resources.ResourceManager.GetObject(typeof(T).Name + "_ValueList") ??     // try with _ValueList e.g. WallFunction_ValueList
                                      Instances.ComponentServer.EmitObjectIcon(GenericDataParamComponentGuid);          // default to GH icon

    static readonly Guid? componentGuid = typeof(T).GetCustomAttribute<ComponentGuidAttribute>()?.Value;
    public override Guid ComponentGuid => componentGuid ??
      throw new NotImplementedException($"{typeof(T)} has no {nameof(ComponentGuid)}, please use {typeof(ComponentGuidAttribute)}");

    static readonly GH_Exposure exposure = typeof(T).GetCustomAttribute<ExposureAttribute>()?.Value ?? GH_Exposure.hidden;
    public override GH_Exposure Exposure => exposure;

    public Param_Enum() : base
    (
      typeof(T).Name,
      typeof(T).Name,
      string.Empty,
      string.Empty,
      string.Empty
    )
    {
      (this as IGH_ObjectProxy).Exposure = Exposure;

      if (typeof(T).GetCustomAttribute<NameAttribute>() is NameAttribute name)
        Name = name.Name;

      if (typeof(T).GetCustomAttribute<NickNameAttribute>() is NickNameAttribute nickname)
        NickName = nickname.NickName;

      if (typeof(T).GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute description)
        Description = description.Description;

      if (typeof(T).GetCustomAttribute<CategoryAttribute>() is CategoryAttribute category)
      {
        Category = category.Category;
        SubCategory = category.SubCategory;
      }
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }

    protected override GH_GetterResult Prompt_Plural(ref List<T> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref T value) => GH_GetterResult.cancel;

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      if (Kind > GH_ParamKind.input || DataType == GH_ParamData.remote)
      {
        base.AppendAdditionalMenuItems(menu);
        return;
      }

      Menu_AppendWireDisplay(menu);
      Menu_AppendDisconnectWires(menu);

      Menu_AppendPrincipalParameter(menu);
      Menu_AppendReverseParameter(menu);
      Menu_AppendFlattenParameter(menu);
      Menu_AppendGraftParameter(menu);
      Menu_AppendSimplifyParameter(menu);

      var current = InstantiateT();
      if (SourceCount == 0 && PersistentDataCount == 1)
      {
        if(PersistentData.get_FirstItem(true) is T firstValue)
          current = firstValue.Duplicate() as T;
      }

      if (Types.GH_Enumerate.GetValues<T>() is T[] values)
      {
        if (values.Length < 7 || (Optional && typeof(Types.IGH_Flags).IsAssignableFrom(typeof(T))))
        {
          Menu_AppendSeparator(menu);
          foreach (var e in values)
          {
            if (e.IsEmpty) continue;
            var tag = e.Duplicate() as T;

            if (current is Types.IGH_Flags currentF && tag is Types.IGH_Flags tagF)
            {
              var item = Menu_AppendItem(menu, tag.Text, Menu_NamedValueClicked, SourceCount == 0, currentF.HasFlag(tagF));
              item.Tag = tag;
            }
            else
            {
              var item = Menu_AppendItem(menu, tag.Text, Menu_NamedValueClicked, SourceCount == 0, tag.Equals(current));
              item.Tag = tag;
            }
          }
          Menu_AppendSeparator(menu);
        }
        else
        {
          var listBox = new ListBox();
          foreach (var e in values)
          {
            if (e.IsEmpty) continue;
            var tag = e.Duplicate() as T;

            int index = listBox.Items.Add(tag);
            if (e.Equals(current))
              listBox.SelectedIndex = index;
          }

          listBox.DisplayMember = "Text";
          listBox.BorderStyle = BorderStyle.FixedSingle;

          listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

          listBox.Width = (int) (200 * GH_GraphicsUtil.UiScale);
          listBox.Height = (int) (100 * GH_GraphicsUtil.UiScale);
          Menu_AppendCustomItem(menu, listBox);
        }
      }

      Menu_AppendDestroyPersistent(menu);
      Menu_AppendInternaliseData(menu);

      if(Exposure != GH_Exposure.hidden)
        Menu_AppendExtractParameter(menu);

      Menu_AppendItem(menu, "Expose picker", Menu_ExposePicker, SourceCount == 0);
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is T value)
          {
            RecordPersistentDataEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.Append(value.Duplicate() as T);
            OnObjectChanged(GH_ObjectEventType.PersistentData);
          }
        }

        ExpireSolution(true);
      }
    }

    private void Menu_NamedValueClicked(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem item)
      {
        if (item.Tag is T value)
        {
          if
          (
            Optional &&
            PersistentDataCount == 1 &&
            PersistentData.get_FirstItem(true) is T data &&
            value is Types.IGH_Flags flag &&
            data is Types.IGH_Flags self
          )
            self.SetFlag(flag, !self.HasFlag(flag));
          else
            data = value.Duplicate() as T;

          RecordPersistentDataEvent($"Set: {value}");
          PersistentData.Clear();
          PersistentData.Append(data);
          OnObjectChanged(GH_ObjectEventType.PersistentData);

          ExpireSolution(true);
        }
      }
    }

    protected virtual IGH_ActiveObject CreatePickerObject()
    {
      if (Types.GH_Enumerate.CreatePickerObject<T>() is IGH_ActiveObject picker)
        return picker;

      var list = new Grasshopper.Kernel.Special.GH_ValueList
      {
        Name = typeof(T).GetTypeInfo().GetCustomAttribute(typeof(NameAttribute)) is NameAttribute name ?
               name.Name : typeof(T).Name,
        NickName = string.Empty,
        Category = string.Empty,
        SubCategory = string.Empty,
        Description = $"A {TypeName} picker"
      };

      list.ListItems.Clear();

      foreach (var value in Types.GH_Enumerate.GetValues<T>())
      {
        if (value.IsEmpty) continue;
        switch (value.ScriptVariable())
        {
          case Enum e:    list.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(value.Text, $"{System.Convert.ToInt32(e)}")); break;
          case int i:     list.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(value.Text, $"{i}")); break;
          case double d:  list.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(value.Text, $"{d}")); break;
          case string s:  list.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(value.Text, $"\"{s}\"")); break;
        }
      }

      return list;
    }

    protected void Menu_ExposePicker(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem)
      {
        if (CreatePickerObject() is IGH_ActiveObject picker)
        {
          var nameless = string.IsNullOrEmpty(picker.NickName);
          if (this.ConnectNewObject(picker))
          {
            if (nameless) picker.NickName = string.Empty;
            picker.ExpireSolution(true);
          }
        }
      }
    }

    #region IGH_ObjectProxy
    string IGH_ObjectProxy.Location => GetType().Assembly.Location;
    Guid IGH_ObjectProxy.LibraryGuid => Guid.Empty;
    bool IGH_ObjectProxy.SDKCompliant => SDKCompliancy(Rhino.RhinoApp.ExeVersion, Rhino.RhinoApp.ExeServiceRelease);
    bool IGH_ObjectProxy.Obsolete => Obsolete;
    Type IGH_ObjectProxy.Type => GetType();
    GH_ObjectType IGH_ObjectProxy.Kind => GH_ObjectType.CompiledObject;
    Guid IGH_ObjectProxy.Guid => ComponentGuid;
    Bitmap IGH_ObjectProxy.Icon => Icon;
    IGH_InstanceDescription IGH_ObjectProxy.Desc => this;
    GH_Exposure IGH_ObjectProxy.Exposure { get; set; } = exposure;

    protected virtual IGH_DocumentObject CreateInstance() => new Param_Enum<T>();
    IGH_DocumentObject IGH_ObjectProxy.CreateInstance() => CreateInstance();
    IGH_ObjectProxy IGH_ObjectProxy.DuplicateProxy() => (IGH_ObjectProxy) MemberwiseClone();
    #endregion
  }
}
