using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI.Selection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using RhinoInside.Revit.UI.Selection;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class GraphicalElementT<T, R> :
    ElementIdWithPreviewParam<T, R>,
    ISelectionFilter
    where T : Types.GraphicalElement, new()
  {
    protected GraphicalElementT(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    { }

    #region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalElementMenuItems(menu);
      Menu_AppendItem(menu, $"Highlight {GH_Convert.ToPlural(TypeName)}", Menu_HighlightElements, !VolatileData.IsEmpty, false);
    }

    void Menu_HighlightElements(object sender, EventArgs e)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var elementIds = ToElementIds(VolatileData).
                       Where(x => x.Document.Equals(uiDocument.Document)).
                       Select(x => x.Id);

      if (elementIds.Any())
      {
        var ids = elementIds.ToArray();

        uiDocument.Selection.SetElementIds(ids);
        uiDocument.ShowElements(ids);
      }
    }
    #endregion

    public virtual bool AllowElement(DB.Element elem) => elem is R;
    public bool AllowReference(DB.Reference reference, DB.XYZ position)
    {
      if (reference.ElementReferenceType == DB.ElementReferenceType.REFERENCE_TYPE_NONE)
        return AllowElement(Revit.ActiveUIDocument.Document.GetElement(reference));

      return false;
    }

    protected override GH_GetterResult Prompt_Singular(ref T value)
    {
#if REVIT_2018
      const ObjectType objectType = ObjectType.Subelement;
#else
      const ObjectType objectType = ObjectType.Element;
#endif

      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, objectType, this))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = (T) Types.Element.FromElementId(uiDocument.Document, reference.ElementId);
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected GH_GetterResult Prompt_SingularLinked(ref GH_Structure<T> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;

      switch (uiDocument.PickObject(out var reference, ObjectType.LinkedElement, this))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new GH_Structure<T>();

          if (doc.GetElement(reference.ElementId) is DB.RevitLinkInstance instance)
          {
            var linkedDoc = instance.GetLinkDocument();
            var element = (T) Types.Element.FromElementId(linkedDoc, reference.LinkedElementId);
            value.Append(element, new GH_Path(0));

            return GH_GetterResult.success;
          }
          break;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected override GH_GetterResult Prompt_Plural(ref List<T> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;
      var selection = uiDocument.Selection.GetElementIds();
      if (selection?.Count > 0)
      {
        value = selection.Select(id => doc.GetElement(id)).Where(element => AllowElement(element)).
                Select(element => (T) Types.Element.FromElementId(element.Document, element.Id)).ToList();

        return GH_GetterResult.success;
      }
      else
      {
        switch (uiDocument.PickObjects(out var references, ObjectType.Element, this))
        {
          case Autodesk.Revit.UI.Result.Succeeded:
            value = references.Select(r => (T) Types.Element.FromElementId(doc, r.ElementId)).ToList();
            return GH_GetterResult.success;
          case Autodesk.Revit.UI.Result.Cancelled:
            return GH_GetterResult.cancel;
        }
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected GH_GetterResult Prompt_PluralLinked(ref GH_Structure<T> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;

      switch (uiDocument.PickObjects(out var references, ObjectType.LinkedElement, this))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new GH_Structure<T>();

          var groups = references.Select
          (
            r =>
            {
              var instance = doc.GetElement(r.ElementId) as DB.RevitLinkInstance;
              var linkedDoc = instance.GetLinkDocument();

              return (T) Types.Element.FromElementId(linkedDoc, r.LinkedElementId);
            }
          ).GroupBy(x => x.Document);

          int index = 0;
          foreach (var group in groups)
            value.AppendRange(group, new GH_Path(index));

          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected GH_GetterResult Prompt_More(ref GH_Structure<T> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;
      var docGUID = doc.GetFingerprintGUID();

      var documents = value.AllData(true).OfType<T>().GroupBy(x => x.DocumentGUID);
      var activeElements = documents.Where(x => x.Key == docGUID).
                           SelectMany(x => x).
                           Select
                           (
                             x =>
                             {
                               try { return DB.Reference.ParseFromStableRepresentation(doc, x.UniqueID); }
                               catch (Autodesk.Revit.Exceptions.ArgumentException) { return null; }
                             }
                           ).
                           Where(x => x is object).
                           ToArray();

      switch (uiDocument.PickObjects(out var references, ObjectType.Element, this, null, activeElements))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new GH_Structure<T>();

          int index = 0;
          foreach (var document in documents)
          {
            if (document.Key == docGUID)
              continue;

            var path = new GH_Path(index++);
            value.AppendRange(document, path);
          }

          value.AppendRange(references.Select(r => (T) Types.Element.FromElementId(doc, r.ElementId)), new GH_Path(index));
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      base.Menu_AppendPromptOne(menu);

      Menu_AppendItem(menu, $"Set one linked {TypeName}", Menu_PromptOneLinked, SourceCount == 0, false);
    }

    protected override void Menu_AppendPromptMore(ToolStripDropDown menu)
    {
      var name_plural = GH_Convert.ToPlural(TypeName);

      base.Menu_AppendPromptMore(menu);

      Menu_AppendItem(menu, $"Set Multiple linked {name_plural}", Menu_PromptPluralLinked, SourceCount == 0);
      Menu_AppendSeparator(menu);

      Menu_AppendItem(menu, $"Change {name_plural} collection", Menu_PromptMore, SourceCount == 0, false);
    }

    private void Menu_PromptOneLinked(object sender, EventArgs e)
    {
      try
      {
        PrepareForPrompt();
        var data = PersistentData;
        if (Prompt_SingularLinked(ref data) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          PersistentData.Clear();
          if (data is object)
            PersistentData.MergeStructure(data);

          OnObjectChanged(GH_ObjectEventType.PersistentData);
        }
      }
      finally
      {
        RecoverFromPrompt();
        ExpireSolution(true);
      }
    }

    private void Menu_PromptPluralLinked(object sender, EventArgs e)
    {
      try
      {
        PrepareForPrompt();
        var data = PersistentData;
        if (Prompt_PluralLinked(ref data) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          PersistentData.Clear();
          if (data is object)
            PersistentData.MergeStructure(data);

          OnObjectChanged(GH_ObjectEventType.PersistentData);
        }
      }
      finally
      {
        RecoverFromPrompt();
        ExpireSolution(true);
      }
    }

    private void Menu_PromptMore(object sender, EventArgs e)
    {
      try
      {
        PrepareForPrompt();
        var data = PersistentData;
        if (Prompt_More(ref data) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          PersistentData.Clear();
          if (data is object)
            PersistentData.MergeStructure(data);

          OnObjectChanged(GH_ObjectEventType.PersistentData);
        }
      }
      finally
      {
        RecoverFromPrompt();
        ExpireSolution(true);
      }
    }
  }

  public class GraphicalElement : GraphicalElementT<Types.GraphicalElement, DB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("EF607C2A-2F44-43F4-9C39-369CE114B51F");

    public GraphicalElement() : base("Graphical Element", "Graphical Element", "Represents a Revit graphical element.", "Params", "Revit") { }

    protected override Types.GraphicalElement PreferredCast(object data)
    {
      return data is DB.Element element && AllowElement(element) ?
             Types.GraphicalElement.FromElement(element) as Types.GraphicalElement:
             null;
    }

    public override bool AllowElement(DB.Element elem) => Types.GraphicalElement.IsValidElement(elem);
  }
}
