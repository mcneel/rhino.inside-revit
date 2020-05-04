using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.Convert.System.Collections.Generic;

namespace RhinoInside.Revit.GH.Components
{
  public class BuildingPadByOutline : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("ADE71474-5F00-4BD5-9D1E-D518B42137F2");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public BuildingPadByOutline() : base
    (
      "Add BuildingPad", "BuildingPad",
      "Given a set of contour Curves, it adds a BuildingPad element to the active Revit document",
      "Revit", "Site"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.HostObject(), "BuildingPad", "BP", "New BuildingPad", GH_ParamAccess.item);
    }

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
      ref DB.Architecture.BuildingPad element,

      IList<Rhino.Geometry.Curve> boundaries,
      Optional<DB.BuildingPadType> type,
      Optional<DB.Level> level
    )
    {
      ChangeElementType(ref element, type);

      SolveOptionalLevel(doc, boundaries, ref level, out var boundaryBBox);

      var curveLoops = boundaries.ConvertAll(GeometryEncoder.ToCurveLoop);

      if (element is DB.Architecture.BuildingPad buildingPad)
      {
        element.get_Parameter(DB.BuiltInParameter.LEVEL_PARAM).Set(level.Value.Id);

        buildingPad.SetBoundary(curveLoops);
      }
      else
      {
        SolveOptionalType(ref type, doc, DB.ElementTypeGroup.BuildingPadType, (document, param) => DB.BuildingPadType.CreateDefault(document), nameof(type));

        var newPad = DB.Architecture.BuildingPad.Create
        (
          doc,
          type.Value.Id,
          level.Value.Id,
          curveLoops
        );

        ReplaceElement(ref element, newPad, ParametersMask);
      }

      element?.get_Parameter(DB.BuiltInParameter.BUILDINGPAD_HEIGHTABOVELEVEL_PARAM).Set(boundaryBBox.Min.Z / Revit.ModelUnits - level.Value.Elevation);
    }
  }
}
