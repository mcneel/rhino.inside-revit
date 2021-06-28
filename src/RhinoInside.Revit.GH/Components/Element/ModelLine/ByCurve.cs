using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ModelLineByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("240127B1-94EE-47C9-98F8-05DE32447B01");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public ModelLineByCurve() : base
    (
      "Add ModelLine", "ModelLine",
      "Given a Curve, it adds a Curve element to the active Revit document",
      "Revit", "Model"
    )
    { }

    void ReconstructModelLineByCurve
    (
      DB.Document doc,

      [Description("New CurveElement"), ParamType(typeof(Parameters.GraphicalElement))]
      ref DB.ModelCurve curveElement,

      Rhino.Geometry.Curve curve,
      DB.SketchPlane sketchPlane
    )
    {
      var plane = sketchPlane.GetPlane().ToPlane();
      if ((curve = Rhino.Geometry.Curve.ProjectToPlane(curve, plane)) is null)
        ThrowArgumentException(nameof(curve), "Failed to project curve in the sketchPlane.");

      var centerLine = curve.ToCurve();

      if (curve.IsClosed == centerLine.IsBound)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to keep curve closed.");

      if (curveElement is DB.ModelCurve modelCurve && centerLine.IsSameKindAs(modelCurve.GeometryCurve))
        modelCurve.SetSketchPlaneAndCurve(sketchPlane, centerLine);
      else if (doc.IsFamilyDocument)
        ReplaceElement(ref curveElement, doc.FamilyCreate.NewModelCurve(centerLine, sketchPlane));
      else
        ReplaceElement(ref curveElement, doc.Create.NewModelCurve(centerLine, sketchPlane));
    }
  }
}

