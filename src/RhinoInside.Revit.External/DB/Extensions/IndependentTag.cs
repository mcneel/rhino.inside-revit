using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class IndependentTagExtension
  {
#if !REVIT_2022
    public static ISet<ElementId> GetTaggedLocalElementIds(this IndependentTag tag)
    {
      return new HashSet<ElementId> { tag.TaggedLocalElementId };
    }

    public static ICollection<Element> GetTaggedLocalElements(this IndependentTag tag)
    {
      return new List<Element> { tag.Document.GetElement(tag.TaggedLocalElementId) };
    }

    public static ICollection<LinkElementId> GetTaggedElementIds(this IndependentTag tag)
    {
      return new List<LinkElementId> { tag.TaggedElementId };
    }

#if !REVIT_2018
    static Reference GetTaggedReference(this IndependentTag tag)
    {
      return new Reference(tag.Document.GetElement(tag.TaggedLocalElementId));
    }
#endif

    public static IList<Reference> GetTaggedReferences(this IndependentTag tag)
    {
      return new List<Reference> { tag.GetTaggedReference() };
    }

    public static bool HasLeaderElbow(this IndependentTag tag, Reference referenceTagged)
    {
      if (referenceTagged is null)
        throw new System.ArgumentNullException(nameof(referenceTagged));

      if (!tag.HasLeader)
        throw new System.InvalidOperationException("The tag does not have a leader.");

      if (tag.GetTaggedReference() is Reference reference)
      {
        if (!Equals(tag.Document, reference, referenceTagged))
          throw new System.ArgumentException(nameof(referenceTagged));
      }

#if REVIT_2018
      return tag.HasElbow;
#else
      try { return tag.LeaderElbow is object; }
      catch { }

      return false;
#endif
    }

    public static XYZ GetLeaderElbow(this IndependentTag tag, Reference referenceTagged)
    {
      if (referenceTagged is null)
        throw new System.ArgumentNullException(nameof(referenceTagged));

      if (!tag.HasLeader)
        throw new System.InvalidOperationException("The tag does not have a leader.");

      if (tag.GetTaggedReference() is Reference reference)
      {
        if (!Equals(tag.Document, reference, referenceTagged))
          throw new System.ArgumentException(nameof(referenceTagged));
      }

      return tag.LeaderElbow;
    }

    public static XYZ GetLeaderEnd(this IndependentTag tag, Reference referenceTagged)
    {
      if (referenceTagged is null)
        throw new System.ArgumentNullException(nameof(referenceTagged));

      if (!tag.HasLeader)
        throw new System.InvalidOperationException("The tag does not have a leader.");

      if (tag.GetTaggedReference() is Reference reference)
      {
        if (!Equals(tag.Document, reference, referenceTagged))
          throw new System.ArgumentException(nameof(referenceTagged));
      }

      return tag.LeaderEnd;
    }

    public static void SetLeaderElbow(this IndependentTag tag, Reference referenceTagged, XYZ pntElbow)
    {
      if (referenceTagged is null)
        throw new System.ArgumentNullException(nameof(referenceTagged));

      if (!tag.HasLeader)
        throw new System.InvalidOperationException("The tag does not have a leader.");

      if (tag.GetTaggedReference() is Reference reference)
      {
        if (!Equals(tag.Document, reference, referenceTagged))
          throw new System.ArgumentException(nameof(referenceTagged));
      }

      tag.LeaderElbow = pntElbow;
    }

    public static void SetLeaderEnd(this IndependentTag tag, Reference referenceTagged, XYZ pntEnd)
    {
      if (referenceTagged is null)
        throw new System.ArgumentNullException(nameof(referenceTagged));

      if (!tag.HasLeader)
        throw new System.InvalidOperationException("The tag does not have a leader.");

      if (tag.GetTaggedReference() is Reference reference)
      {
        if (!Equals(tag.Document, reference, referenceTagged))
          throw new System.ArgumentException(nameof(referenceTagged));
      }

      tag.LeaderEnd = pntEnd;
    }
#endif

    static bool Equals(Document doc, Reference a, Reference b)
    {
#if REVIT_2018
      return a.EqualTo(b); 
#else
      return a.ConvertToStableRepresentation(doc) == b.ConvertToStableRepresentation(doc);
#endif
    }
  }
}
