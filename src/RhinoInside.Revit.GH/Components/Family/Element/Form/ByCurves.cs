using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  using Convert.Geometry;
  using Kernel.Attributes;

  public class FormByCurves : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("42631B6E-505E-4091-981A-E7605AE5A1FF");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public FormByCurves() : base
    (
      name: "Add LoftForm",
      nickname: "LoftForm",
      description: "Given a list of curves, it adds a Form element to the active Revit document",
      category: "Revit",
      subCategory: "Family"
    )
    { }

    void ReconstructFormByCurves
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Description("New Form")]
      ref ARDB.GenericForm form,

      IList<Rhino.Geometry.Curve> profiles
    )
    {
      if (!document.IsFamilyDocument)
        throw new Exceptions.RuntimeArgumentException("Document", "This component can only run on a Family document");

      var planes = new List<Rhino.Geometry.Plane>();
      foreach (var profile in profiles)
      {
        if (!profile.TryGetPlane(out var plane))
          ThrowArgumentException(nameof(profiles), "All profiles must be planar");

        plane.Origin = profile.IsClosed ? Rhino.Geometry.AreaMassProperties.Compute(profile).Centroid : profile.PointAtNormalizedLength(0.5);
        planes.Add(plane);
      }

      if (profiles.Count == 1)
      {
        var profile = profiles[0];
        var plane = planes[0];

        using (var sketchPlane = ARDB.SketchPlane.Create(document, plane.ToPlane()))
        using (var referenceArray = new ARDB.ReferenceArray())
        {
          foreach (var curve in profile.ToCurveMany())
            referenceArray.Append(new ARDB.Reference(document.FamilyCreate.NewModelCurve(curve, sketchPlane)));

          ReplaceElement(ref form, document.FamilyCreate.NewFormByCap(true, referenceArray));
        }
      }
      else
      {
        using (var referenceArrayArray = new ARDB.ReferenceArrayArray())
        {
          int index = 0;
          foreach (var profile in profiles)
          {
            using (var sketchPlane = ARDB.SketchPlane.Create(document, planes[index++].ToPlane()))
            {
              var referenceArray = new ARDB.ReferenceArray();

              foreach (var curve in profile.ToCurveMany())
                referenceArray.Append(new ARDB.Reference(document.FamilyCreate.NewModelCurve(curve, sketchPlane)));

              referenceArrayArray.Append(referenceArray);
            }
          }

          ReplaceElement(ref form, document.FamilyCreate.NewLoftForm(true, referenceArrayArray));
        }
      }
    }
  }
}
