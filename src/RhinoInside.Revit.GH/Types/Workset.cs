using System;
using System.Drawing;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Workset")]
  public class Workset : ReferenceObject,
    IEquatable<Workset>,
    IGH_ItemDescription
  {
    public Workset() { }
    public Workset(ARDB.Document doc, ARDB.WorksetId id) : base() => SetValue(doc, id);
    public Workset(ARDB.Document doc, ARDB.Workset value) : base(doc, value) => SetValue(doc, value.Id);

    #region System.Object
    public bool Equals(Workset other) => other is object &&
      other.DocumentGUID == other.DocumentGUID && other.UniqueID == UniqueID;
    public override bool Equals(object obj) => (obj is Workset id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => DocumentGUID.GetHashCode() ^ UniqueID.GetHashCode();

    public override string ToString()
    {
      var valid = IsValid;
      string Invalid = Id == ARDB.WorksetId.InvalidWorksetId?
        string.Empty :
        IsReferencedData ?
        (valid ? /*"Referenced "*/ "" : "Unresolved ") :
        (valid ? string.Empty : "Invalid ");
      string TypeName = ((IGH_Goo) this).TypeName;
      string InstanceName = DisplayName ?? string.Empty;

      if (!IsReferencedData)
        return $"{Invalid}{TypeName} : {InstanceName}";

      string InstanceId = valid ? $" : id {Id.IntegerValue}" : $" : {UniqueID}";

      using (var Documents = Revit.ActiveDBApplication.Documents)
      {
        if (Documents.Size > 1)
          InstanceId = $"{InstanceId} @ {Document?.GetFileName() ?? DocumentGUID.ToString("B")}";
      }

      return $"{Invalid}{TypeName} : {InstanceName}{InstanceId}";
    }
    #endregion

    #region DocumentObject
    ARDB.Workset value => base.Value as ARDB.Workset;
    public new ARDB.Workset Value
    {
      get
      {
        if (value?.IsValidObject == false)
          ResetValue();

        return value;
      }
    }

    ARDB.WorksetId id;
    public ARDB.WorksetId Id => id;

    public override string DisplayName => Value.Name;
    #endregion

    #region IGH_Goo
    public override bool IsValid => base.IsValid && Id != ARDB.WorksetId.InvalidWorksetId;
    public override string IsValidWhyNot
    {
      get
      {
        if (DocumentGUID == Guid.Empty) return $"DocumentGUID '{Guid.Empty}' is invalid";
        if (!Guid.TryParse(UniqueID, out var _)) return $"UniqueID '{UniqueID}' is invalid";

        var id = Id;
        if (Document is null)
        {
          return $"Referenced Revit document '{DocumentGUID}' was closed.";
        }
        else
        {
          if (id is null) return $"Referenced Revit element '{UniqueID}' is not available.";
          if (id == ARDB.WorksetId.InvalidWorksetId) return "Id is equal to InvalidElementId.";
        }

        return default;
      }
    }

    public virtual object ScriptVariable() => Value;

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
      {
        target = (Q) (object) new GH_Integer(Id.IntegerValue);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Guid)))
      {
        target = (Q) (object) new GH_Guid(Value?.UniqueId ?? Guid.Empty);
        return true;
      }

      target = default;
      return false;
    }

    protected class Proxy : IGH_GooProxy
    {
      protected readonly Workset owner;
      public Proxy(Workset o) { owner = o; ((IGH_GooProxy) this).UserString = FormatInstance(); }
      public override string ToString() => owner.DisplayName;

      IGH_Goo IGH_GooProxy.ProxyOwner => owner;
      string IGH_GooProxy.UserString { get; set; }
      bool IGH_GooProxy.IsParsable => IsParsable();
      string IGH_GooProxy.MutateString(string str) => str.Trim();

      public virtual void Construct() { }
      public virtual bool IsParsable() => false;
      public virtual string FormatInstance() => owner.DisplayName;
      public virtual bool FromString(string str) => throw new NotImplementedException();

      public bool Valid => owner.IsValid;

      [System.ComponentModel.Description("The document this element belongs to.")]
      public string Document => owner.Document?.GetFileName();

      [System.ComponentModel.Description("The Guid of document this element belongs to.")]
      public Guid DocumentGUID => owner.DocumentGUID;

      protected virtual bool IsValidId(ARDB.Document doc, ARDB.ElementId id) =>
        owner.GetType() == Element.FromElementId(doc, id).GetType();

      [System.ComponentModel.Description("A stable unique identifier for an element within the document.")]
      public string UniqueID => owner.UniqueID;
      [System.ComponentModel.Description("API Object Type.")]
      public virtual Type ObjectType => owner.Value?.GetType();
      [System.ComponentModel.Description("Element is built in Revit.")]
      public bool IsBuiltIn => owner.IsReferencedData && owner.Id.IntegerValue < 0;
    }

    public virtual IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    #region IGH_ItemDescription
    Bitmap IGH_ItemDescription.GetImage(Size size) => default;
    string IGH_ItemDescription.Name => DisplayName;
    string IGH_ItemDescription.NickName => $"{{{Id?.ToString()}}}";
    string IGH_ItemDescription.Description => Document?.GetFileName();
    #endregion

    #region IGH_ReferencedData
    public override bool IsReferencedData => DocumentGUID != Guid.Empty;
    public override bool IsReferencedDataLoaded => Document is object && Id is object;

    public override bool LoadReferencedData()
    {
      if (IsReferencedData)
      {
        UnloadReferencedData();

        if (Types.Document.TryGetDocument(DocumentGUID, out var document))
        {
          if (document.IsWorkshared && Guid.TryParse(UniqueID, out var guid))
          {
            if (document.GetWorksetTable().GetWorkset(guid) is ARDB.Workset ws)
            {
              Document = document;
              id = ws.Id;
            }
          }
        }
      }

      return IsReferencedDataLoaded;
    }

    protected override object FetchValue() => Document.GetWorksetTable()?.GetWorkset(Id);

    protected void SetValue(ARDB.Document doc, ARDB.WorksetId id)
    {
      if (id == ARDB.WorksetId.InvalidWorksetId)
        doc = null;

      Document = doc;
      DocumentGUID = doc.GetFingerprintGUID();

      this.id = id;
      UniqueID = doc?.GetWorksetTable()?.GetWorkset(id)?.UniqueId.ToString() ??
        string.Empty;
    }
    public override void UnloadReferencedData()
    {
      base.UnloadReferencedData();

      if (IsReferencedData)
        id = default;
    }
    #endregion
  }
}
