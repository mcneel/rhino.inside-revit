using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  internal static class ReferenceEqualityComparer
  {
    /// <summary>
    /// IEqualityComparer for <see cref="Autodesk.Revit.DB.Reference"/>
    /// that assumes all references are from the same <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    public static IEqualityComparer<Reference> SameDocument(Document document) => new EqualityComparer(document);

    struct EqualityComparer : IEqualityComparer<Reference>
    {
      readonly Document Document;
      public EqualityComparer(Document doc) => Document = doc;
      bool IEqualityComparer<Reference>.Equals(Reference x, Reference y) => AreEquivalentReferences(Document, x, y);
      int IEqualityComparer<Reference>.GetHashCode(Reference obj)
      {
        int hashCode = 2022825623;
        hashCode = hashCode * -1521134295 + (int) obj.ElementReferenceType;
        hashCode = hashCode * -1521134295 + obj.ElementId.GetHashCode();
        hashCode = hashCode * -1521134295 + obj.LinkedElementId.GetHashCode();
        return hashCode;
      }
    }

    /// <summary>
    /// Compare two <see cref="Autodesk.Revit.DB.Reference"/> objects to establish
    /// if they are referencing same <see cref="Autodesk.Revit.DB.Element"/>.
    /// </summary>
    /// <remarks>
    /// Two <see cref="Autodesk.Revit.DB.Reference"/> instances are considered equivalent
    /// if their stable representations are equal.
    /// </remarks>
    /// <param name="document"></param>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>true if both references are equivalent.</returns>
    public static bool AreEquivalentReferences(this Document document, Reference left, Reference right)
    {
      if (ReferenceEquals(left, right)) return true;
      if (left is null || right is null) return false;

      if (left.ElementReferenceType != right.ElementReferenceType) return false;
      if (left.ElementId != right.ElementId) return false;
      if (left.LinkedElementId != right.LinkedElementId) return false;

      // TODO: Test if EqualTo validates the document and is faster than ConvertToStableRepresentation.
//#if REVIT_2018
//      return left.EqualTo(right);
//#else
      var stableLeft = left?.ConvertToStableRepresentation(document);
      var stableRight = right?.ConvertToStableRepresentation(document);

      return stableLeft == stableRight;
//#endif
    }
  }

  internal static class ReferenceExtension
  {
    public static LinkElementId ToLinkElementId(this Reference reference)
    {
      if (reference is null) throw new ArgumentNullException(nameof(reference));
      return reference.LinkedElementId == ElementId.InvalidElementId ?
        new LinkElementId(reference.ElementId) :
        new LinkElementId(reference.ElementId, reference.LinkedElementId);
    }

    public static Reference CreateLinkReference(this Reference reference, Document document, ElementId linkInstanceId, Document linkedDocument)
    {
      if (reference is null) throw new ArgumentNullException(nameof(reference));
      if (document is null) throw new ArgumentNullException(nameof(document));
      if (!linkInstanceId.IsValid()) throw new ArgumentException(nameof(linkInstanceId));
      if (!linkedDocument.IsValid() || !linkedDocument.IsLinked) throw new ArgumentException(nameof(linkedDocument));

      var stable = reference.ConvertToStableRepresentation(linkedDocument);

      var referenceId = ReferenceId.Parse(stable, linkedDocument);
      referenceId = new ReferenceId
      (
        new GeometryObjectId(linkInstanceId.ToValue(), new int[] { 0 }, GeometryObjectType.RVTLINK),
        referenceId.Element,
        referenceId.Symbol
      );
      stable = referenceId.ToStableRepresentation(document);

      return Reference.ParseFromStableRepresentation(document, stable);
    }

    public static Reference CreateReferenceInLink(this Reference reference, RevitLinkInstance instance)
    {
      if (reference is null) throw new ArgumentNullException(nameof(reference));
      if (instance is null) throw new ArgumentNullException(nameof(instance));
      if (instance.Id != reference.ElementId) throw new ArgumentException(nameof(instance));
      if (instance.GetLinkDocument() is Document linkedDocument)
      {
        var stable = reference.ConvertToStableRepresentation(instance.Document);

        var referenceId = ReferenceId.Parse(stable, instance.Document);
        referenceId = new ReferenceId
        (
          referenceId.Element,
          referenceId.Symbol
        );
        stable = referenceId.ToStableRepresentation(linkedDocument);

        return Reference.ParseFromStableRepresentation(linkedDocument, stable);
      }

      return null;
    }

    internal static string ConvertToPersistentRepresentation(this Reference reference, Document document)
    {
      if (reference is null) throw new ArgumentNullException(nameof(reference));
      if (document is null)  throw new ArgumentNullException(nameof(document));

      if (reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_NONE)
      {
        var referenceId = reference.LinkedElementId == ElementId.InvalidElementId ?
          new ReferenceId(new GeometryObjectId(reference.ElementId.ToValue())) :
          new ReferenceId
          (
            new GeometryObjectId(reference.ElementId.ToValue(), default, GeometryObjectType.RVTLINK, document.GetElement(reference.ElementId).GetTypeId().ToValue()),
            new GeometryObjectId(reference.LinkedElementId.ToValue())
          );

        return referenceId.ToString(document);
      }
      else
      {
        var stable = reference.ConvertToStableRepresentation(document);
        return ReferenceId.Parse(stable, document).ToString(document);
      }
    }

    internal static Reference ParseFromPersistentRepresentation(Document document, string persistent)
    {
      return Reference.ParseFromStableRepresentation(document, ReferenceId.Parse(persistent, document).ToStableRepresentation(document));

    }
  }
}
