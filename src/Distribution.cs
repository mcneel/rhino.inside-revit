using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RhinoInside.Revit
{
  /// <summary>
  /// A Private copy of this type is compiled on several projects.
  /// </summary>
  class Distribution
  {
    #region MinimumRevitVersion
#if REVIT_2027
    public static readonly Version MinimumRevitVersion = new Version(2027, 0);
#elif REVIT_2026
    public static readonly Version MinimumRevitVersion = new Version(2026, 0);
#elif REVIT_2025
    public static readonly Version MinimumRevitVersion = new Version(2025, 0);
#elif REVIT_2024
    public static readonly Version MinimumRevitVersion = new Version(2024, 3);
#elif REVIT_2023
    public static readonly Version MinimumRevitVersion = new Version(2023, 1);
#elif REVIT_2022
    public static readonly Version MinimumRevitVersion = new Version(2022, 1);
#elif REVIT_2021
    public static readonly Version MinimumRevitVersion = new Version(2021, 1);
#elif REVIT_2020
    public static readonly Version MinimumRevitVersion = new Version(2020, 0);
#elif REVIT_2019
    public static readonly Version MinimumRevitVersion = new Version(2019, 1);
#elif REVIT_2018
    public static readonly Version MinimumRevitVersion = new Version(2018, 2);
#elif REVIT_2017
    public static readonly Version MinimumRevitVersion = new Version(2017, 0);
#endif
    #endregion

    public readonly int MajorVersion;
    public readonly int MinorVersion;
    public readonly bool Development;

    public Distribution(int majorVersion, int minorVersion = 0, bool dev = false)
    {
      MajorVersion = majorVersion;
      MinorVersion = minorVersion;
      Development = dev;
    }

    public bool IsAvailable => ExeVersion()?.Major == MajorVersion && ExeVersion()?.Minor >= MinorVersion;

    private static readonly string OptionsKey = $@"HKEY_CURRENT_USER\Software\McNeel\Rhino.Inside\Revit\{MinimumRevitVersion.Major}\";
    public static string DefaultKey
    {
      get => Microsoft.Win32.Registry.GetValue(OptionsKey, "RhinoKey", null) as string;
      set => Microsoft.Win32.Registry.SetValue(OptionsKey, "RhinoKey", value);
    }

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
      var available = distributions.Where(x => x.IsAvailable && (currentKey is null || x.RegistryKey == currentKey)).ToArray();

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

    private static IEnumerable<Distribution> Distributions(bool dev)
    {
#if !DEBUG
      if (dev)
        yield break;
#endif

#if NET
      if (Environment.Version.Major >= 10)
      {
        yield return new Distribution(9, 0, dev);
        yield return new Distribution(8, 32, dev);
      }
      else if (Environment.Version.Major >= 8)
      {
        yield return new Distribution(8, 0, dev);
      }
#endif

#if NETFRAMEWORK
      {
        yield return new Distribution(8, 0, dev);
        yield return new Distribution(7, 0, dev);
      }
#endif
    }

    public static IEnumerable<Distribution> Available => Distributions(false).Concat(Distributions(true)).Where(x => x.IsAvailable);
  }
}
