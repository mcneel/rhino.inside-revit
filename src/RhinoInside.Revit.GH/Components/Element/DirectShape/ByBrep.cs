using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  public class DirectShapeByBrep : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5ADE9AE3-588C-4285-ABC5-09DEB92C6660");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByBrep() : base
    (
      "Add Brep DirectShape", "BrpDShape",
      "Given a Brep, it adds a Brep shape to the active Revit document",
      "Revit", "DirectShape"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Brep", "B", "New BrepShape", GH_ParamAccess.item);
    }

    readonly List<Rhino.Geometry.GeometryBase> GeometryConversionErrors = new List<Rhino.Geometry.GeometryBase>();
    public override void ClearData()
    {
      base.ClearData();
      GeometryConversionErrors.Clear();
    }

    void AddGeometryConversionError(GH_RuntimeMessageLevel level, string text, Rhino.Geometry.GeometryBase geometry)
    {
      AddRuntimeMessage(level, text);
      if(geometry is object) GeometryConversionErrors.Add(geometry.InRhinoUnits());
    }

    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
      base.DrawViewportWires(args);

      foreach (var geometry in GeometryConversionErrors)
      {
        switch (geometry)
        {
          case Rhino.Geometry.Point point:
            args.Display.DrawPoint(point.Location, System.Drawing.Color.Orange);
            break;
          case Rhino.Geometry.Curve curve:
            args.Display.DrawCurve(curve, System.Drawing.Color.Orange, args.DefaultCurveThickness * 8);
            args.Display.DrawPoint(curve.PointAtStart, System.Drawing.Color.Orange);
            args.Display.DrawPoint(curve.PointAtEnd, System.Drawing.Color.Orange);
            break;
        }
      }
    }

    void ReconstructDirectShapeByBrep
    (
      DB.Document doc,
      ref DB.DirectShape element,

      Rhino.Geometry.Brep brep
    )
    {
      ThrowIfNotValid(nameof(brep), brep);

      if (element is DB.DirectShape ds) { }
      else ds = DB.DirectShape.CreateElement(doc, new DB.ElementId(DB.BuiltInCategory.OST_GenericModel));

      using (var context = GeometryEncoder.Context.Push(ds))
      {
        context.RuntimeMessage = (severity, message, geometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, geometry); 

        ds.SetShape(brep.ToShape());
      }

      ReplaceElement(ref element, ds);
    }
  }
}
