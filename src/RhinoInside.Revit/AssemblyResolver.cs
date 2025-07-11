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
    static readonly Dictionary<string, Predicate<Assembly>> SharedAssemblies = new Dictionary<string, Predicate<Assembly>>()
    {
      { "Eto",          Rhinoceros.InitEto },
      { "RhinoCommon",  Rhinoceros.InitRhinoCommon },
      { "Grasshopper",  Rhinoceros.InitGrasshopper },
    };

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
        if (SharedAssemblies.TryGetValue(assemblyName.Name, out var InitAssembly))
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
    #endregion

    #region Load context
    static readonly InternalLoadContext InternalContext = new InternalLoadContext();
    class InternalLoadContext : System.Runtime.Loader.AssemblyLoadContext
    {
      public InternalLoadContext() : base("Rhino.Inside") { }
    }
    #endregion

    static AssemblyResolver()
    {
      // Search Rhino stuff
      System.Threading.Tasks.Task.Run(() =>
      {
        lock (references)
        {
          try
          {
            // List of assembly folders in priority order.
            var paths = new (DirectoryInfo Directory, SearchOption Options)[]
            {
              (new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), SearchOption.AllDirectories),
#if NET
              (new DirectoryInfo(Path.Combine(SystemPath, "netcore")), SearchOption.TopDirectoryOnly),
#endif
              (new DirectoryInfo(SystemPath), SearchOption.TopDirectoryOnly),
              (new DirectoryInfo(PluginsPath), SearchOption.AllDirectories),
            };

            foreach (var path in paths.Where(x => x.Directory.Exists))
            {
              foreach (var dll in path.Directory.EnumerateFilesByExtension(".dll", path.Options))
              {
                try
                {
                  var assemblyName = System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName(dll.FullName);
#if NET
#pragma warning disable SYSLIB0044 // Type or member is obsolete
                  assemblyName.CodeBase = new Uri(dll.FullName).ToString();
#pragma warning restore SYSLIB0044 // Type or member is obsolete
#endif

                  if (references.ContainsKey(assemblyName.Name)) continue;
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
      {
        InternalContext.Resolving += ResolveInternalAssembly;
        InternalContext.ResolvingUnmanagedDll += ResolveUnmanagedDll;
#if NET
        System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += ResolveSharedAssembly;
#endif
      }
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

    static readonly uint InternalThreadId = ThreadHandle.CurrentThreadId;
    static bool SharedAssembliesOnly = true;
    static void ActivationGate_Enter(object sender, EventArgs e) => SharedAssembliesOnly = false;
    static void ActivationGate_Exit(object sender, EventArgs e) => SharedAssembliesOnly = true;

    static Assembly ResolveSharedAssembly(System.Runtime.Loader.AssemblyLoadContext loadContext, AssemblyName assemblyName)
    {
      if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
        return default;

      var internalAssembliesOnly = SharedAssembliesOnly;
      try
      {
        if (SharedAssembliesOnly && !IsSharedReference(assemblyName))
          return default;

        internalAssembliesOnly = false;

        return ResolveAssembly(InternalContext, assemblyName);
      }
      finally
      {
        SharedAssembliesOnly = internalAssembliesOnly;
      }
    }

    static Assembly ResolveInternalAssembly(System.Runtime.Loader.AssemblyLoadContext loadContext, AssemblyName assemblyName)
    {
      if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
        return default;

      var sharedAssembliesOnly = SharedAssembliesOnly;
      try
      {
        if (SharedAssembliesOnly && !IsSharedReference(assemblyName))
          return default;

        sharedAssembliesOnly = false;

//#if !NET
//        if (Logger.Active)
//        {
//          using (var scope = Logger.LogScope
//          (
//            "AppDomain.AssemblyResolve",
//            $"Requesting Assembly = {GetRequestingAssembly().FullName}",
//            $"Requires = '{assemblyName}'"
//          ))
//          {
//            var resolved = default(Assembly);
//            foreach (ResolveEventHandler resolver in InvocationList)
//            {
//              var type = MethodInfo.GetCurrentMethod().DeclaringType;
//              var info = type.GetMethod(nameof(AssemblyResolving), BindingFlags.NonPublic | BindingFlags.Static);
//              if (resolver.Method == info)
//              {
//                resolved = ResolveAssembly(loadContext, assemblyName);
//              }
//              else
//              {
//                try { resolved = resolver(sender, args); }
//                catch (Exception) { }
//              }

//              if (resolved is object)
//              {
//                scope.Log
//                (
//                  resolved.FullName == args.Name ?
//                  Logger.Severity.Succeded :
//                  Logger.Severity.Information,
//                  $"Resolved",
//                  $"Resolver = '{resolver.Method.DeclaringType.Assembly}'",
//                  $"Got = '{resolved.FullName}'",
//                  $"Location = '{resolved.Location}'"
//                );
//                break;
//              }
//            }

//            if (resolved is null)
//              scope.LogWarning("Not Resolved");

//            return resolved;
//          }
//        }
//        else
//#endif
        {
          return ResolveAssembly(loadContext, assemblyName);
        }
      }
      finally
      {
        SharedAssembliesOnly = sharedAssembliesOnly;
      }
    }

    static Assembly ResolveAssembly(System.Runtime.Loader.AssemblyLoadContext loadContext, AssemblyName requested)
    {
      if (Core.CurrentStatus < Core.Status.Available)
        return default;

      // Resolve this Assembly
      {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var executingAssemblyName = executingAssembly.GetName();
        if (requested.Name == executingAssemblyName.Name)
          return requested.Version > executingAssemblyName.Version ? default : executingAssembly;
      }

      // ResolveAssembly may be called from any thread.
      lock (references)
      {
        // Look up if Rhino deploy something for us…
        if (!references.TryGetValue(requested.Name, out var location))
        {
          // Probe with loaded Assemblies if full name coincides.
          var assemblies = loadContext.Assemblies;
          foreach (var assembly in assemblies)
          {
            if (assembly.FullName == requested.FullName)
              return assembly;
          }

          return default;
        }

        // Never return an older assembly.
#if DEBUG
        Debug.Assert(location.assemblyName.Version >= requested.Version);
#else
        if (location.assemblyName.Version < requested.Version)
          return default;
#endif

        if (location.Assembly is null)
        {
          // Never load an Internal assembly from an other thread than the UI thread.
          if (ThreadHandle.CurrentThreadId != InternalThreadId && SharedAssemblies.ContainsKey(location.assemblyName.Name))
            return default;

          // Remove it to not try again if it fails and avoid recursion.
          references.Remove(requested.Name);

          if (!AssemblyCanLoad(requested))
            return default;

#pragma warning disable SYSLIB0044 // Type or member is obsolete
#pragma warning disable SYSLIB0012 // Type or member is obsolete
#if NET
          var assemblyPath = new Uri(location.assemblyName.CodeBase).LocalPath;
#else
          var assemblyPath = location.assemblyName.CodeBase;
#endif
          // Load Assembly
          var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

          Debug.Assert
          (
            assembly.CodeBase.Equals(location.assemblyName.CodeBase, StringComparison.OrdinalIgnoreCase),
            $"Expected = {location.assemblyName.CodeBase}" + Environment.NewLine +
            $"Loaded = {assembly.CodeBase}"
          );
#pragma warning restore SYSLIB0012 // Type or member is obsolete
#pragma warning restore SYSLIB0044 // Type or member is obsolete

          // Add again loaded assembly
          references.Add(requested.Name, location);

          try { location.Activate(assembly); }
          catch { }
        }


        return location.Assembly;
      }
    }

    static IntPtr ResolveUnmanagedDll(Assembly assembly, string dll)
    {
#if NET
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
#endif

      return IntPtr.Zero;
    }

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

    static void AssemblyLoaded(object sender, AssemblyLoadEventArgs args)
    {
      if (args.LoadedAssembly.ReflectionOnly) return;

      var assemblyName = args.LoadedAssembly.GetName();
      if (references.TryGetValue(assemblyName.Name, out var location))
        location.Activate(args.LoadedAssembly);
    }

    #region Utils
    static bool IsSharedReference(AssemblyName assembly)
    {
      if
      (
        SharedAssemblies.ContainsKey(assembly.Name) &&
        GetRequestingAssembly() is Assembly requestingAssembly
      )
        return requestingAssembly.GetReferencedAssemblies().Any(x => SharedAssemblies.ContainsKey(x.Name));

      return false;
    }

    static Assembly GetRequestingAssembly()
    {
      var trace = new StackTrace(1);
      var frames = trace.GetFrames();

      var callingAssembly = Assembly.GetCallingAssembly();

      // Skip Calling Assembly
      int f = 0;
      for (; f < frames.Length; ++f)
      {
        var method = frames[f].GetMethod();
        if (method is null) continue;
        var frameAssembly = method.DeclaringType?.Assembly ?? method.Module?.Assembly;
        if (frameAssembly != callingAssembly)
          break;
      }

      // Skip mscorlib
      for (; f < frames.Length; ++f)
      {
        var method = frames[f].GetMethod();
        if (method is null) continue;
        var frameAssembly = method.DeclaringType?.Assembly ?? method.Module?.Assembly;
        if (frameAssembly != typeof(object).Assembly)
          return frameAssembly;
      }

      return null;
    }
    #endregion
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

#if !NET
namespace System.Runtime.Loader
{
  class AssemblyLoadContext
  {
    private readonly AppDomain AppDomain;

    public static readonly AssemblyLoadContext Default = new AssemblyLoadContext(AppDomain.CurrentDomain);
    public static IEnumerable<AssemblyLoadContext> All => new AssemblyLoadContext[] { Default };
    public IEnumerable<Assembly> Assemblies => AppDomain.GetAssemblies();

    public override string ToString() => $"\"{Name}\" {GetType()} #{AppDomain.Id}";

    public string Name { get; }
    public bool IsCollectible => !AppDomain.IsDefaultAppDomain();

    public event Func<AssemblyLoadContext, AssemblyName, Assembly> Resolving;
    public event Func<Assembly, string, IntPtr> ResolvingUnmanagedDll;

    private AssemblyLoadContext(AppDomain appDomain)
    {
      AppDomain = appDomain;
      Name = AppDomain.FriendlyName;
    }

    public AssemblyLoadContext() : this(default, default) { }
    public AssemblyLoadContext(bool isCollectible) : this(default, isCollectible) { }
    public AssemblyLoadContext(string name, bool isCollectible = false)
    {
      if (isCollectible && AppDomain.CurrentDomain.IsDefaultAppDomain()) throw new NotSupportedException();

      AppDomain = AppDomain.CurrentDomain;
      Name = name;

      var domain = AppDomain.CurrentDomain;
      var assemblyResolve = _AssemblyResolve.GetValue(domain) as ResolveEventHandler;
      var invocationList = assemblyResolve.GetInvocationList();

      foreach (var invocation in invocationList)
        domain.AssemblyResolve -= invocation as ResolveEventHandler;

      domain.AssemblyResolve += AssemblyResolving;

      foreach (var invocation in invocationList)
        domain.AssemblyResolve += invocation as ResolveEventHandler;
    }

    public static AssemblyName GetAssemblyName(string assemblyPath) => AssemblyName.GetAssemblyName(assemblyPath);
    public Assembly LoadFromAssemblyName(AssemblyName assemblyName) => AppDomain.Load(assemblyName);
    public Assembly LoadFromAssemblyPath(string assemblyPath) => Assembly.LoadFrom(assemblyPath);

    protected virtual Assembly Load(AssemblyName assemblyName) => null;
    protected virtual IntPtr LoadUnmanagedDll(string unmanagedDllName) => IntPtr.Zero;

    static readonly FieldInfo _AssemblyResolve = typeof(AppDomain).GetField("_AssemblyResolve", BindingFlags.Instance | BindingFlags.NonPublic);
    Assembly AssemblyResolving(object sender, ResolveEventArgs args)
    {
      if (Resolving?.GetInvocationList() is Delegate[] invocationList)
      {
        var assemblyName = new AssemblyName(args.Name);
        foreach (Func<AssemblyLoadContext, AssemblyName, Assembly> resolver in invocationList)
        {
          try
          {
            var resolved = resolver(this, assemblyName);
            if (resolved is object) return resolved;
          }
          catch { }
        }
      }

      return default;
    }
  }
}
#endif
