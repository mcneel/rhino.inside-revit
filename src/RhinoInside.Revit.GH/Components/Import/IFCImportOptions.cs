using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Insert
{
  [ComponentVersion(introduced: "1.36")]
  public class IFCImportOptions : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("A8F3E5C1-9D4B-4E2A-B8F1-3C5D6E7F8A9B");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public IFCImportOptions() : base
    (
      name: "IFC Import Options",
      nickname: "IFCOpt",
      description: "Configure options for IFC file import or link operations",
      category: "Revit",
      subCategory: "Insert"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Param_GenericObject
        {
          Name = "IFC Import Options",
          NickName = "O",
          Description = "IFC import options object (optional, will create new if not provided)",
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Param_Integer
        {
          Name = "Intent",
          NickName = "I",
          Description = "Import intent:\n  0 = Parametric (standard Revit elements for editing)\n  1 = Reference (lightweight accurate representations, not editable)",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Integer
        {
          Name = "Action",
          NickName = "A",
          Description = "Import action:\n  0 = Open (open IFC as new document)\n  1 = Link (link IFC to current document)",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Auto Join",
          NickName = "AJ",
          Description = "Enable or disable auto-join at the end of import",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Autocorrect Off Axis Lines",
          NickName = "AC",
          Description = "Enable or disable correcting lines that are slightly off-axis",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_GenericObject
        {
          Name = "IFC Import Options",
          NickName = "O",
          Description = "IFC import options object",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Integer
        {
          Name = "Intent",
          NickName = "I",
          Description = "Import intent value",
          Access = GH_ParamAccess.item
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Integer
        {
          Name = "Action",
          NickName = "A",
          Description = "Import action value",
          Access = GH_ParamAccess.item
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Auto Join",
          NickName = "AJ",
          Description = "Auto-join enabled",
          Access = GH_ParamAccess.item
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Autocorrect Off Axis Lines",
          NickName = "AC",
          Description = "Autocorrect off-axis lines enabled",
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      ARDB.IFC.IFCImportOptions options = null;
      if (Params.TryGetData(DA, "IFC Import Options", out ARDB.IFC.IFCImportOptions inputOptions) && inputOptions != null)
        options = inputOptions;
      else
        options = new ARDB.IFC.IFCImportOptions();

      if (Params.TryGetData(DA, "Intent", out int? intent) && intent.HasValue)
      {
        if (Enum.IsDefined(typeof(ARDB.IFC.IFCImportIntent), intent.Value))
          options.Intent = (ARDB.IFC.IFCImportIntent)intent.Value;
      }

      if (Params.TryGetData(DA, "Action", out int? action) && action.HasValue)
      {
        if (Enum.IsDefined(typeof(ARDB.IFC.IFCImportAction), action.Value))
          options.Action = (ARDB.IFC.IFCImportAction)action.Value;
      }

      if (Params.TryGetData(DA, "Auto Join", out bool? autoJoin) && autoJoin.HasValue)
        options.AutoJoin = autoJoin.Value;

      if (Params.TryGetData(DA, "Autocorrect Off Axis Lines", out bool? autocorrect) && autocorrect.HasValue)
        options.AutocorrectOffAxisLines = autocorrect.Value;

      DA.SetData("IFC Import Options", options);

      if (options != null)
      {
        DA.SetData("Intent", (int)options.Intent);
        DA.SetData("Action", (int)options.Action);
        DA.SetData("Auto Join", options.AutoJoin);
        DA.SetData("Autocorrect Off Axis Lines", options.AutocorrectOffAxisLines);
      }
    }
  }
}
