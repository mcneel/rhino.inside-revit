using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Plugin;
using Rhino;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH
{
  [GuestPlugInId("B45A29B1-4343-4035-989E-044E8580D9CF")]
  class Guest : IGuest
  {
    #region IGuest
    PreviewServer previewServer;

    public string Name => "Grasshopper";

    internal static Guest Instance;

    public Guest()
    {
      if (Instance is object)
        throw new InvalidOperationException();

      Instance = this;
    }

    GuestResult IGuest.EntryPoint(object sender, EventArgs args)
    {
      switch (args)
      {
        case CheckInArgs checkIn: return OnCheckIn(checkIn);
        case CheckOutArgs checkOut: return OnCheckOut(checkOut);
      }

      return default;
    }

    GuestResult OnCheckIn(CheckInArgs options)
    {
      try
      {
        if (!LoadStartupAssemblies())
          options.Message = "Failed to load Revit Grasshopper components.";
      }
      catch (FileNotFoundException e) { options.Message = $"{e.Message}{Environment.NewLine}{e.FileName}"; }
      catch (Exception e)             { options.Message = e.Message; }

      // Register PreviewServer
      previewServer = new PreviewServer();
      previewServer.Register();

      Revit.DocumentChanged += OnDocumentChanged;

      External.ActivationGate.Enter += ActivationGate_Enter;
      External.ActivationGate.Exit  += ActivationGate_Exit;

      RhinoDoc.BeginOpenDocument                += BeginOpenDocument;
      RhinoDoc.EndOpenDocumentInitialViewUpdate += EndOpenDocumentInitialViewUpdate;
      Rhino.Commands.Command.EndCommand         += RhinoCommand_EndCommand;

      Instances.CanvasCreatedEventHandler Canvas_Created = default;
      Instances.CanvasCreated += Canvas_Created = (canvas) =>
      {
        Instances.CanvasCreated            -= Canvas_Created;
        Instances.DocumentEditor.Activated += DocumentEditor_Activated;
        canvas.DocumentChanged             += ActiveCanvas_DocumentChanged;
        canvas.KeyDown                     += ActiveCanvas_KeyDown;
      };

      Instances.CanvasDestroyedEventHandler Canvas_Destroyed = default;
      Instances.CanvasDestroyed += Canvas_Destroyed = (canvas) =>
      {
        canvas.KeyDown                     -= ActiveCanvas_KeyDown;
        canvas.DocumentChanged             -= ActiveCanvas_DocumentChanged;
        Instances.DocumentEditor.Activated -= DocumentEditor_Activated;
        Instances.CanvasDestroyed          -= Canvas_Destroyed;
      };

      Instances.DocumentServer.DocumentAdded += DocumentServer_DocumentAdded;
      Instances.DocumentServer.DocumentRemoved += DocumentServer_DocumentRemoved;

      return GuestResult.Succeeded;
    }

    GuestResult OnCheckOut(CheckOutArgs options)
    {
      Instances.DocumentServer.DocumentAdded -= DocumentServer_DocumentAdded;

      Rhino.Commands.Command.EndCommand         -= RhinoCommand_EndCommand;
      RhinoDoc.EndOpenDocumentInitialViewUpdate -= EndOpenDocumentInitialViewUpdate;
      RhinoDoc.BeginOpenDocument                -= BeginOpenDocument;

      External.ActivationGate.Exit  -= ActivationGate_Exit;
      External.ActivationGate.Enter -= ActivationGate_Enter;

      Revit.DocumentChanged -= OnDocumentChanged;

      // Unregister PreviewServer
      previewServer?.Unregister();
      previewServer = null;

      return GuestResult.Succeeded;
    }
    #endregion

    #region Grasshopper Editor
    static readonly GH_RhinoScriptInterface Script = new GH_RhinoScriptInterface();

    /// <summary>
    /// Returns the loaded state of the Grasshopper Main window.
    /// </summary>
    /// <returns>True if the Main Grasshopper Window has been loaded.</returns>
    public static bool IsEditorLoaded() => Script.IsEditorLoaded();

    /// <summary>
    /// Load the main Grasshopper Editor. If the editor has already been loaded nothing
    /// will happen.
    /// </summary>
    public static void LoadEditor()
    {
      Script.LoadEditor();
      if (!Script.IsEditorLoaded())
        throw new InvalidOperationException("Failed to startup Grasshopper");
    }

    /// <summary>
    /// Returns the visible state of the Grasshopper Main window.
    /// </summary>
    /// <returns>True if the Main Grasshopper Window has been loaded and is visible.</returns>
    public static bool IsEditorVisible() => Script.IsEditorVisible();

    /// <summary>
    /// Show the main Grasshopper Editor. The editor will be loaded first if needed.
    /// If the Editor is already on screen, it will be activated.
    /// </summary>
    public static void ShowEditor()
    {
      Script.ShowEditor();
      Instances.DocumentEditor?.Activate();
    }

    /// <summary>
    /// Show Grasshopper window asynchronously
    /// </summary>
    public static async void ShowEditorAsync()
    {
      // Yield execution back to Revit and show Grasshopper window asynchronously.
      await External.ActivationGate.Yield();

      ShowEditor();
    }

    /// <summary>
    /// Hide the main Grasshopper Editor. If the editor hasn't been loaded or if the
    /// Editor is already hidden, nothing will happen.
    /// </summary>
    public static void HideEditor() => Script.HideEditor();

    /// <summary>
    /// Open a Grasshopper document. The editor will be loaded if necessary, but it will not be automatically shown.
    /// </summary>
    /// <param name="filename">Path of file to open (must be a *.gh or *.ghx extension).</param>
    /// <returns>True on success, false on failure.</returns>
    public static bool OpenDocument(string filename) => Script.OpenDocument(filename);

    /// <summary>
    /// Open a Grasshopper document. The editor will be loaded and shown if necessary.
    /// </summary>
    /// <param name="filename">Full path to GH definition file</param>
    /// <param name="showEditor">True to force the Main Grasshopper Window visible.</param>
    public static async void OpenDocumentAsync(string filename, bool showEditor = true)
    {
      // Yield execution back to Revit and show Grasshopper window asynchronously.
      await External.ActivationGate.Yield();

      if (showEditor)
        ShowEditor();

      OpenDocument(filename);
    }

    void ActivationGate_Enter(object sender, EventArgs e)
    {
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
        definition.Enabled = true;

      if (EnableSolutions.HasValue)
      {
        GH_Document.EnableSolutions = EnableSolutions.Value;
        EnableSolutions = null;
      }
    }

    void ActivationGate_Exit(object sender, EventArgs e)
    {
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
        definition.Enabled = false;
    }

    static bool? EnableSolutions;
    private void DocumentServer_DocumentAdded(GH_DocumentServer sender, GH_Document doc)
    {
      doc.ObjectsDeleted += Doc_ObjectsDeleted;
      if (!External.ActivationGate.IsOpen)
      {
        if (GH_Document.EnableSolutions)
        {
          GH_Document.EnableSolutions = false;
          EnableSolutions = true;
        }
      }
    }

    private void DocumentServer_DocumentRemoved(GH_DocumentServer sender, GH_Document doc)
    {
      doc.ObjectsDeleted -= Doc_ObjectsDeleted;
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

    private void RhinoCommand_EndCommand(object sender, Rhino.Commands.CommandEventArgs args)
    {
      if (args.CommandEnglishName == "GrasshopperBake")
      {
        if (!Rhinoceros.Exposed && !RhinoDoc.ActiveDoc.Views.Where(x => x.Floating).Any())
        {
          var cursorPosition = System.Windows.Forms.Cursor.Position;
          if (!Rhinoceros.OpenRevitViewport(cursorPosition.X - 400, cursorPosition.Y - 300))
            Rhinoceros.Exposed = true;
        }
      }
    }
    #endregion

    #region Grasshopper Assemblies
    static bool LoadGHA(string filePath)
    {
      var LoadGHAProc = typeof(GH_ComponentServer).GetMethod("LoadGHA", BindingFlags.NonPublic | BindingFlags.Instance);
      if (LoadGHAProc is null)
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
      catch (TargetInvocationException e)
      {
        throw e.InnerException;
      }
    }

    static bool EnumGHLink(string filePath, List<string> files)
    {
      try
      {
        foreach (var fullLine in File.ReadAllLines(filePath))
        {
          var line = fullLine.Trim();
          if (string.IsNullOrEmpty(line)) continue;
          if (line.StartsWith("#")) continue;
          if (line.StartsWith("//")) continue;

          if (File.Exists(line)) files.Add(line);
          else if (Directory.Exists(line))
          {
            var folder = new DirectoryInfo(line);

            IEnumerable<FileInfo> assemblyFiles;
            try { assemblyFiles = folder.EnumerateFiles("*.gha"); }
            catch (System.IO.DirectoryNotFoundException) { continue; }

            foreach (var assemblyFile in assemblyFiles)
            {
              // https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles?view=netframework-4.8
              // If the specified extension is exactly three characters long,
              // the method returns files with extensions that begin with the specified extension.
              // For example, "*.xls" returns both "book.xls" and "book.xlsx"
              if (assemblyFile.Extension.ToLower() != ".gha") continue;

              files.Add(assemblyFile.FullName);
            }
          }
        }
      }
      catch { return false; }

      return true;
    }

    static List<string> GetAssembliesList()
    {
      DirectoryInfo[] DefaultAssemblyFolders =
      {
        // %ProgramData%\Grasshopper\Libraries-Inside-Revit-20XX
        new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Grasshopper", $"Libraries-{Rhinoceros.SchemeName}")),

        // %APPDATA%\Grasshopper\Libraries-Inside-Revit-20XX
        new DirectoryInfo(Folders.DefaultAssemblyFolder.Substring(0, Folders.DefaultAssemblyFolder.Length - 1) + '-' + Rhinoceros.SchemeName)
      };

      var map = new System.Collections.Specialized.OrderedDictionary();

      foreach (var folder in DefaultAssemblyFolders)
      {
        if (!folder.Exists) continue;

        IEnumerable<FileInfo> assemblyFiles;
        try { assemblyFiles = folder.EnumerateFiles("*.gha", SearchOption.AllDirectories); }
        catch (System.Security.SecurityException) { continue; }

        foreach (var assemblyFile in assemblyFiles)
        {
          // https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles?view=netframework-4.8
          // If the specified extension is exactly three characters long,
          // the method returns files with extensions that begin with the specified extension.
          // For example, "*.xls" returns both "book.xls" and "book.xlsx"
          if (assemblyFile.Extension.ToLower() != ".gha") continue;

          var key = assemblyFile.FullName.Substring(folder.FullName.Length);
          if (map.Contains(key))
            map.Remove(key);

          map.Add(key, assemblyFile);
        }

        IEnumerable<FileInfo> linkFiles;
        try { linkFiles = folder.EnumerateFiles("*.ghlink", SearchOption.TopDirectoryOnly); }
        catch (System.Security.SecurityException) { continue; }

        foreach (var linkFile in linkFiles)
        {
          var key = linkFile.FullName.Substring(folder.FullName.Length);
          if (map.Contains(key))
            map.Remove(key);

          map.Add(key, linkFile);
        }
      }

      var assembliesList = new List<string>();
      foreach (var entry in map.Values.Cast<FileInfo>())
      {
        var extension = entry.Extension.ToLower();
        if (extension == ".gha")         assembliesList.Add(entry.FullName);
        else if (extension == ".ghlink") EnumGHLink(entry.FullName, assembliesList);
      }

      return assembliesList;
    }

    bool LoadStartupAssemblies()
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

            if (CentralSettings.IsLoadProtected(location))
              throw new InvalidOperationException($"Assembly '{location}' is load protected.");

            return false;
          }
        }
        finally
        {
          Instances.Settings.SetValue("Assemblies:COFF", bCoff);
        }
      }

      var assemblyList = GetAssembliesList();
      foreach (var assemblyFile in assemblyList)
      {
        bool loaded = false;
        string mainContent = string.Empty;
        string expandedContent = string.Empty;

        try
        {
          loaded = LoadGHA(assemblyFile);
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
              MainInstruction = $"Grasshopper cannot load the external assembly {Path.GetFileName(assemblyFile)}. Please contact the provider for assistance.",
              MainContent = mainContent,
              ExpandedContent = expandedContent,
              FooterText = assemblyFile
            }
          )
          {
            taskDialog.Show();
          }
        }
      }

      GH_ComponentServer.UpdateRibbonUI();
      return true;
    }
    #endregion

    #region Revit Document
    static UnitSystem revitUnitSystem = UnitSystem.Unset;
    static UnitSystem modelUnitSystem = UnitSystem.Unset;
    public static UnitSystem ModelUnitSystem
    {
      get => Instances.ActiveCanvas is null ? UnitSystem.Unset : modelUnitSystem;
      private set => modelUnitSystem = value;
    }

    void DocumentEditor_Activated(object sender, EventArgs e)
    {
      var revitUS = UnitSystem.Unset;

      if (Revit.ActiveUIDocument?.Document is DB.Document revitDoc)
      {
        var units = revitDoc.GetUnits();
        revitUS = units.ToUnitSystem(out var _);
      }

      if (RhinoDoc.ActiveDoc is RhinoDoc doc)
      {
        var hasUnits = doc.ModelUnitSystem != UnitSystem.Unset && doc.ModelUnitSystem != UnitSystem.None;
        if (revitUnitSystem != revitUS || !hasUnits)
        {
          revitUnitSystem = revitUS;
          Rhinoceros.AuditUnits(doc);
        }
      }
    }

    void ActiveCanvas_DocumentChanged(GH_Canvas sender, GH_CanvasDocumentChangedEventArgs e)
    {
      if (e.OldDocument is object)
      {
        e.OldDocument.SolutionEnd -= ActiveDefinition_SolutionEnd;
        e.OldDocument.SolutionStart -= ActiveDefinition_SolutionStart;
      }

      if (e.NewDocument is object)
      {
        e.NewDocument.SolutionStart += ActiveDefinition_SolutionStart;
        e.NewDocument.SolutionEnd += ActiveDefinition_SolutionEnd;
      }
    }
    #endregion

    #region DocumentChanged

    struct ReadOnlySortedCollection : ICollection<DB.ElementId>
    {
      readonly ICollection<DB.ElementId> collection;
      public ReadOnlySortedCollection(ICollection<DB.ElementId> source) => collection = source;

      public int Count => collection.Count;
      public bool IsReadOnly => true;

      public bool Contains(DB.ElementId item)
      {
        if (collection is List<DB.ElementId> list)
          return list.BinarySearch(item, ElementIdComparer.NoNullsAscending) >= 0;
        else
          return collection.Contains(item);
      }

      public void CopyTo(DB.ElementId[] array, int arrayIndex) => collection.CopyTo(array, arrayIndex);

      public void Add(DB.ElementId item) => throw new InvalidOperationException("Collection is read-only");
      public bool Remove(DB.ElementId item) => throw new InvalidOperationException("Collection is read-only");
      public void Clear() => throw new InvalidOperationException("Collection is read-only");

      public IEnumerator<DB.ElementId> GetEnumerator() => collection.GetEnumerator();
      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => collection.GetEnumerator();
    }

    void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
    {
#if DEBUG
      var transactions = e.GetTransactionNames();
#endif
      var document = e.GetDocument();
      var added    = new ReadOnlySortedCollection(e.GetAddedElementIds());
      var deleted  = new ReadOnlySortedCollection(e.GetDeletedElementIds());
      var modified = new ReadOnlySortedCollection(e.GetModifiedElementIds());

      if (added.Count > 0 || deleted.Count > 0 || modified.Count > 0)
      {
        foreach (GH_Document definition in Instances.DocumentServer)
        {
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
              if (persistentParam.Locked)
                continue;

              if (persistentParam.NeedsToBeExpired(document, added, deleted, modified))
                change.ExpiredObjects.Add(persistentParam);
            }
            else if (obj is Kernel.IGH_ElementIdComponent persistentComponent)
            {
              if (persistentComponent.Locked)
                continue;

              if (persistentComponent.NeedsToBeExpired(document, added, deleted, modified))
                change.ExpiredObjects.Add(persistentComponent);
            }
          }

          if (change.ExpiredObjects.Count > 0)
            DocumentChangedEvent.Enqueue(change);
        }
      }
    }

    internal class DocumentChangedEvent
    {
      public static bool EnableSolutions { get; set; } = true;

      static readonly ExternalEvent FlushQueue = ExternalEvent.Create(new FlushQueueHandler());
      class FlushQueueHandler : External.UI.ExternalEventHandler
      {
        public override string GetName() => nameof(FlushQueue);
        protected override void Execute(UIApplication app)
        {
          var solutions = new List<GH_Document>();
          while (changeQueue.Count > 0)
          {
            if (changeQueue.Dequeue().NewSolution() is GH_Document solution)
            {
              if (!solutions.Contains(solution))
                solutions.Add(solution);
            }
          }

          if (solutions.Count == 0)
            Instances.ActiveCanvas?.Refresh();
          else foreach (var solution in solutions)
              solution.NewSolution(false);
        }
      }

      public static readonly Queue<DocumentChangedEvent> changeQueue = new Queue<DocumentChangedEvent>();
      public static void Enqueue(DocumentChangedEvent value)
      {
        if (!EnableSolutions)
          return;

        if (value.Definition.SolutionState != GH_ProcessStep.Process)
        {
          // Change NOT made by Grasshopper.
          changeQueue.Enqueue(value);

          FlushQueue.Raise();
        }
      }

      public UndoOperation Operation;
      public DB.Document Document;
      public GH_Document Definition;
      public readonly List<IGH_ActiveObject> ExpiredObjects = new List<IGH_ActiveObject>();

      GH_Document NewSolution()
      {
        Debug.Assert(ExpiredObjects.Count > 0, "An empty change is been enqueued.");

        foreach (var obj in ExpiredObjects)
          obj.ExpireSolution(false);

        return Operation == UndoOperation.TransactionCommitted ? Definition : default;
      }
    }
    #endregion

    #region Transaction Groups
    readonly Queue<DB.TransactionGroup> ActiveTransactionGroups = new Queue<DB.TransactionGroup>();
    readonly Stack<GH_Document> ActiveDocumentStack = new Stack<GH_Document>();

    internal void StartTransactionGroups()
    {
      var now = DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentUICulture);
      var name = ActiveDocumentStack.Peek().DisplayName;

      StartTransactionGroups($"Grasshopper {now}: {name.TripleDot(16)}", true);
    }

    internal void StartTransactionGroups(string name, bool forcedModal)
    {
      using (var documents = Revit.ActiveDBApplication.Documents)
      {
        foreach (var doc in documents.Cast<DB.Document>())
        {
          // Linked document do not allow transactions.
          if (doc.IsLinked)
            continue;

          // Document can not be modified during transaction recovering.
          if (doc.IsReadOnly)
            continue;

          // This document has already a transaction in course.
          if (doc.IsModifiable)
            continue;

          var group = new DB.TransactionGroup(doc, name)
          {
            IsFailureHandlingForcedModal = forcedModal
          };
          group.Start();

          ActiveTransactionGroups.Enqueue(group);
        }
      }
    }

    internal void CommitTransactionGroups()
    {
      while (ActiveTransactionGroups.Count > 0)
      {
        try
        {
          using (var group = ActiveTransactionGroups.Dequeue())
          {
            if (group.IsValidObject)
              group.Assimilate();
          }
        }
        catch { }
      }
    }
    #endregion

    #region Element Tracking
    /// <summary>
    /// Adds Shift+Del sortcut to Delete command
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ActiveCanvas_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
      if (sender is GH_Canvas canvas && canvas.Document is GH_Document document)
      {
        if (e.KeyData == (System.Windows.Forms.Keys.Delete | System.Windows.Forms.Keys.Shift))
        {
          e.SuppressKeyPress = true;

          var selectedObjects = document.SelectedObjects();
          if (selectedObjects.Count > 0)
          {
            document.UndoUtil.RecordRemoveObjectEvent("Delete", selectedObjects);
            document.RemoveObjects(selectedObjects, true);
          }
        }
      }
    }

    private async void Doc_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
    {
      if (e.Document.Context != GH_DocumentContext.Loaded)
        return;

      var canvas = Instances.ActiveCanvas;
      var canvasModifiersEnabled = canvas?.ModifiersEnabled;

      var grasshopperDocument = e.Document;
      var grasshopperDocumentEnabled = grasshopperDocument?.Enabled;

      var activeUIDocument = Revit.ActiveUIDocument;
      if (activeUIDocument is null) return;

      var activeDocument = activeUIDocument.Document;
      var activeView = activeUIDocument.ActiveGraphicalView;

      var authorities = ElementTracking.TrackedElementsDictionary.NewAuthorityCollection();
      {
        foreach (var documentObject in e.Objects)
        {
          if (documentObject is ElementTracking.IGH_TrackingComponent trackingComponent)
          {
            if (trackingComponent.TrackingMode <= ElementTracking.TrackingMode.Disabled)
              continue;

            if (documentObject is IGH_Component component)
            {
              foreach (var param in component.Params.Input)
              {
                if (param is ElementTracking.IGH_TrackingParam)
                {
                  if (ElementTracking.ElementStreamId.TryGetAuthority(grasshopperDocument, param, out var inputAuthority))
                    authorities.Add(inputAuthority);
                }
              }

              foreach (var param in component.Params.Output)
              {
                if (param is ElementTracking.IGH_TrackingParam)
                {
                  if (ElementTracking.ElementStreamId.TryGetAuthority(grasshopperDocument, param, out var outputAuthority))
                    authorities.Add(outputAuthority);
                }
              }
            }

            if (ElementTracking.ElementStreamId.TryGetAuthority(grasshopperDocument, documentObject, out var authority))
              authorities.Add(authority);
          }
        }
      }

      bool rolledback = false;
      var allowModelessHandling = System.Windows.Forms.Control.ModifierKeys != System.Windows.Forms.Keys.Shift;
      var transactionGroups = new Queue<(DB.Document Document, DB.TransactionGroup Group)>();
      try
      {
        if (canvasModifiersEnabled.HasValue) canvas.ModifiersEnabled = false;
        if (grasshopperDocumentEnabled.HasValue) grasshopperDocument.Enabled = false;

        var revitDocuments = Revit.ActiveDBApplication.Documents.Cast<DB.Document>();
        foreach (var revitDocument in revitDocuments)
        {
          if (rolledback) break;
          if (revitDocument.IsLinked) continue;

          var elements = ElementTracking.TrackedElementsDictionary.Keys(revitDocument, authorities);
          if (elements.Count == 0) continue;

          if (revitDocument.IsReadOnly || revitDocument.IsModifiable)
          {
            rolledback = true;
            break;
          }

          {
            var transactionGroup = new DB.TransactionGroup(revitDocument, "Release Elements")
            {
              IsFailureHandlingForcedModal = false
            };

            if (transactionGroup.Start() == DB.TransactionStatus.Started)
              transactionGroups.Enqueue((revitDocument, transactionGroup));
          }

          var elementIds = new HashSet<DB.ElementId>(elements.Select(x => x.Id));
          var deletedIds = default(ICollection<DB.ElementId>);
          var modifiedIds = default(ICollection<DB.ElementId>);

          if (allowModelessHandling)
          {
            try { deletedIds = revitDocument.GetDependentElements(elementIds, out modifiedIds, default); }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { deletedIds = elementIds; modifiedIds = new DB.ElementId[0]; }
          }

          using (var tx = new DB.Transaction(revitDocument, "Release Elements"))
          {
            if (tx.Start() == DB.TransactionStatus.Started)
            {
              // Untrack elements on revitDocument owned by deleted callSites
              foreach (var element in elements)
              {
                if (ElementTracking.TrackedElementsDictionary.Remove(element))
                  element.Pinned = false;
              }

              if (allowModelessHandling)
              {
                using (var message = new DB.FailureMessage(ExternalFailures.ElementFailures.TrackedElementReleased))
                {
                  var resolution = DB.DeleteElements.Create(revitDocument, elementIds);
                  message.AddResolution(DB.FailureResolutionType.DeleteElements, resolution);

                  message.SetFailingElements(elementIds);

                  elementIds.SymmetricExceptWith(deletedIds.Concat(modifiedIds));
                  message.SetAdditionalElements(elementIds);

                  revitDocument.PostFailure(message);
                }
              }
              else
              {
                // If Shift is pressed we delete elements here, hopefully without UI
                revitDocument.Delete(elementIds);
              }

              rolledback |= await tx.CommitAsync() != DB.TransactionStatus.Committed;
            }
            else rolledback = true;
          }
        }
      }
      catch { rolledback = true; }
      finally
      {
        while (transactionGroups.Count > 0)
        {
          var (document, group) = transactionGroups.Dequeue();
          using (group)
          {
            if (!group.IsValidObject) continue;
            if (!document.IsValidObject) continue;
            if (rolledback) group.RollBack();
            else group.Assimilate();
          }
        }

        if (canvasModifiersEnabled.HasValue) canvas.ModifiersEnabled = canvasModifiersEnabled.Value;
        if (grasshopperDocumentEnabled.HasValue) grasshopperDocument.Enabled = grasshopperDocumentEnabled.Value;

        ShowEditor();

        if (!activeDocument.IsEquivalent(Revit.ActiveUIDocument.Document))
          e.Document.ExpireSolution();

        if (rolledback)
          e.Document.Undo();
      }
    }

    void ActiveDefinition_SolutionStart(object sender, GH_SolutionEventArgs e)
    {
      // Expire objects that contain elements modified by Grasshopper.
      while (DocumentChangedEvent.changeQueue.Count > 0)
      {
        var change = DocumentChangedEvent.changeQueue.Dequeue();

        foreach (var obj in change.ExpiredObjects)
          obj.ExpireSolution(false);
      }

      ActiveDocumentStack.Push(e.Document);
      StartTransactionGroups();
    }

    void ActiveDefinition_SolutionEnd(object sender, GH_SolutionEventArgs e)
    {
      if (ActiveDocumentStack.Peek() != e.Document)
        throw new InvalidOperationException();

      CommitTransactionGroups();
      ActiveDocumentStack.Pop();

      // Warn the user about objects that contain elements modified by Grasshopper.
      {
        var expiredObjectsCount = 0;
        foreach (var change in DocumentChangedEvent.changeQueue)
        {
          if (e.Document == change.Definition)
            expiredObjectsCount += change.ExpiredObjects.Count;

          foreach (var obj in change.ExpiredObjects)
            obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "This object will be expired because it contains obsolete Revit elements.");
        }
        if (expiredObjectsCount > 0)
        {
          Instances.DocumentEditor.SetStatusBarEvent
          (
            new GH_RuntimeMessage
            (
              expiredObjectsCount == 1 ?
              $"An object will be expired because contains obsolete Revit elements." :
              $"{expiredObjectsCount} objects will be expired because contain obsolete Revit elements.",
              GH_RuntimeMessageLevel.Remark,
              "Rhino.Inside"
            )
          );
        }
      }

      ModelUnitSystem = RhinoDoc.ActiveDoc.ModelUnitSystem;
    }
    #endregion
  }
}
