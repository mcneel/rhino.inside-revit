using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using Kernel.Attributes;

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

    static readonly ARDB.BuiltInParameter[] ParametersMask = new ARDB.BuiltInParameter[]
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.LEVEL_PARAM,
      ARDB.BuiltInParameter.BUILDINGPAD_HEIGHTABOVELEVEL_PARAM
    };

    void ReconstructBuildingPadByOutline
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New BuildingPad"), NickName("BP")]
      ref ARDB.Architecture.BuildingPad buildingPad,

      IList<Rhino.Geometry.Curve> boundary,
      Optional<ARDB.BuildingPadType> type,
      Optional<ARDB.Level> level
    )
    {
      SolveOptionalLevel(document, boundary, ref level, out var boundaryBBox);

      var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);

      if (buildingPad is object)
      {
        ChangeElementType(ref buildingPad, type);

        buildingPad.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(level.Value.Id);

        buildingPad.SetBoundary(curveLoops);
      }
      else
      {
        SolveOptionalType(document, ref type, ARDB.ElementTypeGroup.BuildingPadType, (doc, param) => ARDB.BuildingPadType.CreateDefault(doc), nameof(type));

        var newPad = ARDB.Architecture.BuildingPad.Create
        (
          document,
          type.Value.Id,
          level.Value.Id,
          curveLoops
        );

        ReplaceElement(ref buildingPad, newPad, ParametersMask);
      }

      buildingPad?.get_Parameter(ARDB.BuiltInParameter.BUILDINGPAD_HEIGHTABOVELEVEL_PARAM).Update(boundaryBBox.Min.Z / Revit.ModelUnits - level.Value.GetElevation());
    }
  }
}
