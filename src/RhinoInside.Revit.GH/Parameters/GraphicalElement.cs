using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI.Selection;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Display;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;
  using External.UI.Selection;

  public abstract class GraphicalElementT<T, R> :
    Element<T, R>,
    IGH_PreviewObject,
    ISelectionFilter
    where T : class, Types.IGH_GraphicalElement
    where R : ARDB.Element
  {
    protected GraphicalElementT(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    {
      ObjectChanged += OnObjectChanged;
    }

    protected override T PreferredCast(object data)
    {
      return data is ARDB.Element element && AllowElement(element) ?
             Types.GraphicalElement.FromElement(element) as T :
             null;
    }

    #region IGH_PreviewObject
    bool IGH_PreviewObject.Hidden { get; set; }
    bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;
    BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion

    #region ISelectionFilter
    public virtual bool AllowElement(ARDB.Element elem) => elem is R && Types.GraphicalElement.IsValidElement(elem);
    public bool AllowReference(ARDB.Reference reference, ARDB.XYZ position)
    {
      if (reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_NONE)
        return AllowElement(Revit.ActiveUIDocument.Document.GetElement(reference));

      return false;
    }
    #endregion

    #region UI methods
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Point", "Vector", "Plane", "Box", "Rectangle" /*, "Transform" */}
    );

    protected override GH_GetterResult Prompt_Singular(ref T value)
    {
      const ObjectType objectType = ObjectType.Element;

      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, objectType, this))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = Types.Element.FromReference(uiDocument.Document, reference) as T;
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
          if (Types.Element.FromReference(doc, reference) is T element)
          {
            value = new GH_Structure<T>();
            value.Append(element);
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
                Select(element => Types.Element.FromElementId(element.Document, element.Id) as T).ToList();

        return GH_GetterResult.success;
      }
      else
      {
        switch (uiDocument.PickObjects(out var references, ObjectType.Element, this))
        {
          case Autodesk.Revit.UI.Result.Succeeded:
            value = references.Select(r => Types.Element.FromReference(doc, r) as T).ToList();
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

          var groups = references.
            Select(r => Types.Element.FromReference(doc, r) as T).
            GroupBy(x => x.Document);

          int index = 0;
          foreach (var group in groups)
            value.AppendRange(group, new GH_Path(index++));

          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    class LinkedElementSelectionFilter : ISelectionFilter
    {
      readonly ISelectionFilter SelectionFilter;
      public LinkedElementSelectionFilter(ISelectionFilter filter) => SelectionFilter = filter;

      public bool AllowElement(ARDB.Element elem) => elem is ARDB.RevitLinkInstance;
      public bool AllowReference(ARDB.Reference reference, ARDB.XYZ position)
      {
        if (reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_NONE)
        {
          if (Revit.ActiveUIDocument.Document.GetElement(reference) is ARDB.RevitLinkInstance link)
            return SelectionFilter.AllowElement(link.GetLinkDocument().GetElement(reference.LinkedElementId));
        }

        return false;
      }
    }

    protected GH_GetterResult Prompt_Elements(ref GH_Structure<T> value, ObjectType objectType, bool multiple, bool preSelect)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;
      var docGUID = doc.GetFingerprintGUID();

      var documents = value.AllData(true).OfType<T>().GroupBy(x => x.DocumentGUID);
      var activeElements = (
                            preSelect ?
                            documents.Where(x => x.Key == docGUID).
                            SelectMany(x => x).
                            Where(x => x.IsValid).
                            Select(x => x.Reference).
                            OfType<ARDB.Reference>() :
                            Enumerable.Empty<ARDB.Reference>()
                           ).
                           ToArray();

      var result = Autodesk.Revit.UI.Result.Failed;
      var references = default(IList<ARDB.Reference>);
      {
        var selectionFilter = objectType == ObjectType.LinkedElement ?
          new LinkedElementSelectionFilter(this) :
          (ISelectionFilter) this;

        if (multiple)
        {
          if(preSelect)
            result = uiDocument.PickObjects(out references, objectType, selectionFilter, string.Empty, activeElements);
          else
            result = uiDocument.PickObjects(out references, objectType, selectionFilter);
        }
        else
        {
          result = uiDocument.PickObject(out var reference, objectType, selectionFilter);
          if (result == Autodesk.Revit.UI.Result.Succeeded)
            references = new ARDB.Reference[] { reference };
        }
      }

      switch(result)
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new GH_Structure<T>();

          foreach (var document in documents.Where(x => x.Key != docGUID))
            value.AppendRange(document, new GH_Path(DocumentExtension.DocumentSessionId(document.Key)));

          value.AppendRange(references.Select(r => Types.Element.FromReference(doc, r) as T), new GH_Path(DocumentExtension.DocumentSessionId(docGUID)));

          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected virtual void Menu_AppendPromptNew(ToolStripDropDown menu) { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (Revit.ActiveUIDocument?.Document is null) return;

      if (SourceCount == 0)
      {
        var comboBox = BuildFilterList();
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Width = (int) (250 * GH_GraphicsUtil.UiScale);
        comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
        comboBox.Tag = menu;

        Menu_AppendCustomItem(menu, comboBox);
        Menu_AppendPromptNew(menu);
      }

      Menu_AppendItem(menu, $"Set one {TypeName}", Menu_PromptOne, SourceCount == 0, false);
    }

    protected override void Menu_AppendPromptMore(ToolStripDropDown menu)
    {
      if (Revit.ActiveUIDocument?.Document is null) return;

      var name_plural = GH_Convert.ToPlural(TypeName);

      Menu_AppendItem(menu, $"Set Multiple {name_plural}", Menu_PromptPlural, SourceCount == 0);
      Menu_AppendItem(menu, $"Change {TypeName} collection", Menu_PromptPreselect, SourceCount == 0, false);
    }

    protected override void Menu_AppendManageCollection(ToolStripDropDown menu)
    {
      if (MutableNickName)
      {
        using (var Documents = Revit.ActiveDBApplication.Documents)
        {
          if (Documents.Cast<ARDB.Document>().Where(x => x.IsLinked).Any())
          {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, $"Set one linked {TypeName}", Menu_PromptOneLinked, SourceCount == 0, false);
            Menu_AppendItem(menu, $"Set Multiple linked {GH_Convert.ToPlural(TypeName)}", Menu_PromptPluralLinked, SourceCount == 0);
          }
        }

        base.Menu_AppendManageCollection(menu);
      }
    }

    protected override void Menu_AppendInternaliseData(ToolStripDropDown menu)
    {
      //base.Menu_AppendInternaliseData(menu);
      Menu_AppendItem(menu, $"Internalise selection", Menu_InternaliseData, SourceCount > 0 || !MutableNickName, false);

      if (Revit.ActiveUIDocument?.Document is ARDB.Document activeDoc)
      {
        var any = ToElementIds(VolatileData).
          Where(x => activeDoc.Equals(x.Document)).
          Select(x => x.Id).
          Any();

        Menu_AppendItem(menu, $"Externalise selection", Menu_ExternaliseData, any, false);
      }
    }

    protected override void Menu_AppendBakeItem(ToolStripDropDown menu)
    {
      base.Menu_AppendBakeItem(menu);

      if (VolatileData.DataCount == 1)
      {
        var cplane = VolatileData.AllData(true).FirstOrDefault() is Types.GraphicalElement element &&
          element.Location.IsValid;

        Menu_AppendItem(menu, $"Set CPlane", Menu_SetCPlane, cplane, false);
      }
    }

    public override void Menu_AppendActions(ToolStripDropDown menu)
    {
      if (Revit.ActiveUIDocument?.Document is ARDB.Document doc)
      {
        var highlight = ToElementIds(VolatileData).Any(x => doc.Equals(x.Document));

        Menu_AppendItem(menu, $"Highlight {GH_Convert.ToPlural(TypeName)}", Menu_HighlightElements, highlight, false);
      }

      base.Menu_AppendActions(menu);
    }

    private void Menu_PromptOne(object sender, EventArgs e)
    {
      try
      {
        PrepareForPrompt();
        var data = default(T);
        if (Prompt_Singular(ref data) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          MutableNickName = true;
          if (Kind == GH_ParamKind.floating)
          {
            IconDisplayMode = GH_IconDisplayMode.application;
            Attributes?.ExpireLayout();
          }

          PersistentData.Clear();
          if (data is object)
            PersistentData.Append(data);

          OnObjectChanged(GH_ObjectEventType.PersistentData);
        }
      }
      finally
      {
        RecoverFromPrompt();
        ExpireSolution(true);
      }
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

          MutableNickName = true;
          if (Kind == GH_ParamKind.floating)
          {
            IconDisplayMode = GH_IconDisplayMode.application;
            Attributes?.ExpireLayout();
          }

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

    private void Menu_PromptPlural(object sender, EventArgs e)
    {
      try
      {
        PrepareForPrompt();
        var data = default(List<T>);
        if (Prompt_Plural(ref data) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          MutableNickName = true;
          if (Kind == GH_ParamKind.floating)
          {
            IconDisplayMode = GH_IconDisplayMode.application;
            Attributes?.ExpireLayout();
          }

          PersistentData.Clear();
          if (data is object)
            PersistentData.AppendRange(data);

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

          MutableNickName = true;
          if (Kind == GH_ParamKind.floating)
          {
            IconDisplayMode = GH_IconDisplayMode.application;
            Attributes?.ExpireLayout();
          }

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

    private void Menu_PromptPreselect(object sender, EventArgs e)
    {
      try
      {
        PrepareForPrompt();
        var data = m_data.Duplicate();
        if (Prompt_Elements(ref data, ObjectType.Element, true, true) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          MutableNickName = true;
          if (Kind == GH_ParamKind.floating)
          {
            IconDisplayMode = GH_IconDisplayMode.application;
            Attributes?.ExpireLayout();
          }

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

    protected EventHandler Menu_PromptNew(Autodesk.Revit.UI.RevitCommandId commandId) => async (sender, args) =>
    {
      var activeApp = Revit.ActiveUIApplication;
      using (var scope = new External.UI.EditScope(activeApp))
      {
        var activeDoc = activeApp.ActiveUIDocument.Document;
        var changes = await scope.ExecuteCommandAsync(commandId);
        if (changes.GetSummary(activeDoc, out var added, out var deleted, out var modified) > 0)
        {
          RecordPersistentDataEvent("Change data");

          MutableNickName = true;
          if (Kind == GH_ParamKind.floating)
          {
            IconDisplayMode = GH_IconDisplayMode.application;
            Attributes?.ExpireLayout();
          }

          PersistentData.Clear();
          PersistentData.AppendRange(added.Select(x => Types.Element.FromElementId(activeDoc, x)).OfType<T>());

          OnObjectChanged(GH_ObjectEventType.PersistentData);
          ExpireSolution(true);
        }
      }
    };

    private void Menu_HighlightElements(object sender, EventArgs e)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;
      var elementIds = ToElementIds(VolatileData).
                       Where(x => doc.Equals(x.Document)).
                       Select(x => x.Id);

      if (elementIds.Any())
      {
        var ids = elementIds.ToArray();
        uiDocument.Selection.SetElementIds(ids);
        uiDocument.ShowElements(ids);
      }
    }

    private void Menu_SetCPlane(object sender, EventArgs e)
    {
      if (VolatileData.AllData(true).FirstOrDefault() is Types.GraphicalElement element)
      {
        if
        (
          Rhino.RhinoDoc.ActiveDoc is Rhino.RhinoDoc doc &&
          (doc.Views.ActiveView ?? doc.Views.FirstOrDefault()) is RhinoView view &&
          view.ActiveViewport is RhinoViewport vport
        )
        {
          view.BringToFront();
          doc.Views.ActiveView = view;

          var cplane = vport.GetConstructionPlane();
          cplane.Plane = element.Location;
          vport.PushConstructionPlane(cplane);

          view.Redraw();
        }
      }
    }

    private void Menu_InternaliseData(object sender, EventArgs e)
    {
      RecordPersistentDataEvent("Internalise data");

      PersistentData.Clear();
      PersistentData.MergeStructure(m_data.Duplicate());
      m_data.Clear();
      OnObjectChanged(GH_ObjectEventType.PersistentData);

      if (!MutableNickName)
      {
        MutableNickName = true;
        if (Kind == GH_ParamKind.floating)
        {
          IconDisplayMode = GH_IconDisplayMode.application;
          Attributes?.ExpireLayout();
        }

        OnObjectChanged(GH_ObjectEventType.NickName);
      }

      RemoveAllSources();
      ExpireSolution(true);
    }

    private async void Menu_ExternaliseData(object sender, EventArgs e)
    {
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SaveSelection);
      var activeApp = Revit.ActiveUIApplication;
      using (var scope = new External.UI.EditScope(activeApp))
      {
        var activeDoc = activeApp.ActiveUIDocument.Document;

        var elementIds = ToElementIds(VolatileData).
          Where(x => activeDoc.Equals(x.Document)).
          Select(x => x.Id).
          ToList();

        var previous = activeApp.ActiveUIDocument.Selection.GetElementIds();
        Rhinoceros.InvokeInHostContext(() => activeApp.ActiveUIDocument.Selection.SetElementIds(elementIds));

        var changes = await scope.ExecuteCommandAsync(commandId);
        if (changes.GetSummary(activeDoc, out var added, out var deleted, out var modified) > 0)
        {
          var selectionFilter = added.Select(x => activeDoc.GetElement(x)).OfType<ARDB.SelectionFilterElement>().FirstOrDefault();
          if (selectionFilter is object)
          {
            RecordUndoEvent("Externalise data");

            PersistentData.Clear();
            OnObjectChanged(GH_ObjectEventType.PersistentData);

            MutableNickName = false;
            if (Kind == GH_ParamKind.floating)
            {
              IconDisplayMode = GH_IconDisplayMode.name;
              Attributes?.ExpireLayout();
            }

            NickName = selectionFilter.Name;
            OnObjectChanged(GH_ObjectEventType.NickName);

            RemoveAllSources();
            ExpireSolution(true);
          }
        }

        Rhinoceros.InvokeInHostContext(() => activeApp.ActiveUIDocument.Selection.SetElementIds(previous));
      }
    }
    #endregion

    #region ElementFilter
    private ComboBox BuildFilterList()
    {
      var comboBox = new ComboBox();
      comboBox.Items.Add("<Not Externalized>");
      if (MutableNickName)
        comboBox.SelectedIndex = 0;

      var doc = Revit.ActiveUIDocument?.Document;
      if (doc is object)
      {
        var filters = default(ARDB.FilterElement[]);

        using (var collector = new ARDB.FilteredElementCollector(doc))
        {
          var filterCollector = collector.OfClass(typeof(ARDB.FilterElement));
          filters = collector.Cast<ARDB.FilterElement>().OrderBy(x => x.Name).ToArray();
        }

        comboBox.Items.Add("<Active Selection>");

        if (!MutableNickName && NickName == "<Active Selection>")
          comboBox.SelectedIndex = 1;

        foreach (var filter in filters)
        {
          int index = comboBox.Items.Add(filter.Name);
          if (!MutableNickName)
          {
            if (filter.Name == NickName)
              comboBox.SelectedIndex = index;
          }
        }
      }

      return comboBox;
    }

    private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.SelectedIndex != -1)
        {
          if (comboBox.Items[comboBox.SelectedIndex] is string value)
          {
            if (comboBox.Tag is ToolStripDropDown menu)
              menu.Close();

            RecordUndoEvent("Set: NickName");
            MutableNickName = comboBox.SelectedIndex == 0;

            if (Kind == GH_ParamKind.floating)
            {
              IconDisplayMode = MutableNickName ? GH_IconDisplayMode.application : GH_IconDisplayMode.name;
              Attributes?.ExpireLayout();
            }

            PersistentData.Clear();
            if (comboBox.SelectedIndex == 0)
              PersistentData.MergeStructure(m_data);

            OnObjectChanged(GH_ObjectEventType.PersistentData);

            if (comboBox.SelectedIndex > 0)
            {
              NickName = value;
              OnObjectChanged(GH_ObjectEventType.NickName);
              ExpireSolution(true);
            }
            else
            {
              ClearRuntimeMessages();
              OnDisplayExpired(false);
            }
          }
        }
      }
    }

    private void OnObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
    {
      switch (e.Type)
      {
        case GH_ObjectEventType.Sources:
          MutableNickName = true;
          if (Kind == GH_ParamKind.floating)
          {
            IconDisplayMode = GH_IconDisplayMode.application;
            Attributes?.ExpireLayout();
          }

          break;
      }
    }

    protected override void LoadVolatileData()
    {
      if (!MutableNickName && (Kind == GH_ParamKind.floating || Kind == GH_ParamKind.input) && DataType != GH_ParamData.remote)
      {
        m_data.Clear();

        var doc = Revit.ActiveUIDocument?.Document;
        if (doc is object)
        {
          if (NickName == "<Active Selection>")
          {
            var selection = Revit.ActiveUIDocument.Selection.GetElementIds();
            var path = new GH_Path(0);
            m_data.AppendRange(selection.Select(x => PreferredCast(doc.GetElement(x))), path);
          }
          else
          {
            using (var collector = new ARDB.FilteredElementCollector(doc))
            {
              var filterCollector = collector.OfClass(typeof(ARDB.FilterElement));
              int filteredElementsCount = 0;

              var filters = collector.Cast<ARDB.FilterElement>();
              if (filters.Where(x => x.Name == NickName).FirstOrDefault() is ARDB.FilterElement filter)
              {
                if (filter is ARDB.SelectionFilterElement selection)
                {
                  var values = selection.GetElementIds().
                               Select(x => PreferredCast(doc.GetElement(x))).
                               Where(x => { if (x is object) return true; filteredElementsCount++; return false; });

                  var path = new GH_Path(0);
                  m_data.AppendRange(values, path);
                }
                else if (filter is ARDB.ParameterFilterElement parameter)
                {
                  if (parameter.GetElementFilter() is ARDB.ElementFilter parameterFilter)
                  {
                    using (var elements = new ARDB.FilteredElementCollector(doc))
                    {
                      var values = elements.
                                   WhereElementIsNotElementType().
                                   WherePasses(parameterFilter).
                                   Select(x => PreferredCast(x)).
                                   Where(x => { if (x is object) return true; filteredElementsCount++; return false; });

                      var path = new GH_Path(0);
                      m_data.AppendRange(values, path);
                    }
                  }
                }

                var dataCount = m_data.DataCount;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{dataCount}/{dataCount + filteredElementsCount} {(dataCount != 1 ? GH_Convert.ToPlural(TypeName) : TypeName)} collected from document '{doc.GetFileName()}'");
              }
              else
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to collect '{NickName}' elements from document '{doc.GetFileName()}'");
              }
            }
          }
        }
        else
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to collect '{NickName}' elements");
        }
      }
      else base.LoadVolatileData();
    }
    #endregion
  }

  public class GraphicalElement : GraphicalElementT<Types.IGH_GraphicalElement, ARDB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("EF607C2A-2F44-43F4-9C39-369CE114B51F");

    public GraphicalElement() : base
    (
      "Graphical Element",
      "Graphical Element",
      "Contains a collection of Revit graphical elements",
      "Params", "Revit"
    )
    { }

    protected override Types.IGH_GraphicalElement InstantiateT() => new Types.GraphicalElement();
  }
}
