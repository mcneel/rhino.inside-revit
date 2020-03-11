using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI.Selection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using RhinoInside.Revit.UI.Selection;
using RhinoInside.Revit.GH.Types.Elements.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class GeometricElement : GeometricElementT<Types.GeometricElement, DB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("EF607C2A-2F44-43F4-9C39-369CE114B51F");

    public GeometricElement() : base("Geometric Element", "Geometric Element", "Represents a Revit document geometric element.", "Params", "Revit") { }

    protected override Types.GeometricElement PreferredCast(object data)
    {
      return data is DB.Element element && AllowElement(element) ?
             new Types.GeometricElement(element) :
             null;
    }

    public override bool AllowElement(DB.Element elem) => Types.GeometricElement.IsValidElement(elem);
  }
}
