using System;
using System.Diagnostics;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Kernel.Attributes
{
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public class ComponentGuidAttribute : Attribute
  {
    public readonly Guid Value;
    public ComponentGuidAttribute(string value) => Value = new Guid(value);
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface| AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class NameAttribute : Attribute
  {
    public readonly string Name;
    public NameAttribute(string value) => Name = value;
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class NickNameAttribute : Attribute
  {
    public readonly string NickName;
    public NickNameAttribute(string value) => NickName = value;
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class DescriptionAttribute : Attribute
  {
    public readonly string Description;
    public DescriptionAttribute(string value) => Description = value;
  }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public class CategoryAttribute : Attribute
  {
    public readonly string Category;
    public readonly string SubCategory;
    public CategoryAttribute(string category, string subCategory) { Category = category; SubCategory = subCategory; }
  }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public class ExposureAttribute : Attribute
  {
    public readonly GH_Exposure Value;
    public ExposureAttribute(GH_Exposure value) => Value = value;
  }

  [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class DefaultValueAttribute : Attribute
  {
    public readonly object Value;
    public DefaultValueAttribute(object value) => Value = value;
  }

  [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
  public class ParamTypeAttribute : Attribute
  {
    public readonly Type Type;
    public ParamTypeAttribute(Type type) => Type = type;
  }
}

namespace RhinoInside.Revit.GH
{
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public sealed class ComponentVersionAttribute : Attribute
  {
    public readonly Version Since;
    public readonly Version Updated;
    public readonly Version Obsolete;

    internal ComponentVersionAttribute(string since) : this(since, default, default) { }
    internal ComponentVersionAttribute(string since, string updated) : this(since, updated, default) { }
    internal ComponentVersionAttribute(string since, string updated, string obsolete)
    {
      Since = Version.Parse(since);
      Updated = updated is object ? Version.Parse(updated) : default;
      Obsolete = obsolete is object ? Version.Parse(obsolete) : default;

#if DEBUG
      var _since_ = Since;
      var _updated_ = Updated ?? _since_;
      var _obsolete_ = Obsolete ?? _updated_;

      Debug.Assert(_updated_ >= _since_);
      Debug.Assert(_obsolete_ >= _updated_);
#endif
    }

    public static Version GetTypeVersionCurrentVersion(Type type)
    {
      var maxVersion = new Version();
      var assembly = typeof(ComponentVersionAttribute).Assembly;
      for (; type is object; type = type.BaseType)
      {
        // TypeVersionAttribute is private so it can not be used outside its assembly
        if (type.Assembly != assembly) continue;

        var typeVersion = (ComponentVersionAttribute[]) type.GetCustomAttributes(typeof(ComponentVersionAttribute), false);
        if (typeVersion.Length > 0)
        {
          var updated = typeVersion[0].Updated ?? typeVersion[0].Since;
          if (updated > maxVersion) maxVersion = updated;
        }
      }

      return maxVersion;
    }
  }
}
