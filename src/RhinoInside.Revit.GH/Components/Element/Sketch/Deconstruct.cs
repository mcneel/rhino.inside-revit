using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;

  public class SketchDeconstruct : Component
  {
    public override Guid ComponentGuid => new Guid("F9BC3F5E-7415-485E-B74C-5CB855B818B8");
    protected override string IconTag => "S";
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;

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
      manager.AddParameter(new Parameters.SketchPlane(), "Sketch Plane", "SP", "Sketch plane", GH_ParamAccess.item);
      manager.AddCurveParameter("Profile", "PC", "Sketch profile curves", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      ARDB.Sketch sketch = null;
      if (!DA.GetData("Sketch", ref sketch))
        return;

      DA.SetData("Sketch Plane", sketch?.SketchPlane);
      DA.SetDataList("Profile", sketch?.Profile.ToPolyCurves());
    }
  }
}
