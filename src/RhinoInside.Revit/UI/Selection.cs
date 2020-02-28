using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RhinoInside.Revit.UI.Selection
{
  /*internal*/ public static class Selection
  {
    private static Result Pick<TResult>(out TResult value, Func<TResult> picker)
    {
      using (new ModalForm.EditScope())
      {
        value = default;
        try { value = Rhinoceros.InvokeInHostContext(picker); }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
        catch (Exception) { return Result.Failed; }
        return Result.Succeeded;
      }
    }

    public static Result PickObject(this UIDocument doc, out DB.Reference reference, ObjectType objectType)
    {
      return Pick(out reference, () => doc.Selection.PickObject(objectType));
    }

    public static Result PickObject(this UIDocument doc, out DB.Reference reference, ObjectType objectType, string statusPrompt)
    {
      return Pick(out reference, () => doc.Selection.PickObject(objectType, statusPrompt));
    }

    public static Result PickObject(this UIDocument doc, out DB.Reference reference, ObjectType objectType, ISelectionFilter selectionFilter)
    {
      return Pick(out reference, () => doc.Selection.PickObject(objectType, selectionFilter));
    }

    public static Result PickObject(this UIDocument doc, out DB.Reference reference, ObjectType objectType, ISelectionFilter selectionFilter, string statusPrompt)
    {
      return Pick(out reference, () => doc.Selection.PickObject(objectType, selectionFilter, statusPrompt));
    }

    public static Result PickObjects(this UIDocument doc, out IList<DB.Reference> reference, ObjectType objectType)
    {
      return Pick(out reference, () => doc.Selection.PickObjects(objectType));
    }

    public static Result PickObjects(this UIDocument doc, out IList<DB.Reference> reference, ObjectType objectType, string statusPrompt)
    {
      return Pick(out reference, () => doc.Selection.PickObjects(objectType, statusPrompt));
    }

    public static Result PickObjects(this UIDocument doc, out IList<DB.Reference> reference, ObjectType objectType, ISelectionFilter selectionFilter)
    {
      return Pick(out reference, () => doc.Selection.PickObjects(objectType, selectionFilter));
    }

    public static Result PickObjects(this UIDocument doc, out IList<DB.Reference> reference, ObjectType objectType, ISelectionFilter selectionFilter, string statusPrompt)
    {
      return Pick(out reference, () => doc.Selection.PickObjects(objectType, selectionFilter, statusPrompt));
    }

    public static Result PickObjects(this UIDocument doc, out IList<DB.Reference> reference, ObjectType objectType, ISelectionFilter selectionFilter, string statusPrompt, IList<DB.Reference> pPreSelected)
    {
      return Pick(out reference, () => doc.Selection.PickObjects(objectType, selectionFilter, statusPrompt, pPreSelected));
    }

    public static Result PickPoint(this UIDocument doc, out DB.XYZ point, ObjectSnapTypes snapSettings, string statusPrompt)
    {
      return Pick(out point, () => doc.Selection.PickPoint(snapSettings, statusPrompt));
    }

  }
}
