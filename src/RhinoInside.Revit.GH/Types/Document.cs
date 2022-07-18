using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;
  using External.UI.Extensions;

  [Kernel.Attributes.Name("Document")]
  public interface IGH_Document : IGH_Goo, IEquatable<IGH_Document>
  {
    Guid DocumentGUID { get; }

    Uri ModelURI { get; }
    string PathName { get; }

    Uri CentralModelURI { get; }
    string CentralPathName { get; }

    ARDB.Document Value { get; }
  }

  [Kernel.Attributes.Name("Document"), AssemblyPriority]
  public class Document : IGH_Document, IGH_ReferenceData, ICloneable
  {
    #region System.Object
    public bool Equals(IGH_Document other)
    {
      if (ReferenceEquals(this, other)) return true;
      if (other is null) return false;

      if (DocumentGUID != other.DocumentGUID) return false;
      if (ModelURI != other.ModelURI) return false;

      return true;
    }

    public override bool Equals(object obj) => (obj is Document doc) && Equals(doc);
    public override int GetHashCode() => DocumentGUID.GetHashCode();

    public sealed override string ToString()
    {
      var typeName = $"Revit {((IGH_Goo) this).TypeName}";

      if (!IsReferencedData)
        return $"{typeName} : {DisplayName}";

      var tip = IsValid ?
      (
        IsReferencedDataLoaded ?
        $"{typeName} : {DisplayName}" :
        $"Unresolved {typeName} : {DisplayName}"
      ) :
      (
        ModelURI is null ? $"⚠ Invalid {typeName}" :
        $"❌ Not loaded {typeName} : {DisplayName}"
      );

      return tip;
    }
    object ICloneable.Clone() => MemberwiseClone();
    #endregion

    #region IGH_Goo
    string IGH_Goo.TypeName
    {
      get
      {
        var type = GetType();
        var name = type.GetTypeInfo().GetCustomAttribute(typeof(Kernel.Attributes.NameAttribute)) as Kernel.Attributes.NameAttribute;
        return name?.Name ?? type.Name;
      }
    }
    string IGH_Goo.TypeDescription => $"Represents a Revit {((IGH_Goo) this).TypeName.ToLowerInvariant()}";
    public virtual bool IsValid => document.IsValid();
    public virtual bool IsEmpty => DocumentGUID == Guid.Empty;
    public virtual string IsValidWhyNot => document.IsValidWithLog(out var log) ? default : log;
    IGH_Goo IGH_Goo.Duplicate() => (IGH_Goo) MemberwiseClone();
    object IGH_Goo.ScriptVariable() => Value;
    public IGH_GooProxy EmitProxy() => new Proxy(this);

    bool GH_IO.GH_ISerializable.Read(GH_IReader reader)
    {
      Guid documentGUID = default;
      DocumentGUID = reader.TryGetGuid("DocumentGUID", ref documentGUID) ? documentGUID : default;

      string modelUri = default;
      ModelURI = reader.TryGetString("ModelURI", ref modelUri) ? new Uri(modelUri) : default;

      string pathName = default;
      PathName = reader.TryGetString("PathName", ref pathName) ? pathName : default;

      string centralModelUri = default;
      CentralModelURI = reader.TryGetString("CentralModelURI", ref centralModelUri) ? new Uri(centralModelUri) : default;

      string centralPathName = default;
      CentralPathName = reader.TryGetString("CentralPathName", ref centralPathName) ? centralPathName : default;

      return true;
    }

    bool GH_IO.GH_ISerializable.Write(GH_IWriter writer)
    {
      if (DocumentGUID != default)
        writer.SetGuid("DocumentGUID", DocumentGUID);

      if (ModelURI != default)
        writer.SetString("ModelURI", ModelURI.ToString());

      if (PathName != default)
        writer.SetString("PathName", PathName);

      if (CentralModelURI != default)
        writer.SetString("CentralModelURI", CentralModelURI.ToString());

      if (CentralPathName != default)
        writer.SetString("CentralPathName", CentralPathName);

      return true;
    }
    #endregion

    #region IGH_ReferenceData
    public bool IsReferencedData => DocumentGUID != default || ModelURI != default;

    public bool IsReferencedDataLoaded => Value.IsValid();

    public bool LoadReferencedData()
    {
      RefreshReferenceData();

      if (TryGetDocument(DocumentGUID, out document))
        return true;

      return false;
    }

    public void UnloadReferencedData()
    {
      // Reference Data may be changed since document may be
      // saved with an other file name or beeing workshared.
      RefreshReferenceData();
      Value = default;
    }

    void RefreshReferenceData()
    {
      if (document?.IsValidObject == true)
      {
        using (var modelPath = document.GetLocalModelPath())
        {
          if (modelPath?.IsFilePath() == true)
          {
            var modelUri = modelPath.ToUri();
            if (modelUri != ModelURI)
            {
              ModelURI = modelUri;
              PathName = modelPath.ToUserVisiblePath();
            }
          }
        }

        if (document.IsWorkshared)
        {
          using (var centralPath = document.GetWorksharingCentralModelPath())
          {
            var centralUri = centralPath.ToUri();
            if (centralUri != CentralModelURI)
            {
              CentralModelURI = centralUri;
              CentralPathName = centralPath.ToUserVisiblePath();
            }
          }
        }
        else
        {
          CentralModelURI = default;
          CentralPathName = default;
        }
      }
      else
      {
        if (ModelURI is object)
        {
          try
          {
            using (var modelPath = ModelURI.ToModelPath())
              PathName = modelPath.ToUserVisiblePath();
          }
          catch (Autodesk.Revit.Exceptions.ApplicationException) { }

          if (ModelURI.IsFileUri(out var localPath))
          {
            if (File.Exists(localPath))
            {
              try
              {
                using (var info = ARDB.BasicFileInfo.Extract(localPath))
                {
                  if (info.IsWorkshared)
                  {
                    CentralPathName = info.CentralPath;
                    if (Uri.TryCreate(info.CentralPath, UriKind.Absolute, out var centralModelURI))
                      CentralModelURI = centralModelURI;
                  }
                  else
                  {
                    CentralPathName = default;
                    CentralModelURI = default;
                  }
                }
              }
              catch (Autodesk.Revit.Exceptions.ApplicationException) { }
            }

            return;
          }
          else
          {
            PathName = default;
            ModelURI = default; 
          }
        }

        if (CentralModelURI is object)
        {
          using (var modelPath = CentralModelURI.ToModelPath())
            CentralPathName = modelPath.ToUserVisiblePath();
        }
      }
    }

    public ARDB.BasicFileInfo FileInfo
    {
      get 
      {
        if (FilePath is string filePath && File.Exists(filePath))
          return ARDB.BasicFileInfo.Extract(filePath);

        return default;
      }
    }
    #endregion

    #region Properties
    ARDB.Document document;
    public ARDB.Document Value
    {
      get
      {
        if (document?.IsValidObject == false)
          document = default;

        return document;
      }
      private set => document = value;
    }

    public Guid DocumentGUID { get; protected set; } = default;

    public Uri ModelURI { get; protected set; } = default;
    public string PathName { get; protected set; } = default;

    public Uri CentralModelURI { get; protected set; } = default;
    public string CentralPathName { get; protected set; } = default;
    #endregion

    public Document() { }
    protected Document(ARDB.Document value)
    {
      if (value is null) return;
      document = value;
      DocumentGUID = value.GetFingerprintGUID();
      RefreshReferenceData();
    }

    static Document()
    {
      if (Core.Host.Value is Autodesk.Revit.UI.UIApplication host)
      {
        foreach (var document in host.Application.Documents.Cast<ARDB.Document>())
          AddDocument(document);

        // Create a Grasshopper document
        // Create a Revit document
        //Crash!!

        host.Application.DocumentCreated += Host_DocumentCreated;
        host.Application.DocumentOpened += Host_DocumentOpened;
        host.Application.DocumentClosed += Host_DocumentClosed;
        host.Application.DocumentClosing += Host_DocumentClosing;
      }
    }

    private static void Host_DocumentCreated(object sender, ARDB.Events.DocumentCreatedEventArgs e)
    {
      AddDocument(e.Document);
    }
    private static void Host_DocumentOpened(object sender, ARDB.Events.DocumentOpenedEventArgs e)
    {
      AddDocument(e.Document);
    }

    static void Host_DocumentClosing(object sender, ARDB.Events.DocumentClosingEventArgs e)
    {
      ClosingDocuments.Add(e.DocumentId, e.Document);
      RemoveDocument(e.Document);
    }
    static void Host_DocumentClosed(object sender, ARDB.Events.DocumentClosedEventArgs e)
    {
      if (!ClosingDocuments.TryGetValue(e.DocumentId, out var document)) return;
      ClosingDocuments.Remove(e.DocumentId);

      if (e.Status != ARDB.Events.RevitAPIEventStatus.Succeeded)
        AddDocument(document);
    }

    static void AddDocument(ARDB.Document document)
    {
      if (document is null) return;

      if (DocumentsRegistry.TryGetValue(document.GetFingerprintGUID(), out var twins))
        twins.Add(document);
      else
        DocumentsRegistry.Add(document.GetFingerprintGUID(), new List<ARDB.Document>() { document });
    }

    static void RemoveDocument(ARDB.Document document)
    {
      if (document is null) return;

      if (DocumentsRegistry.TryGetValue(document.GetFingerprintGUID(), out var twins))
      {
        {
          var index = twins.IndexOf(document);
          if (index >= 0)
            twins.RemoveAt(index);
        }

        if (twins.Count == 0)
          DocumentsRegistry.Remove(document.GetFingerprintGUID());
      }
    }

    static readonly Dictionary<int, ARDB.Document> ClosingDocuments = new Dictionary<int, ARDB.Document>();
    static readonly Dictionary<Guid, List<ARDB.Document>> DocumentsRegistry = new Dictionary<Guid, List<ARDB.Document>>();

    internal static bool TryGetDocument(Guid guid, out ARDB.Document document)
    {
      // Only return a document when the query is not ambiguous.
      // An ambiguous situation happens when two documents with the same GUID are loaded at the same time.
      if (DocumentsRegistry.TryGetValue(guid, out var twins) && twins.Count == 1)
      {
        document = twins[0];
        return true;
      }

      // Look for Linked documents here.
      if (Revit.ActiveUIApplication.TryGetDocument(guid, out document))
        return true;

      document = default;
      return false;
    }

    public static Document FromValue(object value)
    {
      if (value is IGH_Goo goo)
        value = goo.ScriptVariable();

      switch (value)
      {
        case ARDB.Document document: return FromValue(document);
        case ARDB.Element element: return FromValue(element.Document);
        case string str:
          using (var documents = Revit.ActiveDBApplication.Documents)
          {
            var docs = documents.Cast<ARDB.Document>();

            if (str.Contains(Path.DirectorySeparatorChar) || str.Contains(Path.AltDirectorySeparatorChar))
            {
              // Find a matching PathName
              var match_path = docs.Where(x => x.PathName.Equals(str, StringComparison.OrdinalIgnoreCase)).ToArray();
              if (match_path.Length == 1)
                return FromValue(match_path[0]);
            }
            else if (!string.IsNullOrEmpty(System.IO.Path.GetExtension(str)))
            {
              // Find a matching Title
              var match_title = docs.Where(x => x.GetTitle().Equals(str, StringComparison.OrdinalIgnoreCase)).ToArray();
              if (match_title.Length == 1)
                return FromValue(match_title[0]);
            }
            else
            {
              // Find a matching Name
              var match_name = docs.Where(x => x.GetName().Equals(str, StringComparison.OrdinalIgnoreCase)).ToArray();
              if (match_name.Length == 1)
                return FromValue(match_name[0]);
            }
          }
          break;
      }

      return default;
    }

    public static Document FromValue(ARDB.Document document)
    {
      if (document?.IsValidObject != true)
        return null;

      var value = document.IsFamilyDocument ?
        (Document) new FamilyDocument(document) :
        (Document) new ProjectDocument(document);

      return value;
    }

    internal static Document FromValue(DocumentState state)
    {
      var doc = default(Document);
      if (!string.IsNullOrEmpty(state.PathName))
      {
        switch (Path.GetExtension(state.PathName).ToLowerInvariant())
        {
          case ".rte":
          case ".rvt": doc = new ProjectDocument(); break;
          case ".rfa": doc = new FamilyDocument(); break;
        }
      }

      if (doc is object)
      {
        doc.DocumentGUID = state.DocumentGUID;
        doc.ModelURI = state.ModelURI;
        doc.PathName = state.PathName;
        doc.CentralModelURI = state.CentralModelURI;
        doc.CentralPathName = state.CentralPathName;
      }

      return doc;
    }

    bool IGH_Goo.CastFrom(object source) => false;
    public virtual bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.Document)))
      {
        target = (Q) (object) Value;
        return true;
      }

      target = default;
      return false;
    }

    public virtual string DisplayName =>
      IsValid ? Name :
      DocumentGUID != default ? DocumentGUID.ToString("B").ToUpper() :
      "<None>";

    #region Proxy
    internal class Proxy : IGH_GooProxy
    {
      readonly Document owner;
      public Proxy(Document o) { owner = o; ((IGH_GooProxy) this).UserString = FormatInstance(); }
      public override bool Equals(object obj)
      {
        var result = obj is Proxy other && (ReferenceEquals(owner, other.owner) || owner?.Equals(other.owner) == true);
        return result;
      }
      public override int GetHashCode() => owner?.GetHashCode() ?? 0;

      IGH_Goo IGH_GooProxy.ProxyOwner => owner;
      string IGH_GooProxy.UserString { get; set; }
      bool IGH_GooProxy.IsParsable => IsParsable();
      string IGH_GooProxy.MutateString(string str) => str.Trim();

      public virtual void Construct() { }
      public virtual bool IsParsable() => false;
      public virtual string FormatInstance() => DisplayName;
      public virtual bool FromString(string str) => throw new NotImplementedException();

      [System.ComponentModel.Browsable(false)]
      public bool Valid => owner?.IsValid ?? true;

      [System.ComponentModel.Browsable(false)]
      public string ShortDisplayName
      {
        get
        {
          if (owner is null) return "<Active Document>";
          if (string.IsNullOrEmpty(owner.Title)) return owner.DisplayName;
          if (owner.IsValid) return owner.Title.TripleDotPath(27);
          return $"⚠ {owner.Title.TripleDotPath(25)}";
        }
      }

      [System.ComponentModel.Browsable(false)]
      public string DisplayName
      {
        get
        {
          if (owner is null) return "<Active Document>";
          if (string.IsNullOrEmpty(owner.Title)) return owner.DisplayName;
          return owner.Title;
        }
      }

      public Guid? DocumentGUID => owner?.DocumentGUID;
      public string ModelURI => owner?.ModelURI?.ToString();
      public string PathName => owner?.PathName;
      public string CentralModelURI => owner?.CentralModelURI?.ToString();
      public string CentralPathName => owner?.CentralPathName;
    }
    #endregion

    #region Identity
    internal ARDB.ModelPath GetModelPath() => Value is ARDB.Document document?
      document.GetModelPath() : (CentralModelURI ?? ModelURI).ToModelPath();

    public string GetModelPathName() => GetModelPath().ToUserVisiblePath() ?? CentralPathName ?? PathName;

    public string Title => Value?.GetTitle() ??
      Path.GetFileNameWithoutExtension(CentralPathName ?? PathName);

    public string Name => Value?.GetName() ??
      Path.GetFileName(PathName ?? CentralPathName);

    public bool? IsFamilyDocument => Value?.IsFamilyDocument ??
      Path.GetExtension(CentralPathName ?? PathName)?.ToLowerInvariant() == ".rfa";

    public UnitSystem DisplayUnitSystem => Value is ARDB.Document document ?
      new UnitSystem { Value = (ARDB.UnitSystem) document.DisplayUnitSystem } :
      default;
    #endregion

    #region File
    public string FilePath
    {
      get
      {
        if (IsEmpty) return default;
        if (Value is ARDB.Document document) return document.GetPathName();
        if (ModelURI is object && ModelURI.IsFileUri(out var localPath)) return localPath;

        return default;
      }
    }

    public string FileName => Path.GetFileName(FilePath);

    public string FileExtension => Path.GetExtension(FilePath);
    #endregion

    #region Version
    public Guid? ExportID => Value?.GetExportID();

    public bool? IsModified => Value?.IsModified;

    public bool? IsEditable => Value is ARDB.Document document ?
      !document.IsLinked : default(bool?);

    public (Guid VersionGUID, int NumberOfSaves)? Version
    {
      get
      {
        if (Value is ARDB.Document document)
        {
          using (var version = ARDB.Document.GetDocumentVersion(document))
            return (version.VersionGUID, version.NumberOfSaves);
        }
        else
        {
          using (var info = FileInfo)
          if (info is object)
          {
            using (var version = info.GetDocumentVersion())
              return (version.VersionGUID, version.NumberOfSaves);
          }
        }

        return default;
      }
    }
    #endregion

    #region Worksharing
    public bool? IsWorkshared
    {
      get
      {
        if (IsEmpty) return default;
        if (Value is ARDB.Document document) return document.IsWorkshared;
        return !(CentralModelURI is null);
      }
    }

    public bool? IsDetached
    {
      get
      {
        if (IsEmpty) return default;
        if (Value is ARDB.Document document) return document.IsDetached;
        return default;
      }
    }

    public bool? IsCentral
    {
      get
      {
        if (IsEmpty) return default;
        return IsWorkshared == true && PathName == CentralPathName;
      }
    }

    public bool? HasPendingChanges
    {
      get
      {
        if (IsEmpty|| IsWorkshared != true) return default;
        if (Value is ARDB.Document document && document.IsModified) return true;
        if (FileInfo is ARDB.BasicFileInfo info) return !info.AllLocalChangesSavedToCentral;
        return default;
      }
    }

    public (Guid VersionGUID, int NumberOfSaves)? CentralVersion
    {
      get
      {
        if (IsWorkshared == true)
        {
          using (var info = FileInfo)
            if (info is object)
              return (info.LatestCentralEpisodeGUID, info.LatestCentralVersion);
        }

        return default;
      }
    }

    #endregion
  }

  [Kernel.Attributes.Name("Project Document")]
  public class ProjectDocument : Document
  {
    public ProjectDocument() : base(default) { }
    internal ProjectDocument(ARDB.Document value) : base(value) { }
  }

  [Kernel.Attributes.Name("Family Document")]
  public class FamilyDocument : Document
  {
    public FamilyDocument() : base(default) { }
    internal FamilyDocument(ARDB.Document value) : base(value) { }
  }

  class DocumentState : IGH_Goo, IEquatable<DocumentState>
  {
    #region Systme.Object
    public bool Equals(DocumentState other)
    {
      if (ReferenceEquals(this, other)) return true;
      if (other is null) return false;

      return StateGUID == other.StateGUID;
    }

    public override bool Equals(object obj) => (obj is Document doc) && Equals(doc);

    public override int GetHashCode() => StateGUID.GetHashCode();
    #endregion

    public string Name { get; set; }
    public Guid StateGUID { get; set; } = Guid.NewGuid();

    public Guid DocumentGUID { get; set; } = default;

    public Uri ModelURI { get; set; } = default;
    public string PathName { get; set; } = default;

    public Uri CentralModelURI { get; set; } = default;
    public string CentralPathName { get; set; } = default;

    #region IGH_Goo
    public bool IsValid => true;
    public string IsValidWhyNot => default;

    public string TypeName => "Document State";
    public string TypeDescription => "Stores a document state";

    public IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();
    public bool CastFrom(object source)
    {
      if (source is Document document)
      {
        Name = document.Title;
        StateGUID = document.DocumentGUID;
        DocumentGUID = document.DocumentGUID;
        ModelURI = document.ModelURI;
        PathName = document.PathName;
        CentralModelURI = document.CentralModelURI;
        CentralPathName = document.CentralPathName;
        return true;
      }

      return false;
    }

    public bool CastTo<T>(out T target)
    {
      if (typeof(T).IsAssignableFrom(typeof(IGH_Document)))
      {
        target = (T) (object) Document.FromValue(this);
        return target is object;
      }

      target = default;
      return false;
    }
    public object ScriptVariable() => default;
    #endregion

    #region IO
    public bool Read(GH_IReader reader)
    {
      Name = reader.GetString("Name");
      StateGUID = reader.GetGuid("StateGUID");

      Guid documentGUID = default;
      DocumentGUID = reader.TryGetGuid("DocumentGUID", ref documentGUID) ? documentGUID : default;

      string modelUri = default;
      ModelURI = reader.TryGetString("ModelURI", ref modelUri) ? new Uri(modelUri) : default;

      string pathName = default;
      PathName = reader.TryGetString("PathName", ref pathName) ? pathName : default;

      string centralModelUri = default;
      CentralModelURI = reader.TryGetString("CentralModelURI", ref centralModelUri) ? new Uri(centralModelUri) : default;

      string centralPathName = default;
      CentralPathName = reader.TryGetString("CentralPathName", ref centralPathName) ? centralPathName : default;

      return true;
    }

    public bool Write(GH_IWriter writer)
    {
      writer.SetString("Name", Name);
      writer.SetGuid("StateGUID", StateGUID);

      if (DocumentGUID != default)
        writer.SetGuid("DocumentGUID", DocumentGUID);

      if (ModelURI != default)
        writer.SetString("ModelURI", ModelURI.ToString());

      if (PathName != default)
        writer.SetString("PathName", PathName);

      if (CentralModelURI != default)
        writer.SetString("CentralModelURI", CentralModelURI.ToString());

      if (CentralPathName != default)
        writer.SetString("CentralPathName", CentralPathName);

      return true;
    }
    #endregion

    #region UI
    public IGH_GooProxy EmitProxy() => default;
    #endregion

    #region Document States
    const string SchemeDocumentStateStructure = "rhino.inside.revit.document.state.structure";

    public static bool WriteStateStructure(GH_Document document, GH_Structure<DocumentState> structure, GH_Path current)
    {
      var writer = new GH_LooseChunk(SchemeDocumentStateStructure);

      if (structure is object && !structure.IsEmpty)
      {
        if (current is object && current.Valid && structure.PathExists(current))
        {
          var currentPath = writer.CreateChunk("CurrentPath");
          current.Write(currentPath);
        }

        {
          var documentStates = writer.CreateChunk("DocumentStates");
          structure.Write(documentStates);
        }
      }

      // Purge unused states
      if (ReadStateStructure(document, out var previous, out var _))
      {
        var statesToPurge = new HashSet<Guid>(previous.NonNulls.Select(x => x.StateGUID));
        statesToPurge.ExceptWith(structure.NonNulls.Select(x => x.StateGUID));

        foreach (var stateToPurge in statesToPurge)
          document.ValueTable.DeleteValue($"{SchemeDocumentStateData}:{stateToPurge:B}");
      }

      document.ValueTable.SetValue(SchemeDocumentStateStructure, writer.Serialize_Xml());
      document.Modified();
      return true;
    }

    public static bool ReadStateStructure(GH_Document document, out GH_Structure<DocumentState> structure, out GH_Path current)
    {
      structure = default;
      current = default;

      if (document.ValueTable.ConstainsEntry(SchemeDocumentStateStructure))
      {
        var xml = document.ValueTable.GetValue(SchemeDocumentStateStructure, default(string));
        var reader = new GH_LooseChunk(SchemeDocumentStateStructure);
        reader.Deserialize_Xml(xml);

        if (reader.FindChunk("DocumentStates") is GH_Chunk documentStates)
        {
          structure = new GH_Structure<DocumentState>();
          structure.Read(documentStates);

          if (reader.FindChunk("CurrentPath") is GH_Chunk currentPath)
          {
            current = new GH_Path();
            current.Read(currentPath);
          }
        }

        return true;
      }

      return false;
    }

    #endregion

    #region Document State
    const string SchemeDocumentStateData = "rhino.inside.revit.document.state.data";
    public static bool StoreState(GH_Document document, Guid name)
    {
      var result = StoreState(document, name, out var stored, out var failed);
      if (failed > 0) Grasshopper.Instances.DocumentEditor?.SetStatusBarEvent
      (
        new GH_RuntimeMessage
        (
          $"Failed to store {failed} objects state.",
          GH_RuntimeMessageLevel.Error, "State server"
        )
      );

      return result;
    }

    public static bool StoreState(GH_Document document, Guid name, out int stored, out int failed)
    {
      stored = 0;
      failed = 0;

      var docObjects = document.Objects;
      var stateObjects = docObjects.Where(x => x is IGH_StateAwareObject || x is Parameters.IGH_PersistentStateAwareObject);
      var count = stateObjects.Count();
      var writer = new GH_LooseChunk(SchemeDocumentStateData);

      int index = 0;
      foreach (var stateObject in stateObjects)
      {
        bool saved = false;
        if (stateObject is Parameters.IGH_PersistentStateAwareObject persistentObject)
        {
          try
          {
            var data = writer.CreateChunk("StateData", index);
            saved = persistentObject.SaveState(data);
          }
          catch { }

          if (!saved)
            writer.RemoveChunk("StateData", index);
        }
        else if (stateObject is IGH_StateAwareObject stateAware)
        {
          try
          {
            if (stateAware.SaveState() is string state)
            {
              writer.SetString("StateData", index, state);
              saved = true;
            }
          }
          catch { }
        }

        if (saved)
          writer.SetGuid("ObjectID", index++, stateObject.InstanceGuid);
        else
          failed++;
      }

      stored = count - failed;
      writer.SetInt32("StateCount", stored);

      document.ValueTable.SetValue($"{SchemeDocumentStateData}:{name:B}", writer.Serialize_Xml());
      document.Modified();

      return true;
    }

    public static bool RestoreState(GH_Document document, Guid name)
    {
      var result = RestoreState(document, name, out var restored, out var reseted, out var failed);
      if (failed > 0) Grasshopper.Instances.DocumentEditor?.SetStatusBarEvent
      (
        new GH_RuntimeMessage
        (
          $"Failed to restore {failed} objects state.",
          GH_RuntimeMessageLevel.Error, "State server"
        )
      );

      return result;
    }

    public static bool RestoreState(GH_Document document, Guid name, out int restored, out int reseted, out int failed)
    {
      restored = 0;
      reseted = 0;
      failed = 0;

      var docObjects = document.Objects;
      var stateObjects = docObjects.Where(x => x is IGH_StateAwareObject || x is Parameters.IGH_PersistentStateAwareObject).
        ToDictionary(x => x.InstanceGuid, x => x);

      var result = false;
      if (document.ValueTable.ConstainsEntry($"{SchemeDocumentStateData}:{name:B}"))
      {
        var xml = document.ValueTable.GetValue($"{SchemeDocumentStateData}:{name:B}", default(string));
        var reader = new GH_LooseChunk(SchemeDocumentStateData);
        reader.Deserialize_Xml(xml);

        var count = reader.GetInt32("StateCount");
        for (int index = 0; index < count; ++index)
        {
          var id = default(Guid);
          if (!reader.TryGetGuid("ObjectID", index, ref id))
            continue;

          if (stateObjects.TryGetValue(id, out var stateObject))
          {
            try
            {
              if (stateObject is Parameters.IGH_PersistentStateAwareObject persistentObject)
              {
                if (reader.FindChunk("StateData", index) is GH_IReader data)
                {
                  if (persistentObject.LoadState(data))
                  {
                    restored++;
                    stateObjects.Remove(id);
                  }
                }
              }
              else if (stateObject is IGH_StateAwareObject stateAware)
              {
                var data = default(string);
                if (reader.TryGetString("StateData", index, ref data))
                {
                  stateAware.LoadState(data);
                  restored++;
                  stateObjects.Remove(id);
                }
              }
            }
            catch { failed++; }
          }
        }

        result = true;
      }

      ResetState(document, stateObjects.Values, out reseted);

      return result;
    }

    public static bool ResetState(GH_Document document)
    {
      var stateObjects = document.Objects.Where(x => x is IGH_StateAwareObject || x is Parameters.IGH_PersistentStateAwareObject);
      return ResetState(document, stateObjects, out var _);
    }

    public static bool ResetState(GH_Document document, IEnumerable<Grasshopper.Kernel.IGH_DocumentObject> objects, out int reseted)
    {
      reseted = 0;

      foreach (var stateObject in objects)
      {
        try
        {
          if (stateObject is Parameters.IGH_PersistentStateAwareObject persistentObject)
          {
            if (persistentObject.ResetState())
              reseted++;
          }
          else if (stateObject is IGH_StateAwareObject stateAware)
          {
            if (stateObject.GetType().IsGenericSubclassOf(typeof(GH_PersistentParam<>)))
            {
              dynamic persistentParam = stateObject;
              persistentParam.PersistentData.Clear();
              reseted++;
            }
          }
        }
        catch { }
      }

      return true;
    }
    #endregion
  }
}
