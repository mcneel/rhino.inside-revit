using System;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
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

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Form", "F", "New Form", GH_ParamAccess.item);
    }

    void ReconstructFormByGeometry
    (
      DB.Document doc,
      ref DB.Element element,

      Brep brep
    )
    {
      if (!doc.IsFamilyDocument)
        throw new InvalidOperationException("This component can only run on a Family document");

      brep.TryGetUserString(DB.BuiltInParameter.ELEMENT_IS_CUTTING.ToString(), out bool cutting, false);

      if (brep.Faces.Count == 1 && brep.Faces[0].Loops.Count == 1 && brep.Faces[0].TryGetPlane(out var capPlane))
      {
        using (var sketchPlane = DB.SketchPlane.Create(doc, capPlane.ToPlane()))
        using (var referenceArray = new DB.ReferenceArray())
        {
          try
          {
            foreach (var curve in brep.Faces[0].OuterLoop.To3dCurve().ToCurveMany())
              referenceArray.Append(new DB.Reference(doc.FamilyCreate.NewModelCurve(curve, sketchPlane)));

            ReplaceElement
            (
              ref element,
              doc.FamilyCreate.NewFormByCap
              (
                !cutting,
                referenceArray
              )
            );

            return;
          }
          catch (Autodesk.Revit.Exceptions.InvalidOperationException)
          {
            doc.Delete(referenceArray.OfType<DB.Reference>().Select(x => x.ElementId).ToArray());
          }
        }
      }
      else if ( brep.TryGetExtrusion(out var extrusion) && (extrusion.CapCount == 2 || !extrusion.IsClosed(0)))
      {
        using (var sketchPlane = DB.SketchPlane.Create(doc, extrusion.GetProfilePlane(0.0).ToPlane()))
        using (var referenceArray = new DB.ReferenceArray())
        {
          try
          {
            foreach (var curve in extrusion.Profile3d(new Rhino.Geometry.ComponentIndex(Rhino.Geometry.ComponentIndexType.ExtrusionBottomProfile, 0)).ToCurveMany())
              referenceArray.Append(new DB.Reference(doc.FamilyCreate.NewModelCurve(curve, sketchPlane)));

            ReplaceElement
            (
              ref element,
              doc.FamilyCreate.NewExtrusionForm
              (
                !cutting,
                referenceArray,
                extrusion.PathLineCurve().Line.Direction.ToXYZ(UnitConverter.ToHostUnits)
              )
            );
            return;
          }
          catch(Autodesk.Revit.Exceptions.InvalidOperationException)
          {
             doc.Delete(referenceArray.OfType<DB.Reference>().Select(x => x.ElementId).ToArray());
          }
        }
      }

      using (GeometryEncoder.Context.Push(doc))
      {
        var solid = brep.ToSolid();
        if (solid != null)
        {
          if (element is DB.FreeFormElement freeFormElement)
          {
            freeFormElement.UpdateSolidGeometry(solid);
          }
          else
          {
            ReplaceElement(ref element, DB.FreeFormElement.Create(doc, solid));

            if (doc.OwnerFamily.IsConceptualMassFamily)
              element.get_Parameter(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY).Set(new DB.ElementId(DB.BuiltInCategory.OST_MassForm));
          }

          element.get_Parameter(DB.BuiltInParameter.ELEMENT_IS_CUTTING)?.Set(cutting ? 1 : 0);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Brep to Form");
      }
    }
  }
}
