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
  using ERDB = External.DB;

  public class AddForm : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("D2FDF2A0-1E48-4075-814A-685D91A6CD94");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public AddForm() : base
    (
      name: "Add Form",
      nickname: "Form",
      description: "Given its Geometry, it adds a Form element to the active Revit document",
      category: "Revit",
      subCategory: "Component"
    )
    { }

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.IS_VISIBLE_PARAM,
      ARDB.BuiltInParameter.GEOM_VISIBILITY_PARAM,
      ARDB.BuiltInParameter.MATERIAL_ID_PARAM,
      ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY,
      ARDB.BuiltInParameter.ELEMENT_IS_CUTTING,
      ARDB.BuiltInParameter.OFFSETFACES_SHOW_SHAPE_HANDLES,
    };

    void ReconstructAddForm
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

              ReplaceElement(ref form, document.FamilyCreate.NewFormByCap(!cutting, referenceArray), ExcludeUniqueProperties);

              form.get_Parameter(ARDB.BuiltInParameter.IS_VISIBLE_PARAM)?.Update(true);
              form.get_Parameter(ARDB.BuiltInParameter.GEOM_VISIBILITY_PARAM).Update(ERDB.FamilyElementVisibility.DefaultModel);
              form.get_Parameter(ARDB.BuiltInParameter.MATERIAL_ID_PARAM)?.Update(ElementIdExtension.Invalid);
              form.get_Parameter(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY)?.Update(ElementIdExtension.Invalid);
              form.get_Parameter(ARDB.BuiltInParameter.ELEMENT_IS_CUTTING)?.Update(0);
              form.get_Parameter(ARDB.BuiltInParameter.OFFSETFACES_SHOW_SHAPE_HANDLES)?.Update(true);
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

        if (brep.ToSolid() is ARDB.Solid solid)
        {
          if (form is ARDB.FreeFormElement freeFormElement)
            freeFormElement.UpdateSolidGeometry(solid);
          else
            ReplaceElement(ref form, ARDB.FreeFormElement.Create(document, solid), ExcludeUniqueProperties);

          form.get_Parameter(ARDB.BuiltInParameter.IS_VISIBLE_PARAM)?.Update(true);
          form.get_Parameter(ARDB.BuiltInParameter.GEOM_VISIBILITY_PARAM).Update(ERDB.FamilyElementVisibility.DefaultModel);
          form.get_Parameter(ARDB.BuiltInParameter.MATERIAL_ID_PARAM)?.Update(ElementIdExtension.Invalid);
          form.get_Parameter(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY)?.Update(ElementIdExtension.Invalid);
          form.get_Parameter(ARDB.BuiltInParameter.ELEMENT_IS_CUTTING)?.Update(0);
          form.get_Parameter(ARDB.BuiltInParameter.OFFSETFACES_SHOW_SHAPE_HANDLES)?.Update(true);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Brep to Form");
      }
    }
  }
}
