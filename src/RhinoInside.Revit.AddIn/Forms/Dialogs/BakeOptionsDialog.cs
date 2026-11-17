using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Forms
{
  using Properties;
  using External.DB;
  using External.DB.Extensions;

  class BakeOptionsDialog : ModalDialog
  {
    const string OPTROOT = "LastBakeOptions";

    readonly ARDB.Document Document;

    ComboBox _worksetSelector = new ComboBox() { ReadOnly = true, Enabled = false };
    ComboBox _categorySelector = new ComboBox() { ReadOnly = true };
    ComboBox _materialSelector = new ComboBox() { ReadOnly = true };
    CheckBox _groupBox = new CheckBox() { Enabled = true };

    public ARDB.WorksetId SelectedWorkset { get; private set; } = ARDB.WorksetId.InvalidWorksetId;
    public ARDB.ElementId SelectedCategory { get; private set; } = ARDB.ElementId.InvalidElementId;
    public ARDB.ElementId SelectedMaterial { get; private set; } = ARDB.ElementId.InvalidElementId;
    public bool Group { get; private set; } = false;

    public BakeOptionsDialog(ARUI.UIApplication uiApp) : base(uiApp, initialSize: new Size(400, -1))
    {
      Title = "Bake Selected";
      Document = uiApp.ActiveUIDocument?.Document;
      DefaultButton.Text = "Bake";
      DefaultButton.Click += BakeButton_Click;

      InitLayout();
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        _worksetSelector?.Dispose(); _worksetSelector = null;
        _categorySelector?.Dispose(); _categorySelector = null;
        _materialSelector?.Dispose(); _materialSelector = null;
        _groupBox?.Dispose(); _groupBox = null;
      }

      base.Dispose(disposing);
    }

    void InitLayout()
    {
      // collect worksets
      if (Document.IsWorkshared)
      {
        _worksetSelector.Enabled = true;
        var wsTable = Document.GetWorksetTable();
        foreach (var workset in new ARDB.FilteredWorksetCollector(Document).OfKind(ARDB.WorksetKind.UserWorkset).ToWorksets())
          _worksetSelector.Items.Add(new ListItem { Key = workset.Name, Text = workset.Name, Tag = workset.Id });

        var activeWorkset = wsTable.GetWorkset(wsTable.GetActiveWorksetId());
        if (activeWorkset.Kind == ARDB.WorksetKind.UserWorkset)
          _worksetSelector.SelectedKey = activeWorkset.Name;
      }

      // collect categories (only applicable to directshapes since baking creates directshapes)
      foreach (var group in DirectShapeCategories.GroupBy(x => x.CategoryType).OrderBy(x => x.Key.ToString()))
      {
        foreach (var category in group.OrderBy(x => x.Name))
        {
          var id = category.ToBuiltInCategory();
          _categorySelector.Items.Add(new ListItem { Key = category.Name, Text = category.Name, Tag = id });
          if (id == ARDB.BuiltInCategory.OST_GenericModel)
            _categorySelector.SelectedKey = category.Name;
        }
      }

      // collect materials
      _materialSelector.Items.Add(new ListItem { Key = "<By Category>", Text = "<By Category>", Tag = ARDB.ElementId.InvalidElementId });
      _materialSelector.SelectedIndex = 0;
      foreach (var material in new ARDB.FilteredElementCollector(Document).OfClass<ARDB.Material>().OrderBy(x => x.Name))
        _materialSelector.Items.Add(new ListItem { Key = material.Name, Text = material.Name, Tag = material.Id });

      _groupBox.Checked = Group;

      // select the previously selected category if any
      if (AddInOptions.Current.CustomOptions.Get(OPTROOT, "LastSelectedCategory") is string lastCategoryName)
        if (_categorySelector.Items.Select(x => x.Key).Contains(lastCategoryName))
          _categorySelector.SelectedKey = lastCategoryName;

      // select the previously selected workset if any
      if (AddInOptions.Current.CustomOptions.Get(OPTROOT, "LastSelectedWorkset") is string lastWorksetName)
        if (_worksetSelector.Items.Select(x => x.Key).Contains(lastWorksetName))
          _worksetSelector.SelectedKey = lastWorksetName;

      Content = new TableLayout
      {
        Spacing = new Size(5, 10),
        Padding = new Padding(5),
        Rows =
        {
          new TableLayout
          {
            Spacing = new Size(5, 10),
            Padding = new Padding(5),
            Rows =
            {
              new TableRow { Cells = { new Label { Text = "Workset" },  _worksetSelector } },
              new TableRow { Cells = { new Label { Text = "Category" }, _categorySelector } },
              new TableRow { Cells = { new Label { Text = "Paint" },    _materialSelector } },
              new TableRow { Cells = { new Label { Text = "Group" },    _groupBox } },
            }
          },
        }
      };
    }

    IEnumerable<ARDB.Category> DirectShapeCategories =>
      BuiltInCategories.Values.
      Where(categoryId => ARDB.DirectShape.IsValidCategoryId(new ARDB.ElementId(categoryId), Document)).
      Select(categoryId => Document.GetCategory(categoryId)).
      Where(x => x is object);

    private void BakeButton_Click(object sender, EventArgs e)
    {
      // set selected workset
      if (Document.IsWorkshared)
      {
        foreach (var workset in new ARDB.FilteredWorksetCollector(Document).OfKind(ARDB.WorksetKind.UserWorkset))
        {
          if (workset.Name != _worksetSelector.SelectedKey) continue;

          AddInOptions.Current.CustomOptions.Set(OPTROOT, "LastSelectedWorkset", workset.Name);
          SelectedWorkset = workset.Id;
        }
      }

      // set selected category
      foreach (var category in DirectShapeCategories)
      {
        if (category.Name != _categorySelector.SelectedKey) continue;

        AddInOptions.Current.CustomOptions.Set(OPTROOT, "LastSelectedCategory", category.Name);
        SelectedCategory = category.Id;
      }

      if ((_materialSelector.SelectedValue as ListItem)?.Tag is ARDB.ElementId materialId)
        SelectedMaterial = materialId;

      Group = _groupBox.Checked is true;

      AddInOptions.Save();
      Close(DialogResult.Ok);
    }
  }
}
