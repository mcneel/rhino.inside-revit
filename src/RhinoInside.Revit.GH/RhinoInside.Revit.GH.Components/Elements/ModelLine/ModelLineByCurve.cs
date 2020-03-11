using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements.ModelLine
{
  public class ModelLineByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("240127B1-94EE-47C9-98F8-05DE32447B01");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public ModelLineByCurve() : base
    (
      "AddModelLine.ByCurve", "ByCurve",
      "Given a Curve, it adds a Curve element to the active Revit document",
      "Revit", "Model"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GeometricElement(), "CurveElement", "C", "New CurveElement", GH_ParamAccess.list);
    }

    void ReconstructModelLineByCurve
    (
      DB.Document doc,
      ref Autodesk.Revit.DB.Element element,

      Rhino.Geometry.Curve curve,
      Autodesk.Revit.DB.SketchPlane sketchPlane
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      var plane = sketchPlane.GetPlane().ToRhino().ChangeUnits(scaleFactor);
      if
      (
        ((curve = curve.ChangeUnits(scaleFactor)) is null) ||
        ((curve = Rhino.Geometry.Curve.ProjectToPlane(curve, plane)) == null)
      )
        ThrowArgumentException(nameof(curve), "Failed to project curve in the sketchPlane.");

      var centerLine = curve.ToHost();

      if (curve.IsClosed == centerLine.IsBound)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to keep curve closed.");

      if (element is DB.ModelCurve modelCurve && centerLine.IsSameKindAs(modelCurve.GeometryCurve))
        modelCurve.SetSketchPlaneAndCurve(sketchPlane, centerLine);
      else if (doc.IsFamilyDocument)
        ReplaceElement(ref element, doc.FamilyCreate.NewModelCurve(centerLine, sketchPlane));
      else
        ReplaceElement(ref element, doc.Create.NewModelCurve(centerLine, sketchPlane));
    }
  }
}

