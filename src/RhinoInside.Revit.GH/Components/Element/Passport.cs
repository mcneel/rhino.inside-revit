using System;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.36")]
  public class ElementPassport : Component
  {
    public override Guid ComponentGuid => new Guid("BD534A54-B7ED-4D56-AA53-1F446D445DF6");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => "PASS";

    public ElementPassport()
    : base("Element Passport", "Passport", "Element identity within the document that references it.", "Revit", "Element")
    {
      Scope = ElementScope.Absolute;
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "Document ID", "DID", "A unique identifier for the document the Element resides", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.ElementSource(), "Model", "M", "The model that references this element", GH_ParamAccess.item);
      manager.AddTextParameter("Unique ID", "UID", "A stable across upgrades and workset operations unique identifier for the Element", GH_ParamAccess.item);
      manager.AddTextParameter("Id", "ID", "A unique identifier for an Element within the document that contains it", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var reference = default(Types.Reference);
      if (!DA.GetData(0, ref reference) || reference is null) return;

      switch (Scope)
      {
        case ElementScope.Local:
          DA.SetData(0, reference.Document.GetPersistentGUID());
          DA.SetData(1, reference.Document);
          DA.SetData(2, reference.UniqueId);
          DA.SetData(3, $"{reference.Id.ToValue()}");
          break;

        case ElementScope.Absolute:
          var absoluteId = External.DB.ReferenceId.Parse(reference.ReferenceUniqueId, reference.ReferenceDocument);
          DA.SetData(0, reference.ReferenceDocumentId);
          DA.SetData(1, reference.Source);
          DA.SetData(2, absoluteId.ToStableRepresentation(reference.ReferenceDocument));
          DA.SetData(3, absoluteId.ToStableRepresentation(null));
          break;

        case ElementScope.Persistent:
          var persistentId = External.DB.ReferenceId.Parse(reference.ReferenceUniqueId, reference.ReferenceDocument);
          DA.SetData(0, reference.ReferenceDocumentId);
          DA.SetData(1, null);
          DA.SetData(2, persistentId.ToString(reference.ReferenceDocument));
          DA.SetData(3, null);
          break;
      }
    }

    protected override void AfterSolveInstance()
    {
      base.AfterSolveInstance();
      Message = Scope.ToString();
    }

    #region UI
    enum ElementScope
    {
      Local = 0,
      Absolute = 1,
      Persistent = 2
    }
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, $"{ElementScope.Local} references", (o, s) => ElementScopeMenuClicked(ElementScope.Local), true, Scope == ElementScope.Local);
      Menu_AppendItem(menu, $"{ElementScope.Absolute} references", (o, s) => ElementScopeMenuClicked(ElementScope.Absolute), true, Scope == ElementScope.Absolute);
#if DEBUG
      Menu_AppendItem(menu, $"{ElementScope.Persistent} references", (o, s) => ElementScopeMenuClicked(ElementScope.Persistent), true, Scope == ElementScope.Persistent);
#endif
    }

    private ElementScope Scope
    {
      get => (ElementScope) GetValue(nameof(Scope), (int) ElementScope.Local);
      set
      {
        if (Scope == value) return;
        SetValue(nameof(Scope), (int) value);
      }
    }

    void ElementScopeMenuClicked(ElementScope scope)
    {
      RecordUndoEvent($"Set: {scope} references");
      Scope = scope;
      ExpireSolution(true);
    }
    #endregion
  }
}
