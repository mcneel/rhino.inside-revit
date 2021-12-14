using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ModelPathExtension
  {
    /// <summary>
    /// Determines whether the specified <see cref="Autodesk.Revit.DB.ModelPath"/>
    /// equals to this <see cref="Autodesk.Revit.DB.ModelPath"/>.
    /// </summary>
    /// <remarks>
    /// Two <see cref="Autodesk.Revit.DB.ModelPath"/> instances are considered equivalent
    /// if they represent the same target model file.
    /// </remarks>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool IsEquivalent(this ModelPath self, ModelPath other)
    {
      if (ReferenceEquals(self, other))
        return true;

      if (self is null || other is null)
        return false;

      return self.Compare(other) == 0;
    }

    /// <summary>
    /// Returns whether this path is a file path (as opposed to a server path or cloud path)
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static bool IsFilePath(this ModelPath self)
    {
      return !self.ServerPath && !self.IsCloudPath();
    }

    /// <summary>
    /// Returns whether this path is a server path (as opposed to a file path or cloud path)
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static bool IsServerPath(this ModelPath self)
    {
      return self.ServerPath && !self.IsCloudPath();
    }

    /// <summary>
    /// Returns whether this path is a cloud path (as opposed to a file path or server path)
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static bool IsCloudPath(this ModelPath self)
    {
#if REVIT_2019
      return self.CloudPath;
#else
      return self.GetProjectGUID() != Guid.Empty && self.GetModelGUID() != Guid.Empty;
#endif
    }

    /// <summary>
    /// Returns the region of the cloud account and project which contains this model.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string GetRegion(this ModelPath self)
    {
      if (self.IsCloudPath())
      {
#if REVIT_2021
        return self.Region;
#else
        return "GLOBAL";
#endif
      }

      return default;
    }
  }

  public static class ModelUri
  {
    public const string UriSchemeServer = "RSN";
    public const string UriSchemeCloud = "cloud";
    internal static readonly Uri Empty = new Uri("empty:");

    public static Uri ToUri(this ModelPath modelPath)
    {
      if (modelPath is null)
        return default;

      if (modelPath.Empty)
        return ModelUri.Empty;

      if (modelPath.IsCloudPath())
      {
        return new UriBuilder(UriSchemeCloud, modelPath.GetRegion(), 0, $"{modelPath.GetProjectGUID():D}/{modelPath.GetModelGUID():D}").Uri;
      }
      else
      {
        var path = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
          return uri;
      }

      throw new ArgumentException($"Failed to convert {nameof(ModelPath)} in to {nameof(Uri)}", nameof(modelPath));
    }

    public static ModelPath ToModelPath(this Uri uri)
    {
      if (uri is null)
        return default;

      if (uri.IsFile)
        return new FilePath(uri.LocalPath);

      if (IsServerUri(uri, out var centralServerLocation, out var path))
        return new ServerPath(centralServerLocation, path);

      if (IsCloudUri(uri, out var region, out var projectId, out var modelId))
      {
        //return Rhinoceros.InvokeInHostContext(() =>
        {
          try
          {
#if REVIT_2021
            return ModelPathUtils.ConvertCloudGUIDsToCloudPath(region, projectId, modelId);
#elif REVIT_2019
            return ModelPathUtils.ConvertCloudGUIDsToCloudPath(projectId, modelId);
#else
            return default(ModelPath);
#endif
          }
          catch (Autodesk.Revit.Exceptions.ApplicationException) { return default; }
        }/*)*/;
      }

      throw new ArgumentException($"Failed to convert {nameof(Uri)} in to {nameof(ModelPath)}", nameof(uri));
    }

    public static bool IsEmptyUri(this Uri uri)
    {
      return uri.Scheme.Equals(Empty.Scheme, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool IsFileUri(this Uri uri, out string path)
    {
      if (uri.IsFile)
      {
        path = uri.LocalPath;
        return true;
      }

      path = default;
      return false;
    }

    public static bool IsServerUri(this Uri uri, out string centralServerLocation, out string path)
    {
      if (uri.Scheme.Equals(UriSchemeServer, StringComparison.InvariantCultureIgnoreCase))
      {
        centralServerLocation = uri.Host;
        path = uri.AbsolutePath;
        return true;
      }

      centralServerLocation = default;
      path = default;
      return false;
    }

    public static bool IsCloudUri(this Uri uri, out string region, out Guid projectId, out Guid modelId)
    {
      if (uri.Scheme.Equals(UriSchemeCloud, StringComparison.InvariantCultureIgnoreCase))
      {
        var fragments = uri.AbsolutePath.Split('/');
        if (fragments.Length == 3)
        {
          if
          (
            fragments[0] == string.Empty &&
            Guid.TryParseExact(fragments[1], "D", out projectId) &&
            Guid.TryParseExact(fragments[2], "D", out modelId)
          )
          {
            if (uri.Host == string.Empty)
              region = "GLOBAL";
            else
              region = uri.Host.ToUpperInvariant();

            return true;
          }
        }
      }

      region = default;
      projectId = default;
      modelId = default;
      return false;
    }
  }
}
