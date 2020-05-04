using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI.Selection;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.UI.Selection;
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
    {
      ObjectChanged += OnObjectChanged;
    }

    #region ISelectionFilter
    public virtual bool AllowElement(DB.Element elem) => elem is R;
    public bool AllowReference(DB.Reference reference, DB.XYZ position)
    {
      if (reference.ElementReferenceType == DB.ElementReferenceType.REFERENCE_TYPE_NONE)
        return AllowElement(Revit.ActiveUIDocument.Document.GetElement(reference));

      return false;
    }

    #endregion

    #region UI methods
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
          value = (T) Types.Element.FromReference(uiDocument.Document, reference);
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
            value = references.Select(r => (T) Types.Element.FromReference(doc, r)).ToList();
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
                            OfType<DB.Reference>() :
                            Enumerable.Empty<DB.Reference>()
                           ).
                           ToArray();

      var result = Autodesk.Revit.UI.Result.Failed;
      var references = default(IList<DB.Reference>);
      {
        if (multiple)
        {
          if(preSelect)
            result = uiDocument.PickObjects(out references, objectType, this, string.Empty, activeElements);
          else
            result = uiDocument.PickObjects(out references, objectType, this);
        }
        else
        {
          result = uiDocument.PickObject(out var reference, objectType, this);
          if (result == Autodesk.Revit.UI.Result.Succeeded)
            references = new DB.Reference[] { reference };
        }
      }

      switch(result)
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new GH_Structure<T>();

          foreach (var document in documents.Where(x => x.Key != docGUID))
            value.AppendRange(document, new GH_Path(DocumentExtension.DocumentSessionId(document.Key)));

          value.AppendRange(references.Select(r => (T) Types.Element.FromReference(doc, r)), new GH_Path(DocumentExtension.DocumentSessionId(docGUID)));

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
      var comboBox = BuildFilterList();
      comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      comboBox.Width = (int) (250 * GH_GraphicsUtil.UiScale);
      comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
      comboBox.Tag = menu;

      Menu_AppendCustomItem(menu, comboBox);
      Menu_AppendItem(menu, $"Set one {TypeName}", Menu_PromptOne, SourceCount == 0, false);
    }

    protected override void Menu_AppendPromptMore(ToolStripDropDown menu)
    {
      var name_plural = GH_Convert.ToPlural(TypeName);

      Menu_AppendItem(menu, $"Set Multiple {name_plural}", Menu_PromptPlural, SourceCount == 0);
      Menu_AppendItem(menu, $"Change {name_plural} collection", Menu_PromptPreselect, SourceCount == 0, false);
    }

    protected override void Menu_AppendManageCollection(ToolStripDropDown menu)
    {
      if (MutableNickName)
      {
        using (var Documents = Revit.ActiveDBApplication.Documents)
        {
          if (Documents.Cast<DB.Document>().Where(x => x.IsLinked).Any())
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
      base.Menu_AppendInternaliseData(menu);
      //Menu_AppendItem(menu, $"Externalize data", Menu_ExternalizeData, SourceCount == 0, !MutableNickName);
    }

    public override void Menu_AppendActions(ToolStripDropDown menu)
    {
      Menu_AppendItem(menu, $"Highlight {GH_Convert.ToPlural(TypeName)}", Menu_HighlightElements, !VolatileData.IsEmpty, false);
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
        if (Prompt_Elements(ref data, ObjectType.LinkedElement, false, false) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          MutableNickName = true;
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
        if (Prompt_Elements(ref data, ObjectType.LinkedElement, true, false) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          MutableNickName = true;
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
        var data = PersistentData;
        if (Prompt_Elements(ref data, ObjectType.Element, true, true) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          MutableNickName = true;
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

    private void Menu_HighlightElements(object sender, EventArgs e)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var elementIds = ToElementIds(VolatileData).
                       Where(x => x.Document.Equals(uiDocument.Document)).
                       Select(x => x.Id);

      if (elementIds.Any())
      {
        Rhinoceros.InvokeInHostContext(() =>
        {
          var ids = elementIds.ToArray();
          uiDocument.Selection.SetElementIds(ids);
          uiDocument.ShowElements(ids);
        });
      }
    }

    //private void Menu_ExternalizeData(object sender, EventArgs e)
    //{
    //  if (sender is ToolStripMenuItem item)
    //  {
    //    var active = Revit.ActiveUIDocument?.Document;
    //    if (active is null)
    //      return;

    //    bool any = false;
    //    var documents = PersistentData.AllData(true).Cast<Types.GraphicalElement>().Where(x => active.Equals(x.Document)).GroupBy(x => x.Document);
    //    foreach (var doc in documents.Select(x => x.Key))
    //    {
    //      using (var collector = new DB.FilteredElementCollector(doc))
    //      {
    //        var filterCollector = collector.OfClass(typeof(DB.FilterElement));
    //        var filters = collector.Cast<DB.FilterElement>();
    //        any |= filters.Where(x => x.Name == NickName).Any();
    //      }
    //    }

    //    var result = any ?
    //      MessageBox.Show
    //      (
    //        owner: Instances.ActiveCanvas,
    //        caption: "Internalize data",
    //        icon: MessageBoxIcon.Question,
    //        text: $"{NickName} filter already exist, do you want to overwrite it?",
    //        buttons: MessageBoxButtons.YesNoCancel
    //      ):
    //      DialogResult.Yes;

    //    if (result == DialogResult.Cancel)
    //      return;

    //    if (result == DialogResult.Yes)
    //    {

    //    }

    //    MutableNickName = item.Checked;
    //    ExpireSolution(true);
    //  }
    //}
    #endregion

    #region ElementFilter
    private ComboBox BuildFilterList()
    {
      var comboBox = new ComboBox();

      var doc = Revit.ActiveUIDocument?.Document;
      if (doc is object)
      {
        var filters = default(DB.FilterElement[]);

        using (var collector = new DB.FilteredElementCollector(doc))
        {
          var filterCollector = collector.OfClass(typeof(DB.FilterElement));
          filters = collector.Cast<DB.FilterElement>().OrderBy(x => x.Name).ToArray();
        }

        comboBox.Items.Add("<Not Externalized>");
        comboBox.Items.Add("<Active Selection>");

        if (MutableNickName)
          comboBox.SelectedIndex = 0;
        else if(NickName == "<Active Selection>")
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
            using (var collector = new DB.FilteredElementCollector(doc))
            {
              var filterCollector = collector.OfClass(typeof(DB.FilterElement));
              int filteredElementsCount = 0;

              var filters = collector.Cast<DB.FilterElement>();
              if (filters.Where(x => x.Name == NickName).FirstOrDefault() is DB.FilterElement filter)
              {
                if (filter is DB.SelectionFilterElement selection)
                {
                  var values = selection.GetElementIds().
                               Select(x => PreferredCast(doc.GetElement(x))).
                               Where(x => { if (x is object) return true; filteredElementsCount++; return false; });

                  var path = new GH_Path(0);
                  m_data.AppendRange(values, path);
                }
                else if (filter is DB.ParameterFilterElement parameter)
                {
                  if (parameter.GetElementFilter() is DB.ElementFilter parameterFilter)
                  {
                    using (var elements = new DB.FilteredElementCollector(doc))
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
              }
              else
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to collect '{NickName}' elements from document '{doc.Title}'");
              }

              var dataCount = m_data.DataCount;
              AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{dataCount}/{dataCount + filteredElementsCount} {(dataCount != 1 ? GH_Convert.ToPlural(TypeName) : TypeName)} collected from document '{doc.Title}'");
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

  public class GraphicalElement : GraphicalElementT<Types.GraphicalElement, DB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("EF607C2A-2F44-43F4-9C39-369CE114B51F");

    public GraphicalElement() : base
    (
      "Graphical Element",
      "Graphical Element",
      "Represents a Revit graphical element.",
      "Params", "Revit"
    )
    {
    }

    protected override Types.GraphicalElement PreferredCast(object data)
    {
      return data is DB.Element element && AllowElement(element) ?
             Types.GraphicalElement.FromElement(element) as Types.GraphicalElement:
             null;
    }

    public override bool AllowElement(DB.Element elem) => Types.GraphicalElement.IsValidElement(elem);
  }
}
