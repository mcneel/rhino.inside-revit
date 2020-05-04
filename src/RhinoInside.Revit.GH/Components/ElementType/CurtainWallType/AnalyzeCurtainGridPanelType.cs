using System;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyseCurtainGridPanelType : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("6F11977F-7CF3-41F1-8A69-2F4CD7287DEF");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ACGPT";

    public AnalyseCurtainGridPanelType() : base(
      name: "Analyze Curtain Grid Panel Type",
      nickname: "A-CGPT",
      description: "Analyze given curtain grid panel type",
      category: "Revit",
      subCategory: "Analyze"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Curtain Grid Panel Type",
        nickname: "CGPT",
        description: "Curtain Grid Panel Type",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.CurtainPanelSystemFamily>(),
        name: "Panel System Family",
        nickname: "PSF",
        description: "Panel system family",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Offset",
        nickname: "O",
        description: "Panel type offset",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Thickness",
        nickname: "T",
        description: "Panel type thickness",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      // panel types can be DB.FamilySymbol or DB.PanelType
      DB.FamilySymbol famInst = default;
      if (!DA.GetData("Curtain Grid Panel Type", ref famInst))
        return;

      var inputType = famInst.GetType();
      // make sure other derivatives of DB.FamilySymbol do not pass this filter
      // we are only interested in panel types
      if (inputType == typeof(DB.FamilySymbol) || inputType == typeof(DB.PanelType))
      {
        // TODO: find a way to determine whether panel type is an Empty type or not
        // maybe the Id/Unique is fixed? Compare across multiple example models of various languages
        DA.SetData("Panel System Family", new Types.CurtainPanelSystemFamily(External.DB.CurtainPanelSystemFamily.Unknown));

        switch (famInst)
        {
          case DB.PanelType panelType:
            PipeHostParameter(DA, panelType, DB.BuiltInParameter.CURTAIN_WALL_SYSPANEL_OFFSET, "Offset");
            PipeHostParameter(DA, panelType, DB.BuiltInParameter.CURTAIN_WALL_SYSPANEL_THICKNESS, "Thickness");
            break;

          case DB.FamilySymbol finst:
            // make sure family symbol belongs to a Panel Family
            // finst.Family.IsCurtainPanelFamily returns FALSE !!!
            var isCurtainPanelFamily = finst.Family.FamilyCategory.Id.IntegerValue == (int) DB.BuiltInCategory.OST_CurtainWallPanels;
            // can not extract Offset and Thickness since they are not builtin for curtain panel custom families
            // TODO: maybe extract other info for Panel Families?!
            break;
        }
      }
    }
  }
}
