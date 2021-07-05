using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
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
      DB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Description("New Form")]
      ref DB.GenericForm form,

      IList<Rhino.Geometry.Curve> profiles
    )
    {
      if (!document.IsFamilyDocument)
        throw new InvalidOperationException("This component can only run in Family editor");

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

        using (var sketchPlane = DB.SketchPlane.Create(document, plane.ToPlane()))
        using (var referenceArray = new DB.ReferenceArray())
        {
          foreach (var curve in profile.ToCurveMany())
            referenceArray.Append(new DB.Reference(document.FamilyCreate.NewModelCurve(curve, sketchPlane)));

          ReplaceElement(ref form, document.FamilyCreate.NewFormByCap(true, referenceArray));
        }
      }
      else
      {
        using (var referenceArrayArray = new DB.ReferenceArrayArray())
        {
          int index = 0;
          foreach (var profile in profiles)
          {
            using (var sketchPlane = DB.SketchPlane.Create(document, planes[index++].ToPlane()))
            {
              var referenceArray = new DB.ReferenceArray();

              foreach (var curve in profile.ToCurveMany())
                referenceArray.Append(new DB.Reference(document.FamilyCreate.NewModelCurve(curve, sketchPlane)));

              referenceArrayArray.Append(referenceArray);
            }
          }

          ReplaceElement(ref form, document.FamilyCreate.NewLoftForm(true, referenceArrayArray));
        }
      }
    }
  }
}
