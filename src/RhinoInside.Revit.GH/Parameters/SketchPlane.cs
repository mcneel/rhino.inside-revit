using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class SketchPlane : ElementIdWithoutPreviewParam<Types.SketchPlane, Autodesk.Revit.DB.SketchPlane>
  {
    public override Guid ComponentGuid => new Guid("93BF1F61-69AD-433F-A202-352C14E4CED8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public SketchPlane() : base("Sketch Plane", "Sketch Plane", "Represents a Revit document sketch plane.", "Params", "Revit Primitives") { }
  }
}
