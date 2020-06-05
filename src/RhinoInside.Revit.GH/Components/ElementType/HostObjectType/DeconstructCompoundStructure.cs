using System;
using System.Linq;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DeconstructCompoundStructure : Component
  {
    public override Guid ComponentGuid => new Guid("D0853B76-49FA-4BA8-869C-293A9C30FFE1");
    public override GH_Exposure Exposure => GH_Exposure.senary;
    protected override string IconTag => "DCS";

    public DeconstructCompoundStructure() : base
    (
      name: "Deconstruct Compound Structure",
      nickname: "DCS",
      description: "Deconstructs given compound structure into its properties",
      category: "Revit",
      subCategory: "Host"
    )
    { }

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
          name: "Total Thickness",
          nickname: "TT",
          description: "Total thickness of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Sample Height",
          nickname: "SH",
          description: "Sample height of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Cutoff Height",
          nickname: "COH",
          description: "Cutoff height or compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Minimum Sample Height",
          nickname: "MSH",
          description: "Minimum sample height of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Minimum Layer Thickness",
          nickname: "MLT",
          description: "Minimum thickness allowed for compound structure layers",
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
      manager.AddParameter(
          param: new Parameters.Param_Enum<Types.OpeningWrappingCondition>(),
          name: "Wrapping At Inserts",
          nickname: "IW",
          description: "Inserts wrapping condition on compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Param_Enum<Types.EndCapCondition>(),
          name: "Wrapping At Ends",
          nickname: "EW",
          description: "End cap condition of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Has Structural Deck",
          nickname: "HSD",
          description: "Whether compound structure has structural deck",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Is Vertically Compound",
          nickname: "IVC",
          description: "Whether compound structure is vertically compound",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Is Vertically Homogeneous",
          nickname: "IVH",
          description: "Whether compound structure is vertically homogeneous",
          access: GH_ParamAccess.item
          );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      Types.DataObject<DB.CompoundStructure> dataObj = default;
      if (!DA.GetData("Compound Structure", ref dataObj))
        return;

      var cstruct = dataObj.Value;

      // Deconstruct the data object into output params
      DA.SetData("Total Thickness", cstruct.GetWidth());
      DA.SetData("Sample Height", cstruct.SampleHeight);
      DA.SetData("Cutoff Height", cstruct.CutoffHeight);
      DA.SetData("Minimum Sample Height", cstruct.MinimumSampleHeight);
      DA.SetData("Minimum Layer Thickness", DB.CompoundStructure.GetMinimumLayerThickness());
      DA.SetDataList("Layers", cstruct.GetLayers().Select(x => new Types.DataObject<DB.CompoundStructureLayer>(apiObject: x, srcDocument: dataObj.Document)));
      DA.SetData("First Core Layer Index", cstruct.GetFirstCoreLayerIndex());
      DA.SetData("Last Core Layer Index", cstruct.GetLastCoreLayerIndex());
      DA.SetData("Structural Material Index", cstruct.StructuralMaterialIndex);
      DA.SetData("Variable Layer Index", cstruct.VariableLayerIndex);
      DA.SetData("Wrapping At Inserts", cstruct.OpeningWrapping);
      DA.SetData("Wrapping At Ends", cstruct.EndCap);
      DA.SetData("Has Structural Deck", cstruct.HasStructuralDeck);
      DA.SetData("Is Vertically Compound", cstruct.IsVerticallyCompound);
      DA.SetData("Is Vertically Homogeneous", cstruct.IsVerticallyHomogeneous());
    }
  }
}
