using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.10")]
  public class AddDraftingView : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("C62D18A8-93C5-406F-8C13-778D50A16B8D");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddDraftingView() : base
    (
      name: "Add Drafting View",
      nickname: "DraftingView",
      description: "Given a name, it adds a drafting view to the active Revit document",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
       (
        new Param_String
        {
          Name = "Name",
          NickName = "N",
          Description = "View name",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ViewFamilyType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Drafting view type",
          Optional = true,
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "Template",
          NickName = "T",
          Description = $"Template drafting view (only parameters are copied)",
          Optional = true
        }, ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = _View_,
          NickName = _View_.Substring(0, 1),
          Description = $"Output {_View_}",
        }
      )
    };

    protected const string _View_ = "View";

    public static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
{
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.VIEW_NAME,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ViewDrafting>
      (
        doc.Value, _View_, viewDrafting =>
        {
          // Input
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Parameters.ViewFamilyType.GetDataOrDefault(this, DA, "Type", out Types.ViewFamilyType type, doc, ARDB.ElementTypeGroup.ViewTypeDrafting)) return null;
          Params.TryGetData(DA, "Template", out ARDB.ViewDrafting template);

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_View_, out var untracked, ref viewDrafting, doc.Value, name, ARDB.ViewType.DraftingView.ToString()))
            viewDrafting = Reconstruct(viewDrafting, type.Value, name, template);

          DA.SetData(_View_, viewDrafting);
          return untracked ? null : viewDrafting;
        }
      );
    }

    bool Reuse(ARDB.ViewDrafting viewDrafting, ARDB.ViewFamilyType type)
    {
      if (viewDrafting is null) return false;
      if (type.Id != viewDrafting.GetTypeId()) viewDrafting.ChangeTypeId(type.Id);

      return true;
    }

    ARDB.ViewDrafting Create(ARDB.ViewFamilyType type)
    {
      return ARDB.ViewDrafting.Create(type.Document, type.Id);
    }

    ARDB.ViewDrafting Reconstruct(ARDB.ViewDrafting viewDrafting, ARDB.ViewFamilyType type, string name, ARDB.ViewDrafting template)
    {
      if (!Reuse(viewDrafting, type))
        viewDrafting = Create(type);

      viewDrafting.CopyParametersFrom(template, ExcludeUniqueProperties);
      if (name is object) viewDrafting?.get_Parameter(ARDB.BuiltInParameter.VIEW_NAME).Update(name);

      return viewDrafting;
    }
  }
}
