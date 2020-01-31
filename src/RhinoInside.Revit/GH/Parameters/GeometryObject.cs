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
  public abstract class ElementIdGeometryParam<X, R> : ElementIdParam<X, R>, IGH_PreviewObject
    where X : class, Types.IGH_ElementId, IGH_PreviewData
  {
    protected ElementIdGeometryParam(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory) { }

    #region IGH_PreviewObject
    bool IGH_PreviewObject.Hidden { get; set; }
    bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;
    BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion
  }

  public abstract class GeometricElementT<T, R> :
    ElementIdGeometryParam<T, R>,
    ISelectionFilter
    where T : Types.GeometricElement, new()
  {
    protected GeometricElementT(string name, string nickname, string description, string category, string subcategory) :
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

      switch(uiDocument.PickObjects(out var references, ObjectType.LinkedElement, this))
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

      switch(uiDocument.PickObjects(out var references, ObjectType.Element, this, null, activeElements))
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

  public class GeometricElement : GeometricElementT<Types.GeometricElement, DB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("EF607C2A-2F44-43F4-9C39-369CE114B51F");

    public GeometricElement() : base("Geometric Element", "Geometric Element", "Represents a Revit document geometric element.", "Params", "Revit") { }

    protected override Types.GeometricElement PreferredCast(object data)
    {
      return data is DB.Element element && AllowElement(element) ?
             new Types.GeometricElement(element) :
             null;
    }

    public override bool AllowElement(DB.Element elem) => Types.GeometricElement.IsValidElement(elem);
  }

  public class Vertex : ElementIdGeometryParam<Types.Vertex, DB.Point>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("BC1B160A-DC04-4139-AB7D-1AECBDE7FF88");
    public Vertex() : base("Vertex", "Vertex", "Represents a Revit vertex.", "Params", "Revit") { }

#region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Vertex> value)
    {
      using (new ModalForm.EditScope())
      {
        var values = new List<Types.Vertex>();
        Types.Vertex vertex = null;
        while(Prompt_Singular(ref vertex) == GH_GetterResult.success)
          values.Add(vertex);

        if (values.Count > 0)
        {
          value = values;
          return GH_GetterResult.success;
        }
      }

      return GH_GetterResult.cancel;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Vertex value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Edge, "Click on an edge near an end to select a vertex, TAB for alternates, ESC quit."))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          var element = uiDocument.Document.GetElement(reference);
          if (element?.GetGeometryObjectFromReference(reference) is DB.Edge edge)
          {
            var curve = edge.AsCurve();
            var result = curve.Project(reference.GlobalPoint);
            var points = new DB.XYZ[] { curve.GetEndPoint(0), curve.GetEndPoint(1) };
            int index = result.XYZPoint.DistanceTo(points[0]) < result.XYZPoint.DistanceTo(points[1]) ? 0 : 1;

            value = new Types.Vertex(uiDocument.Document, reference, index);
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
#endregion
  }

  public class Edge : ElementIdGeometryParam<Types.Edge, DB.Edge>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("B79FD0FD-63AE-4776-A0A7-6392A3A58B0D");
    public Edge() : base("Edge", "Edge", "Represents a Revit edge.", "Params", "Revit") { }

#region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Edge> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch(uiDocument.PickObjects(out var references, ObjectType.Edge))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = references.Select((x) => new Types.Edge(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Edge value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Edge))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new Types.Edge(uiDocument.Document, reference);
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
#endregion
  }

  public class Face : ElementIdGeometryParam<Types.Face, DB.Face>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("759700ED-BC79-4986-A6AB-84921A7C9293");
    public Face() : base("Face", "Face", "Represents a Revit face.", "Params", "Revit") { }

#region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Face> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObjects(out var references, ObjectType.Face))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = references.Select((x) => new Types.Face(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Face value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Face))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new Types.Face(uiDocument.Document, reference);
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
#endregion
  }
}
