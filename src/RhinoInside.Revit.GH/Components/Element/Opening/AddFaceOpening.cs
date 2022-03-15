using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Components.Element.Opening;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Openings
{
  public class AddFaceOpening : AddOpening
  {
    public override Guid ComponentGuid => new Guid("69A10E5D-5DF0-4227-95D3-2629529C1DEF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddFaceOpening() : base
    (
      name: "Add Face Opening",
      nickname: "FaceOpen",
      description: "Given its outline boundary and a host element, it adds an opening to the active Revit document",
      category: "Revit",
      subCategory: "Host",
      isPerpendicular: true
    )
    { }
  }
}

