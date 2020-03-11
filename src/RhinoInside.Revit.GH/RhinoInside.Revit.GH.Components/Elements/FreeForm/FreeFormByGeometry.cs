using System;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.FreeForm
{
  public class FreeFormByGeometry : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("D2FDF2A0-1E48-4075-814A-685D91A6CD94");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public FreeFormByGeometry() : base
    (
      "AddForm.ByGeometry", "ByGeometry",
      "Given its Geometry, it adds a Form element to the active Revit document",
      "Revit", "Mass"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GeometricElement(), "Form", "F", "New Form", GH_ParamAccess.item);
    }

    void ReconstructFormByGeometry
    (
      DB.Document doc,
      ref Autodesk.Revit.DB.Element element,

      Rhino.Geometry.Brep brep
    )
    {
      if (!doc.IsFamilyDocument)
        throw new InvalidOperationException("This component can only run in Family editor");

      var scaleFactor = 1.0 / Revit.ModelUnits;
      brep = brep.ChangeUnits(scaleFactor);
      brep.GetUserBoolean(DB.BuiltInParameter.ELEMENT_IS_CUTTING.ToString(), out var cutting);

      if (brep.Faces.Count == 1 && brep.Faces[0].Loops.Count == 1 && brep.Faces[0].TryGetPlane(out var capPlane))
      {
        using (var sketchPlane = DB.SketchPlane.Create(doc, capPlane.ToHost()))
        using (var referenceArray = new DB.ReferenceArray())
        {
          try
          {
            foreach (var curve in brep.Faces[0].OuterLoop.To3dCurve().ToHostMultiple())
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
        using (var sketchPlane = DB.SketchPlane.Create(doc, extrusion.GetProfilePlane(0.0).ToHost()))
        using (var referenceArray = new DB.ReferenceArray())
        {
          try
          {
            foreach (var curve in extrusion.Profile3d(new Rhino.Geometry.ComponentIndex(Rhino.Geometry.ComponentIndexType.ExtrusionBottomProfile, 0)).ToHostMultiple())
              referenceArray.Append(new DB.Reference(doc.FamilyCreate.NewModelCurve(curve, sketchPlane)));

            ReplaceElement
            (
              ref element,
              doc.FamilyCreate.NewExtrusionForm
              (
                !cutting,
                referenceArray, extrusion.PathLineCurve().Line.Direction.ToHost()
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

      {
        var solid = brep.ToHost();
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
      }
    }
  }
}
