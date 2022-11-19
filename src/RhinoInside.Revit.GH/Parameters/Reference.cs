using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Bake
{
  public interface IGH_BakeAwareElement : IGH_BakeAwareData
  {
    bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    );
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;

  public abstract class Reference<T> :
  PersistentParam<T>,
  IGH_BakeAwareObject,
  Kernel.IGH_ReferenceParam
  where T : class, Types.IGH_Reference
  {
    public override string TypeName
    {
      get
      {
        var name = typeof(T).GetTypeInfo().GetCustomAttribute(typeof(Kernel.Attributes.NameAttribute)) as Kernel.Attributes.NameAttribute;
        return name?.Name ?? typeof(T).Name;
      }
    }

    protected Reference(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    #region UI
    public override bool AppendMenuItems(ToolStripDropDown menu)
    {
      // Name
      if (IconCapableUI && Attributes.IsTopLevel)
        Menu_AppendObjectNameEx(menu);
      else
        Menu_AppendObjectName(menu);

      // Preview
      if (this is IGH_PreviewObject preview)
      {
        if (Attributes.IsTopLevel && preview.IsPreviewCapable)
          Menu_AppendPreviewItem(menu);
      }

      // Enabled
      if (Kind == GH_ParamKind.floating)
        Menu_AppendEnableItem(menu);

      // Bake
      Menu_AppendBakeItem(menu);

      // Runtime messages
      Menu_AppendRuntimeMessages(menu);

      // Custom items.
      AppendAdditionalMenuItems(menu);
      Menu_AppendSeparator(menu);

      // Publish.
      Menu_AppendPublish(menu);

      // Help.
      Menu_AppendObjectHelp(menu);

      return true;
    }

    protected new virtual void Menu_AppendBakeItem(ToolStripDropDown menu)
    {
      if (this is IGH_BakeAwareObject bakeObject)
      {
        if (Grasshopper.Instances.DocumentEditor.MainMenuStrip.Items.Find("mnuBakeSelected", true).
          OfType<ToolStripMenuItem>().FirstOrDefault() is ToolStripMenuItem menuItem)
          Menu_AppendItem(menu, "Bake…", Menu_BakeItemClick, menuItem?.Image, bakeObject.IsBakeCapable, false);
        else
          Menu_AppendItem(menu, "Bake…", Menu_BakeItemClick, bakeObject.IsBakeCapable, false);
      }
    }

    void Menu_BakeItemClick(object sender, EventArgs e)
    {
      if (this is IGH_BakeAwareObject bakeObject)
      {
        if (Rhino.Commands.Command.InCommand())
        {
          MessageBox.Show
          (
            Form.ActiveForm,
            $"We're sorry but Baking is only possible{Environment.NewLine}" +
            "when no other Commands are running.",
            "Bake failure",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
          );
        }
        else if (RhinoDoc.ActiveDoc is RhinoDoc doc)
        {
          var redrawEnabled = doc.Views.RedrawEnabled;
          doc.Views.RedrawEnabled = false;
          var ur = doc.BeginUndoRecord("GrasshopperBake");
          try
          {
            Grasshopper.Plugin.Commands.BakeObject = this;

            var guids = new List<Guid>();
            bakeObject.BakeGeometry(doc, default, guids);
          }
          finally
          {
            Grasshopper.Plugin.Commands.BakeObject = default;
            doc.EndUndoRecord(ur);
            doc.Views.RedrawEnabled = redrawEnabled;
          }

          // Update views to show baked objects
          doc.Views.Redraw();
        }
      }
    }

    protected override void PrepareForPrompt() { }
    protected override void RecoverFromPrompt() { }
    #endregion

    #region IGH_ReferenceParam
    bool Kernel.IGH_ReferenceParam.NeedsToBeExpired
    (
      ARDB.Document doc,
      ICollection<ARDB.ElementId> added,
      ICollection<ARDB.ElementId> deleted,
      ICollection<ARDB.ElementId> modified
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

    #region IGH_BakeAwareObject
    public bool IsBakeCapable => VolatileData.AllData(true).Any(x => x is IGH_BakeAwareData);

    public void BakeGeometry(RhinoDoc doc, List<Guid> guids) => BakeGeometry(doc, null, guids);
    public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> guids) =>
      Rhinoceros.InvokeInHostContext(() => BakeElements(doc, att, guids));
      
    public void BakeElements(RhinoDoc doc, ObjectAttributes att, List<Guid> guids)
    {
      if (doc is null) throw new ArgumentNullException(nameof(doc));
      if (guids is null) throw new ArgumentNullException(nameof(guids));
      att = att?.Duplicate() ?? doc.CreateDefaultAttributes();

      var idMap = new Dictionary<ARDB.ElementId, Guid>();

      // In case some element has no Category it should go to Root 'Revit' layer.
      if (new Types.Category().BakeElement(idMap, false, doc, att, out var layerGuid))
        att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

      bool progress = Grasshopper.Plugin.Commands.BakeObject == this &&
        1 == Rhino.UI.StatusBar.ShowProgressMeter(doc.RuntimeSerialNumber, 0, VolatileData.DataCount, "Baking…", true, true);

      try
      {
        foreach (var goo in VolatileData.AllData(true))
        {
          if (progress)
            Rhino.UI.StatusBar.UpdateProgressMeter(doc.RuntimeSerialNumber, 1, false);

          if (goo is null) continue;
          if (!goo.IsValid) continue;

          if (goo is Bake.IGH_BakeAwareElement bakeAwareElement)
          {
            if (bakeAwareElement.BakeElement(idMap, true, doc, att, out var guid))
              guids.Add(guid);
          }
          else if (goo is IGH_BakeAwareData bakeAwareData)
          {
            if (bakeAwareData.BakeGeometry(doc, att, out var guid))
              guids.Add(guid);
          }
        }
      }
      finally
      {
        if (progress) Rhino.UI.StatusBar.HideProgressMeter(doc.RuntimeSerialNumber);
      }
    }
    #endregion
  }
}
