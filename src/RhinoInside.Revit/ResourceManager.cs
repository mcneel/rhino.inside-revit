#if NET10_0_OR_GREATER
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace System.Resources.Extensions
{
  /// <summary>
  /// Resource Manager exposes an assembly's resources to an application.
  /// </summary>
  sealed class ResourceManager : Resources.ResourceManager
  {
    private ResourceManager(string baseName, Assembly assembly) : base(baseName, assembly) { }

    private readonly Dictionary<string, ResourceSet> ResourceSets = new Dictionary<string, ResourceSet>();

    private static void AddResourceSet(Dictionary<string, ResourceSet> resourceSets, string cultureName, ref ResourceSet rs)
    {
      lock (resourceSets)
      {
        if (resourceSets.TryGetValue(cultureName, out var lostRace))
        {
          if (!ReferenceEquals(lostRace, rs))
          {
            if (!resourceSets.ContainsValue(rs))
              rs.Dispose();
            rs = lostRace;
          }
        }
        else
        {
          resourceSets.Add(cultureName, rs);
        }
      }
    }

    /// <summary>
    /// Looks up a set of resources for a particular CultureInfo
    /// </summary>
    /// <param name="culture"></param>
    /// <param name="createIfNotExists"></param>
    /// <param name="tryParents"></param>
    /// <returns></returns>
    protected override Resources.ResourceSet InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
    {
      ResourceSet rs = null;
      var resourceSets = ResourceSets;
      lock (resourceSets)
      {
        if (resourceSets.TryGetValue(culture.Name, out rs))
          return rs;
      }

      if (createIfNotExists)
      {
        var cultures = new CultureInfo[] { culture, CultureInfo.InvariantCulture };
        var culturesCount = 0;
        for (int c = 0; c < cultures.Length; ++c)
        {
          culturesCount++;
          if (MainAssembly.GetManifestResourceStream(GetResourceFileName(cultures[c])) is Stream stream)
          {
            rs = new ResourceSet(stream);
            break;
          }
        }

        if (rs is object)
        {
          for (int c = 0; c < culturesCount; ++c)
            AddResourceSet(ResourceSets, cultures[c].Name, ref rs);
        }
      }

      return rs;
    }

    /// <summary>
    /// Constructs a Resource Manager when necessary.
    /// </summary>
    /// <param name="assembly"></param>
    public static void AssemblyLoaded(Assembly assembly)
    {
      if (assembly.GetReferencedAssemblies().Any(x => x.Name == "mscorlib"))
      {
        var resourcesTypeName = $"{assembly.GetName().Name}.Properties.Resources";
        if (assembly.GetType(resourcesTypeName) is Type assemblyPropertiesResourcesType)
        {
          if (assemblyPropertiesResourcesType.GetField("resourceMan", BindingFlags.Static | BindingFlags.NonPublic) is FieldInfo resourceMan)
          {
            if(resourceMan.FieldType == typeof(Resources.ResourceManager) && resourceMan.GetValue(null) is null)
              resourceMan.SetValue(null, new ResourceManager(resourcesTypeName, assembly));
          }
        }
      }
    }
  }

  /// <summary>
  /// 
  /// </summary>
  sealed class ResourceSet : Resources.ResourceSet
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream"></param>
    public ResourceSet(Stream stream) : base(new DeserializingResourceReader(stream)) { }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override Type GetDefaultReader() => typeof(DeserializingResourceReader);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override Type GetDefaultWriter() => typeof(PreserializedResourceWriter);
  }
}
#endif
