using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Forms
{
  using Properties;
  using External.DB.Extensions;

  class BakeOptionsDialog : ModalDialog
  {
    const string OPTROOT = "LastBakeOptions";

    readonly ARDB.Document Document;

    readonly ComboBox _categorySelector = new ComboBox();
    readonly ComboBox _worksetSelector = new ComboBox() { Enabled = false };

    public ARDB.ElementId SelectedCategory { get; private set; } = ARDB.ElementId.InvalidElementId;
    public ARDB.WorksetId SelectedWorkset { get; private set; } = ARDB.WorksetId.InvalidWorksetId;

    public BakeOptionsDialog(ARUI.UIApplication uiApp) : base(uiApp, initialSize: new Size(400, -1))
    {
      Title = "Bake Selected";
      Document = uiApp.ActiveUIDocument?.Document;
      DefaultButton.Text = "Bake";
      DefaultButton.Click += BakeButton_Click;

      InitLayout();
    }

    void InitLayout()
    {
      // collect categories (only applicable to directshapes since baking creates directshapes)
      foreach (var group in DirectShapeCategories.GroupBy(x => x.CategoryType).OrderBy(x => x.Key.ToString()))
        foreach (var category in group.OrderBy(x => x.Name))
        {
          _categorySelector.Items.Add(new ListItem { Key = category.Name, Text = category.Name });
          if ((ARDB.BuiltInCategory) category.Id.IntegerValue == ARDB.BuiltInCategory.OST_GenericModel)
            _categorySelector.SelectedKey = category.Name;
        }

      // select the previously selected category if any
      if (AddInOptions.Current.CustomOptions.Get(OPTROOT, "LastSelectedCategory") is string lastCategoryName)
        if (_categorySelector.Items.Select(x => x.Key).Contains(lastCategoryName))
          _categorySelector.SelectedKey = lastCategoryName;

      // collect worksets
      if (Document.IsWorkshared)
      {
        var wsTable = Document.GetWorksetTable();
        _worksetSelector.Enabled = true;
        foreach (ARDB.Workset workset in new ARDB.FilteredWorksetCollector(Document).OfKind(ARDB.WorksetKind.UserWorkset).ToWorksets())
            _worksetSelector.Items.Add(new ListItem { Key = workset.Name, Text = workset.Name });

        var activeWorkset = wsTable.GetWorkset(wsTable.GetActiveWorksetId());
        if (activeWorkset.Kind == ARDB.WorksetKind.UserWorkset)
          _worksetSelector.SelectedKey = activeWorkset.Name;
      }

      // select the previously selected workset if any
      if (AddInOptions.Current.CustomOptions.Get(OPTROOT, "LastSelectedWorkset") is string lastWorksetName)
        if (_worksetSelector.Items.Select(x => x.Key).Contains(lastWorksetName))
          _worksetSelector.SelectedKey = lastWorksetName;

      Content = new TableLayout
      {
        Spacing = new Size(5, 10),
        Padding = new Padding(5),
        Rows = {
          new TableLayout
          {
            Spacing = new Size(5, 10),
            Padding = new Padding(5),
            Rows = {
              new TableRow {
                Cells = { new Label { Text = "Select Category" },  _categorySelector }
              },
              new TableRow {
                Cells = { new Label { Text = "Select Workset" }, _worksetSelector }
              },
            }
          },
        }
      };
    }

    IEnumerable<ARDB.Category> DirectShapeCategories =>
      BuiltInCategoryExtension.BuiltInCategories.
      Where(categoryId => ARDB.DirectShape.IsValidCategoryId(new ARDB.ElementId(categoryId), Document)).
      Select(categoryId => Document.GetCategory(categoryId)).
      Where(x => x is object);

    private void BakeButton_Click(object sender, EventArgs e)
    {
      // set selected category
      foreach (var category in DirectShapeCategories)
      {
        if (category.Name != _categorySelector.SelectedKey) continue;

        AddInOptions.Current.CustomOptions.Set(OPTROOT, "LastSelectedCategory", category.Name);
        SelectedCategory = category.Id;
      }

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

      AddInOptions.Save();
      Close(DialogResult.Ok);
    }
  }
}
