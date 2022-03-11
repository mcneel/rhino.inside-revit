using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI.Selection;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.UI.Selection;

  public abstract class GeometryObject<X, R> : ElementIdParam<X, R>, IGH_PreviewObject
  where X : class, Types.IGH_ElementId
  {
    protected GeometryObject(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    { }

    #region IGH_PreviewObject
    bool IGH_PreviewObject.Hidden { get; set; }
    bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;
    BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion
  }

  public class Vertex : GeometryObject<Types.Vertex, ARDB.Point>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override Guid ComponentGuid => new Guid("BC1B160A-DC04-4139-AB7D-1AECBDE7FF88");
    public Vertex() : base("Vertex", "Vertex", "Contains a collection of Revit vertices", "Params", "Revit Primitives") { }

    #region UI methods
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Box", "Point" }
    );

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
      if (uiDocument is null) return GH_GetterResult.cancel;

      switch (uiDocument.PickObject(out var reference, ObjectType.Edge, "Click on an edge near an end to select a vertex, TAB for alternates, ESC quit."))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          var element = uiDocument.Document.GetElement(reference);
          if (element?.GetGeometryObjectFromReference(reference) is ARDB.Edge edge)
          {
            var curve = edge.AsCurve();
            var result = curve.Project(reference.GlobalPoint);
            var points = new ARDB.XYZ[] { curve.GetEndPoint(0), curve.GetEndPoint(1) };
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

  public class Edge : GeometryObject<Types.Edge, ARDB.Edge>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override Guid ComponentGuid => new Guid("B79FD0FD-63AE-4776-A0A7-6392A3A58B0D");
    public Edge() : base("Edge", "Edge", "Contains a collection of Revit edges", "Params", "Revit Primitives") { }

    #region UI methods
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Box", "Curve" }
    );

    protected override GH_GetterResult Prompt_Plural(ref List<Types.Edge> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      if (uiDocument is null) return GH_GetterResult.cancel;

      switch (uiDocument.PickObjects(out var references, ObjectType.Edge))
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
      if (uiDocument is null) return GH_GetterResult.cancel;

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

  public class Face : GeometryObject<Types.Face, ARDB.Face>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override Guid ComponentGuid => new Guid("759700ED-BC79-4986-A6AB-84921A7C9293");
    public Face() : base("Face", "Face", "Contains a collection of Revit faces", "Params", "Revit Primitives") { }

    #region UI methods
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Box", "Surface", "Mesh" }
    );

    protected override GH_GetterResult Prompt_Plural(ref List<Types.Face> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      if (uiDocument is null) return GH_GetterResult.cancel;

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
      if (uiDocument is null) return GH_GetterResult.cancel;

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
