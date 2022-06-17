using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Kernel.Attributes;

  public class SketchPlaneByPlane : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("1FA679E4-1821-483A-99F8-DC166B0595F4");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public SketchPlaneByPlane() : base
    (
      name: "Add Sketch Plane",
      nickname: "SketchPlane",
      description: "Given a Plane, it adds a SketchPlane element to the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    void ReconstructSketchPlaneByPlane
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New SketchPlane")]
      ref ARDB.SketchPlane sketchPlane,

      Rhino.Geometry.Plane plane
    )
    {
      if (!plane.IsValid)
        ThrowArgumentException(nameof(plane), "Plane is not valid.");

      if (sketchPlane is object)
      {
        bool pinned = sketchPlane.Pinned;
        sketchPlane.Pinned = false;

        var plane0 = sketchPlane.GetPlane();
        using (var plane1 = plane.ToPlane())
        {
          if (!plane0.Normal.IsParallelTo(plane1.Normal))
          {
            var axisDirection = plane0.Normal.CrossProduct(plane1.Normal);
            double angle = plane0.Normal.AngleTo(plane1.Normal);

            using (var axis = ARDB.Line.CreateUnbound(plane0.Origin, axisDirection))
              ARDB.ElementTransformUtils.RotateElement(document, sketchPlane.Id, axis, angle);

            plane0 = sketchPlane.GetPlane();
          }

          {
            double angle = plane0.XVec.AngleOnPlaneTo(plane1.XVec, plane1.Normal);
            if (angle != 0.0)
            {
              using (var axis = ARDB.Line.CreateUnbound(plane0.Origin, plane1.Normal))
                ARDB.ElementTransformUtils.RotateElement(document, sketchPlane.Id, axis, angle);
            }
          }

          var trans = plane1.Origin - plane0.Origin;
          if (!trans.IsZeroLength())
            ARDB.ElementTransformUtils.MoveElement(document, sketchPlane.Id, trans);
        }

        sketchPlane.Pinned = pinned;
      }
      else
        ReplaceElement(ref sketchPlane, ARDB.SketchPlane.Create(document, plane.ToPlane()));
    }
  }
}
