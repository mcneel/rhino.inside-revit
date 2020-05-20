using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components
{
  public class SketchDeconstruct : Component
  {
    public override Guid ComponentGuid => new Guid("F9BC3F5E-7415-485E-B74C-5CB855B818B8");
    protected override string IconTag => "S";
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public SketchDeconstruct() : base
    (
      name: "Deconstruct Sketch",
      nickname: "SketchD",
      description: string.Empty,
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Sketch(), "Sketch", "S", "Sketch to deconstruct", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddPlaneParameter("Plane", "P", "Sketch plane", GH_ParamAccess.item);
      manager.AddCurveParameter("Profile", "PC", "Sketch profile curves", GH_ParamAccess.list);
      //manager.AddBrepParameter("Region", "R", "Sketch regions of closed profiles", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.Sketch sketch = null;
      if (!DA.GetData("Sketch", ref sketch) || sketch is null)
        return;

      DA.SetData("Plane", sketch.Plane);
      DA.SetDataList("Profile", sketch.Profile);
      //DA.SetData("Region", sketch.Region);
    }
  }
}
