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
}
