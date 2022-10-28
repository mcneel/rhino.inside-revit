using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RhinoInside.Revit.External.UI.Selection
{
  using External.ApplicationServices.Extensions;

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

    public static Result PickObjects(this UIDocument doc, out IList<Reference> reference, ObjectType objectType)
    {
      return Pick(doc.Application, out reference, () => doc.Selection.PickObjects(objectType));
    }

    public static Result PickObjects(this UIDocument doc, out IList<Reference> reference, ObjectType objectType, string statusPrompt)
    {
      return Pick(doc.Application, out reference, () => doc.Selection.PickObjects(objectType, statusPrompt));
    }

    public static Result PickObjects(this UIDocument doc, out IList<Reference> reference, ObjectType objectType, ISelectionFilter selectionFilter)
    {
      return Pick(doc.Application, out reference, () => doc.Selection.PickObjects(objectType, selectionFilter));
    }

    public static Result PickObjects(this UIDocument doc, out IList<Reference> reference, ObjectType objectType, ISelectionFilter selectionFilter, string statusPrompt)
    {
      return Pick(doc.Application, out reference, () => doc.Selection.PickObjects(objectType, selectionFilter, statusPrompt));
    }

    public static Result PickObjects(this UIDocument doc, out IList<Reference> reference, ObjectType objectType, ISelectionFilter selectionFilter, string statusPrompt, IList<Reference> pPreSelected)
    {
      return Pick(doc.Application, out reference, () => doc.Selection.PickObjects(objectType, selectionFilter, statusPrompt, pPreSelected));
    }

    public static Result PickElementsByRectangle(this UIDocument doc, out IList<Element> elements, ISelectionFilter selectionFilter, string statusPrompt)
    {
      return Pick(doc.Application, out elements, () => doc.Selection.PickElementsByRectangle(selectionFilter, statusPrompt));
    }

    public static Result PickPoint(this UIDocument doc, out XYZ point, ObjectSnapTypes snapSettings, string statusPrompt)
    {
      return Pick(doc.Application, out point, () => doc.Selection.PickPoint(snapSettings, statusPrompt));
    }

    public static Result PickPoint(this UIDocument doc, out XYZ point, string statusPrompt)
    {
      return Pick(doc.Application, out point, () => doc.Selection.PickPoint(GetObjectSnapTypes(doc), statusPrompt));
    }

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
