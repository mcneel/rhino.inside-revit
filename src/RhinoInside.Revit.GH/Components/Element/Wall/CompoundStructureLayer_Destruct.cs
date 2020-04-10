using System;
using System.Linq;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CompoundStructureLayer_Destruct : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("BC64525A-10B6-46DB-A134-CF803738B1A0");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "CSLd";

    public CompoundStructureLayer_Destruct() : base(
      name: "Compound Structure Layer (Destruct)",
      nickname: "CSL(D)",
      description: "Destructs given compound structure layer into its properties",
      category: "Revit",
      subCategory: "Analyse"
    )
    {
    }

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
          name: "Layer Id",
          nickname: "ID",
          description: "Id of the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.LayerFunction_ValueList(),
          name: "Layer Function",
          nickname: "LF",
          description: "Function of the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Layer Width",
          nickname: "W",
          description: "Width of the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Layer Cap Flag",
          nickname: "CF?",
          description: "Whether compound structure layer participates in wrapping at end caps and/or inserts",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Material(),
          name: "Layer Material",
          nickname: "M",
          description: "Material assigned to the given compound structure layer",
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
          param: new Parameters.DeckEmbeddingType_ValueList(),
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

      DB.CompoundStructureLayer cslayer = dataObj.Value;

      // destruct the data object into output params
      DA.SetData("Layer Id", cslayer.LayerId);
      DA.SetData("Layer Function", new Types.LayerFunction(cslayer.Function));
      DA.SetData("Layer Width", cslayer.Width);
      DA.SetData("Layer Cap Flag", cslayer.LayerCapFlag);
      DA.SetData("Layer Material", Types.Element.FromElement(dataObj.Document.GetElement(cslayer.MaterialId)));
      DA.SetData("Deck Profile", Types.Element.FromElement(dataObj.Document.GetElement(cslayer.DeckProfileId)));
      DA.SetData("Deck Embedding Type", new Types.DeckEmbeddingType(cslayer.DeckEmbeddingType));
    }
  }
}
