using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

using DB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class LayerFunction_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("439BA763-7B63-4701-BCB3-764F4D9748BD");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public LayerFunction_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Layer Function";
      NickName = "LF";
      Description = "Picker for layer function of a wall compound structure layer";

      ListItems.Clear();
      ListItems.Add(
        new GH_ValueListItem("Structure", ((int) DB.MaterialFunctionAssignment.Structure).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Substrate", ((int) DB.MaterialFunctionAssignment.Substrate).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Insulation", ((int) DB.MaterialFunctionAssignment.Insulation).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Finish 1", ((int) DB.MaterialFunctionAssignment.Finish1).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Finish 2", ((int) DB.MaterialFunctionAssignment.Finish2).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Membrane", ((int) DB.MaterialFunctionAssignment.Membrane).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("StructuralDeck", ((int) DB.MaterialFunctionAssignment.StructuralDeck).ToString())
        );
    }
  }
}
