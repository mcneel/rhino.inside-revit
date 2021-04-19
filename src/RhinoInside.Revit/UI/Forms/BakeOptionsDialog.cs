using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Interop;
using System.Diagnostics;
using System.Xml.Serialization;

using Eto.Forms;
using Eto.Drawing;
using Forms = Eto.Forms;

using DB = Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using RhinoInside.Revit.Settings;

namespace RhinoInside.Revit.UI
{
  internal class BakeOptionsDialog : ModalDialog
  {
    const string OPTROOT = "LastBakeOptions";

    readonly DB.Document Document;

    readonly Forms.ComboBox _categorySelector = new Forms.ComboBox();
    readonly Forms.ComboBox _worksetSelector = new Forms.ComboBox() { Enabled = false };

    public DB.ElementId SelectedCategory { get; private set; } = DB.ElementId.InvalidElementId;
    public DB.WorksetId SelectedWorkset { get; private set; } = DB.WorksetId.InvalidWorksetId;

    public BakeOptionsDialog(UIApplication uiApp) : base(uiApp, initialSize: new Size(400, -1))
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
      var directShapeCategories = Enum.GetValues(typeof(DB.BuiltInCategory)).Cast<DB.BuiltInCategory>().
                                  Where(categoryId => DB.DirectShape.IsValidCategoryId(new DB.ElementId(categoryId), Document)).
                                  Select(categoryId => DB.Category.GetCategory(Document, categoryId)).
                                  Where(x => x is object);

      foreach (var group in directShapeCategories.GroupBy(x => x.CategoryType).OrderBy(x => x.Key.ToString()))
        foreach (var category in group.OrderBy(x => x.Name))
        {
          _categorySelector.Items.Add(new ListItem { Key = category.Name, Text = category.Name });
          if ((DB.BuiltInCategory) category.Id.IntegerValue == DB.BuiltInCategory.OST_GenericModel)
            _categorySelector.SelectedKey = category.Name;
        }

      // select the previously selected category if any
      if (AddinOptions.Current.CustomOptions.Get(OPTROOT, "LastSelectedCategory") is string lastCategoryName)
        if (_categorySelector.Items.Select(x => x.Key).Contains(lastCategoryName))
          _categorySelector.SelectedKey = lastCategoryName;

      // collect worksets
      if (Document.IsWorkshared)
      {
        var wsTable = Document.GetWorksetTable();
        _worksetSelector.Enabled = true;
        foreach (DB.Workset workset in new DB.FilteredWorksetCollector(Document).OfKind(DB.WorksetKind.UserWorkset).ToWorksets())
            _worksetSelector.Items.Add(new ListItem { Key = workset.Name, Text = workset.Name });

        var activeWorkset = wsTable.GetWorkset(wsTable.GetActiveWorksetId());
        if (activeWorkset.Kind == DB.WorksetKind.UserWorkset)
          _worksetSelector.SelectedKey = activeWorkset.Name;
      }

      // select the previously selected workset if any
      if (AddinOptions.Current.CustomOptions.Get(OPTROOT, "LastSelectedWorkset") is string lastWorksetName)
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

    private void BakeButton_Click(object sender, EventArgs e)
    {
      // set selected category
      foreach (DB.Category category in Document.Settings.Categories)
        if (category.Name == _categorySelector.SelectedKey)
        {
          AddinOptions.Current.CustomOptions.Set(OPTROOT, "LastSelectedCategory", category.Name);
          SelectedCategory = category.Id;
        }

      // set selected workset
      if (Document.IsWorkshared)
        foreach (DB.Workset workset in new DB.FilteredWorksetCollector(Document).OfKind(DB.WorksetKind.UserWorkset).ToWorksets())
          if (workset.Name == _worksetSelector.SelectedKey)
          {
            AddinOptions.Current.CustomOptions.Set(OPTROOT, "LastSelectedWorkset", workset.Name);
            SelectedWorkset = workset.Id;
          }

      AddinOptions.Save();
      Close(DialogResult.Ok);
    }
  }
}
