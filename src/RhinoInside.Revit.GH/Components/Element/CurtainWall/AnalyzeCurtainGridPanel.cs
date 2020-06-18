using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyzeCurtainGridPanel : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("08507225-C8DA-44A8-A282-C9B1AF1C61F4");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "ACGP";

    public AnalyzeCurtainGridPanel() : base(
      name: "Analyze Panel",
      nickname: "A-P",
      description: "Analyze given panel element",
      category: "Revit",
      subCategory: "Wall"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Panel(),
        name: "Panel",
        nickname: "P",
        description: "Panel element to analyze",
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
      //  name: "Locked",
      //  nickname: "L",
      //  description: "Whether curtain grid panel is locked",
      //  access: GH_ParamAccess.item
      //  );
      //manager.AddBooleanParameter(
      //  name: "Lockable",
      //  nickname: "L",
      //  description: "Whether curtain grid panel is lockable",
      //  access: GH_ParamAccess.item
      //  );

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
      var instance = default(DB.FamilyInstance);
      if (!DA.GetData("Panel", ref instance))
        return;

      DA.SetData("Type", instance.Symbol);
      DA.SetData("Base Point", instance.GetTransform().Origin.ToPoint3d());
      DA.SetData("Orientation", instance.FacingOrientation.ToVector3d());

      if (instance is DB.Panel panel)
      {
        //DA.SetData("Locked", panel.Locked);
        //DA.SetData("Lockable", panel.Lockable);
        DA.SetData("Host Panel", panel.Document.GetElement(panel.FindHostPanel()));
      }
      else
      {
        //DA.SetData("Locked", false);
        //DA.SetData("Lockable", false);
        DA.SetData("Host Panel", null);
      }

      PipeHostParameter(DA, instance, DB.BuiltInParameter.GENERIC_WIDTH, "Width");
      PipeHostParameter(DA, instance, DB.BuiltInParameter.GENERIC_HEIGHT, "Height");
    }
  }
}
