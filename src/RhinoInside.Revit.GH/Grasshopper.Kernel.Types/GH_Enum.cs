using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Grasshopper.Kernel.Types
{
  public abstract class GH_Enum<T> : GH_Enumerate
    where T : Enum
  {
    public GH_Enum() { }
    public GH_Enum(T value) => m_value = (int) (object) value;
    public new T Value { get => (T) (object) base.Value; set => base.Value = (int) (object) value; }

    public override bool IsValid => Enum.IsDefined(typeof(T), Value);
    public override string TypeName
    {
      get
      {
        var value = GetType().GetTypeInfo().GetCustomAttribute(typeof(DisplayNameAttribute)) as DisplayNameAttribute;
        return value?.DisplayName ?? typeof(T).Name;
      }
    }
    public override string TypeDescription
    {
      get
      {
        var value = GetType().GetTypeInfo().GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
        return value?.Description ?? $"{typeof(T).Module.Name} {TypeName}";
      }
    }
    public override Type UnderlyingEnumType => typeof(T);

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case GH_Number number:
          if (GH_Convert.ToInt32(number.Value, out var num, GH_Conversion.Secondary))
            if (Enum.IsDefined(typeof(T), num))
            {
              base.Value = num;
              return true;
            }
          break;
        case GH_Integer integer:
          if (Enum.IsDefined(typeof(T), integer.Value))
          {
            base.Value = integer.Value;
            return true;
          }
          break;
        case T value:
          Value = value;
          return true;
      }
      return false;
    }
    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q) == typeof(T))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public override object ScriptVariable() => Value;

    class Proxy : IGH_GooProxy
    {
      readonly GH_Enum<T> owner;
      IGH_Goo IGH_GooProxy.ProxyOwner => owner;
      bool IGH_GooProxy.IsParsable => true;
      string IGH_GooProxy.UserString { get; set; }

      public Proxy(GH_Enum<T> o) { owner = o; (this as IGH_GooProxy).UserString = FormatInstance(); }
      public void Construct() { }
      public string FormatInstance() => Enum.Format(typeof(T), owner.Value, "G");
      public bool FromString(string str) => Enum.TryParse(str, out owner.m_value);
      public string MutateString(string str) => str.Trim();

      public bool Valid => owner.IsValid;
      public Type Type => typeof(T);
      public string Name => owner.ToString();
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);
    public override string ToString() => Value.ToString();
  }
}
