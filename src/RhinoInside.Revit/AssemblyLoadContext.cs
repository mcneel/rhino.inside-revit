#if !NET
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace System.Runtime.Loader
{
  class AssemblyLoadContext
  {
    static AssemblyLoadContext()
    {
      AppDomain = AppDomain.CurrentDomain;

      var assemblyResolve = _AssemblyResolve.GetValue(AppDomain) as ResolveEventHandler;
      if (assemblyResolve?.GetInvocationList() is Delegate[] invocationList)
      {
        foreach (var invocation in invocationList)
          AppDomain.AssemblyResolve -= invocation as ResolveEventHandler;

        AppDomain.AssemblyResolve += Resolve;

        foreach (var invocation in invocationList)
          AppDomain.AssemblyResolve += invocation as ResolveEventHandler;
      }
      else AppDomain.AssemblyResolve += Resolve;

      AppDomain.AssemblyLoad += AssemblyLoad;
    }

    private static readonly AppDomain AppDomain;
    private static readonly List<AssemblyLoadContext> AllContexts = new List<AssemblyLoadContext>();
    private static readonly Dictionary<Assembly, AssemblyLoadContext> AssemblyContexts = new Dictionary<Assembly, AssemblyLoadContext>();
    public static IEnumerable<AssemblyLoadContext> All => AllContexts;
    public static readonly AssemblyLoadContext Default = new AssemblyLoadContext("Default", isCollectible: false);
    public static AssemblyLoadContext GetLoadContext(Assembly assembly)
    {
      if (assembly is null) throw new ArgumentNullException(nameof(assembly));
      return AssemblyContexts.TryGetValue(assembly, out var context) ? context : Default;
    }
    public static AssemblyName GetAssemblyName(string assemblyPath) => AssemblyName.GetAssemblyName(assemblyPath);

    #region ContextualReflectionContext
    private static readonly AsyncLocal<AssemblyLoadContext> _CurrentContextualReflectionContext = new AsyncLocal<AssemblyLoadContext>();
    public static AssemblyLoadContext CurrentContextualReflectionContext => _CurrentContextualReflectionContext?.Value;

    public ContextualReflectionScope EnterContextualReflection() => new ContextualReflectionScope(this);
    public static ContextualReflectionScope EnterContextualReflection(Assembly activating)
    {
      if (activating == null)
        return new ContextualReflectionScope(null);

      return GetLoadContext(activating).EnterContextualReflection();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct ContextualReflectionScope : IDisposable
    {
      private readonly AssemblyLoadContext _previous;

      internal ContextualReflectionScope(AssemblyLoadContext activating)
      {
        _previous = CurrentContextualReflectionContext;
        _CurrentContextualReflectionContext.Value = activating;
      }

      public void Dispose()
      {
        _CurrentContextualReflectionContext.Value = _previous;
      }
    }
    #endregion

    public override string ToString() => $"\"{Name}\" {GetType()} #{Id}";

    private readonly int Id;
    public string Name { get; }
    public bool IsCollectible => !AppDomain.IsDefaultAppDomain();
    public IEnumerable<Assembly> Assemblies => AppDomain.GetAssemblies().Where(x => ReferenceEquals(GetLoadContext(x), this));

    public event Func<AssemblyLoadContext, AssemblyName, Assembly> Resolving;
    public event Func<Assembly, string, IntPtr> ResolvingUnmanagedDll;

    public AssemblyLoadContext() : this(default, default) { }
    public AssemblyLoadContext(bool isCollectible) : this(default, isCollectible) { }
    public AssemblyLoadContext(string name, bool isCollectible = false)
    {
      if (isCollectible && AppDomain.CurrentDomain.IsDefaultAppDomain()) throw new NotSupportedException();

      Name = name;

      lock (AllContexts)
      {
        AllContexts.Add(this);
        Id = AllContexts.Count;
      }
    }

    public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
    {
      using (EnterContextualReflection())
        return AppDomain.Load(assemblyName);
    }

    public Assembly LoadFromAssemblyPath(string assemblyPath)
    {
      using (EnterContextualReflection())
        return Assembly.LoadFrom(assemblyPath);
    }

    protected virtual Assembly Load(AssemblyName assemblyName) => null;

    protected virtual IntPtr LoadUnmanagedDll(string unmanagedDllName) => IntPtr.Zero;

    #region Resolve
    private static readonly FieldInfo _AssemblyResolve = typeof(AppDomain).GetField("_AssemblyResolve", BindingFlags.Instance | BindingFlags.NonPublic);
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
      if ((args.RequestingAssembly ?? GetRequestingAssembly()) is Assembly requestingAssembly)
      {
        if (GetLoadContext(requestingAssembly) is AssemblyLoadContext context)
        {
          var assemblyName = new AssemblyName(args.Name);

          if (context.ResolveUsingLoad(assemblyName) is Assembly loadedAssembly)
            return loadedAssembly;

          return context.ResolveUsingEvent(assemblyName);
        }
      }

      return null;
    }

    private static void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
      if (args.LoadedAssembly.ReflectionOnly) return;

      var context = CurrentContextualReflectionContext;
      if (context is null && GetRequestingAssembly() is Assembly requestingAssembly)
        AssemblyContexts.TryGetValue(requestingAssembly, out context);

      if (context is object)
        AssemblyContexts.Add(args.LoadedAssembly, context);
    }

    private Assembly ResolveUsingLoad(AssemblyName assemblyName)
    {
      try
      {
        if (Load(assemblyName) is Assembly loadedAssembly)
          return AssertValidAssemblyName(loadedAssembly, assemblyName);
      }
      catch {}

      return null;
    }

    private Assembly ResolveUsingEvent(AssemblyName assemblyName)
    {
      if (Resolving?.GetInvocationList() is Delegate[] invocationList)
      {
        foreach (Func<AssemblyLoadContext, AssemblyName, Assembly> resolver in invocationList)
        {
          try
          {
            if (resolver(this, assemblyName) is Assembly resolvedAssembly)
              return AssertValidAssemblyName(resolvedAssembly, assemblyName);
          }
          catch { }
        }
      }

      return null;
    }

    private static Assembly AssertValidAssemblyName(Assembly assembly, AssemblyName assemblyName)
    {
      var loadedName = assembly.GetName().Name;
      if (string.IsNullOrEmpty(loadedName) || !assemblyName.Name.Equals(loadedName, StringComparison.InvariantCultureIgnoreCase))
        throw new InvalidOperationException("Invalid assembly name.");

      return assembly;
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
}
#endif
