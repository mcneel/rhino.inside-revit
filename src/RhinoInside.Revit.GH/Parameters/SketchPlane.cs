using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class SketchPlane : Element<Types.SketchPlane, Autodesk.Revit.DB.SketchPlane>
  {
    public override Guid ComponentGuid => new Guid("93BF1F61-69AD-433F-A202-352C14E4CED8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public SketchPlane() : base("Sketch Plane", "Sketch Plane", "Contains a collection of Revit sketch plane elements", "Params", "Revit Primitives") { }
  }
}
