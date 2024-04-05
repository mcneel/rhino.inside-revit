using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.ApplicationServices
{
  using Extensions;

  #region HostServices
  public abstract class HostServices : IDisposable
  {
    protected internal HostServices(bool disposable) => Disposable = disposable;

    #region IDisposable
#pragma warning disable CA1063 // Implement IDisposable Correctly
    readonly bool Disposable;
    protected abstract void Dispose(bool disposing);
    void IDisposable.Dispose()
    {
      if (!Disposable) return;

      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
#pragma warning restore CA1063 // Implement IDisposable Correctly
    #endregion

    public static implicit operator HostServices(Application value) => new HostServicesU(value, disposable: true);
    public static implicit operator HostServices(ControlledApplication value) => new HostServicesC(value, disposable: true);

    #region Runtime
    internal static HostServices Current;

    internal static bool StartUp(ControlledApplication app)
    {
      // Register Revit Failures
      DB.ExternalFailures.CreateFailureDefinitions();

      Current = new HostServicesC(app, disposable: false);
      Current.ApplicationInitialized += Initialized;
      return true;
    }

    private static void Initialized(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
    {
      Current.ApplicationInitialized -= Initialized;
      Current = new HostServicesU(sender as Application, disposable: false);

      // From now on DB is available
      //
    }

    internal static bool Shutdown(ControlledApplication app)
    {
      Current?.Dispose(true);
      Current = null;
      return true;
    }
    #endregion

    #region Version
    public abstract string VersionName { get; }
    public abstract string VersionNumber { get; }
    public abstract string VersionBuild { get; }
    public abstract string SubVersionNumber { get; }

    public abstract ProductType Product { get; }
    public abstract LanguageType Language { get; }
    #endregion

    #region Journaling
    public abstract string RecordingJournalFilename { get; }
    public abstract void WriteJournalComment(string comment, bool timeStamp);
    #endregion

    #region Folders
    public abstract string CurrentUsersDataFolderPath { get; }
    public abstract string CurrentUserAddinsLocation { get; }
    #endregion

    #region SharedParameters
    public abstract string SharedParametersFilename { get; set; }
    public abstract DefinitionFile OpenSharedParameterFile();

    public DefinitionFile CreateSharedParameterFile()
    {
      string sharedParametersFilename = SharedParametersFilename;
      string tempParametersFilename = System.IO.Path.GetTempFileName() + ".txt";
      try
      {
        // Create Temp Shared Parameters File
        using (System.IO.File.CreateText(tempParametersFilename)) { }
        SharedParametersFilename = tempParametersFilename;
        return OpenSharedParameterFile();
      }
      finally
      {
        // Restore User Shared Parameters File
        SharedParametersFilename = sharedParametersFilename;
        try { System.IO.File.Delete(tempParametersFilename); }
        catch { }
      }
    }
    public DefinitionFile LoadSharedParameterFile(string fileName = null)
    {
      if (!System.IO.File.Exists(fileName)) return null;

      string sharedParametersFilename = SharedParametersFilename;
      try
      {
        // Set Temp Shared Parameters Name
        SharedParametersFilename = fileName;
        return OpenSharedParameterFile();
      }
      finally
      {
        // Restore User Shared Parameters File
        SharedParametersFilename = sharedParametersFilename;
      }
    }
    #endregion

    #region Events
    public abstract event EventHandler<Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs> ApplicationInitialized;
    public abstract event EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs> DocumentChanged;
    public abstract event EventHandler<Autodesk.Revit.DB.Events.DocumentClosingEventArgs> DocumentClosing;
    #endregion
  }

  sealed class HostServicesC : HostServices
  {
    ControlledApplication _app;
    public HostServicesC(ControlledApplication app, bool disposable) : base(disposable) => _app = app;
    protected override void Dispose(bool disposing)
    {
      if (_app is object)
      {
        if (disposing)
        {
          //_app.Dispose();
        }

        _app = null;
      }
    }

    #region Version
    public override string VersionName => _app.VersionName;
    public override string VersionNumber => _app.VersionNumber;
    public override string VersionBuild => _app.VersionBuild;
    public override string SubVersionNumber => _app.GetSubVersionNumber();

    public override ProductType Product => _app.Product;
    public override LanguageType Language => _app.Language;
    #endregion

    #region Journaling
    public override string RecordingJournalFilename => _app.RecordingJournalFilename;
    public override void WriteJournalComment(string comment, bool timeStamp) => _app.WriteJournalComment(comment, timeStamp);
    #endregion

    #region Folders
    public override string CurrentUsersDataFolderPath =>
#if REVIT_2019
      _app.CurrentUsersDataFolderPath;
#else
      System.IO.Path.Combine
      (
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
        "Autodesk",
        "Revit",
        _app.VersionName
      );
#endif
    public override string CurrentUserAddinsLocation => _app.CurrentUserAddinsLocation;
    #endregion

    #region SharedParameters
    public override string SharedParametersFilename
    {
      get => _app.SharedParametersFilename;
      set => _app.SharedParametersFilename = value;
    }

    public override DefinitionFile OpenSharedParameterFile() => _app.OpenSharedParameterFile();
    #endregion

    #region Events
    public override event EventHandler<Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs> ApplicationInitialized
    {
      add    => _app.ApplicationInitialized += ActivationGate.AddEventHandler(value);
      remove => _app.ApplicationInitialized -= ActivationGate.RemoveEventHandler(value);
    }
    public override event EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs> DocumentChanged
    {
      add    => _app.DocumentChanged += ActivationGate.AddEventHandler(value);
      remove => _app.DocumentChanged -= ActivationGate.RemoveEventHandler(value);
    }
    public override event EventHandler<Autodesk.Revit.DB.Events.DocumentClosingEventArgs> DocumentClosing
    {
      add    => _app.DocumentClosing += ActivationGate.AddEventHandler(value);
      remove => _app.DocumentClosing -= ActivationGate.RemoveEventHandler(value);
    }
    #endregion
  }

  sealed class HostServicesU : HostServices
  {
    Application _app;
    public HostServicesU(Application app, bool disposable) : base(disposable) => _app = app;

    protected override void Dispose(bool disposing)
    {
      if (_app is object)
      {
        if (disposing)
        {
          _app.Dispose();
        }

        _app = null;
      }
    }
    
    #region Version
    public override string VersionName => _app.VersionName;
    public override string VersionNumber => _app.VersionNumber;
    public override string VersionBuild => _app.VersionBuild;
    public override string SubVersionNumber => _app.GetSubVersionNumber();

    public override ProductType Product => _app.Product;
    public override LanguageType Language => _app.Language;
    #endregion

    #region Journaling
    public override string RecordingJournalFilename => _app.RecordingJournalFilename;
    public override void WriteJournalComment(string comment, bool timeStamp) => _app.WriteJournalComment(comment, timeStamp);
    #endregion

    #region Folders
    public override string CurrentUsersDataFolderPath =>
#if REVIT_2019
      _app.CurrentUsersDataFolderPath;
#else
      System.IO.Path.Combine
      (
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
        "Autodesk",
        "Revit",
        _app.VersionName
      );
#endif
    public override string CurrentUserAddinsLocation => _app.CurrentUserAddinsLocation;
    #endregion

    #region SharedParameters
    public override string SharedParametersFilename
    {
      get => _app.SharedParametersFilename;
      set => _app.SharedParametersFilename = value;
    }

    public override DefinitionFile OpenSharedParameterFile() => _app.OpenSharedParameterFile();
    #endregion

    #region Events
    public override event EventHandler<Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs> ApplicationInitialized
    {
      add    => _app.ApplicationInitialized += ActivationGate.AddEventHandler(value);
      remove => _app.ApplicationInitialized -= ActivationGate.RemoveEventHandler(value);
    }
    public override event EventHandler<Autodesk.Revit.DB.Events.DocumentChangedEventArgs> DocumentChanged
    {
      add    => _app.DocumentChanged += ActivationGate.AddEventHandler(value);
      remove => _app.DocumentChanged -= ActivationGate.RemoveEventHandler(value);
    }
    public override event EventHandler<Autodesk.Revit.DB.Events.DocumentClosingEventArgs> DocumentClosing
    {
      add    => _app.DocumentClosing += ActivationGate.AddEventHandler(value);
      remove => _app.DocumentClosing -= ActivationGate.RemoveEventHandler(value);
    }
    #endregion
  }
  #endregion
}
