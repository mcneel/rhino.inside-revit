using System;

namespace RhinoInside.Revit.UI
{
  using System.Collections.Generic;
  using System.Linq;
  using Autodesk.Revit.DB;
  using Autodesk.Revit.UI;
  using Autodesk.Revit.UI.Selection;
  using External.UI.Selection;

  public static class RevitEditor
  {
    public static Result PickObject(out Reference reference, ObjectType objectType)
    {
      var pointOnElement = typeof(PointElementReference);
      var edge = typeof(Edge);
      var face = typeof(Face);
      var linkedElementId = typeof(LinkElementId);
      var elementId = typeof(ElementId);
      var subElement = typeof(Subelement);

      return Revit.ActiveUIDocument.PickObject(out reference, objectType);
    }

  }

  public enum GetResult
  {
    Fail = int.MinValue,
    Cancel  = -1,
    Nothing =  0,
    Succeed =  1,
  }

  public class GetElement : ISelectionFilter
  {
    public GetElement() { }

    string prompt;
    public void SetCommandPrompt(string prompt) => this.prompt = prompt;

    public bool AlreadySelectedObjectSelect { get; set; }
    public bool ObjectsWerePreselected      { get; private set; }
    public bool DeselectAllBeforePostSelect { get; set; }
    public bool GroupSelect                 { get; set; } = false;
    public bool ClearPickListOnEntry        { get; set; } = true;
    public bool OneByOnePostSelect          { get; set; } = false;

    public void AppendToPickList(Reference reference)
    {
      if (reference is null) throw new ArgumentNullException(nameof(reference));
      if (AllowReference(reference)) PickList.Add(reference);
    }

    public GetResult Get()
    {
      if (AlreadySelectedObjectSelect) Revit.ActiveUIDocument.Selection.GetElementIds();
      if (ClearPickListOnEntry) PickList.Clear();

      switch (Revit.ActiveUIDocument.PickObject(out var value, ObjectType.Element))
      {
        default:
        case Result.Failed:    PickList = new List<Reference>(); return GetResult.Fail;
        case Result.Cancelled: PickList = new List<Reference>(); return GetResult.Cancel;
        case Result.Succeeded: PickList = new List<Reference> { value }; return GetResult.Succeed;
      }
    }

    public GetResult GetMultiple()
    {
      //Rhino.Input.Custom.GetObject g;
      if (ClearPickListOnEntry) PickList.Clear();

      var result = Result.Failed;
      if (OneByOnePostSelect)
      {
        result = Revit.ActiveUIDocument.PickObjects
        (
          out PickList,
          ObjectType.Element,
          this,
          prompt ?? "Pick Objects",
          PickList
        );
      }
      else
      {
        result = Revit.ActiveUIDocument.PickElementsByRectangle
        (
          out var elements,
          this,
          prompt ?? "Pick Objects"
        );

        PickList = elements.Select(x => new Reference(x)).ToList();
      }

      switch (result)
      {
        default:
        case Result.Failed:    PickList.Clear(); return GetResult.Fail;
        case Result.Cancelled: PickList.Clear(); return GetResult.Cancel;
        case Result.Succeeded: return GetResult.Succeed;
      }
    }

    bool AllowReference(Reference reference)
    {
      if (Revit.ActiveUIDocument.Document.GetElement(reference.ElementId) is Element element)
        return (this as ISelectionFilter).AllowElement(element);

      return false;
    }

    bool ISelectionFilter.AllowElement(Element elem)
    {
      return GroupSelect || !(elem is Group);
    }

    bool ISelectionFilter.AllowReference(Reference reference, XYZ position)
    {
      return true;
    }

    IList<Reference> PickList = new List<Reference>();
    public IList<Reference> References =>
      new System.Collections.ObjectModel.ReadOnlyCollection<Reference>(PickList);
  }
}
