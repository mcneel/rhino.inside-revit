using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RhinoInside.Revit.External.UI.Selection
{
  using External.ApplicationServices.Extensions;
  using External.DB;
  using External.DB.Extensions;

  public static class Selection
  {
    private static Result Pick<TResult>(UIApplication uiApplication, out TResult value, Func<TResult> picker)
    {
      using (new EditScope(uiApplication))
      {
        value = default;
        try { value = HostedApplication.Active.InvokeInHostContext(picker); }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
        catch (Exception) { return Result.Failed; }
        return Result.Succeeded;
      }
    }

    #region Selection
    public static void ResetSelection(this UIDocument doc)
    {
      try
      {
        using (var selection = doc.Selection)
#if REVIT_2023
          selection.SetReferences(selection.GetReferences());
#else
          selection.SetElementIds(selection.GetElementIds());
#endif
      }
      catch { }
    }

    public static IList<Reference> GetSelection(this UIDocument doc)
    {
      using (var selection = doc.Selection)
      {
#if REVIT_2023
        return selection.GetReferences();
#else
        using (var document = doc.Document)
          return selection.GetElementIds().Select(x => new Reference(document.GetElement(x))).ToArray();
#endif
      }
    }

    public static void SetSelection(this UIDocument doc, IList<Reference> references)
    {
      using (var selection = doc.Selection)
      {
#if REVIT_2023
        selection.SetReferences(references);
#else
        using (var document = doc.Document)
          selection.SetElementIds
          (
            references.
            Where(x => x.LinkedElementId == ElementIdExtension.Invalid && x.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_NONE).
            Select(x => x.ElementId).
            ToArray()
          );
#endif
      }
    }
    #endregion

    #region PickObject
    public static Result PickObject(this UIDocument doc, out Reference reference, ObjectType objectType)
    {
      return Pick(doc.Application, out reference, () => doc.Selection.PickObject(objectType));
    }

    public static Result PickObject(this UIDocument doc, out Reference reference, ObjectType objectType, string statusPrompt)
    {
      return Pick(doc.Application, out reference, () => doc.Selection.PickObject(objectType, statusPrompt));
    }

    public static Result PickObject(this UIDocument doc, out Reference reference, ObjectType objectType, ISelectionFilter selectionFilter)
    {
      return Pick(doc.Application, out reference, () => doc.Selection.PickObject(objectType, selectionFilter));
    }

    public static Result PickObject(this UIDocument doc, out Reference reference, ObjectType objectType, ISelectionFilter selectionFilter, string statusPrompt)
    {
      return Pick(doc.Application, out reference, () => doc.Selection.PickObject(objectType, selectionFilter, statusPrompt));
    }
    #endregion

    #region PickObjects
    public static Result PickObjects(this UIDocument doc, out IList<Reference> references, ObjectType objectType)
    {
      return Pick(doc.Application, out references, () => doc.Selection.PickObjects(objectType));
    }

    public static Result PickObjects(this UIDocument doc, out IList<Reference> references, ObjectType objectType, string statusPrompt)
    {
      return Pick(doc.Application, out references, () => doc.Selection.PickObjects(objectType, statusPrompt));
    }

    public static Result PickObjects(this UIDocument doc, out IList<Reference> references, ObjectType objectType, ISelectionFilter selectionFilter)
    {
      return Pick(doc.Application, out references, () => doc.Selection.PickObjects(objectType, selectionFilter));
    }

    public static Result PickObjects(this UIDocument doc, out IList<Reference> references, ObjectType objectType, ISelectionFilter selectionFilter, string statusPrompt)
    {
      return Pick(doc.Application, out references, () => doc.Selection.PickObjects(objectType, selectionFilter, statusPrompt));
    }

    public static Result PickObjects(this UIDocument doc, out IList<Reference> references, ObjectType objectType, ISelectionFilter selectionFilter, string statusPrompt, IList<Reference> pPreSelected)
    {
      return Pick(doc.Application, out references, () => doc.Selection.PickObjects(objectType, selectionFilter, statusPrompt, pPreSelected));
    }

    public static Result PickObjects(this UIDocument uiDocument, out IList<Reference> references, ISelectionFilter selectionFilter, string statusPrompt = null)
    {
      var uiResult = Result.Failed;
      references = default;

      (uiResult, references) = HostedApplication.Active.InvokeInHostContext(() =>
      {
        using (new EditScope(uiDocument.Application))
        {
          var views = uiDocument.GetOpenUIViews().
            Select(x => uiDocument.Document.GetElement(x.ViewId) as View).
            Where(x => x.AreGraphicsOverridesAllowed());

          var _values = new List<Reference>();
          var document = uiDocument.Document;
          var listSelectionFilter = new SelectionSetFilter(selectionFilter);
          using (var group = new TransactionGroup(document, "Pick Elements"))
          {
            group.Start();

            using (var overrides = GetHighlightGraphicSettings(document))
            {
              try
              {
                while (true)
                {
                  if (_values.LastOrDefault() is Reference value)
                  {
                    foreach (var view in views)
                    {
                      using (var scope = document.CommitScope())
                      {
                        view.SetElementOverrides(value.ElementId, overrides);
                        scope.Commit();
                      }
                    }
                  }

                  try
                  {
                    var reference = string.IsNullOrEmpty(statusPrompt) ?
                      uiDocument.Selection.PickObject(ObjectType.Element, listSelectionFilter) :
                      uiDocument.Selection.PickObject(ObjectType.Element, listSelectionFilter, statusPrompt);

                    _values.Add(reference);
                    listSelectionFilter.AddFilteredElementId(reference.ElementId);
                  }
                  catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                  {
                    return (_values.Count > 0 ? Result.Succeeded : Result.Cancelled, _values);
                  }
                }
              }
              catch { return (Result.Failed, default(List<Reference>)); }
              finally
              {
                try { group.RollBack(); }
                catch { }
              }
            }
          }
        }
      });

      return uiResult;
    }

    static OverrideGraphicSettings GetHighlightGraphicSettings(Document document)
    {
      using (var collector = new FilteredElementCollector(document))
      {
        var colors = document.Application.GetColorSettings();
        var highlight = colors.SelectionColor;
        var transparency = colors.SelectionSemitransparent ? 50 : 0;

        var patternId = collector.
          OfClass(typeof(FillPatternElement)).
          Cast<FillPatternElement>().
          FirstOrDefault
          (
            x =>
            {
              using (var pattern = x.GetFillPattern())
                return pattern.Target == FillPatternTarget.Drafting && pattern.IsSolidFill;
            }
          )?.
          Id ?? ElementIdExtension.Invalid;

        return new OverrideGraphicSettings().
          SetProjectionLineColor(highlight).
          SetSurfaceForegroundPatternId(patternId).
          SetSurfaceForegroundPatternColor(highlight).
          SetSurfaceTransparency(transparency).
          SetCutForegroundPatternId(patternId).
          SetCutForegroundPatternColor(highlight);
      }
    }

    class SelectionSetFilter : ISelectionFilter
    {
      readonly HashSet<ElementId> ElementIds = new HashSet<ElementId>(default(ElementIdEqualityComparer));
      readonly ISelectionFilter PreviousFilter;

      public SelectionSetFilter(ISelectionFilter previousFilter) => PreviousFilter = previousFilter;

      public bool AllowElement(Element elem)
      {
        return PreviousFilter.AllowElement(elem) && !ElementIds.Contains(elem.Id);
      }

      public bool AllowReference(Reference reference, XYZ position)
      {
        return PreviousFilter.AllowReference(reference, position) && !ElementIds.Contains(reference.ElementId);
      }

      public void AddFilteredElementId(ElementId elementId)
      {
        ElementIds.Add(elementId);
      }
    }
    #endregion

    #region Elements
    public static Result PickElementsByRectangle(this UIDocument doc, out IList<Element> elements, ISelectionFilter selectionFilter, string statusPrompt = null)
    {
      return Pick
      (
        doc.Application, out elements,
        () => string.IsNullOrEmpty(statusPrompt) ?
        doc.Selection.PickElementsByRectangle(selectionFilter) :
        doc.Selection.PickElementsByRectangle(selectionFilter, statusPrompt)
      );
    }
    #endregion

    #region PickPoint
    public static Result PickPoint(this UIDocument doc, out XYZ point, string statusPrompt = null)
    {
      return Pick(doc.Application, out point, () => statusPrompt is object ? doc.Selection.PickPoint(GetObjectSnapTypes(doc), statusPrompt) : doc.Selection.PickPoint(GetObjectSnapTypes(doc)));
    }

    public static Result PickPoint(this UIDocument doc, out XYZ point, ObjectSnapTypes objectSnapTypes, string statusPrompt = null)
    {
      return Pick(doc.Application, out point, () => statusPrompt is object ? doc.Selection.PickPoint(objectSnapTypes, statusPrompt) : doc.Selection.PickPoint(objectSnapTypes));
    }
    #endregion

    #region PickPoints
    public static Result PickPoints(this UIDocument doc, out IList<XYZ> points, string statusPrompt = null)
    {
      return PickPoints(doc, out points, GetObjectSnapTypes(doc), statusPrompt);
    }

    public static Result PickPoints(this UIDocument doc, out IList<XYZ> points, ObjectSnapTypes objectSnapTypes, string statusPrompt = null)
    {
      return Pick
      (
        doc.Application, out points, () =>
        {
          var ds = default(DirectShape);
          using (var group = new TransactionGroup(doc.Document, "Pick Points"))
          {
            group.Start();

            try
            {
              var values = new List<XYZ>();
              while (true)
              {
                try
                {
                  var xyz = statusPrompt is object ? doc.Selection.PickPoint(objectSnapTypes, statusPrompt) : doc.Selection.PickPoint(objectSnapTypes);
                  using (var scope = doc.Document.CommitScope())
                  {
                    ds = ds ?? DirectShape.CreateElement(doc.Document, new ElementId(BuiltInCategory.OST_GenericModel));
                    ds.AppendShape(new GeometryObject[] { Point.Create(xyz) });
                    //if (values.LastOrDefault() is XYZ last)
                    //{
                    //  var line = Line.CreateBound(last, xyz);
                    //  if (doc.Document.GetCategory(BuiltInCategory.OST_GenericModelHiddenLines) is Category category)
                    //    line.SetGraphicsStyleId(category.GetGraphicsStyle(GraphicsStyleType.Projection).Id);

                    //  ds.AppendShape(new GeometryObject[] { line });
                    //}
                    scope.Commit();
                  }
                  values.Add(xyz);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException) { if (values.Count == 0) throw; else return values; }
              }
            }
            finally { group.RollBack(); }
          }
        }
     );
    }
    #endregion

    internal static ObjectSnapTypes GetObjectSnapTypes(UIDocument doc)
    {
      var snapTypes = default(ObjectSnapTypes);

      var app = doc.Application.Application;

      if (!app.TryGetProfileValue("Snapping", "SnapToEndPoints", out var SnapToEndPoints) || int.TryParse(SnapToEndPoints, out var snapToEndPoints) && snapToEndPoints != 0)
        snapTypes |= ObjectSnapTypes.Endpoints;

      if (!app.TryGetProfileValue("Snapping", "SnapToMiddle", out var SnapToMiddle) || int.TryParse(SnapToMiddle, out var snapToMiddle) && snapToMiddle != 0)
        snapTypes |= ObjectSnapTypes.Midpoints;

      if (!app.TryGetProfileValue("Snapping", "SnapToNearest", out var SnapToNearest) || int.TryParse(SnapToNearest, out var snapToNearest) && snapToNearest != 0)
        snapTypes |= ObjectSnapTypes.Nearest;

      if (!app.TryGetProfileValue("Snapping", "SnapToWorkPlaneGrid", out var SnapToWorkPlaneGrid) || int.TryParse(SnapToWorkPlaneGrid, out var snapToWorkPlaneGrid) && snapToWorkPlaneGrid != 0)
        snapTypes |= ObjectSnapTypes.WorkPlaneGrid;

      if (!app.TryGetProfileValue("Snapping", "SnapToIntersections", out var SnapToIntersections) || int.TryParse(SnapToIntersections, out var snapToIntersections) && snapToIntersections != 0)
        snapTypes |= ObjectSnapTypes.Intersections;

      if (!app.TryGetProfileValue("Snapping", "SnapToCenters", out var SnapToCenters) || int.TryParse(SnapToCenters, out var snapToCenters) && snapToCenters != 0)
        snapTypes |= ObjectSnapTypes.Centers;

      if (!app.TryGetProfileValue("Snapping", "SnapToPerpendicular", out var SnapToPerpendicular) || int.TryParse(SnapToPerpendicular, out var snapToPerpendicular) && snapToPerpendicular != 0)
        snapTypes |= ObjectSnapTypes.Perpendicular;

      if (!app.TryGetProfileValue("Snapping", "SnapToTangents", out var SnapToTangents) || int.TryParse(SnapToTangents, out var snapToTangents) && snapToTangents != 0)
        snapTypes |= ObjectSnapTypes.Tangents;

      if (!app.TryGetProfileValue("Snapping", "SnapToQuadrants", out var SnapToQuadrants) || int.TryParse(SnapToQuadrants, out var snapToQuadrants) && snapToQuadrants != 0)
        snapTypes |= ObjectSnapTypes.Quadrants;

      if (!app.TryGetProfileValue("Snapping", "SnapToPoints", out var SnapToPoints) || int.TryParse(SnapToPoints, out var snapToPoints) && snapToPoints != 0)
        snapTypes |= ObjectSnapTypes.Points;

      return snapTypes;
    }

    internal static IDisposable NoSelectionScope(this Document document)
    {
      return UI.Selection.NoSelectionScope.Documents.Contains(document) ? default : new NoSelectionScope(document);
    }
  }

  class NoSelectionScope : IDisposable
  {
    static readonly HashSet<Document> _Documents = new HashSet<Document>();
    public static IReadOnlyCollection<Document> Documents => _Documents;

    UIDocument UIDocument;
    ICollection<ElementId> ElementIds;

    public NoSelectionScope(Document document) => HostedApplication.Active.InvokeInHostContext(() =>
    {
      UIDocument = new UIDocument(document);
      ElementIds = UIDocument.Selection.GetElementIds();
      if (ElementIds.Count > 0) UIDocument.Selection.SetElementIds(ElementIdExtension.EmptySet);

      _Documents.Add(document);
    });

    public void Dispose() => HostedApplication.Active.InvokeInHostContext(() =>
    {
      using (UIDocument)
      {
        if (UIDocument.IsValidObject)
        {
          _Documents.Remove(UIDocument.Document);

          /*if (ElementIds.Count > 0) */
          UIDocument.Selection.SetElementIds(ElementIds);
          UIDocument = default;
        }
        else
        {
          foreach(var document in _Documents.ToArray())
            if (!document.IsValidObject) _Documents.Remove(document);
        }
      }
    });
  }
}

#if !REVIT_2023
namespace Autodesk.Revit.UI.Events
{
  using System.Linq;

  public class SelectionChangedEventArgs : EventArgs, IDisposable
  {
    internal SelectionChangedEventArgs(Document doc, ICollection<ElementId> selectedElements)
    {
      document = doc;
      selectionSet = new SortedSet<ElementId>(selectedElements, RhinoInside.Revit.External.DB.Extensions.ElementIdComparer.NoNullsAscending);
    }

    #region RevitAPIEventArgs
    public void Dispose() => IsValidObject = false;
    public bool IsValidObject { get; private set; } = true;

    public bool Cancellable => false;
    public bool IsCancelled() => false;
    #endregion

    readonly Document document;
    public Document GetDocument() => document;

    readonly ISet<ElementId> selectionSet;
    public ISet<ElementId> GetSelectedElements() => selectionSet;

    public IList<Reference> GetReferences() => selectionSet.Select(x => new Reference(document.GetElement(x))).ToList();
  }
}
#endif
