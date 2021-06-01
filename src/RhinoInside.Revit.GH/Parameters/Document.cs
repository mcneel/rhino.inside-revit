using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Document : PersistentParam<Types.IGH_Document>
  {
    public override Guid ComponentGuid => new Guid("F3427D5C-3793-4E32-B219-8172D56EF04C");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "D";

    public Document() : base("Document", "DOC", "Contains a collection of Revit documents", "Params", "Revit Primitives")
    { }

    protected override Types.IGH_Document PreferredCast(object data) => Types.Document.FromValue(data);
    protected override Types.IGH_Document InstantiateT() => new Types.Document();

    public static bool TryGetCurrentDocument(IGH_ActiveObject activeObject, out Types.Document document)
    {
      document = Types.Document.FromValue(Revit.ActiveDBDocument);
      if (document?.Value is null)
      {
        if (activeObject.GetTopLevelObject() is IGH_ActiveObject active)
        {
          if (document is null)
            active.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There is no current Revit document or is not available");
          else
            active.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Document '{document.DisplayName}' is not available");
        }

        return false;
      }

      // In case the user has more than one document open we show which one this component is working on
      if (Revit.ActiveDBApplication.Documents.Size > 1)
      {
        if (activeObject.GetTopLevelObject() is IGH_Component active)
          active.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Using current document '{document.Title}'");
      }
      return true;
    }

    public static bool TryGetDocumentOrCurrent(IGH_Component component, IGH_DataAccess DA, string name, out Types.Document document)
    {
      var _Document_ = name is null ? -1 : component.Params.IndexOfInputParam(name);
      if (_Document_ < 0)
        return TryGetCurrentDocument(component, out document);

      document = default;
      return DA.GetData(_Document_, ref document);
    }

    public static bool GetDataOrDefault(IGH_Component component, IGH_DataAccess DA, string name, out DB.Document document)
    {
      TryGetDocumentOrCurrent(component, DA, name, out var doc);
      document = doc?.Value;
      return document is object;
    }

    #region UI
    protected override GH_GetterResult Prompt_Singular(ref Types.IGH_Document value) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Plural(ref List<Types.IGH_Document> values) => GH_GetterResult.cancel;

    static DB.DocumentType GetDocumentType(DB.Document doc)
    {
      if (doc.IsFamilyDocument)
        return DB.DocumentType.Family;
      else
        return DB.DocumentType.Project;
    }

    protected EventHandler Menu_PromptFile(Autodesk.Revit.UI.RevitCommandId commandId) => async (sender, args) =>
    {
      var activeApp = Revit.ActiveUIApplication;
      using (var scope = new External.UI.EditScope(activeApp))
      {
        var activeDoc = activeApp.ActiveUIDocument?.Document;
        await scope.ExecuteCommandAsync(commandId);
        var newActiveDoc = activeApp.ActiveUIDocument?.Document;

        if (!activeDoc.IsEquivalent(newActiveDoc) && Types.Document.FromValue(newActiveDoc) is Types.Document newDocument)
        {
          RecordPersistentDataEvent("Change data");

          PersistentData.Clear();
          PersistentData.Append(newDocument);

          OnObjectChanged(GH_ObjectEventType.PersistentData);
          ExpireSolution(true);
        }
      }
    };

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0 || Revit.ActiveUIDocument is null)
        return;

      var listBox = new ListBox
      {
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale),
        Sorted = true
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      var docTypeBox = new ComboBox
      {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Tag = listBox
      };
      docTypeBox.SelectedIndexChanged += DocumentTypeBox_SelectedIndexChanged;
      docTypeBox.SetCueBanner("Document type filter…");
      docTypeBox.Items.Add(DB.DocumentType.Project);
      docTypeBox.Items.Add(DB.DocumentType.Family);

      if (PersistentValue?.Value is DB.Document current)
        RefreshDocumentsList(listBox, GetDocumentType(current));
      else
        RefreshDocumentsList(listBox, DB.DocumentType.Other);

      Menu_AppendCustomItem(menu, docTypeBox);
      Menu_AppendCustomItem(menu, listBox);
    }

    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }

    protected override void Menu_AppendManageCollection(ToolStripDropDown menu)
    {
      base.Menu_AppendManageCollection(menu);

      if (SourceCount != 0) return;

      var activeApp = Revit.ActiveUIApplication;

#if REVIT_2019
      // New
      {
        var create = Menu_AppendItem(menu, "New");

#if REVIT_2022
        var NewProjectId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.NewProject);
#else
        var NewProjectId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.NewRevitFile);
#endif
        Menu_AppendItem
        (
          create.DropDown, "Project…",
          Menu_PromptFile(NewProjectId),
          activeApp.CanPostCommand(NewProjectId),
          false
        );

#if REVIT_2022
        var NewFamilyId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.NewFamily);
#else
        var NewFamilyId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.NewFamilyFile);
#endif
        Menu_AppendItem
        (
          create.DropDown, "Family…",
          Menu_PromptFile(NewFamilyId),
          activeApp.CanPostCommand(NewFamilyId),
          false
        );

        var NewConceptualMassId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.NewConceptualMass);
        Menu_AppendItem
        (
          create.DropDown, "Conceptual Mass…",
          Menu_PromptFile(NewConceptualMassId),
          activeApp.CanPostCommand(NewConceptualMassId),
          false
        );
      }
#endif
      // Open
      {
        var open = Menu_AppendItem(menu, "Open");

        var OpenRevitFileId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.OpenRevitFile);
        Menu_AppendItem
        (
          open.DropDown, "Revit file…",
          Menu_PromptFile(OpenRevitFileId),
          activeApp.CanPostCommand(OpenRevitFileId),
          false
        );

        var documents = PersistentData.OfType<Types.Document>().
          Where(x => x.IsReferencedData && !x.IsReferencedDataLoaded).
          Where(x => x.ModelURI is object);

        if (documents.Any())
        {
          Menu_AppendSeparator(open.DropDown);
          foreach (var document in documents)
          {
            var enabled = !document.ModelURI.IsFileUri(out var localPath) || System.IO.File.Exists(localPath);
            var item = Menu_AppendItem(open.DropDown, document.PathName.TripleDotPath(64), Menu_OpenReferencedDocument, enabled);
            item.Tag = document;
          }
        }
      }
    }

    private void DocumentTypeBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox)
      {
        if (comboBox.Tag is ListBox listBox)
          RefreshDocumentsList(listBox, ((DB.DocumentType?) comboBox.SelectedItem) ?? DB.DocumentType.Other);
      }
    }

    private void RefreshDocumentsList(ListBox listBox, DB.DocumentType docType)
    {
      listBox.SelectedIndexChanged -= ListBox_SelectedIndexChanged;
      listBox.Items.Clear();

      var documents = Revit.ActiveUIApplication.
        Application.Documents.
        Cast<DB.Document>().
        Where(x => !x.IsLinked && (docType == DB.DocumentType.Other || GetDocumentType(x) == docType)).
        Select(Types.Document.FromValue);

      listBox.DisplayMember = nameof(Types.Document.DisplayName);
      listBox.Items.AddRange(documents.ToArray());
      listBox.SelectedIndex = listBox.Items.OfType<Types.IGH_Document>().IndexOf(PersistentValue, 0).FirstOr(-1);
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (sender is ListBox listBox)
      {
        if (listBox.SelectedIndex != -1)
        {
          if (listBox.Items[listBox.SelectedIndex] is Types.IGH_Document value)
          {
            RecordPersistentDataEvent($"Set: {value}");
            PersistentData.Clear();
            PersistentData.AppendRange(listBox.SelectedItems.OfType<Types.IGH_Document>());
            OnObjectChanged(GH_ObjectEventType.PersistentData);
          }
        }

        ExpireSolution(true);
      }
    }

    private void Menu_OpenReferencedDocument(object sender, EventArgs args)
    {
      if (sender is ToolStripItem item && item.Tag is Types.Document document)
      {
        if (OpenAndActivateDocument(document, true) is object)
          ExpireSolution(true);
      }
    }

    static Autodesk.Revit.UI.UIDocument OpenAndActivateDocument(Types.Document document, bool forceLoad)
    {
      try
      {
        if (document.LoadReferencedData())
        {
          return AddIn.Host.ActiveUIDocument = new Autodesk.Revit.UI.UIDocument(document.Value);
        }
        else if
        (
          forceLoad &&
          document.ModelURI.ToModelPath() is DB.ModelPath modelPath &&
          AddIn.Host.Value is Autodesk.Revit.UI.UIApplication host &&
          MessageBox.Show
          (
            Form.ActiveForm,
            $"The model '{document.FileName}' is not currently open on Revit{Environment.NewLine}" +
            $"Do you want to open it from:{Environment.NewLine}" +
            Environment.NewLine +
            $"{document.PathName}",
            "Open Model",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
          ) == DialogResult.Yes
        )
        {
          using (var options = new DB.OpenOptions())
            return host.OpenAndActivateDocument(modelPath, options, false);
        }
        else
        {
          Grasshopper.Instances.DocumentEditor.SetStatusBarEvent
          (
            new GH_RuntimeMessage
            (
              $"'{document.PathName}' is currently not available.",
              GH_RuntimeMessageLevel.Warning
            )
          );
        }
      }
      catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
      catch (Autodesk.Revit.Exceptions.ApplicationException e)
      {
        Grasshopper.Instances.DocumentEditor.SetStatusBarEvent
        (
          new GH_RuntimeMessage
          (
            e.Message,
            GH_RuntimeMessageLevel.Error,
            e.Source
          )
        );
      }
      finally
      {
        if (forceLoad)
          Grasshopper.Instances.DocumentEditor.Activate();
      }

      return default;
    }
    #endregion
  }
}
