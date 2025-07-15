using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;
using Microsoft.Win32.SafeHandles;
using static RhinoInside.Revit.Diagnostics;

namespace RhinoInside.Revit
{
  static class AssemblyResolver
  {
    static readonly string SystemPath = Path.Combine(Core.Distribution.Path);
    static readonly string PluginsPath = Path.Combine(Core.Distribution.InstallPath, "Plug-ins");

    #region AssemblyReference
    public sealed class AssemblyReference
    {
      internal AssemblyReference(AssemblyName name)
      {
        assemblyName = name;
        Assembly = default;
        activated = default;
      }

      internal readonly AssemblyName assemblyName;
      public AssemblyName Name => assemblyName.Clone() as AssemblyName;
      public Assembly Assembly { get; private set; }

      event AssemblyLoadEventHandler activated;
      public event AssemblyLoadEventHandler Activated
      {
        add
        {
          if (Assembly is object) value?.SafeInvoke(AppDomain.CurrentDomain, new AssemblyLoadEventArgs(Assembly));
          else activated += value;
        }

        remove => activated -= value;
      }

      internal void Activate(Assembly assembly)
      {
#pragma warning disable SYSLIB0044 // Type or member is obsolete
#pragma warning disable SYSLIB0012 // Type or member is obsolete
        if (!assembly.CodeBase.Equals(assemblyName.CodeBase, StringComparison.OrdinalIgnoreCase))
          return;
#pragma warning restore SYSLIB0012 // Type or member is obsolete
#pragma warning restore SYSLIB0044 // Type or member is obsolete

        Assembly = assembly;

        bool failed = false;
        if (ExternalReferences.TryGetValue(assemblyName.Name, out var InitAssembly))
        {
          using (SynchronizationContextGuard.Current)
            failed = !InitAssembly(Assembly);
        }

        if (failed)
          throw new InvalidOperationException($"Failed to activate {assembly.FullName}");

        NotifyActivatedAsync();
      }

      async void NotifyActivatedAsync()
      {
        if (activated is object)
        {
          await System.Threading.Tasks.Task.Yield();

          try { activated(AppDomain.CurrentDomain, new AssemblyLoadEventArgs(Assembly)); }
          finally { activated = default; }
        }
      }

      public override string ToString()
      {
#pragma warning disable SYSLIB0044 // Type or member is obsolete
        return $"Activated={Assembly is object}, CodeBase={assemblyName.CodeBase}";
#pragma warning restore SYSLIB0044 // Type or member is obsolete
      }
    }

    static readonly Dictionary<string, AssemblyReference> references = new Dictionary<string, AssemblyReference>();
    public static IReadOnlyDictionary<string, AssemblyReference> References => references;

    static void AssemblyLoaded(object sender, AssemblyLoadEventArgs args)
    {
      var loadedAssembly = args.LoadedAssembly;
      if (loadedAssembly.ReflectionOnly || loadedAssembly.IsDynamic) return;

      var assemblyName = loadedAssembly.GetName();
      if (references.TryGetValue(assemblyName.Name, out var location))
        location.Activate(loadedAssembly);
    }
    #endregion

    #region AssemblyLoadContext
    static readonly System.Runtime.Loader.AssemblyLoadContext ExternalContext;

    static readonly InternalLoadContext InternalContext = new InternalLoadContext();
    private sealed class InternalLoadContext : System.Runtime.Loader.AssemblyLoadContext
    {
      public InternalLoadContext() : base("Rhino.Inside")
      {
#if NET
        ResolvingUnmanagedDll += ResolveUnmanagedDll;
#endif
      }
      protected override Assembly Load(AssemblyName assemblyName)
      {
        if (ResolveAssembly(assemblyName) is Assembly solved)
          return solved;

        return base.Load(assemblyName);
      }

      internal Assembly ResolveAssembly(AssemblyName requested)
      {
        if (Core.CurrentStatus < Core.Status.Available)
          return default;

        if (requested.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
          return default;

        var location = default(AssemblyReference);
        var loadedAssembly = default(Assembly);

        // ResolveAssembly may be called from any thread.
        lock (references)
        {
          // Look up if Rhino deploy something for us…
          if (!references.TryGetValue(requested.Name, out location))
          {
            // Probe with loaded Assemblies if full name coincides.
            foreach (var assembly in Assemblies)
            {
              if (assembly.FullName == requested.FullName)
                return assembly;
            }

            return default;
          }

#pragma warning disable SYSLIB0044 // Type or member is obsolete
#pragma warning disable SYSLIB0012 // Type or member is obsolete
          if (location.Assembly is null)
          {
            // Never load an External assembly from an other thread than the UI thread.
            if (ThreadHandle.CurrentThreadId != ExternalThreadId && ExternalReferences.ContainsKey(location.assemblyName.Name))
              return default;

            // Remove it to not try again if it fails and avoid recursion.
            references.Remove(requested.Name);

            if (!AssemblyCanLoad(requested))
              return default;

            // Load Assembly
#if NET
            var assemblyPath = new Uri(location.assemblyName.CodeBase).LocalPath;
            if
            (
              (requested.Name.Equals("System", StringComparison.OrdinalIgnoreCase)  ||
              requested.Name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)) &&
              $"{Path.GetDirectoryName(assemblyPath)}\\".Equals(SystemPath, StringComparison.OrdinalIgnoreCase)
            )
            {
              loadedAssembly = ExternalContext.LoadFromAssemblyName(new AssemblyName(requested.Name));
            }
#else
            var assemblyPath = location.assemblyName.CodeBase;
#endif
            if (loadedAssembly is null)
            {
              // Never return an older assembly.
              if (location.assemblyName.Version >= requested.Version)
                loadedAssembly = LoadFromAssemblyPath(assemblyPath);
            }

            // Add again loaded assembly
            references.Add(requested.Name, location);
          }
        }

        lock (location)
        {
          if (loadedAssembly is object)
          {
            if (!loadedAssembly.CodeBase.Equals(location.assemblyName.CodeBase, StringComparison.OrdinalIgnoreCase))
              Logger.LogWarning
              (
                $"Unexpected Assembly CodeBase",
                $"Expected = {location.assemblyName.CodeBase}",
                $"CodeBase = {loadedAssembly.CodeBase}"
              );

            try { location.Activate(loadedAssembly); }
            catch { }
          }

          return location.Assembly;
        }
#pragma warning restore SYSLIB0012 // Type or member is obsolete
#pragma warning restore SYSLIB0044 // Type or member is obsolete
      }

#if NET
      IntPtr ResolveUnmanagedDll(Assembly assembly, string dll)
      {
        if (!Path.IsPathFullyQualified(dll))
        {
          var assemblyName = assembly.GetName();
#pragma warning disable SYSLIB0044 // Type or member is obsolete
#pragma warning disable SYSLIB0012 // Type or member is obsolete
          if (references.TryGetValue(assemblyName.Name, out var assemblyReference) && assemblyReference.assemblyName.CodeBase == assembly.CodeBase)
          {
            try
            {
              var lib = System.Runtime.InteropServices.NativeLibrary.Load
              (
                $"{Path.Combine(SystemPath, dll)}.dll",
                assembly,
                System.Runtime.InteropServices.DllImportSearchPath.AssemblyDirectory |
                System.Runtime.InteropServices.DllImportSearchPath.UseDllDirectoryForDependencies |
                System.Runtime.InteropServices.DllImportSearchPath.System32 |
                System.Runtime.InteropServices.DllImportSearchPath.SafeDirectories
              );
              return lib;
            }
            catch { }
          }
#pragma warning restore SYSLIB0012 // Type or member is obsolete
#pragma warning restore SYSLIB0044 // Type or member is obsolete
        }

        return IntPtr.Zero;
      }
#endif

      static bool AssemblyCanLoad(AssemblyName assemblyName)
      {
        if (assemblyName.Name == "RhinoCommon")
        {
          if (NativeLoader.GetReportOnLoad("opennurbs.dll"))
          {
            // Disable report opennurbs.dll is loaded 
            NativeLoader.SetReportOnLoad("opennurbs.dll", enable: false);

            // Check if 'opennurbs.dll' is already loaded
            var openNURBS = LibraryHandle.GetLoadedModule("opennurbs.dll");
            if (openNURBS != LibraryHandle.Zero)
            {
              var openNURBSVersion = FileVersionInfo.GetVersionInfo(openNURBS.ModuleFileName);

              using
              (
                var taskDialog = new TaskDialog($"Rhino.Inside {Core.Version} - openNURBS Conflict")
                {
                  Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.OpenNURBSConflict",
                  MainIcon = External.UI.TaskDialogIcons.IconError,
                  TitleAutoPrefix = false,
                  AllowCancellation = true,
                  MainInstruction = "An unsupported openNURBS version is already loaded. Rhino.Inside cannot run.",
                  MainContent = "Please restart Revit and load Rhino.Inside first to work around the problem.",
                  FooterText = $"Currently loaded openNURBS version: {openNURBSVersion.FileMajorPart}.{openNURBSVersion.FileMinorPart}.{openNURBSVersion.FileBuildPart}.{openNURBSVersion.FilePrivatePart}"
                }
              )
              {
                taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "More information…");
                taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Report Error…", "by email to tech@mcneel.com");
                taskDialog.DefaultButton = TaskDialogResult.CommandLink2;
                switch (taskDialog.Show())
                {
                  case TaskDialogResult.CommandLink1:
                    Browser.Start($@"{Core.WebSite}reference/known-issues");
                    break;
                  case TaskDialogResult.CommandLink2:

                    var RhinoInside_dmp = Path.Combine
                    (
                      Path.GetDirectoryName(Core.Host.Services.RecordingJournalFilename),
                      Path.GetFileNameWithoutExtension(Core.Host.Services.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
                    );

                    MiniDumper.Write(RhinoInside_dmp);

                    ErrorReport.SendEmail
                    (
                      app: Core.Host,
                      subject: $"{Core.Product}.{Core.Platform} - openNURBS Conflict",
                      body: null,
                      includeAddinsList: true,
                      attachments: new string[]
                      {
                      Core.Host.Services.RecordingJournalFilename,
                      RhinoInside_dmp
                      }
                    );

                    Core.CurrentStatus = Core.Status.Failed;
                    break;
                }
              }

              return false;
            }
          }
        }

        return true;
      }
    }
    #endregion

    #region ExternalReferences
    static readonly Dictionary<string, Predicate<Assembly>> ExternalReferences = new Dictionary<string, Predicate<Assembly>>()
    {
      { "Eto",          Rhinoceros.InitEto },
      { "RhinoCommon",  Rhinoceros.InitRhinoCommon },
      { "Grasshopper",  Rhinoceros.InitGrasshopper },
    };
    static bool IsExternalReference(AssemblyName assembly) => ExternalReferences.ContainsKey(assembly.Name);

    static readonly uint ExternalThreadId = ThreadHandle.CurrentThreadId;
    static bool ExternalAssembliesOnly = true;

    static void ActivationGate_Enter(object sender, EventArgs e) => ExternalAssembliesOnly = false;
    static void ActivationGate_Exit(object sender, EventArgs e) => ExternalAssembliesOnly = true;

    static Assembly ExternalContextResolving(System.Runtime.Loader.AssemblyLoadContext loadContext, AssemblyName assemblyName)
    {
      // Resolve this Assembly
      {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var executingAssemblyName = executingAssembly.GetName();
        if (assemblyName.Name == executingAssemblyName.Name)
          return assemblyName.Version > executingAssemblyName.Version ? default : executingAssembly;
      }

      if (ExternalAssembliesOnly && !IsExternalReference(assemblyName))
        return default;

      var internalAssembliesOnly = ExternalAssembliesOnly;
      try
      {
        internalAssembliesOnly = false;
        return InternalContext.ResolveAssembly(assemblyName);
      }
      finally
      {
        ExternalAssembliesOnly = internalAssembliesOnly;
      }
    }
    #endregion

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
            // Report if opennurbs.dll is loaded
            NativeLoader.SetReportOnLoad("opennurbs.dll", enable: true);

            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;
            {
              if (External.ActivationGate.IsOpen)
                ActivationGate_Enter(default, EventArgs.Empty);

              External.ActivationGate.Enter += ActivationGate_Enter;
              External.ActivationGate.Exit += ActivationGate_Exit;
            }
          }
          else
          {
            {
              External.ActivationGate.Exit -= ActivationGate_Exit;
              External.ActivationGate.Enter -= ActivationGate_Enter;

              if (External.ActivationGate.IsOpen)
                ActivationGate_Exit(default, EventArgs.Empty);
            }
            AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoaded;

            // Disable report opennurbs.dll is loaded 
            NativeLoader.SetReportOnLoad("opennurbs.dll", enable: false);
          }
          enabled = value;
        }
      }
    }

    static AssemblyResolver()
    {
      // Search Rhino stuff
      System.Threading.Tasks.Task.Run(() =>
      {
        lock (references)
        {
          try
          {
            var architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

            // List of assembly folders in priority order.
            var paths = new (DirectoryInfo Directory, SearchOption Options)[]
            {
#if NET
              (new DirectoryInfo(Path.Combine(SystemPath, "netcore", "runtimes", $"win-{architecture}", "native")), SearchOption.TopDirectoryOnly),
              (new DirectoryInfo(Path.Combine(SystemPath, "netcore", "runtimes", "win", "native")), SearchOption.TopDirectoryOnly),
              (new DirectoryInfo(Path.Combine(SystemPath, "netcore", "runtimes", $"win-{architecture}", "lib", "net8.0")), SearchOption.TopDirectoryOnly),
              (new DirectoryInfo(Path.Combine(SystemPath, "netcore", "runtimes", "win", "lib", "net8.0")), SearchOption.TopDirectoryOnly),
              (new DirectoryInfo(Path.Combine(SystemPath, "netcore", "runtimes", $"win-{architecture}", "lib", "net7.0")), SearchOption.TopDirectoryOnly),
              (new DirectoryInfo(Path.Combine(SystemPath, "netcore", "runtimes", "win", "lib", "net7.0")), SearchOption.TopDirectoryOnly),
              (new DirectoryInfo(Path.Combine(SystemPath, "netcore")), SearchOption.TopDirectoryOnly),
#endif
              (new DirectoryInfo(SystemPath), SearchOption.TopDirectoryOnly),
              (new DirectoryInfo(Path.Combine(PluginsPath, "Grasshopper")), SearchOption.TopDirectoryOnly),
            };

            foreach (var path in paths.Where(x => x.Directory.Exists))
            {
              foreach (var dll in path.Directory.EnumerateFilesByExtension(".dll", path.Options))
              {
                try
                {
                  var assemblyName = System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName(dll.FullName);
                  if (references.ContainsKey(assemblyName.Name)) continue;

#if NET
#pragma warning disable SYSLIB0044 // Type or member is obsolete
                  assemblyName.CodeBase = new Uri(dll.FullName).ToString();
#pragma warning restore SYSLIB0044 // Type or member is obsolete
#endif
                  references.Add(assemblyName.Name, new AssemblyReference(assemblyName));
                }
                catch { }
              }
            }
          }
          catch { }
        }
      });

      // Setup Resolving event
      ExternalContext = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(AssemblyResolver).Assembly);
      ExternalContext.Resolving += ExternalContextResolving;
    }
  }

  struct SynchronizationContextGuard : IDisposable
  {
    public static SynchronizationContextGuard Current => new SynchronizationContextGuard(System.Threading.SynchronizationContext.Current);

    readonly System.Threading.SynchronizationContext Previous;
    private SynchronizationContextGuard(System.Threading.SynchronizationContext context) => Previous = context;
    readonly void IDisposable.Dispose()
    {
      if (Previous != null && System.Threading.SynchronizationContext.Current != Previous)
        System.Threading.SynchronizationContext.SetSynchronizationContext(Previous);
    }
  }
}
