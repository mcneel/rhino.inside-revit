using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Dimension : GraphicalElementT<Types.Dimension, ARDB.Dimension>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override Guid ComponentGuid => new Guid("BC546B0C-1BF0-48C6-AAA9-F4FD429DAD39");

    public Dimension() : base
    (
      name: "Dimension",
      nickname: "Dimension",
      description: "Contains a collection of Revit dimension elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Curve", }
    );

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var create = Menu_AppendItem(menu, $"Set new {TypeName}");

      var AlignedDimensionId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.AlignedDimension);
      Menu_AppendItem
      (
        create.DropDown, "Aligned",
        Menu_PromptNew(AlignedDimensionId),
        Revit.ActiveUIApplication.CanPostCommand(AlignedDimensionId),
        false
      );

      var LinearDimensionId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.LinearDimension);
      Menu_AppendItem
      (
        create.DropDown, "Linear",
        Menu_PromptNew(LinearDimensionId),
        Revit.ActiveUIApplication.CanPostCommand(LinearDimensionId),
        false
      );

      var AngularDimensionId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.AngularDimension);
      Menu_AppendItem
      (
        create.DropDown, "Angular",
        Menu_PromptNew(AngularDimensionId),
        Revit.ActiveUIApplication.CanPostCommand(AngularDimensionId),
        false
      );

      var RadialDimensionId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.RadialDimension);
      Menu_AppendItem
      (
        create.DropDown, "Radial",
        Menu_PromptNew(RadialDimensionId),
        Revit.ActiveUIApplication.CanPostCommand(RadialDimensionId),
        false
      );

      var DiameterDimensionId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.DiameterDimension);
      Menu_AppendItem
      (
        create.DropDown, "Diamenter",
        Menu_PromptNew(DiameterDimensionId),
        Revit.ActiveUIApplication.CanPostCommand(DiameterDimensionId),
        false
      );

      var ArcLengthDimensionId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ArcLengthDimension);
      Menu_AppendItem
      (
        create.DropDown, "Arc Length",
        Menu_PromptNew(ArcLengthDimensionId),
        Revit.ActiveUIApplication.CanPostCommand(ArcLengthDimensionId),
        false
      );

      var SpotElevationId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SpotElevation);
      Menu_AppendItem
      (
        create.DropDown, "Spot Elevation",
        Menu_PromptNew(SpotElevationId),
        Revit.ActiveUIApplication.CanPostCommand(SpotElevationId),
        false
      );

      var SpotCoordinateId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SpotCoordinate);
      Menu_AppendItem
      (
        create.DropDown, "Spot Coordinate",
        Menu_PromptNew(SpotCoordinateId),
        Revit.ActiveUIApplication.CanPostCommand(SpotCoordinateId),
        false
      );

      var SpotSlopeId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SpotSlope);
      Menu_AppendItem
      (
        create.DropDown, "Spot Slope",
        Menu_PromptNew(SpotSlopeId),
        Revit.ActiveUIApplication.CanPostCommand(SpotSlopeId),
        false
      );
    }
    #endregion
  }

  public class DimensionType : ElementType<Types.DimensionType, ARDB.DimensionType>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("1554AF4F-19C2-49E7-B836-28383AF7F035");

    public DimensionType() : base
    (
      name: "Dimension Type",
      nickname: "DimType",
      description: "Contains a collection of Revit dimension types",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var open = Menu_AppendItem(menu, $"Open");

      var LinearDimensionTypesId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.LinearDimensionTypes);
      Menu_AppendItem
      (
        open.DropDown, "Linear types…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, LinearDimensionTypesId),
        activeApp.CanPostCommand(LinearDimensionTypesId), false
      );

      var AngularDimensionTypesId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.AngularDimensionTypes);
      Menu_AppendItem
      (
        open.DropDown, "Angular types…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, AngularDimensionTypesId),
        activeApp.CanPostCommand(AngularDimensionTypesId), false
      );

      var RadialDimensionTypesId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.RadialDimensionTypes);
      Menu_AppendItem
      (
        open.DropDown, "Radial types…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, RadialDimensionTypesId),
        activeApp.CanPostCommand(RadialDimensionTypesId), false
      );

      var DiameterDimensionTypesId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.DiameterDimensionTypes);
      Menu_AppendItem
      (
        open.DropDown, "Diameter types…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, DiameterDimensionTypesId),
        activeApp.CanPostCommand(DiameterDimensionTypesId), false
      );

      var SpotElevationTypesId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SpotElevationTypes);
      Menu_AppendItem
      (
        open.DropDown, "Spot elevation types…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, SpotElevationTypesId),
        activeApp.CanPostCommand(SpotElevationTypesId), false
      );

      var SpotCoordinateTypesId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SpotCoordinateTypes);
      Menu_AppendItem
      (
        open.DropDown, "Spot coordinate types…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, SpotCoordinateTypesId),
        activeApp.CanPostCommand(SpotCoordinateTypesId), false
      );

      var SpotSlopeTypesId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SpotSlopeTypes);
      Menu_AppendItem
      (
        open.DropDown, "Spot slope types…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, SpotSlopeTypesId),
        activeApp.CanPostCommand(SpotSlopeTypesId), false
      );
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {

    }
    #endregion
  }
}
