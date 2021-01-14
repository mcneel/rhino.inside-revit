using System;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Host
{
  public class DeconstructCompoundStructureLayer : Component
  {
    public override Guid ComponentGuid => new Guid("BC64525A-10B6-46DB-A134-CF803738B1A0");
    public override GH_Exposure Exposure => GH_Exposure.senary;
    protected override string IconTag => "DCSL";

    public DeconstructCompoundStructureLayer() : base
    (
      name: "Deconstruct Compound Structure Layer",
      nickname: "DecStructLayer",
      description: "Deconstructs given compound structure layer into its properties",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.DataObject<DB.CompoundStructureLayer>(),
        name: "Compound Structure Layer",
        nickname: "CSL",
        description: "Compound Structure Layer",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddIntegerParameter(
          name: "Index",
          nickname: "IDX",
          description: "Index of the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Param_Enum<Types.LayerFunction>(),
          name: "Function",
          nickname: "F",
          description: "Function of the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Material(),
          name: "Material",
          nickname: "M",
          description: "Material assigned to the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Thickness",
          nickname: "T",
          description: "Thickness of the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Wraps",
          nickname: "W",
          description: "Whether compound structure layer participates in wrapping at end caps and/or inserts",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Element(),
          name: "Deck Profile",
          nickname: "DP",
          description: "Deck profile of structural deck layer",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Param_Enum<Types.DeckEmbeddingType>(),
          name: "Deck Embedding Type",
          nickname: "DET",
          description: "Embedding type for structural deck layer",
          access: GH_ParamAccess.item
          );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      Types.DataObject<DB.CompoundStructureLayer> dataObj = default;
      if (!DA.GetData("Compound Structure Layer", ref dataObj))
        return;

      var cslayer = dataObj.Value;

      // Deconstruct the data object into output params
      DA.SetData("Index", cslayer.LayerId);
      DA.SetData("Function", cslayer.Function);
      DA.SetData("Material", Types.Element.FromElement(dataObj.Document.GetElement(cslayer.MaterialId)));
      DA.SetData("Thickness", cslayer.Width * Revit.ModelUnits);
      DA.SetData("Wraps", cslayer.LayerCapFlag);
      DA.SetData("Deck Profile", Types.Element.FromElement(dataObj.Document.GetElement(cslayer.DeckProfileId)));
      DA.SetData("Deck Embedding Type", cslayer.DeckEmbeddingType);
    }
  }
}
