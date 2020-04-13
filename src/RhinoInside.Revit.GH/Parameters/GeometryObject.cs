using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI.Selection;
using RhinoInside.Revit.External.UI.Selection;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Vertex : ElementIdWithPreviewParam<Types.Vertex, DB.Point>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("BC1B160A-DC04-4139-AB7D-1AECBDE7FF88");
    public Vertex() : base("Vertex", "Vertex", "Represents a Revit vertex.", "Params", "Revit") { }

#region UI methods
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Vertex> value)
    {
      var values = new List<Types.Vertex>();
      Types.Vertex vertex = null;
      while(Prompt_Singular(ref vertex) == GH_GetterResult.success)
        values.Add(vertex);

      if (values.Count > 0)
      {
        value = values;
        return GH_GetterResult.success;
      }

      return GH_GetterResult.cancel;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Vertex value)
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

            value = new Types.Vertex(uiDocument.Document, reference, index);
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

  public class Edge : ElementIdWithPreviewParam<Types.Edge, DB.Edge>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("B79FD0FD-63AE-4776-A0A7-6392A3A58B0D");
    public Edge() : base("Edge", "Edge", "Represents a Revit edge.", "Params", "Revit") { }

#region UI methods
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Edge> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch(uiDocument.PickObjects(out var references, ObjectType.Edge))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = references.Select((x) => new Types.Edge(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Edge value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Edge))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new Types.Edge(uiDocument.Document, reference);
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

  public class Face : ElementIdWithPreviewParam<Types.Face, DB.Face>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("759700ED-BC79-4986-A6AB-84921A7C9293");
    public Face() : base("Face", "Face", "Represents a Revit face.", "Params", "Revit") { }

#region UI methods
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Face> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObjects(out var references, ObjectType.Face))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = references.Select((x) => new Types.Face(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Face value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Face))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new Types.Face(uiDocument.Document, reference);
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
