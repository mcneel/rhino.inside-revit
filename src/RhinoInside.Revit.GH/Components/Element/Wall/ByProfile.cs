using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class WallByProfile : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("78b02ae8-2b78-45a7-962e-92e7d9097598");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public WallByProfile() : base
    (
      name: "Add Wall By Profile",
      nickname: "Wall",
      description: "Given a base curve and profile curves, it adds a Wall element to the active Revit document",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Wall(), "Wall", "W", "New Wall", GH_ParamAccess.item);
    }

    void ReconstructWallByProfile
    (
      DB.Document doc,
      ref DB.Wall element,

      IList<Rhino.Geometry.Curve> profileCurves,
      Optional<DB.WallType> type,
      Optional<DB.Level> level,
      [Optional, DefaultValue(false)] DB.Structure.StructuralWallUsage structuralUsage
    )
    {
      SolveOptionalType(ref type, doc, DB.ElementTypeGroup.WallType, nameof(type));
      SolveOptionalLevel(doc, 0.0, ref level);

      var newWall = DB.Wall.Create(
        doc,
        profileCurves.Select(x => x.ToCurve()).ToList(),
        type.Value.Id,
        level.Value.Id,
        structuralUsage != DB.Structure.StructuralWallUsage.NonBearing
      );

      ReplaceElement(ref element, newWall);
    }
  }
}
