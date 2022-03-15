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
  public class AddVerticalOpening : AddOpening
  {
    public override Guid ComponentGuid => new Guid("C9C0F4D2-B75E-42C8-A98F-909DF4AB4A1A");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddVerticalOpening() : base
    (
      name: "Add Vertical Opening",
      nickname: "VerticalOpen",
      description: "Given its outline boundary and a host element, it adds a vertical opening to the active Revit document",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override bool IsPerpendicular => false;

  }
}

