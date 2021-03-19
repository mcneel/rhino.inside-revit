using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  #region HostApplication
  public abstract class HostApplication : IDisposable
  {
    protected HostApplication() { }
    public abstract void Dispose();

    public static implicit operator HostApplication(Application value) => new HostApplicationU(value);
    public static implicit operator HostApplication(ControlledApplication value) => new HostApplicationC(value);

    public abstract object Value { get; }

    #region Version
    public abstract string VersionName { get; }
    public abstract string VersionNumber { get; }
    public abstract string VersionBuild { get; }
    public abstract string SubVersionNumber { get; }

    public abstract LanguageType Language { get; }
    #endregion
  }

  class HostApplicationC : HostApplication
  {
    readonly ControlledApplication _app;
    public HostApplicationC(ControlledApplication app) => _app = app;
    public override void Dispose() { }
    public override object Value => _app;

    #region Version
    public override string VersionName => _app.VersionName;
    public override string VersionNumber => _app.VersionNumber;
    public override string VersionBuild => _app.VersionBuild;
    public override string SubVersionNumber => _app.SubVersionNumber;

    public override LanguageType Language => _app.Language;
    #endregion
  }

  class HostApplicationU : HostApplication
  {
    readonly Application _app;
    public HostApplicationU(Application app) => _app = app;
    public override void Dispose() => _app.Dispose();
    public override object Value => _app;

    #region Version
    public override string VersionName => _app.VersionName;
    public override string VersionNumber => _app.VersionNumber;
    public override string VersionBuild => _app.VersionBuild;
    public override string SubVersionNumber => _app.SubVersionNumber;
    #endregion

    public override LanguageType Language => _app.Language;
  }
  #endregion


  /// <summary>
  /// Base class for an external Revit application
  /// </summary>
  public abstract class ExternalApplication : IExternalDBApplication
  {
    protected abstract ExternalDBApplicationResult OnShutdown(ControlledApplication application);
    ExternalDBApplicationResult IExternalDBApplication.OnShutdown(ControlledApplication application) =>
      ActivationGate.Open(() => OnShutdown(application), this);

    protected abstract ExternalDBApplicationResult OnStartup(ControlledApplication application);
    ExternalDBApplicationResult IExternalDBApplication.OnStartup(ControlledApplication application) =>
      ActivationGate.Open(() => OnStartup(application), this);
  }
}
