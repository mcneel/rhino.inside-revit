using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

using DB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class DeckEmbeddingType_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("DB470316-67C1-45E0-904D-9D0137C847B2");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public DeckEmbeddingType_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Deck Embedding Type";
      NickName = "DET";
      Description = "Picker for deck embedding type of a wall compound structure layer";

      ListItems.Clear();
      ListItems.Add(
        new GH_ValueListItem("Merge with Layer Above", ((int) DB.StructDeckEmbeddingType.MergeWithLayerAbove).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Standalone", ((int) DB.StructDeckEmbeddingType.Standalone).ToString())
        );
    }
  }
}
