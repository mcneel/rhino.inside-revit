using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters.Elements.HostObject
{
  public class HostObject : GeometricElementT<Types.Elements.HostObject.HostObject, Autodesk.Revit.DB.HostObject>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("E3462915-3C4D-4864-9DD4-5A73F91C6543");

    public HostObject() : base("Host", "Host", "Represents a Revit document host element.", "Params", "Revit") { }
  }
}
