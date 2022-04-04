using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.SpatialElements
{
  [ComponentVersion(introduced: "1.7")]
  public class SpatialElementGeometry : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("419062DF-CD1C-4AEB-B4CA-E19402FE3317");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public SpatialElementGeometry() : base
    (
      name: "Spatial Element Geometry",
      nickname: "SE-Geometry",
      description: "Get the geometry of the specified spatial element",
      category: "Revit",
      subCategory: "Spatial"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.SpatialElement()
        {
          Name = "Spatial Element",
          NickName = "SE",
          Description = "Spatial element to extract geometry",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.SpatialElementBoundaryLocation>()
        {
          Name = "Boundary Location",
          NickName = "BL",
          Description = "Location line setting of the spatial element",
          Access = GH_ParamAccess.item,
          Optional = true
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_Brep()
        {
          Name = "Geometry",
          NickName = "G",
          Description = "Geometry of the given spatial element",
          Access = GH_ParamAccess.item,
          DataMapping = GH_DataMapping.Graft
        }
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Boundary of the given spatial element",
          Access = GH_ParamAccess.tree
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Boundary elements of the given spatial element",
          Access = GH_ParamAccess.tree
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Spatial Element", out ARDB.SpatialElement spatialElement)) return;
      if (!Params.TryGetData(DA, "Boundary Location", out ARDB.SpatialElementBoundaryLocation? boundaryLocation)) return;

      if (boundaryLocation == null) return;
      if (boundaryLocation == ARDB.SpatialElementBoundaryLocation.CoreBoundary ||
          boundaryLocation == ARDB.SpatialElementBoundaryLocation.CoreCenter)
      {
          throw new Exceptions.RuntimeArgumentException("Boundary Location", "Boundary location should be chosen between finish or center.", boundaryLocation);
      }

      var boundaryOptions = new ARDB.SpatialElementBoundaryOptions();
      boundaryOptions.SpatialElementBoundaryLocation = boundaryLocation.Value;
      var profiles = GetSpatialElementProfiles(DA, spatialElement, boundaryOptions, out var elements);

      Brep brep = null;
      if (!spatialElement.GetType().Equals(typeof(ARDB.Area)))
        brep = GetSpatialElementGeometry(spatialElement.Document, spatialElement, boundaryOptions);
      
      DA.SetData("Geometry", brep);
      DA.SetDataTree(1, profiles);
      DA.SetDataTree(2, elements);
    }

    private GH_Structure<GH_Curve> GetSpatialElementProfiles(IGH_DataAccess DA, ARDB.SpatialElement se, ARDB.SpatialElementBoundaryOptions bo, out GH_Structure<Types.GraphicalElement> elements)
    {
      var profiles = new GH_Structure<GH_Curve>();
      elements = new GH_Structure<Types.GraphicalElement>();

      var boundarySegments = se.GetBoundarySegments(bo);
      var geoPath1 = DA.ParameterTargetPath(1).AppendElement(DA.ParameterTargetIndex(1));
      var geoPath2 = DA.ParameterTargetPath(2).AppendElement(DA.ParameterTargetIndex(2));
      var index = 0;
      foreach (IList<ARDB.BoundarySegment> segments in boundarySegments)
      {
        var geo1 = geoPath1.AppendElement(index);
        var geo2 = geoPath2.AppendElement(index);

        profiles.EnsurePath(geo1);
        elements.EnsurePath(geo2);

        foreach (ARDB.BoundarySegment segment in segments)
        {
          profiles.Append(new GH_Curve(segment.GetCurve().ToCurve()));
          elements.Append(Types.GraphicalElement.FromElementId(se.Document, segment.ElementId) as Types.GraphicalElement);
        }

        index = index + 1;
        
      }
      return profiles;
    }

    private Brep GetSpatialElementGeometry(ARDB.Document doc, ARDB.SpatialElement se, ARDB.SpatialElementBoundaryOptions bo)
    {
      var geometryCalculator = new ARDB.SpatialElementGeometryCalculator(doc, bo);
      var geometry = geometryCalculator.CalculateSpatialElementGeometry(se).GetGeometry();
      return GeometryDecoder.ToBrep(geometry);
    }

  }
}
