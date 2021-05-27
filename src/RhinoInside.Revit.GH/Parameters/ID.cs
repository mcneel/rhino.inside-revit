using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.DocObjects;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Bake
{
  public interface IGH_BakeAwareElement : IGH_BakeAwareData
  {
    bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    );
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class ElementIdParam<T, R> :
  PersistentParam<T>,
  IGH_BakeAwareObject,
  Kernel.IGH_ElementIdParam
  where T : class, Types.IGH_ElementId
  {
    public override string TypeName
    {
      get
      {
        var name = typeof(T).GetTypeInfo().GetCustomAttribute(typeof(Kernel.Attributes.NameAttribute)) as Kernel.Attributes.NameAttribute;
        return name?.Name ?? typeof(T).Name;
      }
    }

    protected ElementIdParam(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    [Flags]
    public enum DataGrouping
    {
      None = 0,
      Document = 1,
      Workset = 2,
      DesignOption = 4,
      Category = 8,
    };

    public DataGrouping Grouping { get; set; } = DataGrouping.None;

    public sealed override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int grouping = (int) DataGrouping.None;
      reader.TryGetInt32("Grouping", ref grouping);
      Grouping = (DataGrouping) grouping;

      return true;
    }
    public sealed override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (Grouping != DataGrouping.None)
        writer.SetInt32("Grouping", (int) Grouping);

      return true;
    }

    protected override void ProcessVolatileData()
    {
      if (Grouping != DataGrouping.None)
      {
        if (Kind == GH_ParamKind.floating)
        {
          if ((Grouping & DataGrouping.Document) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grouped by Document");

          if ((Grouping & DataGrouping.Workset) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grouped by Workset");

          if ((Grouping & DataGrouping.DesignOption) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grouped by Design Option");

          if ((Grouping & DataGrouping.Category) != 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grouped by Category");
        }

        var data = new GH_Structure<T>();
        var pathCount = m_data.PathCount;
        for (int p = 0; p < pathCount; ++p)
        {
          var path = m_data.Paths[p];
          var branch = m_data.get_Branch(path);
          foreach (var item in branch)
          {
            if (item is Types.IGH_ElementId value)
            {
              var group = path;

              if ((Grouping & DataGrouping.Document) != 0)
              {
                var docId = DocumentExtension.DocumentSessionId(value.DocumentGUID);
                group = group.AppendElement(docId);
              }

              if (Grouping > DataGrouping.Document)
              {
                var element = value.Document?.GetElement(value.Id);

                if ((Grouping & DataGrouping.Workset) != 0)
                {
                  var catId = element?.WorksetId?.IntegerValue ?? 0;
                  group = group.AppendElement(catId);
                }

                if ((Grouping & DataGrouping.DesignOption) != 0)
                {
                  var catId = element?.DesignOption?.Id.IntegerValue ?? 0;
                  group = group.AppendElement(catId);
                }

                if ((Grouping & DataGrouping.Category) != 0)
                {
                  var catId = element?.Category?.Id.IntegerValue ?? 0;
                  group = group.AppendElement(catId);
                }
              }

              data.Append((T) value, group);
            }
            else data.Append(null, path.AppendElement(int.MinValue));
          }
        }

        m_data = data;
      }

      base.ProcessVolatileData();
    }

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
          var ur = doc.BeginUndoRecord("GrasshopperBake");
          try
          {
            Grasshopper.Plugin.Commands.BakeObject = this;

            var guids = new List<Guid>();
            bakeObject.BakeGeometry(doc, default, guids);

            //foreach (var view in doc.Views)
            //  view.Redraw();
          }
          finally
          {
            Grasshopper.Plugin.Commands.BakeObject = default;
            doc.EndUndoRecord(ur);
          }
        }
      }
    }

    protected override void Menu_AppendPreProcessParameter(ToolStripDropDown menu)
    {
      base.Menu_AppendPreProcessParameter(menu);

      var Group = Menu_AppendItem(menu, "Group by");

      Group.Checked = Grouping != DataGrouping.None;
      Menu_AppendItem(Group.DropDown, "Document",      (s, a) => Menu_GroupBy(DataGrouping.Document),      true, (Grouping & DataGrouping.Document) != 0);
      Menu_AppendItem(Group.DropDown, "Workset",       (s, a) => Menu_GroupBy(DataGrouping.Workset),       true, (Grouping & DataGrouping.Workset) != 0);
      Menu_AppendItem(Group.DropDown, "Design Option", (s, a) => Menu_GroupBy(DataGrouping.DesignOption),  true, (Grouping & DataGrouping.DesignOption) != 0);
      Menu_AppendItem(Group.DropDown, "Category",      (s, a) => Menu_GroupBy(DataGrouping.Category),      true, (Grouping & DataGrouping.Category) != 0);
    }

    private void Menu_GroupBy(DataGrouping value)
    {
      RecordUndoEvent("Set: Grouping");

      if ((Grouping & value) != 0)
        Grouping &= ~value;
      else
        Grouping |= value;

      OnObjectChanged(GH_ObjectEventType.Options);

      if (Kind == GH_ParamKind.output)
        ExpireOwner();

      ExpireSolutionTopLevel(true);
    }

    protected override void PrepareForPrompt() { }
    protected override void RecoverFromPrompt() { }
    #endregion

    #region IGH_ElementIdParam
    bool Kernel.IGH_ElementIdParam.NeedsToBeExpired
    (
      DB.Document doc,
      ICollection<DB.ElementId> added,
      ICollection<DB.ElementId> deleted,
      ICollection<DB.ElementId> modified
    )
    {
      if (DataType != GH_ParamData.local)
        return false;

      if (Phase == GH_SolutionPhase.Blank)
        CollectData();

      foreach (var data in VolatileData.AllData(true).OfType<Types.IGH_ElementId>())
      {
        if (!data.Id.IsValid() || !data.Document.IsValid())
          continue;

        if (!doc.Equals(data.Document))
          continue;

        if (modified.Contains(data.Id))
          return true;

        if (deleted.Contains(data.Id))
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
      if (att is null) att = doc.CreateDefaultAttributes();
      else att = att.Duplicate();
      if (guids is null) throw new ArgumentNullException(nameof(guids));

      var idMap = new Dictionary<DB.ElementId, Guid>();

      // In case some element has no Category it should go to Root 'Revit' layer.
      if (new Types.Category().BakeElement(idMap, false, doc, att, out var layerGuid))
        att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

      bool progress = Grasshopper.Plugin.Commands.BakeObject == this &&
        1 == Rhino.UI.StatusBar.ShowProgressMeter(doc.RuntimeSerialNumber, 0, VolatileData.DataCount, "Baking…", true, true);

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

      if (progress)
        Rhino.UI.StatusBar.HideProgressMeter(doc.RuntimeSerialNumber);
    }
    #endregion
  }
}
