using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DesignOption
{
  public class DesignOptionActive : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("B6349DDA-4486-44EB-9AF7-3D13404A3F3E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "A";

    public DesignOptionActive() : base
    (
      name: "Active Design Option",
      nickname: "ADsgnOpt",
      description: "Gets the active Design Option",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Active Design Option", "O", "Active design option", GH_ParamAccess.item)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      var option = new Types.DesignOption(doc, DB.DesignOption.GetActiveDesignOptionId(doc));
      DA.SetData("Active Design Option", option);
    }
  }
}
