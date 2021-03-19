using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace RhinoInside.Revit
{
  static class AssemblyResolver
  {
    internal static readonly string SystemDir =
#if DEBUG
      Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\McNeel\Rhinoceros\7.0-WIP-Developer-Debug-trunk\Install", "InstallPath", null) as string ??
#endif
      Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "InstallPath", null) as string ??
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino WIP");

    static readonly FieldInfo _AssemblyResolve = typeof(AppDomain).GetField("_AssemblyResolve", BindingFlags.Instance | BindingFlags.NonPublic);

    struct AssemblyLocation
    {
      public AssemblyLocation(AssemblyName name, string location) { Name = name; Location = location; Assembly = default; }
      public readonly AssemblyName Name;
      public readonly string Location;
      public Assembly Assembly;
    }

    static readonly Dictionary<string, AssemblyLocation> AssemblyLocations = new Dictionary<string, AssemblyLocation>();
    static AssemblyResolver()
    {
      try
      {
        var installFolder = new DirectoryInfo(SystemDir);
        foreach (var dll in installFolder.EnumerateFiles("*.dll", SearchOption.AllDirectories))
        {
          try
          {
            if (dll.Extension.ToLower() != ".dll") continue;

            var assemblyName = AssemblyName.GetAssemblyName(dll.FullName);

            if (AssemblyLocations.TryGetValue(assemblyName.Name, out var location))
            {
              if (location.Name.Version > assemblyName.Version) continue;
              AssemblyLocations.Remove(assemblyName.Name);
            }

            AssemblyLocations.Add(assemblyName.Name, new AssemblyLocation(assemblyName, dll.FullName));
          }
          catch { }
        }
      }
      catch { }
    }

    static bool enabled;
    public static bool Enabled
    {
      get => enabled;
      set
      {
        if (enabled != value)
        {
          if (value)
          {
            if (_AssemblyResolve is null) AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            else
            {
              External.ActivationGate.Enter += ActivationGate_Enter;
              External.ActivationGate.Exit += ActivationGate_Exit;
            }
          }
          else
          {
            if (_AssemblyResolve is null) AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            else
            {
              External.ActivationGate.Exit -= ActivationGate_Exit;
              External.ActivationGate.Enter -= ActivationGate_Enter;
            }
          }
          enabled = value;
        }
      }
    }

    static void ActivationGate_Enter(object sender, EventArgs e)
    {
      var domain = AppDomain.CurrentDomain;
      var assemblyResolve = _AssemblyResolve.GetValue(domain) as ResolveEventHandler;
      var invocationList = assemblyResolve.GetInvocationList();

      foreach (var invocation in invocationList)
        domain.AssemblyResolve -= invocation as ResolveEventHandler;

      domain.AssemblyResolve += AssemblyResolve;

      foreach (var invocation in invocationList)
        AppDomain.CurrentDomain.AssemblyResolve += invocation as ResolveEventHandler;
    }

    static void ActivationGate_Exit(object sender, EventArgs e)
    {
      AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
    }

    static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
    {
      var assemblyName = new AssemblyName(args.Name).Name;
      if (!AssemblyLocations.TryGetValue(assemblyName, out var location))
        return null;

      if (location.Assembly is null)
      {
        // Remove it to not try again if it fails
        AssemblyLocations.Remove(assemblyName);

        // Add again loaded assembly
        location.Assembly = Assembly.LoadFrom(location.Location);
        AssemblyLocations.Add(assemblyName, location);
      }

      return location.Assembly;
    }
  }
}
