using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.GH.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Insert
{
  [ComponentVersion(introduced: "1.36")]
  public class CADReadOptions : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("3CB945C3-CA4A-43F0-8793-E6F99B928682");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public CADReadOptions() : base
    (
      name: "CAD Read Options",
      nickname: "CAD-Read",
      description: "Configure options to read CAD files.",
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
          Name = "Options",
          NickName = "O",
          Description = "CAD read options object (optional, will create new if not provided)",
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Param_Enum<Types.ImportUnit>
        {
          Name = "Unit",
          NickName = "U",
          Description = "Unit system for the import",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Custom Scale",
          NickName = "Cs",
          Description = "Custom Unit scale",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Orient To View",
          NickName = "Ov",
          Description = "Place the import at the same orientation as the provided View.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Correct Off Axis Lines",
          NickName = "AC",
          Description = "Enable or disable correcting lines that are slightly off-axis",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Enum<Types.ImportColorMode>
        {
          Name = "Color Mode",
          NickName = "Cm",
          Description = "Color mode for the import",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Visible Layers Only",
          NickName = "Vlo",
          Description = "Import only visible layers.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Layers",
          NickName = "L",
          Description = "Import only layers this list.",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_GenericObject
        {
          Name = "Options",
          NickName = "O",
          Description = "CAD import options object",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Enum<Types.ImportUnit>
        {
          Name = "Unit",
          NickName = "U",
          Description = "Unit system for the import",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Custom Scale",
          NickName = "Cs",
          Description = "Custom Unit scale",
          Optional = true,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Orient To View",
          NickName = "Ov",
          Description = "Place the import at the same orientation as the provided View.",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Correct Off Axis Lines",
          NickName = "AC",
          Description = "Enable or disable correcting lines that are slightly off-axis",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Enum<Types.ImportColorMode>
        {
          Name = "Color Mode",
          NickName = "Cm",
          Description = "Color mode for the import",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Visible Layers Only",
          NickName = "Vlo",
          Description = "Import only visible layers.",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Layers",
          NickName = "L",
          Description = "Import only layers this list.",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Secondary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Options", out Types.CADReadOptions input)) return;
      if (!Params.TryGetData(DA, "Unit", out ARDB.ImportUnit? unit)) return;
      if (!Params.TryGetData(DA, "Custom Scale", out double? scale)) return;
      if (!Params.TryGetData(DA, "Orient To View", out bool? orientToView)) return;
      if (!Params.TryGetData(DA, "Correct Off Axis Lines", out bool? autocorrectOffAxisLines)) return;
      if (!Params.TryGetData(DA, "Color Mode", out ARDB.ImportColorMode? colorMode)) return;
      if (!Params.TryGetData(DA, "Visible Layers Only", out bool? visibleLayers)) return;
      if (!Params.TryGetDataList(DA, "Layers", out IList<string> layerSelection)) return;

      var options = new Types.CADReadOptions()
      {
        Unit = unit ?? input?.Unit,
        CustomScale = scale ?? input?.CustomScale,
        OrientToView = orientToView ?? input?.OrientToView,
        AutocorrectOffAxisLines = autocorrectOffAxisLines ?? input?.AutocorrectOffAxisLines,
        ColorMode = colorMode ?? input?.ColorMode,
        VisibleLayersOnly = visibleLayers ?? input?.VisibleLayersOnly,
        LayerSelection = layerSelection ?? input?.LayerSelection,
      };

      DA.SetData("Options", options);
      Params.TrySetData(DA, "Orient To View", () => options.OrientToView);
      Params.TrySetData(DA, "Unit", () => options.Unit);
      Params.TrySetData(DA, "Custom Scale", () => options.CustomScale);
      Params.TrySetData(DA, "Correct Off Axis Lines", () => options.AutocorrectOffAxisLines);
      Params.TrySetData(DA, "Color Mode", () => options.ColorMode);
      Params.TrySetData(DA, "Visible Layers Only", () => options.VisibleLayersOnly);
      Params.TrySetDataList(DA, "Layers", () => options.LayerSelection?.OrderBy(x => x));
    }
  }
}
