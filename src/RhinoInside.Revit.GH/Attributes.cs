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
      var _since_ = Introduced;
      var _updated_ = Updated ?? _since_;
      var _obsolete_ = Deprecated ?? _updated_;

      Debug.Assert(_updated_ >= _since_);
      Debug.Assert(_obsolete_ >= _updated_);
#endif
    }

    public static void GetVersionHistory(Type type, out Version introduced, out Version updated, out Version deprecated)
    {
      introduced = GetIntroducedVersion(type);
      updated = GetUpdatedVersion(type);
      deprecated = GetDeprecatedVersion(type);
    }

    internal static Version GetIntroducedVersion(Type type)
    {
      var since = default(Version);
      var assembly = typeof(ComponentVersionAttribute).Assembly;
      for (var t = type; t is object; t = t.BaseType)
      {
        // TypeVersionAttribute is private so it can not be used outside its assembly
        if (t.Assembly != assembly) continue;

        var typeVersion = (ComponentVersionAttribute[]) t.GetCustomAttributes(typeof(ComponentVersionAttribute), false);
        if (typeVersion.Length > 0)
        {
          var version = typeVersion[0].Introduced;
          if (version is null) continue;
#if DEBUG
          if (since is null) since = version;
          else if (version > since)
            throw new InvalidOperationException($"{type.FullName} since version should be greater than base class {t.FullName}");
#else
          return version;
#endif
        }
      }

      return since;
    }

    internal static Version GetUpdatedVersion(Type type)
    {
      var updated = default(Version);
      var assembly = typeof(ComponentVersionAttribute).Assembly;
      for (var t = type; t is object; t = t.BaseType)
      {
        // TypeVersionAttribute is private so it can not be used outside its assembly
        if (t.Assembly != assembly) continue;

        var typeVersion = (ComponentVersionAttribute[]) t.GetCustomAttributes(typeof(ComponentVersionAttribute), false);
        if (typeVersion.Length > 0)
        {
          var version = typeVersion[0].Updated;
          if (version is null) continue;
          return version;
        }
      }

      return updated;
    }

    internal static Version GetDeprecatedVersion(Type type)
    {
      var obsolete = default(Version);
      var assembly = typeof(ComponentVersionAttribute).Assembly;
      for (var t = type; t is object; t = t.BaseType)
      {
        // TypeVersionAttribute is private so it can not be used outside its assembly
        if (t.Assembly != assembly) continue;

        var typeVersion = (ComponentVersionAttribute[]) t.GetCustomAttributes(typeof(ComponentVersionAttribute), false);
        if (typeVersion.Length > 0)
        {
          var version = typeVersion[0].Deprecated;
          if (version is null) continue;
#if DEBUG
          if (obsolete is null) obsolete = version;
          else if (version < obsolete)
            throw new InvalidOperationException($"{type.FullName} obsolete version should be less than base class {t.FullName}");
#else
          return version;
#endif
        }
      }

      return obsolete;
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
