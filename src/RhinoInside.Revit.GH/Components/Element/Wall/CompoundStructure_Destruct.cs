using System;
using System.Linq;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CompoundStructure_Destruct : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("D0853B76-49FA-4BA8-869C-293A9C30FFE1");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "CSd";

    public CompoundStructure_Destruct() : base(
      name: "Compound Structure (Destruct)",
      nickname: "CS(D)",
      description: "Destructs given compound structure into its properties",
      category: "Revit",
      subCategory: "Analyse"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.DataObject<DB.CompoundStructure>(),
        name: "Compound Structure",
        nickname: "CS",
        description: "Compound Structure",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddNumberParameter(
          name: "Width",
          nickname: "W",
          description: "Total width of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.DataObject<DB.CompoundStructureLayer>(),
          name: "Layers",
          nickname: "L",
          description: "Individual layers of compound structure",
          access: GH_ParamAccess.list
          );
      manager.AddIntegerParameter(
          name: "Layer Count",
          nickname: "LC",
          description: "Number of layers in compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Cutoff Height",
          nickname: "COH",
          description: "Cutoff height or compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.EndCapCondition_ValueList(),
          name: "End Cap Condition",
          nickname: "ECC",
          description: "End cap condition of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Has Structural Deck",
          nickname: "HSD?",
          description: "Whether compound structure has structural deck",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Is Vertically Compound",
          nickname: "IVC?",
          description: "Whether compound structure is vertically compound",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Sample Height",
          nickname: "SH",
          description: "Sample height of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Minimum Sample Height",
          nickname: "MSH",
          description: "Minimum sample height of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.OpeningWrappingCondition_ValueList(),
          name: "Opening Wrapping Condition",
          nickname: "OWC",
          description: "Opening wrapping condition or compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddIntegerParameter(
          name: "Structural Material Index",
          nickname: "SMIDX",
          description: "Index of layer whose material defines the structural properties of the type for the purposes of analysis",
          access: GH_ParamAccess.item
          );
      manager.AddIntegerParameter(
          name: "Variable Layer Index",
          nickname: "VLIDX",
          description: "Index of layer which is designated as variable",
          access: GH_ParamAccess.item
          );
      manager.AddIntegerParameter(
          name: "First Core Layer Index",
          nickname: "FCLIDX",
          description: "Index of first core layer of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddIntegerParameter(
          name: "Last Core Layer Index",
          nickname: "LCLIDX",
          description: "Index of last core layer of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Minimum Allowable Layer Thickness",
          nickname: "MLT",
          description: "Minimum thickness allowed for compound structure layers",
          access: GH_ParamAccess.item
          );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      Types.DataObject<DB.CompoundStructure> dataObj = default;
      if (!DA.GetData("Compound Structure", ref dataObj))
        return;

      DB.CompoundStructure cstruct = dataObj.Value;

      // destruct the data object into output params
      DA.SetData("Width", cstruct.GetWidth());
      DA.SetDataList("Layers", cstruct.GetLayers().Select(x => new Types.DataObject<DB.CompoundStructureLayer>(apiObject: x, srcDocument: dataObj.Document)).ToList());
      DA.SetData("Layer Count", cstruct.LayerCount);
      DA.SetData("Cutoff Height", cstruct.CutoffHeight);
      DA.SetData("End Cap Condition", new Types.EndCapCondition(cstruct.EndCap));
      DA.SetData("Has Structural Deck", cstruct.HasStructuralDeck);
      DA.SetData("Is Vertically Compound", cstruct.IsVerticallyCompound);
      DA.SetData("Sample Height", cstruct.SampleHeight);
      DA.SetData("Minimum Sample Height", cstruct.MinimumSampleHeight);
      DA.SetData("Opening Wrapping Condition", new Types.OpeningWrappingCondition(cstruct.OpeningWrapping));
      DA.SetData("Structural Material Index", cstruct.StructuralMaterialIndex);
      DA.SetData("Variable Layer Index", cstruct.VariableLayerIndex);
      DA.SetData("First Core Layer Index", cstruct.GetFirstCoreLayerIndex());
      DA.SetData("Last Core Layer Index", cstruct.GetLastCoreLayerIndex());
      DA.SetData("Minimum Allowable Layer Thickness", DB.CompoundStructure.GetMinimumLayerThickness());
    }
  }
}
