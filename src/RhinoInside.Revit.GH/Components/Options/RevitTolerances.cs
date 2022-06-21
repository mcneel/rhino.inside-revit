using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Options
{
  [ComponentVersion(introduced: "1.9")]
  public class RevitTolerances : Component
  {
    public override Guid ComponentGuid => new Guid("825D7AB3-0B45-43A7-9961-D0C6BB3C8E0A");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public RevitTolerances() : base
    (
      "Document Tolerances", "Tolerances",
      "Gets Revit tolereance values",
      "Revit", "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddNumberParameter("Angle", "A", "Angle tolerance (radians)\nTwo angle measurements closer than this value are considered identical.", GH_ParamAccess.item);
      manager.AddNumberParameter("Vertex", "V", "Vertex tolerance\nTwo points within this distance are considered coincident.", GH_ParamAccess.item);
      manager.AddNumberParameter("Curve", "C", "Short Curve tolerance\nA curve shorter than this distance is considered degenerated.", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DA.SetData("Angle", Revit.ActiveDBApplication.AngleTolerance);
      DA.SetData("Vertex", Revit.ActiveDBApplication.VertexTolerance / Revit.ModelUnits);
      DA.SetData("Curve", Revit.ActiveDBApplication.ShortCurveTolerance / Revit.ModelUnits);
    }
  }
}
