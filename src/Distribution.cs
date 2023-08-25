using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RhinoInside.Revit
{
  /// <summary>
  /// A Private copy of this type is compiled on several projects.
  /// </summary>
  class Distribution
  {
    public readonly int MajorVersion;
    public readonly bool Development;

    public Distribution(int majorVersion, bool dev = false)
    {
      MajorVersion = majorVersion;
      Development = dev;
    }

    public bool Available => ExeVersion()?.Major == MajorVersion;

    public static string CurrentKey
    {
      get => Environment.GetEnvironmentVariable("RhinoInside_CurrentKey");
      set => Environment.SetEnvironmentVariable("RhinoInside_CurrentKey", value);
    }

    internal string ProductKey => Development ?
      $@"SOFTWARE\McNeel\Rhinoceros\{MajorVersion}.0-WIP-Developer-Debug-trunk" :
      $@"SOFTWARE\McNeel\Rhinoceros\{MajorVersion}.0";

    internal string RegistryKey => Development ?
      $@"HKEY_CURRENT_USER\{ProductKey}" :
      $@"HKEY_LOCAL_MACHINE\{ProductKey}";

    #region Install
    string InstallKey => $@"{RegistryKey}\Install";
    public string BuildType => Microsoft.Win32.Registry.GetValue(InstallKey, "BuildType", null) as string;
    public string Version => Microsoft.Win32.Registry.GetValue(InstallKey, "Version", null) as string;
    public string Path => Microsoft.Win32.Registry.GetValue(InstallKey, "Path", null) as string;
    public string InstallPath => Microsoft.Win32.Registry.GetValue(InstallKey, "InstallPath", null) as string;
    public string ExePath => string.IsNullOrEmpty(Path) ? null : System.IO.Path.Combine(Path, "Rhino.exe");
    #endregion

    public string BuildTag => VersionInfo.IsDebug ? "(Development)" : string.Empty;

    FileVersionInfo _VersionInfo;
    public FileVersionInfo VersionInfo
    {
      get
      {
        if (_VersionInfo is null && File.Exists(ExePath))
          _VersionInfo = FileVersionInfo.GetVersionInfo(ExePath);

        return _VersionInfo;
      }
    }

    public Version ExeVersion()
    {
      var rhinoVersionInfo = VersionInfo;
      return new Version
      (
        rhinoVersionInfo?.FileMajorPart ?? 0,
        rhinoVersionInfo?.FileMinorPart ?? 0,
        rhinoVersionInfo?.FileBuildPart ?? 0,
        rhinoVersionInfo?.FilePrivatePart ?? 0
      );
    }

    public static Distribution Default(int majorVersion)
    {
      var distributions = new Distribution[]
      {
        new Distribution(majorVersion),
#if DEBUG
        new Distribution(majorVersion, dev: true),
#endif
      };

      var currentKey = CurrentKey;
      CurrentKey = null;

      var available = distributions.Where(x => x.Available && (currentKey is null || x.RegistryKey == currentKey)).ToArray();

      switch (available.Length)
      {
        case 0: return distributions[0];
        case 1: return available[0];
#if DEBUG
        case 2: return available[1];
#endif
      }

      return null;
    }
  }
}
