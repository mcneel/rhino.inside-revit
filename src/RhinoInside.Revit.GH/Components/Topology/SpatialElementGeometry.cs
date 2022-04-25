using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  [ComponentVersion(introduced: "1.7")]
  public class SpatialElementGeometry : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("A1878F3D-AC42-4AD3-BCE0-0D2C7CD661EB");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public SpatialElementGeometry() : base
    (
      name: "Spatial Element Geometry",
      nickname: "SE-Geometry",
      description: "Get the geometry of the specified spatial element",
      category: "Revit",
      subCategory: "Topology"
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
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.SpatialElementBoundaryLocation>()
        {
          Name = "Boundary Location",
          NickName = "BL",
          Description = "Location line setting of the spatial element",
        }.SetDefaultVale(ARDB.SpatialElementBoundaryLocation.Finish)
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
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Brep()
        {
          Name = "Top Faces",
          NickName = "TF",
          Description = "Boundary top faces of the given spatial element",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Top Elements",
          NickName = "TE",
          Description = "Boundary top elements of the given spatial element",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Brep()
        {
          Name = "Side Faces",
          NickName = "SF",
          Description = "Boundary side faces of the given spatial element",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Side Elements",
          NickName = "SE",
          Description = "Boundary side elements of the given spatial element",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Brep()
        {
          Name = "Bottom Faces",
          NickName = "BF",
          Description = "Boundary bottom faces of the given spatial element",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Bottom Elements",
          NickName = "BE",
          Description = "Boundary bottom elements of the given spatial element",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Secondary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Spatial Element", out ARDB.SpatialElement spatialElement)) return;
      if (!Params.TryGetData(DA, "Boundary Location", out ARDB.SpatialElementBoundaryLocation? boundaryLocation)) return;

      if (boundaryLocation == null) return;
      if (boundaryLocation != ARDB.SpatialElementBoundaryLocation.Finish &&
          boundaryLocation != ARDB.SpatialElementBoundaryLocation.Center)
      {
        throw new Exceptions.RuntimeArgumentException("Boundary Location", "Only Finish and Center are allowed.", boundaryLocation);
      }

      if (ARDB.SpatialElementGeometryCalculator.CanCalculateGeometry(spatialElement))
      {
        var document = spatialElement.Document;
        var boundaryOptions = new ARDB.SpatialElementBoundaryOptions
        {
          SpatialElementBoundaryLocation = boundaryLocation.Value,
          StoreFreeBoundaryFaces = true
        };

        using (var calculator = new ARDB.SpatialElementGeometryCalculator(document, boundaryOptions))
        {
          using (var results = calculator.CalculateSpatialElementGeometry(spatialElement))
          {
            if (results.GetGeometry() is ARDB.Solid shell)
            {
              Params.TrySetData(DA, "Geometry", () => shell.ToBrep());

              var _TopFaces_        = Params.IndexOfOutputParam("Top Faces");
              var _TopElements_     = Params.IndexOfOutputParam("Top Elements");
              var _SideFaces_       = Params.IndexOfOutputParam("Side Faces");
              var _SideElements_    = Params.IndexOfOutputParam("Side Elements");
              var _BottomFaces_     = Params.IndexOfOutputParam("Bottom Faces");
              var _BottomElements_  = Params.IndexOfOutputParam("Bottom Elements");

              if (_BottomFaces_ >= 0 || _TopFaces_ >= 0 || _SideFaces_ >= 0 || _BottomElements_ >= 0 || _TopElements_ >= 0 || _SideElements_ >= 0)
              {
                var topFacesPath = _TopFaces_ < 0 ? default : DA.ParameterTargetPath(_TopFaces_).AppendElement(DA.ParameterTargetIndex(_TopFaces_));
                var topFaces = _TopFaces_ < 0 ? default : new GH_Structure<GH_Brep>();
                var topElementsPath = _TopElements_ < 0 ? default : DA.ParameterTargetPath(_TopElements_).AppendElement(DA.ParameterTargetIndex(_TopElements_));
                var topElements = _TopElements_ < 0 ? default : new GH_Structure<Types.GraphicalElement>();

                var sideFacesPath = _SideFaces_ < 0 ? default : DA.ParameterTargetPath(_SideFaces_).AppendElement(DA.ParameterTargetIndex(_SideFaces_));
                var sideFaces = _SideFaces_ < 0 ? default : new GH_Structure<GH_Brep>();
                var sideElementsPath = _SideElements_ < 0 ? default : DA.ParameterTargetPath(_SideFaces_).AppendElement(DA.ParameterTargetIndex(_SideFaces_));
                var sideElements = _SideElements_ < 0 ? default : new GH_Structure<Types.GraphicalElement>();

                var bottomFacesPath = _BottomFaces_ < 0 ? default : DA.ParameterTargetPath(_BottomFaces_).AppendElement(DA.ParameterTargetIndex(_BottomFaces_));
                var bottomFaces = _BottomFaces_ < 0 ? default : new GH_Structure<GH_Brep>();
                var bottomElementsPath = _BottomElements_ < 0 ? default : DA.ParameterTargetPath(_BottomElements_).AppendElement(DA.ParameterTargetIndex(_BottomElements_));
                var bottomElements = _BottomElements_ < 0 ? default : new GH_Structure<Types.GraphicalElement>();

                var index = 0;
                foreach (var faces in shell.Faces.Cast<ARDB.Face>().Select(x => results.GetBoundaryFaceInfo(x)))
                {
                  var topF = _TopFaces_ < 0 ? default : new List<GH_Brep>();
                  var topE = _TopElements_ < 0 ? default : new List<Types.GraphicalElement>();
                  var sideF = _SideFaces_ < 0 ? default : new List<GH_Brep>();
                  var sideE = _SideElements_ < 0 ? default : new List<Types.GraphicalElement>();
                  var bottomF = _BottomFaces_ < 0 ? default : new List<GH_Brep>();
                  var bottomE = _BottomElements_ < 0 ? default : new List<Types.GraphicalElement>();

                  topFaces?.EnsurePath(topFacesPath.AppendElement(index));
                  topElements?.EnsurePath(topElementsPath.AppendElement(index));
                  sideFaces?.EnsurePath(sideFacesPath.AppendElement(index));
                  sideElements?.EnsurePath(sideElementsPath.AppendElement(index));
                  bottomFaces?.EnsurePath(bottomFacesPath.AppendElement(index));
                  bottomElements?.EnsurePath(bottomElementsPath.AppendElement(index));

                  index++;

                  if (faces is null) continue;
                  foreach (var face in faces)
                  {
                    switch (face.SubfaceType)
                    {
                      case ARDB.SubfaceType.Bottom:
                        bottomF?.Add(new GH_Brep(face.GetSubface().ToBrep()));
                        bottomE?.Add(Types.GraphicalElement.FromElementId(document, face.SpatialBoundaryElement) as Types.GraphicalElement);
                        break;

                      case ARDB.SubfaceType.Top:
                        topF?.Add(new GH_Brep(face.GetSubface().ToBrep()));
                        topE?.Add(Types.GraphicalElement.FromElementId(document, face.SpatialBoundaryElement) as Types.GraphicalElement);
                        break;

                      case ARDB.SubfaceType.Side:
                        sideF?.Add(new GH_Brep(face.GetSubface().ToBrep()));
                        sideE?.Add(Types.GraphicalElement.FromElementId(document, face.SpatialBoundaryElement) as Types.GraphicalElement);
                        break;
                    }
                  }

                  bottomFaces?.AppendRange(bottomF);  bottomElements?.AppendRange(bottomE);
                  topFaces?.AppendRange(topF);        topElements?.AppendRange(topE);
                  sideFaces?.AppendRange(sideF);      sideElements?.AppendRange(sideE);
                }

                if (_TopFaces_ >= 0) DA.SetDataTree(_TopFaces_, topFaces);
                if (_TopElements_ >= 0) DA.SetDataTree(_TopElements_, topElements);
                if (_SideFaces_ >= 0) DA.SetDataTree(_SideFaces_, sideFaces);
                if (_SideElements_ >= 0) DA.SetDataTree(_SideElements_, sideElements);
                if (_BottomFaces_ >= 0) DA.SetDataTree(_BottomFaces_, bottomFaces);
                if (_BottomElements_ >= 0) DA.SetDataTree(_BottomElements_, bottomElements);
              }
            }
          }
        }
      }
    }
  }

  [ComponentVersion(introduced: "1.7")]
  public class SpatialElementBoundary : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("419062DF-CD1C-4AEB-B4CA-E19402FE3317");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public SpatialElementBoundary() : base
    (
      name: "Spatial Element Boundary",
      nickname: "SE-Boundary",
      description: "Get the boundary of the specified spatial element",
      category: "Revit",
      subCategory: "Topology"
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
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.SpatialElementBoundaryLocation>()
        {
          Name = "Boundary Location",
          NickName = "BL",
          Description = "Location line setting of the spatial element",
        }.SetDefaultVale(ARDB.SpatialElementBoundaryLocation.Finish)
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Boundary of the given spatial element",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Segments",
          NickName = "S",
          Description = "Boundary segments of the given spatial element",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Boundary elements of the given spatial element",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Secondary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Spatial Element", out ARDB.SpatialElement spatialElement)) return;
      if (!Params.TryGetData(DA, "Boundary Location", out ARDB.SpatialElementBoundaryLocation? boundaryLocation)) return;

      var boundaryOptions = new ARDB.SpatialElementBoundaryOptions
      {
        SpatialElementBoundaryLocation = boundaryLocation.Value,
        StoreFreeBoundaryFaces = true
      };

      var _Boundary_ = Params.IndexOfOutputParam("Boundary");
      var _Segments_ = Params.IndexOfOutputParam("Segments");
      if (_Segments_ >= 0 || _Boundary_ >= 0)
      {
        var index = Math.Max(_Boundary_, _Segments_);
        var segments = GetSegments(DA.ParameterTargetPath(index).AppendElement(DA.ParameterTargetIndex(index)), spatialElement, boundaryOptions);
        if (_Boundary_ >= 0)
        {
          var tol = GeometryObjectTolerance.Model;
          DA.SetDataList(_Boundary_, segments.Branches.SelectMany(c => Curve.JoinCurves(c.Select(x => x.Value)/*, tol.VertexTolerance, preserveDirection: true*/)));
        }

        if (_Segments_ >= 0) DA.SetDataTree(_Segments_, segments);
      }

      var _Elements_ = Params.IndexOfOutputParam("Elements");
      if (_Elements_ >= 0) DA.SetDataTree(_Elements_, GetElements(DA.ParameterTargetPath(_Elements_).AppendElement(DA.ParameterTargetIndex(_Elements_)), spatialElement, boundaryOptions));
    }

    GH_Structure<GH_Curve> GetSegments(GH_Path path, ARDB.SpatialElement se, ARDB.SpatialElementBoundaryOptions options)
    {
      var index = 0;

      var boundary = new GH_Structure<GH_Curve>();
      foreach (var segments in se.GetBoundarySegments(options))
      {
        boundary.EnsurePath(path.AppendElement(index++));

        foreach (var segment in segments)
          boundary.Append(new GH_Curve(segment.GetCurve().ToCurve()));
      }

      return boundary;
    }

    GH_Structure<Types.GraphicalElement> GetElements(GH_Path path, ARDB.SpatialElement element, ARDB.SpatialElementBoundaryOptions options)
    {
      var index = 0;

      var elements = new GH_Structure<Types.GraphicalElement>();
      foreach (var segments in element.GetBoundarySegments(options))
      {
        elements.EnsurePath(path.AppendElement(index++));

        foreach (var segment in segments)
        {
          if (element.Document.GetElement(segment.ElementId) is ARDB.RevitLinkInstance instance)
            elements.Append(Types.GraphicalElement.FromElementId(instance.GetLinkDocument(), segment.LinkElementId) as Types.GraphicalElement);
          else
            elements.Append(Types.GraphicalElement.FromElementId(element.Document, segment.ElementId) as Types.GraphicalElement);
        }
      }

      return elements;
    }
  }
}
