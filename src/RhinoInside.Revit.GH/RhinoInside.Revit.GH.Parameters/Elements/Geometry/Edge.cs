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
  public class Edge : ElementIdGeometryParam<Types.Elements.Geometry.Edge, DB.Edge>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("B79FD0FD-63AE-4776-A0A7-6392A3A58B0D");
    public Edge() : base("Edge", "Edge", "Represents a Revit edge.", "Params", "Revit") { }

    #region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Elements.Geometry.Edge> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObjects(out var references, ObjectType.Edge))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = references.Select((x) => new Types.Elements.Geometry.Edge(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Elements.Geometry.Edge value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Edge))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new Types.Elements.Geometry.Edge(uiDocument.Document, reference);
          return GH_GetterResult.success;
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
