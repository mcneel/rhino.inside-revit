using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Materials
{
  [ComponentVersion(introduced: "1.4")]
  public class QueryAppearanceAssets : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("716903D0-867A-404A-8C23-878EFCF8115A");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "A";

    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.AppearanceAssetElement));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.MaterialAssets);
      Menu_AppendItem
      (
        menu, $"Open Material Assets…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryAppearanceAssets() : base
    (
      name: "Query Appearance Assets",
      nickname: "Appearance Assetss",
      description: "Get document appearance assets list",
      category: "Revit",
      subCategory: "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition (new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Asset name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Library", "L", "Library name", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Title", "T", "Asset title", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.AppearanceAsset>("Assets", "A", "Assets list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      Params.TryGetData(DA, "Name", out string name);
      Params.TryGetData(DA, "Library", out string library);
      Params.TryGetData(DA, "Title", out string title);
      Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var materialsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          materialsCollector = materialsCollector.WherePasses(filter);

        var assets = collector.Cast<ARDB.AppearanceAssetElement>();

        if (!string.IsNullOrEmpty(name))
          assets = assets.Where(x => x.Name.IsSymbolNameLike(name));

        if (library is object || title is object)
        {
          assets = assets.Where
          (
            x =>
            {
              var pass = true;
              using (var asset = x.GetRenderingAsset())
              {
                pass &= asset.LibraryName.IsSymbolNameLike(library);
                pass &= asset.Title.IsSymbolNameLike(title);
              }

              return pass;
            }
          );
        }

        DA.SetDataList
        (
          "Assets",
          assets.
          Select(x => new Types.AppearanceAssetElement(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
