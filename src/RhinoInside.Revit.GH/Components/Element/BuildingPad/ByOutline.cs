using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;

namespace RhinoInside.Revit.GH.Components.Site
{
  public class BuildingPadByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("ADE71474-5F00-4BD5-9D1E-D518B42137F2");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public BuildingPadByOutline() : base
    (
      name: "Add BuildingPad",
      nickname: "BuildingPad",
      description: "Given a set of contour Curves, it adds a BuildingPad element to the active Revit document",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    static readonly DB.BuiltInParameter[] ParametersMask = new DB.BuiltInParameter[]
    {
      DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      DB.BuiltInParameter.ELEM_FAMILY_PARAM,
      DB.BuiltInParameter.ELEM_TYPE_PARAM,
      DB.BuiltInParameter.LEVEL_PARAM,
      DB.BuiltInParameter.BUILDINGPAD_HEIGHTABOVELEVEL_PARAM
    };

    void ReconstructBuildingPadByOutline
    (
      DB.Document doc,

      [Description("New BuildingPad"), NickName("BP")]
      ref DB.Architecture.BuildingPad buildingPad,

      IList<Rhino.Geometry.Curve> boundaries,
      Optional<DB.BuildingPadType> type,
      Optional<DB.Level> level
    )
    {
      ChangeElementType(ref buildingPad, type);

      SolveOptionalLevel(doc, boundaries, ref level, out var boundaryBBox);

      var curveLoops = boundaries.ConvertAll(GeometryEncoder.ToCurveLoop);

      if (buildingPad is object)
      {
        buildingPad.get_Parameter(DB.BuiltInParameter.LEVEL_PARAM).Set(level.Value.Id);

        buildingPad.SetBoundary(curveLoops);
      }
      else
      {
        SolveOptionalType(doc, ref type, DB.ElementTypeGroup.BuildingPadType, (document, param) => DB.BuildingPadType.CreateDefault(document), nameof(type));

        var newPad = DB.Architecture.BuildingPad.Create
        (
          doc,
          type.Value.Id,
          level.Value.Id,
          curveLoops
        );

        ReplaceElement(ref buildingPad, newPad, ParametersMask);
      }

      buildingPad?.get_Parameter(DB.BuiltInParameter.BUILDINGPAD_HEIGHTABOVELEVEL_PARAM).Set(boundaryBBox.Min.Z / Revit.ModelUnits - level.Value.GetHeight());
    }
  }
}
