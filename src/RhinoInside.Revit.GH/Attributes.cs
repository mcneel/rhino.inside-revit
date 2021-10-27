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
    public readonly Version Introduced;
    public readonly Version Updated;
    public readonly Version Deprecated;

    internal ComponentVersionAttribute(string introduced) : this(introduced, default, default) { }
    internal ComponentVersionAttribute(string introduced, string updated) : this(introduced, updated, default) { }
    internal ComponentVersionAttribute(string introduced, string updated, string deprecated)
    {
      Introduced = Version.Parse(introduced);
      Updated = updated is object ? Version.Parse(updated) : default;
      Deprecated = deprecated is object ? Version.Parse(deprecated) : default;

#if DEBUG
      var _introduced_ = Introduced;
      var _updated_ = Updated ?? _introduced_;
      var _deprecated_ = Deprecated ?? _updated_;

      Debug.Assert(_updated_ >= _introduced_);
      Debug.Assert(_deprecated_ >= _updated_);
#endif
    }

    public static bool GetVersionHistory(Type type, out Version introduced, out Version updated, out Version deprecated)
    {
      var versions = (ComponentVersionAttribute[]) type.GetCustomAttributes(typeof(ComponentVersionAttribute), false);
      var version = versions.Length == 1 ? versions[0] : default;

      introduced = version?.Introduced;
      updated    = version?.Updated;
      deprecated = version?.Deprecated;
      return version is object;
    }

    internal static Version GetCurrentVersion(Type type)
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
          var updated = typeVersion[0].Updated ?? typeVersion[0].Introduced;
          if (updated > maxVersion) maxVersion = updated;
        }
      }

      return maxVersion;
    }
  }
}
