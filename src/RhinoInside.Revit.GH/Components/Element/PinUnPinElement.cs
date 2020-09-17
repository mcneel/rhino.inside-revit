using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.System.Drawing;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element
{
  public class PinUnPinElement: TransactionComponent
  {
    public override Guid ComponentGuid => new Guid("cc205221-1583-47d1-a715-226c39c3fb34");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "PU";

    public PinUnPinElement()
    : base(
      name: "Pin/Unpin Element",
      nickname: "P/UP",
      description: "Pins or Unpins elements from Revit document",
      category: "Revit",
      subCategory: "Element"
      )
    {

    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.GraphicalElement(),
        name: "Element",
        nickname: "E",
        description: "Element to query for its identity",
        access: GH_ParamAccess.item
      );

      manager[manager.AddBooleanParameter(
        name: "Pin",
        nickname: "P",
        description: "State of pin (defaults to false)",
        access: GH_ParamAccess.item
      )].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Element",
        nickname: "E",
        description: "Element to query for its identity",
        access: GH_ParamAccess.item
      );

      manager.AddBooleanParameter(
        name: "Pin",
        nickname: "P",
        description: "State of pin (defaults to false)",
        access: GH_ParamAccess.item
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Element element = default;
      if (!DA.GetData("Element", ref element))
        return;

      bool state = false;
      if(DA.GetData("Pin", ref state))
        element.Pinned = state;

      DA.SetData("Element", element);
      DA.SetData("Pin", element.Pinned);
    }
  }
}
