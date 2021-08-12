using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CurveElement : GraphicalElementT<Types.CurveElement, Autodesk.Revit.DB.CurveElement>
  {
    public override Guid ComponentGuid => new Guid("24892092-5A53-4A12-8A90-436C2559FF56");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;

    public CurveElement() : base("Curve Element", "Curve", "Contains a collection of Revit curve elements", "Params", "Revit Primitives") { }
  }
}
