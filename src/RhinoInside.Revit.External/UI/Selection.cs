using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RhinoInside.Revit.External.UI.Selection
{
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
  }
}

#if !REVIT_2023
namespace Autodesk.Revit.UI.Events
{
  public class SelectionChangeEventArgs : EventArgs, IDisposable
  {
    internal SelectionChangeEventArgs(Document doc, ICollection<ElementId> selectedElements)
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
