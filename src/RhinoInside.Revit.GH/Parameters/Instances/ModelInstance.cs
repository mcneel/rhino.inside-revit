using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ModelInstance : PersistentParam<Types.IGH_ModelInstance>, Kernel.IGH_ReferenceParam
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("EF2EFE84-8B2E-4613-A352-B1CE95671238");
    protected override string IconTag => DefaultNickName;

    internal static readonly string DefaultName = "Model";
    internal static readonly string DefaultNickName = "M";
    internal static readonly string DefaultDescription = "Contains a collection of Revit models";

    public ModelInstance() : base
    (
      name: DefaultName,
      nickname: DefaultName,
      description: DefaultDescription,
      category: "Params",
      subcategory: "Revit"
    )
    { }

    protected override Types.IGH_ModelInstance PreferredCast(object data)
    {
      switch (data)
      {
        case ARDB.RevitLinkInstance _: return Types.RevitLinkInstance.FromValue(data) as Types.RevitLinkInstance;
        case ARDB.Document _: return Types.ProjectDocument.FromValue(data);
      }

      return null;
    }

    public static bool GetModelOrCurrent(IGH_Component component, IGH_DataAccess DA, out Types.IGH_ModelInstance model)
    {
      model = default;

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
        if (Document.TryGetCurrentDocument(component, out var document) && document is Types.IGH_ModelInstance instance)
        {
          model = instance;
          return true;
        }

        return false;
      }

      return DA.GetData(_Model_, ref model);
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
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    #endregion
  }
}
