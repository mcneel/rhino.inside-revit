using System;
using System.Drawing;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  // public interface IGH_PersistentReference
  /// <summary>
  /// Interface to implement into classes that has a stable <see cref="ARDB.Reference"/>.
  /// For example: <see cref="ARDB.Element"/>, <see cref="ARDB.GeometryObject"/>
  /// </summary>
  public interface IGH_ElementId : IGH_ReferenceObject, IEquatable<IGH_ElementId>
  {
    ARDB.Reference Reference { get; }
    ARDB.ElementId Id { get; }
  }

  public abstract class ElementId : ReferenceObject,
    IGH_ElementId,
    IGH_ItemDescription
  {
    #region System.Object
    public bool Equals(IGH_ElementId other) => other is object &&
      other.DocumentGUID == DocumentGUID && other.UniqueID == UniqueID;
    public override bool Equals(object obj) => (obj is IGH_ElementId id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode() => DocumentGUID.GetHashCode() ^ UniqueID.GetHashCode();

    public override string ToString()
    {
      var valid = IsValid;
      string Invalid = Id == ARDB.ElementId.InvalidElementId ?
        (string.IsNullOrWhiteSpace(UniqueID) ? string.Empty : "Unresolved ") :
        valid ? string.Empty :
        (IsReferencedData ? "❌ Deleted " : "⚠ Invalid ");
      string TypeName = ((IGH_Goo) this).TypeName;
      string InstanceName = DisplayName ?? string.Empty;

      if (!string.IsNullOrWhiteSpace(InstanceName))
        InstanceName = $" : {InstanceName}";

      if (!IsReferencedData)
        return $"{Invalid}{TypeName}{InstanceName}";

      string InstanceId = valid ? $" : id {Id.ToValue()}" : $" : {UniqueID}";

      using (var Documents = Revit.ActiveDBApplication.Documents)
      {
        if (Documents.Size > 1)
          InstanceId = $"{InstanceId} @ {Document?.GetTitle() ?? DocumentGUID.ToString("B")}";
      }

      return $"{Invalid}{TypeName}{InstanceName}{InstanceId}";
    }
    #endregion

    #region IGH_Goo
    public override bool IsValid => base.IsValid && Id.IsValid();
    public override string IsValidWhyNot
    {
      get
      {
        if (DocumentGUID == Guid.Empty) return $"DocumentGUID '{Guid.Empty}' is invalid";
        if (!External.DB.UniqueId.TryParse(UniqueID, out var _, out var _)) return $"UniqueID '{UniqueID}' is invalid";

        if (Document is null)
        {
          return $"Referenced Revit document '{DocumentGUID}' was closed.";
        }
        else
        {
          var id = Id;
          if (id is null) return $"Referenced Revit element '{UniqueID}' is not available.";
          if (id == ARDB.ElementId.InvalidElementId) return "Id is equal to InvalidElementId.";
        }

        return default;
      }
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.ElementId)))
      {
        target = (Q) (object) Id;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
      {
        target = (Q) (object) new GH_Integer(Id.IntegerValue);
        return true;
      }

      target = default;
      return false;
    }
    #endregion

    #region IGH_ItemDescription
    Bitmap IGH_ItemDescription.GetImage(Size size) => default;
    string IGH_ItemDescription.Name => DisplayName;
    string IGH_ItemDescription.NickName => $"{{{Id?.ToString()}}}";
    string IGH_ItemDescription.Description => Document?.GetTitle();
    #endregion

    #region IGH_ReferencedData
    public override bool IsReferencedData => DocumentGUID != Guid.Empty;
    #endregion

    #region IGH_ElementId
    public abstract ARDB.Reference Reference { get; }
    public abstract ARDB.ElementId Id { get; }
    #endregion

    public ElementId() { }

    protected ElementId(ARDB.Document doc, object value) : base(doc, value) { }
  }
}
