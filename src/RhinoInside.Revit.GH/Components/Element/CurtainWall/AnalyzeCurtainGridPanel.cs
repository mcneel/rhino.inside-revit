using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyseCurtainGridPanel : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("08507225-C8DA-44A8-A282-C9B1AF1C61F4");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ACGP";

    public AnalyseCurtainGridPanel() : base(
      name: "Analyze Curtain Grid Panel",
      nickname: "A-CGP",
      description: "Analyze given curtain grid panel",
      category: "Revit",
      subCategory: "Analyze"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.FamilyInstance(),
        name: "Panel",
        nickname: "P",
        description: "Curtain Grid Panel",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Type",
        nickname: "T",
        description: "Panel Symbol. This can be a DB.PanelType of a DB.FamilySymbol depending on the type of panel hosted on the curtain wall.",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.HostObject(),
        name: "Host Panel",
        nickname: "HP",
        description: "Finds the host panel (i.e., wall) associated with this panel",
        access: GH_ParamAccess.item
        );
      manager.AddPointParameter(
        name: "Base Point",
        nickname: "BP",
        description: "Base point/anchor of the curtain panel",
        access: GH_ParamAccess.item
        );
      manager.AddVectorParameter(
        name: "Orientation",
        nickname: "O",
        description: "Orientation vector of the curtain panel",
        access: GH_ParamAccess.item
        );
      // DB.Panel is missing a .Locked property ?!
      //manager.AddBooleanParameter(
      //  name: "Locked?",
      //  nickname: "L?",
      //  description: "Whether curtain grid panel is locked",
      //  access: GH_ParamAccess.item
      //  );
      manager.AddBooleanParameter(
        name: "Lockable",
        nickname: "L",
        description: "Whether curtain grid panel is lockable",
        access: GH_ParamAccess.item
        );

      // panel properties
      manager.AddNumberParameter(
        name: "Width",
        nickname: "W",
        description: "Panel width",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Height",
        nickname: "H",
        description: "Panel height",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      DB.FamilyInstance panelInstance = default;
      if (!DA.GetData("Panel", ref panelInstance))
        return;

      switch (panelInstance)
      {
        case DB.Panel panel:
          // sets either DB.PanelType or DB.FamilySymbol
          // Panels don't always have DB.PanelType assigned
          DA.SetData("Type", Types.ElementType.FromElement(panel.PanelType ?? panel.Symbol));
          DA.SetData("Host Panel", Types.Element.FromElement(panel.Document.GetElement(panel.FindHostPanel())));
          DA.SetData("Base Point", panel.Transform.Origin.ToPoint3d());
          DA.SetData("Orientation", panel.FacingOrientation.ToVector3d());

          DA.SetData("Lockable", panel.Lockable);
          PipeHostParameter(DA, panel, DB.BuiltInParameter.GENERIC_WIDTH, "Width");
          PipeHostParameter(DA, panel, DB.BuiltInParameter.GENERIC_HEIGHT, "Height");
          break;
        case DB.FamilyInstance famInst:
          DA.SetData("Type", Types.ElementType.FromElement(famInst.Symbol));
          DA.SetData("Base Point", famInst.GetTransform().Origin.ToPoint3d());
          DA.SetData("Orientation", famInst.FacingOrientation.ToVector3d());

          DA.SetData("Lockable", false);
          PipeHostParameter(DA, famInst, DB.BuiltInParameter.GENERIC_WIDTH, "Width");
          PipeHostParameter(DA, famInst, DB.BuiltInParameter.GENERIC_HEIGHT, "Height");
          break;
      }
    }
  }
}
