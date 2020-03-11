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
  public class Face : ElementIdGeometryParam<Types.Elements.Geometry.Face, DB.Face>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("759700ED-BC79-4986-A6AB-84921A7C9293");
    public Face() : base("Face", "Face", "Represents a Revit face.", "Params", "Revit") { }

    #region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Elements.Geometry.Face> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObjects(out var references, ObjectType.Face))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = references.Select((x) => new Types.Elements.Geometry.Face(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Elements.Geometry.Face value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Face))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new Types.Elements.Geometry.Face(uiDocument.Document, reference);
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
