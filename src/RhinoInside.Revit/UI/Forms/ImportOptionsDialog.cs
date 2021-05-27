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

using RhinoInside.Revit.Settings;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.UI
{
  internal class ImportOptionsDialog : ModalDialog
  {
    const string ImportOptions = "ImportOptions";

    readonly DB.Document Document;
    readonly ImageView imageView = new ImageView();
    readonly DropDown fileSelector = new DropDown();
    readonly Button fileBrowser = new Button() { Text = "…" };
    readonly DropDown placementSelector = new DropDown();
    readonly DropDown layersSelector = new DropDown();
    readonly DropDown categorySelector = new DropDown();
    readonly DropDown worksetSelector = new DropDown() { Enabled = false };
    readonly ComboBox familyName = new ComboBox() { AutoComplete = true };
    readonly ComboBox typeName = new ComboBox() { AutoComplete = true };

    public string FileName => fileSelector.SelectedKey;
    public DB.ImportPlacement Placement => (DB.ImportPlacement) Enum.Parse(typeof(DB.ImportPlacement), placementSelector.SelectedKey);
    public bool VisibleLayersOnly => bool.Parse(layersSelector.SelectedKey);
    public DB.ElementId CategoryId => new DB.ElementId(int.Parse(categorySelector.SelectedKey));
    public DB.WorksetId WorksetId => worksetSelector.SelectedKey is null ? DB.WorksetId.InvalidWorksetId: new DB.WorksetId(int.Parse(worksetSelector.SelectedKey));
#if REVIT_2022
    public string FamilyName => familyName.Text;
#else
    public string FamilyName => typeName.Text;
#endif
    public string TypeName => typeName.Text;

    public ImportOptionsDialog(Autodesk.Revit.UI.UIApplication uiApp) : base(uiApp, initialSize: new Size(400, -1))
    {
      Title = "Import 3DM";
      Document = uiApp.ActiveUIDocument?.Document;
      DefaultButton.Enabled = false;
      DefaultButton.Text = "Import";
      DefaultButton.Click += ImportButton_Click;

      imageView.BackgroundColor = Colors.DarkGray;
      imageView.Width = 200;
      imageView.Height = 200;

      fileSelector.SelectedKeyChanged += FileSelector_SelectedKeyChanged;
      fileSelector.SelectedIndex = 0;

      fileBrowser.Text = "Browse…";
      fileBrowser.Click += FileBrowser_Click;

      for (int i = 0; i < 10; ++i)
      {
        var fileName = AddinOptions.Current.CustomOptions.Get(ImportOptions, $"FileNameMRU{i}");
        if (File.Exists(fileName))
          fileSelector.Items.Add(Path.GetFileName(fileName), fileName);
      }

      // Layers
      {
        layersSelector.Items.Add("All", false.ToString());
        layersSelector.Items.Add("Visible", true.ToString());
        layersSelector.SelectedIndex = 1;
      }

      if (Document.IsFamilyDocument)
        InitLayoutFamily();
      else
        InitLayoutProject();
    }

    void InitLayoutFamily()
    {
      // Placement
      {
        //placementSelector.Items.Add("Center to Origin", DB.ImportPlacement.Centered.ToString());
        placementSelector.Items.Add("Origin to Origin", DB.ImportPlacement.Origin.ToString());
        //placementSelector.Items.Add("Shared Coordinates to Origin", DB.ImportPlacement.Shared.ToString());
        placementSelector.Items.Add("Base Point to Origin", DB.ImportPlacement.Site.ToString());
        placementSelector.SelectedIndex = 0;
      }

      // Category
      {
        var category = Document.OwnerFamily.FamilyCategory;
        categorySelector.Enabled = false;
        categorySelector.Items.Add(new ListItem { Key = category.Id.ToString(), Text = category.Name, Tag = category });
        categorySelector.SelectedIndex = 0;
      }

      Content = new TableLayout
      {
        Rows =
        {
          new TableRow { Cells = { imageView } },
          new TableRow
          {
            Cells =
            {
              new TableLayout
              {
                Spacing = new Size(5, 10),
                Padding = new Padding(0, 10),
                Rows =
                {
                  new TableRow
                  {
                    Cells =
                    {
                      new Label { Text = "File name" },
                      TableLayout.Horizontal(5, new TableCell[]
                      { new TableCell(fileSelector, true), new TableCell(fileBrowser, false) })
                    },
                  },
                  new TableRow { Cells = { new Label { Text = "Positioning" }, placementSelector } },
                  new TableRow { Cells = { new Label { Text = "Layers" }, layersSelector } },
                  new TableRow { Cells = { new Label { Text = "Category" }, categorySelector } },
                }
              }
            }
          }
        }
      };
    }

    void InitLayoutProject()
    {
      // Placement
      {
        //placementSelector.Items.Add("Center to Center", DB.ImportPlacement.Centered.ToString());
        placementSelector.Items.Add("Origin to Origin", DB.ImportPlacement.Origin.ToString());
        //placementSelector.Items.Add("By Shared Coordinates", DB.ImportPlacement.Shared.ToString());
        placementSelector.Items.Add("Base Point to Base Point", DB.ImportPlacement.Site.ToString());
        placementSelector.SelectedIndex = 0;
      }

      // Category
      {
        var directShapeCategories = BuiltInCategoryExtension.BuiltInCategories.
                                    Where(x => DB.DirectShape.IsValidCategoryId(new DB.ElementId(x), Document)).
                                    Select(x => Document.GetCategory(x)).
                                    OfType<DB.Category>();

        foreach (var group in directShapeCategories.GroupBy(x => x.CategoryType).OrderBy(x => x.Key.ToString()))
          foreach (var category in group.OrderBy(x => x.Name))
          {
            categorySelector.Items.Add(new ListItem { Key = category.Id.ToString(), Text = category.Name });
          }

        categorySelector.SelectedKey = ((int) DB.BuiltInCategory.OST_GenericModel).ToString();
        categorySelector.SelectedKeyChanged += CategorySelector_SelectedKeyChanged;
      }

      // Workset
      if (Document.IsWorkshared)
      {
        worksetSelector.Enabled = true;

        var wsTable = Document.GetWorksetTable();
        foreach (var workset in new DB.FilteredWorksetCollector(Document).OfKind(DB.WorksetKind.UserWorkset))
          worksetSelector.Items.Add(new ListItem { Key = workset.Id.ToString(), Text = workset.Name });

        var activeWorkset = wsTable.GetWorkset(wsTable.GetActiveWorksetId());
        if (activeWorkset.Kind == DB.WorksetKind.UserWorkset)
          worksetSelector.SelectedKey = activeWorkset.Id.ToString();
      }

#if REVIT_2022
      // Family name
      {
        RefreshFamilyNameList();

        familyName.Text = "Direct Shape";
        familyName.SelectedKeyChanged += (sender, args) => RefreshTypeNameList(familyName.SelectedKey);
      }

      // Type name
      {
        RefreshTypeNameList(FamilyName);
      }
#else
      // Type name
      {
        RefreshTypeNameList(default);
      }
#endif

      Content = new TableLayout
      {
        Rows =
        {
          new TableRow { Cells = { imageView } },
          new TableRow
          {
            Cells =
            {
              new TableLayout
              {
                Spacing = new Size(5, 10),
                Padding = new Padding(0, 10),
                Rows =
                {
                  new TableRow
                  {
                    Cells =
                    {
                      new Label { Text = "File name" },
                      TableLayout.Horizontal(5, new TableCell[]
                      { new TableCell(fileSelector, true), new TableCell(fileBrowser, false) })
                    },
                  },
                  new TableRow { Cells = { new Label { Text = "Positioning" }, placementSelector } },
                  new TableRow { Cells = { new Label { Text = "Layers" }, layersSelector } },
                  new TableRow { Cells = { new Label { Text = "Category" }, categorySelector } },
                  new TableRow { Cells = { new Label { Text = "Workset" }, worksetSelector } },
#if REVIT_2022
                  new TableRow { Cells = { new Label { Text = "Family name" }, familyName } },
#endif
                  new TableRow { Cells = { new Label { Text = "Type name" }, typeName } },
                }
              }
            }
          }
        }
      };
    }

    private void CategorySelector_SelectedKeyChanged(object sender, EventArgs e)
    {
      RefreshFamilyNameList();
      RefreshTypeNameList(FamilyName);
    }

    void RefreshFamilyNameList()
    {
      familyName.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(Document))
      {
        var familyNames = collector.WhereElementIsElementType().
          WhereElementIsKindOf(typeof(DB.DirectShapeType)).
          OfCategoryId(CategoryId).
          OfType<DB.DirectShapeType>().
          Select(x => x.FamilyName).
          Distinct().
          OrderBy(x => x);

        foreach (var familyName in familyNames)
          this.familyName.Items.Add(familyName);
      }
    }

    void RefreshTypeNameList(string familyName)
    {
      typeName.Items.Clear();

      using (var collector = new DB.FilteredElementCollector(Document))
      {
        var typeCollector = collector.WhereElementIsElementType().
          WhereElementIsKindOf(typeof(DB.DirectShapeType)).
          OfCategoryId(CategoryId);

        if (!string.IsNullOrEmpty(familyName))
          typeCollector = typeCollector.WhereParameterEqualsTo(DB.BuiltInParameter.ALL_MODEL_FAMILY_NAME, familyName);

        foreach (var name in typeCollector.OfType<DB.DirectShapeType>().Select(x => x.Name).Distinct().OrderBy(x => x))
          typeName.Items.Add(name);
      }
    }

    private void FileSelector_SelectedKeyChanged(object sender, EventArgs e)
    {
      DefaultButton.Enabled = File.Exists(fileSelector.SelectedKey);
      var selectedIndex = fileSelector.SelectedIndex;
      if (selectedIndex > 0)
      {
        var item = fileSelector.Items[selectedIndex];
        fileSelector.Items.RemoveAt(selectedIndex);
        fileSelector.Items.Insert(0, item);
        fileSelector.SelectedIndex = 0;
      }

      if (Eto.Wpf.IO.ShellItem.TryGetImage(fileSelector.SelectedKey, out var bitmap))
      {
        imageView.BackgroundColor = Colors.White;
        imageView.Image = bitmap;
      }
      else
      {
        imageView.BackgroundColor = Colors.DarkGray;
        imageView.Image = default;
      }

      typeName.Text = Path.GetFileNameWithoutExtension(fileSelector.SelectedKey);
      fileSelector.ToolTip = fileSelector.SelectedKey;
    }

    private void FileBrowser_Click(object sender, EventArgs e)
    {
      var open = new OpenFileDialog()
      {
        Filters = { "Rhino 3D models (*.3dm)|*.3dm" }
      };

      if (open.ShowDialog(this) != DialogResult.Ok)
        return;

      fileSelector.Items.Add(open.FileName, open.FileName);
      fileSelector.SelectedKey = open.FileName;
    }

    private void ImportButton_Click(object sender, EventArgs e)
    {
      if (!DB.NamingUtils.IsValidName(FamilyName))
      {
        MessageBox.Show
        (
          this,
          "Family Name cannot contain any of the following characters" + Environment.NewLine +
          "\\ : { } [ ] | ; < > ? ` ~" + Environment.NewLine +
          "or any of the non printable characters.",
          MessageBoxButtons.OK,
          MessageBoxType.Warning
        );

        return;
      }

      if (!DB.NamingUtils.IsValidName(TypeName))
      {
        MessageBox.Show
        (
          this,
          "Type Name cannot contain any of the following characters" + Environment.NewLine +
          "\\ : { } [ ] | ; < > ? ` ~" + Environment.NewLine +
          "or any of the non printable characters.",
          MessageBoxButtons.OK,
          MessageBoxType.Warning
        );

        return;
      }

      using (var collector = new DB.FilteredElementCollector(Document))
      {
        var typeCollector = collector.WhereElementIsElementType().
          WhereElementIsKindOf(typeof(DB.DirectShapeType)).
          OfCategoryId(CategoryId).
          WhereParameterEqualsTo(DB.BuiltInParameter.ALL_MODEL_FAMILY_NAME, FamilyName).
          WhereParameterEqualsTo(DB.BuiltInParameter.ALL_MODEL_TYPE_NAME, TypeName);

        if (typeCollector.FirstElement() is DB.DirectShapeType)
        {
          switch
          (
            MessageBox.Show
            (
              this,
              $"Direct Shape type '{FamilyName} : {TypeName}' already exists?" + Environment.NewLine +
              Environment.NewLine +
              "Do you want to override it?",
              MessageBoxButtons.YesNoCancel,
              MessageBoxType.Question
            )
          )
          {
            case DialogResult.No: return;
            case DialogResult.Cancel: Close(DialogResult.Cancel); return;
          }
        }
      }

      for (int i = 0; i < 10; ++i)
      {
        var fileName = i < fileSelector.Items.Count ? fileSelector.Items[i].Key : default;
        AddinOptions.Current.CustomOptions.Set(ImportOptions, $"FileNameMRU{i}", fileName);
      }

      AddinOptions.Save();
      Close(DialogResult.Ok);
    }
  }
}
