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
    IGH_ItemDescription
  {
    #region System.Object
    public override string ToString()
    {
      var valid = IsValid;
      string Invalid = Id == ARDB.WorksetId.InvalidWorksetId ?
        (string.IsNullOrWhiteSpace(ReferenceUniqueId) ? string.Empty : "Unresolved ") :
        valid ? string.Empty :
        (IsReferencedData ? "❌ Deleted " : "⚠ Invalid ");
      string TypeName = ((IGH_Goo) this).TypeName;
      string InstanceName = DisplayName ?? string.Empty;

      if (string.IsNullOrWhiteSpace(InstanceName))
        InstanceName = $" : <None>";
      else
        InstanceName = $" : {InstanceName}";

      if (!IsReferencedData)
        return $"{Invalid}{TypeName}{InstanceName}";

      string InstanceId = valid ? $" : id {Id.IntegerValue}" : $" : {ReferenceUniqueId}";

      if (/*ReferenceDocument is ARDB.Document && */Document is ARDB.Document document)
      {
        if (document.IsLinked || document.IsFamilyDocument)
          InstanceId = $"{InstanceId} @ {document.GetTitle()}";
      }
      else InstanceId = $"{InstanceId} @ {ReferenceDocumentId:B}";

      //if (IsLinked) TypeName = "Linked " + TypeName;
      return $"{Invalid}{TypeName}{InstanceName}{InstanceId}";
    }
    #endregion

    #region DocumentObject
    public override string DisplayName => Value?.Name;

    public new ARDB.Workset Value
    {
      get
      {
        var value = base.Value as ARDB.Workset;
        if (value?.IsValidObject == false)
        {
          ResetValue();
          value = base.Value as ARDB.Workset;
        }

        return value;
      }
    }

    protected override void ResetValue()
    {
      Id = default;
      base.ResetValue();
    }

    protected override object FetchValue()
    {
      LoadReferencedData();
      return Document?.GetWorksetTable()?.GetWorkset(Id);
    }

    protected void SetValue(ARDB.Document doc, ARDB.WorksetId id)
    {
      ResetValue();

      if (id == ARDB.WorksetId.InvalidWorksetId)
        doc = null;

      Document = doc;
      ReferenceDocumentId = doc.GetFingerprintGUID();

      Id = id;
      ReferenceUniqueId = doc?.GetWorksetTable()?.GetWorkset(id)?.UniqueId.ToString() ?? string.Empty;
    }
    #endregion

    #region ReferenceObject
    public override bool? IsEditable => Value?.IsEditable;
    #endregion

    #region IGH_Goo
    public override bool IsValid => base.IsValid && Id != ARDB.WorksetId.InvalidWorksetId;
    public override string IsValidWhyNot
    {
      get
      {
        if (ReferenceDocumentId == Guid.Empty) return $"Reference Document Id '{Guid.Empty}' is invalid";
        if (!Guid.TryParse(ReferenceUniqueId, out var _)) return $"Reference Unique Id '{ReferenceUniqueId}' is invalid";

        var id = Id;
        if (Document is null)
        {
          return $"Referenced Revit document '{ReferenceDocumentId}' was closed.";
        }
        else
        {
          if (id is null) return $"Referenced Revit element '{ReferenceUniqueId}' is not available.";
          if (id == ARDB.WorksetId.InvalidWorksetId) return "Id is equal to InvalidElementId.";
        }

        return default;
      }
    }

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is ARDB.Element element)
      {
        SetValue(element.Document, element.WorksetId);
        return true;
      }

      return base.CastFrom(source);
    }

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

    protected new class Proxy : ReferenceObject.Proxy
    {
      protected new readonly Workset owner;
      public Proxy(Workset o) : base(o) { }

      [System.ComponentModel.Description("Element is built in Revit.")]
      public bool IsBuiltIn => owner.IsReferencedData && owner.Id.IntegerValue < 0;
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    #region IGH_ItemDescription
    Bitmap IGH_ItemDescription.GetTypeIcon(Size size) => Properties.Resources.Workset;
    string IGH_ItemDescription.Name => DisplayName;
    string IGH_ItemDescription.Identity => $"{{{Id?.ToString()}}}";
    string IGH_ItemDescription.Description => Document?.GetTitle();
    #endregion

    #region IGH_ReferencedData
    public override bool IsReferencedDataLoaded => Document is object && Id is object;

    public override bool LoadReferencedData()
    {
      if (IsReferencedData && !IsReferencedDataLoaded)
      {
        UnloadReferencedData();

        if (Types.Document.TryGetDocument(ReferenceDocumentId, out var document))
        {
          if (document.IsWorkshared && Guid.TryParse(ReferenceUniqueId, out var guid))
          {
            if (document.GetWorksetTable().GetWorkset(guid) is ARDB.Workset ws)
            {
              Document = document;
              Id = ws.Id;
            }
          }
        }
      }

      return IsReferencedDataLoaded;
    }
    #endregion

    public Workset() { }
    public Workset(ARDB.Document doc, ARDB.WorksetId id) : base() => SetValue(doc, id);
    public Workset(ARDB.Document doc, ARDB.Workset value) : base(doc, value) => SetValue(doc, value?.Id ?? ARDB.WorksetId.InvalidWorksetId);

    public ARDB.WorksetId Id { get; private set; }
  }
}
