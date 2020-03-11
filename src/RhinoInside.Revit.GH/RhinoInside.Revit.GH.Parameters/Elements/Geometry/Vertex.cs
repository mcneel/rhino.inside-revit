using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI.Selection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using RhinoInside.Revit.UI.Selection;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Elements.Geometry
{
  public class Vertex : ElementIdGeometryParam<Types.Elements.Geometry.Vertex, DB.Point>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("BC1B160A-DC04-4139-AB7D-1AECBDE7FF88");
    public Vertex() : base("Vertex", "Vertex", "Represents a Revit vertex.", "Params", "Revit") { }

    #region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Elements.Geometry.Vertex> value)
    {
      var values = new List<Types.Elements.Geometry.Vertex>();
      Types.Elements.Geometry.Vertex vertex = null;
      while (Prompt_Singular(ref vertex) == GH_GetterResult.success)
        values.Add(vertex);

      if (values.Count > 0)
      {
        value = values;
        return GH_GetterResult.success;
      }

      return GH_GetterResult.cancel;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Elements.Geometry.Vertex value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Edge, "Click on an edge near an end to select a vertex, TAB for alternates, ESC quit."))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          var element = uiDocument.Document.GetElement(reference);
          if (element?.GetGeometryObjectFromReference(reference) is DB.Edge edge)
          {
            var curve = edge.AsCurve();
            var result = curve.Project(reference.GlobalPoint);
            var points = new DB.XYZ[] { curve.GetEndPoint(0), curve.GetEndPoint(1) };
            int index = result.XYZPoint.DistanceTo(points[0]) < result.XYZPoint.DistanceTo(points[1]) ? 0 : 1;

            value = new Types.Elements.Geometry.Vertex(uiDocument.Document, reference, index);
            return GH_GetterResult.success;
          }
          break;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    #endregion
  }
}
