using System;
using Grasshopper.Kernel;

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
        param: new Parameters.Element(),
        name: "Curtain Grid Panel",
        nickname: "CGP",
        description: "Curtain Grid Panel",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Host Panel",
        nickname: "HP",
        description: "",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Curtain Grid Panel Symbol",
        nickname: "PS",
        description: "",
        access: GH_ParamAccess.item
        );
      manager.AddPointParameter(
        name: "Curtain Grid Panel Base Point",
        nickname: "PBP",
        description: "",
        access: GH_ParamAccess.item
        );
      manager.AddVectorParameter(
        name: "Curtain Grid Panel Orientation Vector",
        nickname: "POV",
        description: "",
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
        name: "Is Lockable?",
        nickname: "IL?",
        description: "Whether curtain grid panel is lockable",
        access: GH_ParamAccess.item
        );

      // panel properties
      manager.AddNumberParameter(
        name: "Panel Width",
        nickname: "W",
        description: "Panel width",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Panel Height",
        nickname: "H",
        description: "Panel height",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Panel Depth",
        nickname: "D",
        description: "Panel depth",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      DB.FamilyInstance panelInstance = default;
      if (!DA.GetData("Curtain Grid Panel", ref panelInstance))
        return;

      switch (panelInstance)
      {
        case DB.Panel panel:
          DA.SetData("Host Panel", Types.Element.FromElement(panel.Document.GetElement(panel.FindHostPanel())));
          // sets either DB.PanelType or DB.FamilySymbol
          // Panels don't always have DB.PanelType assigned
          DA.SetData("Curtain Grid Panel Symbol", Types.ElementType.FromElement(panel.PanelType ?? panel.Symbol));
          DA.SetData("Curtain Grid Panel Base Point", panel.Transform.Origin.ToRhino());
          DA.SetData("Curtain Grid Panel Orientation Vector", new Rhino.Geometry.Vector3d(panel.FacingOrientation.ToRhino()));

          DA.SetData("Is Lockable?", panel.Lockable);
          DA.SetData("Panel Width", panel.LookupParameter("Width")?.AsDouble());
          DA.SetData("Panel Height", panel.LookupParameter("Height")?.AsDouble());
          DA.SetData("Panel Depth", panel.LookupParameter("Thickness")?.AsDouble());
          break;
        case DB.FamilyInstance famInst:
          DA.SetData("Curtain Grid Panel Symbol", Types.ElementType.FromElement(famInst.Symbol));
          break;
      }
    }
  }
}
