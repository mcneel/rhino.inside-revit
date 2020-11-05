using System;
using System.Collections.Generic;
using System.Linq;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  /// <summary>
  /// Interface that represents any <see cref="DB.Element"/> that is a geometric element but is also in a category.
  /// </summary>
  [Kernel.Attributes.Name("Instance")]
  public interface IGH_InstanceElement : IGH_GeometricElement
  {
    Level Level { get; }
  }

  [Kernel.Attributes.Name("Instance")]
  public class InstanceElement : GeometricElement, IGH_InstanceElement
  {
    public InstanceElement() { }
    public InstanceElement(DB.Element element) : base(element) { }

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      if (element.Category is null)
        return false;

      return GeometricElement.IsValidElement(element);
    }

    public override Level Level => (Value is DB.Element element) ?
      new Level(element.Document, element.LevelId) :
      default;

    #region Joins
    public virtual bool? IsJoinAllowedAtStart
    {
      get => default;
      set { if (value is object) throw new InvalidOperationException("Join at start is not valid for this elemenmt."); }
    }
    public virtual bool? IsJoinAllowedAtEnd
    {
      get => default;
      set { if (value is object) throw new InvalidOperationException("Join at end is not valid for this elemenmt."); }
    }

    HashSet<DB.Element> GetJoinedElements()
    {
      bool IsJoinedTo(DB.Element element, DB.ElementId id)
      {
        if (element.Location is DB.LocationCurve elementLocation)
        {
          for (int i = 0; i < 2; i++)
          {
            foreach (var joinned in elementLocation.get_ElementsAtJoin(i).Cast<DB.Element>())
            {
              if (joinned.Id == id)
                return true;
            }
          }
        }

        return false;
      }

      var result = new HashSet<DB.Element>(ElementEqualityComparer.SameDocument);

      if (Value.Location is DB.LocationCurve valueLocation)
      {
        // Get joins at ends
        for (int i = 0; i < 2; i++)
        {
          foreach (var join in valueLocation.get_ElementsAtJoin(i).Cast<DB.Element>())
          {
            if (join.Id != Id)
              result.Add(join);
          }
        }

        // Find joins at mid
        using (var collector = new DB.FilteredElementCollector(Document))
        {
          var elementCollector = collector.OfClass(Value.GetType()).OfCategoryId(Value.Category.Id).
            WherePasses(new DB.BoundingBoxIntersectsFilter(BoundingBox.ToOutline()));

          foreach (var element in elementCollector)
          {
            if (!result.Contains(element) && element.Id != Id && IsJoinedTo(element, Id))
              result.Add(element);
          }
        }
      }

      return result;
    }

    class DisableJoinsDisposable : IDisposable
    {
      readonly List<(InstanceElement, bool?, bool?)> items = new List<(InstanceElement, bool?, bool?)>();

      internal DisableJoinsDisposable(InstanceElement e)
      {
        foreach (var joinElement in e.GetJoinedElements())
        {
          if (GraphicalElement.FromElement(joinElement) is InstanceElement join)
          {
            var start = join.IsJoinAllowedAtStart;
            var end = join.IsJoinAllowedAtEnd;
            if (start.HasValue || end.HasValue)
            {
              if (start.HasValue) join.IsJoinAllowedAtStart = false;
              if (end.HasValue) join.IsJoinAllowedAtEnd = false;
              items.Add((join, start, end));
            }
          }
        }

        {
          var start = e.IsJoinAllowedAtStart;
          var end = e.IsJoinAllowedAtEnd;
          if (start.HasValue || end.HasValue)
          {
            if (start.HasValue) e.IsJoinAllowedAtStart = false;
            if (end.HasValue) e.IsJoinAllowedAtEnd = false;
            items.Add((e, start, end));
          }
        }
      }

      void IDisposable.Dispose()
      {
        foreach (var item in items)
        {
          item.Item1.IsJoinAllowedAtStart = item.Item2;
          item.Item1.IsJoinAllowedAtEnd = item.Item3;
        }
      }
    }

    /// <summary>
    /// Disables this element joins until returned <see cref="IDisposable"/> is disposed.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that should be disposed to restore this element joins state.</returns>
    public IDisposable DisableJoinsScope() => new DisableJoinsDisposable(this);
    #endregion
  }
}
