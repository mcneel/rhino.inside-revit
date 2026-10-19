using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;

  public class ElementSource : PersistentParam<Types.IGH_ElementSource>, Kernel.IGH_ReferenceParam
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("EF2EFE84-8B2E-4613-A352-B1CE95671238");
    protected override string IconTag => DefaultNickName;

    internal static readonly string DefaultName = "Source";
    internal static readonly string DefaultNickName = "S";
    internal static readonly string DefaultDescription = "Contains a collection of Revit element sources";

    public ElementSource() : base
    (
      name: DefaultName,
      nickname: DefaultName,
      description: DefaultDescription,
      category: "Params",
      subcategory: "Revit"
    )
    { }

    protected override Types.IGH_ElementSource PreferredCast(object data)
    {
      switch (data)
      {
        case ARDB.RevitLinkInstance instance: return Types.RevitLinkInstance.FromElement(instance) as Types.RevitLinkInstance;
        case ARDB.Document document: return Types.Document.FromValue(document);
      }

      return null;
    }

    public static bool TryGetElementSource(IGH_Component component, IGH_DataAccess DA, out Types.IGH_ElementSource source)
    {
      source = default;

      var _Model_ = component.Params.IndexOfInputParam(DefaultName);
      if
      (
        _Model_ < 0 || // Not present
        (
          component.Params.Input[_Model_].SourceCount == 0 &&  // Not connected
          component.Params.Input[_Model_].VolatileData.IsEmpty // Not persistent data
        )
      )
      {
        return true;
      }

      return DA.GetData(_Model_, ref source);
    }

    public static bool GetElementSourceOrCurrent(IGH_Component component, IGH_DataAccess DA, out Types.IGH_ElementSource source)
    {
      source = default;

      var _Model_ = component.Params.IndexOfInputParam(DefaultName);
      if
      (
        _Model_ < 0 || // Not present
        (
          component.Params.Input[_Model_].SourceCount == 0 &&  // Not connected
          component.Params.Input[_Model_].VolatileData.IsEmpty // Not persistent data
        )
      )
      {
        if (Document.TryGetCurrentDocument(component, out var document) && document is Types.IGH_ElementSource instance)
        {
          source = instance;
          return true;
        }

        return false;
      }

      return DA.GetData(_Model_, ref source);
    }

    #region IGH_ReferenceParam
    bool Kernel.IGH_ReferenceParam.NeedsToBeExpired
    (
      ARDB.Document doc,
      ISet<ARDB.ElementId> added,
      ISet<ARDB.ElementId> deleted,
      ISet<ARDB.ElementId> modified
    )
    {
      if (Kind != GH_ParamKind.output)
      {
        if (DataType != GH_ParamData.local)
          return false;

        if (Phase == GH_SolutionPhase.Blank)
          CollectData();
      }

      foreach (var data in VolatileData.AllData(true).OfType<Types.IGH_Reference>())
      {
        var document = data.ReferenceDocument;
        var elementId = data.ReferenceId;
        if (!elementId.IsValid() || !document.IsValid())
          continue;

        if (!doc.Equals(document))
          continue;

        if (modified.Contains(elementId))
          return true;

        if (deleted.Contains(elementId))
          return true;
      }

      return false;
    }
    #endregion

    #region UI
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0 || Revit.ActiveUIDocument is null)
        return;

      var listBox = new ListBox
      {
        DisplayMember = nameof(Types.Document.DisplayName),
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale),
        SelectionMode = SelectionMode.MultiExtended
      };
      listBox.SelectedIndexChanged += Source_SelectedIndexChanged;

      var comboBox = new ComboBox
      {
        DisplayMember = nameof(Types.Document.DisplayName),
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };
      comboBox.SelectedIndexChanged += Document_SelectedIndexChanged;
      comboBox.SetCueBanner("Document…");

      RefreshDocumentsList(comboBox);
      comboBox.SelectedItem = Types.Document.FromValue(Revit.ActiveUIDocument.Document);
      RefreshModelsList(listBox, comboBox.SelectedItem as Types.Document);

      Menu_AppendCustomItem(menu, comboBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }

    private void RefreshDocumentsList(ComboBox comboBox)
    {
      comboBox.SelectedIndexChanged -= Document_SelectedIndexChanged;
      comboBox.Items.Clear();

      var documents = Revit.ActiveUIApplication.
        Application.Documents.
        Cast<ARDB.Document>().
        Where(x => !x.IsLinked && x.GetActiveView() is object).
        OrderBy(x => x.GetDocumentType()).
        ThenBy(x => x.GetTitle()).
        Select(Types.Document.FromValue);

      comboBox.Items.AddRange(documents.ToArray());
      comboBox.SelectedIndexChanged += Document_SelectedIndexChanged;
    }

    private void Document_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshModelsList(listBox, comboBox.SelectedItem as Types.Document);
      }
    }
    private void RefreshModelsList(ListBox listBox, Types.Document document)
    {
      listBox.SelectedIndexChanged -= Source_SelectedIndexChanged;
      listBox.Items.Clear();

      document.LoadReferencedData();
      var items = new ARDB.FilteredElementCollector(document.Value).OfClass(typeof(ARDB.RevitLinkInstance)).
        Select(Types.GraphicalElement.FromElement).
        OrderBy(x => x.DisplayName).
        OfType<Types.IGH_ElementSource>().
        Prepend(document).
        ToList();

      listBox.Items.AddRange(items.ToArray());
      foreach (var item in items.Intersect(PersistentData.OfType<Types.IGH_ElementSource>()))
        listBox.SelectedItems.Add(item);
      listBox.SelectedIndexChanged += Source_SelectedIndexChanged;
    }

    protected void Source_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        RecordPersistentDataEvent($"Set: {NickName}");
        PersistentData.Clear();
        PersistentData.AppendRange(listBox.SelectedItems.OfType<Types.IGH_ElementSource>());
        OnObjectChanged(GH_ObjectEventType.PersistentData);

        ExpireSolution(true);
      }
    }
    #endregion
  }
}
