using System;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Kernel.Attributes;

  public class FormByGeometry : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("D2FDF2A0-1E48-4075-814A-685D91A6CD94");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public FormByGeometry() : base
    (
      name: "Add Form",
      nickname: "Form",
      description: "Given its Geometry, it adds a Form element to the active Revit document",
      category: "Revit",
      subCategory: "Family"
    )
    { }

    void ReconstructFormByGeometry
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Description("New Form")]
      ref ARDB.GenericForm form,

      Brep brep
    )
    {
      if (!document.IsFamilyDocument)
        throw new Exceptions.RuntimeArgumentException("Document", "This component can only run on a Family document");

      brep.TryGetUserString(ARDB.BuiltInParameter.ELEMENT_IS_CUTTING.ToString(), out bool cutting, false);

      // If there are no inner-loops we try with a plain DB.Form
      if (brep.Faces.All(face => face.Loops.Count == 1))
      {
        if (brep.Faces.Count == 1 && brep.Faces[0].TryGetPlane(out var capPlane))
        {
          using (var sketchPlane = ARDB.SketchPlane.Create(document, capPlane.ToPlane()))
          using (var referenceArray = new ARDB.ReferenceArray())
          {
            try
            {
              foreach (var curve in brep.Faces[0].OuterLoop.To3dCurve().ToCurveMany())
                referenceArray.Append(new ARDB.Reference(document.FamilyCreate.NewModelCurve(curve, sketchPlane)));

              ReplaceElement
              (
                ref form,
                document.FamilyCreate.NewFormByCap
                (
                  !cutting,
                  referenceArray
                )
              );

              return;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
              document.Delete(referenceArray.OfType<ARDB.Reference>().Select(x => x.ElementId).ToArray());
            }
          }
        }
        else if (brep.TryGetExtrusion(out var extrusion) && (extrusion.CapCount == 2 || !extrusion.IsClosed(0)))
        {
          using (var sketchPlane = ARDB.SketchPlane.Create(document, extrusion.GetProfilePlane(0.0).ToPlane()))
          using (var referenceArray = new ARDB.ReferenceArray())
          {
            try
            {
              foreach (var curve in extrusion.Profile3d(new Rhino.Geometry.ComponentIndex(Rhino.Geometry.ComponentIndexType.ExtrusionBottomProfile, 0)).ToCurveMany())
                referenceArray.Append(new ARDB.Reference(document.FamilyCreate.NewModelCurve(curve, sketchPlane)));

              ReplaceElement
              (
                ref form,
                document.FamilyCreate.NewExtrusionForm
                (
                  !cutting,
                  referenceArray,
                  extrusion.PathLineCurve().Line.Direction.ToXYZ(GeometryEncoder.ModelScaleFactor)
                )
              );
              return;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
              document.Delete(referenceArray.OfType<ARDB.Reference>().Select(x => x.ElementId).ToArray());
            }
          }
        }
      }

      // Else we try with a DB.FreeFormElement
      using (var ctx = GeometryEncoder.Context.Push(document))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);

        var solid = brep.ToSolid();
        if (solid != null)
        {
          if (form is ARDB.FreeFormElement freeFormElement)
          {
            freeFormElement.UpdateSolidGeometry(solid);
          }
          else
          {
            ReplaceElement(ref form, ARDB.FreeFormElement.Create(document, solid));

            if (document.OwnerFamily.IsConceptualMassFamily)
              form.get_Parameter(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY).Update(new ARDB.ElementId(ARDB.BuiltInCategory.OST_MassForm));
          }

          form.get_Parameter(ARDB.BuiltInParameter.ELEMENT_IS_CUTTING)?.Update(cutting ? 1 : 0);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Brep to Form");
      }
    }
  }
}
