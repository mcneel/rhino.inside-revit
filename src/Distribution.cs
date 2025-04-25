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
#if REVIT_2026
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
    public readonly bool Development;

    public Distribution(int majorVersion, bool dev = false)
    {
      MajorVersion = majorVersion;
      Development = dev;
    }

    public bool IsAvailable => ExeVersion()?.Major == MajorVersion;

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

    public static IEnumerable<Distribution> Available
    {
      get
      {
        var distributions = new Distribution[]
        {
        new Distribution(8),
#if NETFRAMEWORK
        new Distribution(7),
#endif
#if DEBUG
        new Distribution(9, dev: true),
        new Distribution(8, dev: true),
#if NETFRAMEWORK
        new Distribution(7, dev: true),
#endif
#endif
        };

        return distributions.Where(x => x.IsAvailable);
      }
    }
  }
}
