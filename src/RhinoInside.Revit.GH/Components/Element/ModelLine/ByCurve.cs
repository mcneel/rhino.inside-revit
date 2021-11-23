using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Kernel.Attributes;

  public class ModelLineByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("240127B1-94EE-47C9-98F8-05DE32447B01");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public ModelLineByCurve() : base
    (
      name: "Add Model Line",
      nickname: "ModelLine",
      description: "Given a Curve, it adds a Curve element to the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    void ReconstructModelLineByCurve
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New CurveElement")]
      ref ARDB.ModelCurve curveElement,

      Rhino.Geometry.Curve curve,
      ARDB.SketchPlane sketchPlane
    )
    {
      var plane = sketchPlane.GetPlane().ToPlane();
      if ((curve = Rhino.Geometry.Curve.ProjectToPlane(curve, plane)) is null)
        ThrowArgumentException(nameof(curve), "Failed to project curve in the sketchPlane.");

      var centerLine = curve.ToCurve();

      if (curve.IsClosed == centerLine.IsBound)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to keep curve closed.");

      if (curveElement is ARDB.ModelCurve modelCurve && centerLine.IsSameKindAs(modelCurve.GeometryCurve))
      {
        if (modelCurve.SketchPlane.IsEquivalent(sketchPlane))
        {
          if (!centerLine.IsAlmostEqualTo(modelCurve.GeometryCurve))
            modelCurve.SetGeometryCurve(centerLine, true);
        }
        else modelCurve.SetSketchPlaneAndCurve(sketchPlane, centerLine);
      }
      else if (document.IsFamilyDocument)
        ReplaceElement(ref curveElement, document.FamilyCreate.NewModelCurve(centerLine, sketchPlane));
      else
        ReplaceElement(ref curveElement, document.Create.NewModelCurve(centerLine, sketchPlane));
    }
  }
}

