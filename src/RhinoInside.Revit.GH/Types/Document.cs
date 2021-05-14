using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Name("Document")]
  public interface IGH_Document : IGH_Goo, IEquatable<IGH_Document>
  {
    Guid DocumentGUID { get; }

    Uri ModelURI { get; }
    string PathName { get; }

    Uri CentralModelURI { get; }
    string CentralPathName { get; }

    DB.Document Value { get; }
  }

  [Name("Document"), AssemblyPriority]
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
        ModelURI is null ? $"Invalid {typeName}" :
        $"Not loaded {typeName} : {DisplayName}"
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
    public virtual string IsValidWhyNot => document.IsValidWithLog(out var log) ? string.Empty : log;
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

    public bool IsReferencedDataLoaded => Value?.IsValidObject == true;

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
      if (document is null)
      {
        if (ModelURI is null) return;
        if (ModelURI.IsFileUri(out var localPath) == true)
        {
          PathName = localPath;

          if (File.Exists(localPath))
          {
            try
            {
              using (var info = DB.BasicFileInfo.Extract(ModelURI.LocalPath))
              {
                if (!info.IsWorkshared)
                {
                  CentralModelURI = ModelUri.Empty;
                  CentralPathName = string.Empty;
                }
                else if (info.IsLocal)
                {
                  CentralModelURI = new UriBuilder(Uri.UriSchemeFile, string.Empty, 0, info.CentralPath).Uri;
                  CentralPathName = info.CentralPath;
                }
              }
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { }
          }
        }
        else if (ModelURI.ToModelPath() is DB.ModelPath modelPath)
        {
          try { PathName = DB.ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath); }
          catch (Autodesk.Revit.Exceptions.ApplicationException) { }
        }
      }
      else if (document.IsValidObject)
      {
        using (var modelPath = document.GetModelPath())
        {
          var modelUri = modelPath.ToUri();
          if (ModelURI != modelUri)
          {
            ModelURI = modelUri;
            PathName = DB.ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
          }
          else
          {
            PathName = document.PathName;
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
              CentralPathName = DB.ModelPathUtils.ConvertModelPathToUserVisiblePath(centralPath);
            }
          }
        }
        else
        {
          CentralModelURI = ModelUri.Empty;
          CentralPathName = string.Empty;
        }
      }
    }
    #endregion

    DB.Document document;
    public DB.Document Value
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
    public string FileName
    {
      get
      {
        if (Value is DB.Document document)
          return document.GetFileName();

        if (!string.IsNullOrEmpty(PathName))
          return Path.GetFileName(PathName);

        return string.Empty;
      }
    }

    public Uri CentralModelURI { get; protected set; } = default;
    public string CentralPathName { get; protected set; } = default;

    public Document() { }
    protected Document(DB.Document value)
    {
      if (value is null) return;
      document = value;
      DocumentGUID = value.GetFingerprintGUID();
      RefreshReferenceData();
    }

    static Document()
    {
      if (AddIn.Host.Value is Autodesk.Revit.UI.UIApplication host)
      {
        foreach (var document in host.Application.Documents.Cast<DB.Document>())
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

    private static void Host_DocumentCreated(object sender, DB.Events.DocumentCreatedEventArgs e)
    {
      AddDocument(e.Document);
    }
    private static void Host_DocumentOpened(object sender, DB.Events.DocumentOpenedEventArgs e)
    {
      AddDocument(e.Document);
    }

    static void Host_DocumentClosing(object sender, DB.Events.DocumentClosingEventArgs e)
    {
      ClosingDocuments.Add(e.DocumentId, e.Document);
      RemoveDocument(e.Document);
    }
    static void Host_DocumentClosed(object sender, DB.Events.DocumentClosedEventArgs e)
    {
      if (!ClosingDocuments.TryGetValue(e.DocumentId, out var document)) return;
      ClosingDocuments.Remove(e.DocumentId);

      if (e.Status != DB.Events.RevitAPIEventStatus.Succeeded)
        AddDocument(document);
    }

    static void AddDocument(DB.Document document)
    {
      if (document is null) return;

      if (DocumentsRegistry.TryGetValue(document.GetFingerprintGUID(), out var twins))
        twins.Add(document);
      else
        DocumentsRegistry.Add(document.GetFingerprintGUID(), new List<DB.Document>() { document });
    }

    static void RemoveDocument(DB.Document document)
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

    static readonly Dictionary<int, DB.Document> ClosingDocuments = new Dictionary<int, DB.Document>();
    static readonly Dictionary<Guid, List<DB.Document>> DocumentsRegistry = new Dictionary<Guid, List<DB.Document>>();

    internal static bool TryGetDocument(Guid guid, out DB.Document document)
    {
      // Only return a document when the query is not ambiguous.
      // An ambiguous situation happens when two documents with the same GUID are loaded at the same time.
      if (DocumentsRegistry.TryGetValue(guid, out var twins) && twins.Count == 1)
      {
        document = twins[0];
        return true;
      }

      document = default;
      return false;
    }

    public static Document FromValue(object value)
    {
      if (value is IGH_Goo goo)
        value = goo.ScriptVariable();

      switch (value)
      {
        case DB.Document document: return FromValue(document);
        case DB.Element element: return FromValue(element.Document);
        case string str:
          using (var documents = Revit.ActiveDBApplication.Documents)
          {
            var docs = documents.Cast<DB.Document>();

            if (str.Contains(':'))
            {
              // Find a matching PathName
              {
                var match_path = docs.Where(x => x.PathName.Equals(str, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (match_path.Length == 1)
                  return FromValue(match_path[0]);
              }

              // Find a matching ModelPath
              {
                var modelPath = default(DB.ModelPath);

                // If 'str' starts like "X:\" is an absolute MS-DOS path
                if (str.Length > 3 && char.IsLetter(str[0]) && str[1] == Path.VolumeSeparatorChar && (str[2] == Path.DirectorySeparatorChar || str[2] == Path.AltDirectorySeparatorChar))
                {
                  // TODO: Open the file on the background
                  // modelPath = new DB.FilePath(str);
                }
                else
                {
                  try
                  {
                    var uri = new Uri(str);
                    if (uri.Scheme == Uri.UriSchemeFile || uri.Scheme == ModelUri.UriSchemeServer || uri.Scheme == ModelUri.UriSchemeCloud)
                    {
                      var match_path = docs.Where(x => x.GetModelPath().ToUri().Equals(uri)).ToArray();
                      if (match_path.Length == 1)
                        return FromValue(match_path[0]);

                      // TODO: Open the file on the background
                      //modelPath = uri.ToModelPath();
                    }
                  }
                  catch (UriFormatException) { }
                }

#if DEBUG
                if (modelPath is object)
                {
                  using (var openOptions = new DB.OpenOptions())
                  {
                    if (Revit.ActiveDBApplication.OpenDocumentFile(modelPath, openOptions) is DB.Document model)
                    {
                      // How long this Document should stay open??
                      // model.Close();

                      return FromValue(model);
                    }
                  }
                }
#endif
              }
            }
            else
            {
              // Find a matching FileName
              var match_file = docs.Where(x => x.GetFileName().Equals(str, StringComparison.OrdinalIgnoreCase)).ToArray();
              if (match_file.Length == 1)
                return FromValue(match_file[0]);

              // Find a matching Title
              var match_title = docs.Where(x => x.GetTitle().Equals(str, StringComparison.OrdinalIgnoreCase)).ToArray();
              if (match_title.Length == 1)
                return FromValue(match_title[0]);
            }
          }
          break;
      }

      return default;
    }

    public static Document FromValue(DB.Document document)
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
      if (typeof(Q).IsAssignableFrom(typeof(DB.Document)))
      {
        target = (Q) (object) Value;
        return true;
      }

      target = default;
      return false;
    }

    public virtual string DisplayName =>
      IsValid ? FileName :
      !string.IsNullOrEmpty(PathName) ? $"{PathName}" :
      !(ModelURI is null) ? $"{ModelURI}" :
      DocumentGUID != Guid.Empty ? $"{DocumentGUID.ToString("B").ToUpperInvariant()}" :
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
          if (string.IsNullOrEmpty(owner.FileName)) return owner.DisplayName;
          if (owner.IsValid) return owner.FileName.TripleDotPath(27);
          return $"⚠ {owner.FileName.TripleDotPath(25)}";
        }
      }

      [System.ComponentModel.Browsable(false)]
      public string DisplayName
      {
        get
        {
          if (owner is null) return "<Active Document>";
          if (string.IsNullOrEmpty(owner.FileName)) return owner.DisplayName;
          return owner.FileName;
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
    public string Title => Value?.GetTitle() ?? Path.GetFileNameWithoutExtension(FileName);
    public UnitSystem DisplayUnitSystem => Value is DB.Document document ?
      new UnitSystem { Value = (DB.UnitSystem) document.DisplayUnitSystem } :
      default;

    #endregion

    #region Worksharing
    public bool? IsWorkshared
    {
      get
      {
        if (Value is DB.Document document) return document.IsWorkshared;
        return !(CentralModelURI is null);
      }
    }
    #endregion
  }

  [Name("Project Document")]
  public class ProjectDocument : Document
  {
    public ProjectDocument() : base(default) { }
    internal ProjectDocument(DB.Document value) : base(value) { }
  }

  [Name("Family Document")]
  public class FamilyDocument : Document
  {
    public FamilyDocument() : base(default) { }
    internal FamilyDocument(DB.Document value) : base(value) { }
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
    public string IsValidWhyNot => string.Empty;

    public string TypeName => "Document State";
    public string TypeDescription => "Stores a document state";

    public IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();
    public bool CastFrom(object source)
    {
      if (source is Document document)
      {
        Name = document.FileName;
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
