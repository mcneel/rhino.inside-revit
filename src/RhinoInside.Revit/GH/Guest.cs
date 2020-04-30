using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;

using Rhino;
using Rhino.PlugIns;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.GUI.Canvas;

namespace RhinoInside.Revit.GH
{
  [GuestPlugInId("B45A29B1-4343-4035-989E-044E8580D9CF")]
  internal class Guest : IGuest
  {
    public static Grasshopper.Plugin.GH_RhinoScriptInterface Script = new Grasshopper.Plugin.GH_RhinoScriptInterface();
    PreviewServer previewServer;
    public string Name => "Grasshopper";
    LoadReturnCode IGuest.OnCheckIn(ref string errorMessage)
    {
      string message = null;
      try
      {
        if(!LoadComponents())
          message = "Failed to load Revit Grasshopper components.";
      }
      catch (FileNotFoundException e) { message = $"{e.Message}{Environment.NewLine}{e.FileName}"; }
      catch (Exception e)             { message = e.Message; }

      if (!(message is null))
      {
        errorMessage = message;
        return LoadReturnCode.ErrorShowDialog;
      }

      // Register PreviewServer
      previewServer = new PreviewServer();
      previewServer.Register();

      Revit.DocumentChanged += OnDocumentChanged;
      Revit.ApplicationUI.Idling += OnIdle;

      External.ActivationGate.Enter += ModalScope_Enter;
      External.ActivationGate.Exit  += ModalScope_Exit;

      RhinoDoc.BeginOpenDocument                += BeginOpenDocument;
      RhinoDoc.EndOpenDocumentInitialViewUpdate += EndOpenDocumentInitialViewUpdate;

      Instances.CanvasCreatedEventHandler Canvas_Created = default;
      Instances.CanvasCreated += Canvas_Created = (canvas) =>
      {
        Instances.CanvasCreated -= Canvas_Created;
        canvas.DocumentChanged  += ActiveCanvas_DocumentChanged;
      };

      Instances.CanvasDestroyedEventHandler Canvas_Destroyed = default;
      Instances.CanvasDestroyed += Canvas_Destroyed = (canvas) =>
      {
        Instances.CanvasDestroyed -= Canvas_Destroyed;
        canvas.DocumentChanged    -= ActiveCanvas_DocumentChanged;
      };

      return LoadReturnCode.Success;
    }

    static Rhino.UnitSystem modelUnitSystem = Rhino.UnitSystem.Unset;
    public static Rhino.UnitSystem ModelUnitSystem
    {
      get => Instances.ActiveCanvas is null ? Rhino.UnitSystem.Unset : modelUnitSystem;
      private set => modelUnitSystem = value;
    }

    void ActiveCanvas_DocumentChanged(GH_Canvas sender, GH_CanvasDocumentChangedEventArgs e)
    {
      if (e.OldDocument is object)
        e.OldDocument.SolutionEnd -= ActiveDefinition_SolutionEnd;

      if (e.NewDocument is object)
        e.NewDocument.SolutionEnd += ActiveDefinition_SolutionEnd;
    }

    void ActiveDefinition_SolutionEnd(object sender, GH_SolutionEventArgs e) => ModelUnitSystem = RhinoDoc.ActiveDoc.ModelUnitSystem;

    void IGuest.OnCheckOut()
    {
      RhinoDoc.EndOpenDocumentInitialViewUpdate -= EndOpenDocumentInitialViewUpdate;
      RhinoDoc.BeginOpenDocument                -= BeginOpenDocument;

      External.ActivationGate.Exit  -= ModalScope_Exit;
      External.ActivationGate.Enter -= ModalScope_Enter;

      Revit.ApplicationUI.Idling -= OnIdle;
      Revit.DocumentChanged -= OnDocumentChanged;

      // Unregister PreviewServer
      previewServer?.Unregister();
      previewServer = null;
    }

    public static void Show()
    {
      Script.ShowEditor();
      Rhinoceros.MainWindow.BringToFront();
    }

    public static async void ShowAsync()
    {
      await External.ActivationGate.Yield();

      Show();
    }

    /// <summary>
    /// Show Grasshopper window and open the given definition document
    /// </summary>
    /// <param name="filename">Full path to GH definition file</param>
    public static void ShowAndOpenDocument(string filename)
    {
      Script.ShowEditor();
      Script.OpenDocument(filename);
      Rhinoceros.MainWindow.BringToFront();
    }

    /// <summary>
    /// Show Grasshopper window asynchronously and open the given definition document
    /// </summary>
    /// <param name="filename">Full path to GH definition file</param>
    public static async void ShowAndOpenDocumentAsync(string filename)
    {
      // wait for the gate to open!
      await External.ActivationGate.Yield();
      // now show the window
      ShowAndOpenDocument(filename);
    }

    static bool LoadGHA(string filePath)
    {
      var LoadGHAProc = typeof(GH_ComponentServer).GetMethod("LoadGHA", BindingFlags.NonPublic | BindingFlags.Instance);
      if (LoadGHAProc == null)
      {
        var message = new StringBuilder();
        message.AppendLine("An attempt is made to invoke an invalid target method.");
        message.AppendLine();
        var assembly = typeof(GH_ComponentServer).Assembly;
        var assemblyName = assembly.GetName();

        message.AppendLine($"Assembly Version={assemblyName.Version}");
        message.AppendLine($"{assembly.Location.Replace(' ', (char) 0xA0)}");

        throw new TargetException(message.ToString());
      }

      try
      {
        return (bool) LoadGHAProc.Invoke
        (
          Instances.ComponentServer,
          new object[] { new GH_ExternalFile(filePath), false }
        );
      }
      catch(TargetInvocationException e)
      {
        throw e.InnerException;
      }
    }

    bool LoadComponents()
    {
      // Load This Assembly as a GHA in Grasshopper
      {
        var bCoff = Instances.Settings.GetValue("Assemblies:COFF", false);
        try
        {
          Instances.Settings.SetValue("Assemblies:COFF", false);

          var location = Assembly.GetExecutingAssembly().Location;
          location = Path.Combine(Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location) + ".GH.gha");
          if (!LoadGHA(location))
          {
            if (!File.Exists(location))
              throw new FileNotFoundException("File Not Found.", location);

            if(CentralSettings.IsLoadProtected(location))
              throw new InvalidOperationException($"Assembly '{location}' is load protected.");

            return false;
          }
        }
        finally
        {
          Instances.Settings.SetValue("Assemblies:COFF", bCoff);
        }
      }

      var assemblyFolders = new DirectoryInfo[]
      {
        // %ProgramData%\Grasshopper\Libraries-Inside-Revit-20XX
        new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Grasshopper", $"Libraries-{Rhinoceros.SchemeName}")),

        // %APPDATA%\Grasshopper\Libraries-Inside-Revit-20XX
        new DirectoryInfo(Folders.DefaultAssemblyFolder.Substring(0, Folders.DefaultAssemblyFolder.Length - 1) + '-' + Rhinoceros.SchemeName)
      };

      foreach (var folder in assemblyFolders)
      {
        IEnumerable<FileInfo> assemblyFiles;
        try { assemblyFiles = folder.EnumerateFiles("*.gha"); }
        catch (System.IO.DirectoryNotFoundException) { continue; }

        foreach (var assemblyFile in assemblyFiles)
        {
          bool loaded = false;
          string mainContent = string.Empty;
          string expandedContent = string.Empty;

          try
          {
            loaded = LoadGHA(assemblyFile.FullName);
          }
          catch (Exception e)
          {
            mainContent = e.Message;
            expandedContent = e.Source;
          }

          if (!loaded)
          {
            using
            (
              var taskDialog = new TaskDialog(MethodBase.GetCurrentMethod().DeclaringType.FullName)
              {
                Title = "Grasshopper Assembly Failure",
                MainIcon = External.UI.TaskDialogIcons.IconError,
                TitleAutoPrefix = false,
                AllowCancellation = false,
                MainInstruction = $"Grasshopper cannot load the external assembly {assemblyFile.Name}. Please contact the provider for assistance.",
                MainContent = mainContent,
                ExpandedContent = expandedContent,
                FooterText = assemblyFile.FullName
              }
            )
            {
              taskDialog.Show();
            }
          }
        }
      }

      GH_ComponentServer.UpdateRibbonUI();
      return true;
    }

    private void ModalScope_Enter(object sender, EventArgs e)
    {
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
        definition.Enabled = true;
    }

    private void ModalScope_Exit(object sender, EventArgs e)
    {
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
        definition.Enabled = false;
    }

    bool activeDefinitionWasEnabled = false;
    void BeginOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
      {
        activeDefinitionWasEnabled = definition.Enabled;
        definition.Enabled = false;
      }
    }

    void EndOpenDocumentInitialViewUpdate(object sender, DocumentOpenEventArgs e)
    {
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
      {
        definition.Enabled = activeDefinitionWasEnabled;
        definition.NewSolution(false);
      }
    }

    void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
    {
      var document = e.GetDocument();
      var added    = e.GetAddedElementIds();
      var deleted  = e.GetDeletedElementIds();
      var modified = e.GetModifiedElementIds();

      if (added.Count > 0 || deleted.Count > 0 || modified.Count > 0)
      {
        foreach (GH_Document definition in Instances.DocumentServer)
        {
          bool expireNow =
          (e.Operation == UndoOperation.TransactionCommitted || e.Operation == UndoOperation.TransactionUndone || e.Operation == UndoOperation.TransactionRedone) &&
          GH_Document.EnableSolutions &&
          Instances.ActiveCanvas.Document == definition &&
          definition.Enabled &&
          definition.SolutionState != GH_ProcessStep.Process;

          var change = new DocumentChangedEvent()
          {
            Operation = e.Operation,
            Document = document,
            Definition = definition
          };

          foreach (var obj in definition.Objects)
          {
            if (obj is Kernel.IGH_ElementIdParam persistentParam)
            {
              if (persistentParam.DataType == GH_ParamData.remote)
                continue;

              if (persistentParam.Phase == GH_SolutionPhase.Blank)
                continue;

              if (persistentParam.NeedsToBeExpired(document, added, deleted, modified))
              {
                if (expireNow)
                  persistentParam.ExpireSolution(false);
                else
                  change.ExpiredObjects.Add(persistentParam);
              }
            }
            else if (obj is Kernel.IGH_ElementIdComponent persistentComponent)
            {
              if (persistentComponent.NeedsToBeExpired(e))
              {
                if (expireNow)
                  persistentComponent.ExpireSolution(false);
                else
                  change.ExpiredObjects.Add(persistentComponent);
              }
            }
          }

          if (definition.SolutionState != GH_ProcessStep.Process)
          {
            changeQuque.Enqueue(change);
          }
          else if (definition == Instances.ActiveCanvas.Document)
          {
            if (change.ExpiredObjects.Count > 0)
            {
              foreach (var obj in change.ExpiredObjects)
              {
                obj.ClearData();
                obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"This object was expired because it contained obsolete Revit elements.");
              }

              Instances.DocumentEditor.SetStatusBarEvent
              (
                new GH_RuntimeMessage
                (
                  change.ExpiredObjects.Count == 1 ?
                  $"An object was expired because it contained obsolete Revit elements." :
                  $"{change.ExpiredObjects.Count} objects were expired because them contained obsolete Revit elements.",
                  GH_RuntimeMessageLevel.Remark,
                  "Document"
                )
              );
            }
          }
        }
      }
    }

    class DocumentChangedEvent
    {
      public UndoOperation Operation;
      public Document Document = null;
      public GH_Document Definition = null;
      public readonly List<IGH_ActiveObject> ExpiredObjects = new List<IGH_ActiveObject>();
      public void Apply()
      {
        foreach (var obj in ExpiredObjects)
          obj.ExpireSolution(false);

        if (Operation == UndoOperation.TransactionCommitted)
        {
          Definition.NewSolution(false);
        }
        else
        {
          // We create a transaction to avoid new changes while undoing or redoing
          using (var transaction = new Transaction(Document))
          {
            transaction.Start(Operation.ToString());
            Definition.NewSolution(false);
          }
        }
      }
    }

    Queue<DocumentChangedEvent> changeQuque = new Queue<DocumentChangedEvent>();

    void OnIdle(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
    {
      while (changeQuque.Count > 0)
        changeQuque.Dequeue().Apply();
    }
  }
}
