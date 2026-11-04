using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Site
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using GH.Exceptions;

#if REVIT_2024
  [ComponentVersion(introduced: "1.37"), ComponentRevitAPIVersion(min: "2024.0")]
  public class AddToposolidByMesh : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("D9B0616F-6433-4628-B304-14BC6643E5C4");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.tertiary);

    public AddToposolidByMesh() : base
    (
      name: "Add Toposolid (Mesh)",
      nickname: "M-Toposolid",
      description: "Given a mesh, it adds a Toposolid element to the active Revit document",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
       (
        new Param_Mesh
        {
          Name = "Mesh",
          NickName = "M",
          Description = "Toposolid surface"
        }
      ),
      new ParamDefinition
       (
        new Parameters.HostObjectType
        {
          Name = "Type",
          NickName = "T",
          Description = "Toposolid type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Toposolid
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
       (
        new Parameters.LevelConstraint
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the {_Toposolid_.ToLowerInvariant()}.{OS.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Boundary'.",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Toposolid()
        {
          Name = _Toposolid_,
          NickName = _Toposolid_.Substring(0, 1),
          Description = $"Output {_Toposolid_}",
        }
      )
    };

    const string _Toposolid_ = "Toposolid";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.LEVEL_PARAM,
      ARDB.BuiltInParameter.TOPOSOLID_HEIGHTABOVELEVEL_PARAM,
    };

    static bool TryGetMeshBoundaries(Mesh mesh, out IList<Curve> boundaries2d, out IList<Curve> boundaries3d)
    {
      var count = mesh.TopologyEdges.Count;
      if (count > 0)
      {
        boundaries3d = new List<Curve>(count);
        for (int e = 0; e < count; ++e)
        {
          var faces = mesh.TopologyEdges.GetConnectedFaces(e);
          switch (faces.Length)
          {
            case 1:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              if (face0.Z <= 0.0) continue;
            }
            break;
            case 2:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              var face1 = mesh.FaceNormals[faces[1]];
              if (face0.Z <= 0.0 == face1.Z <= 0.0) continue;
              var angle = Vector3d.VectorAngle(face0, face1);
              if (angle <= RhinoMath.ToRadians(0.1)) continue;
            }
            break;
            default: continue;
          }

          boundaries3d.Add(new LineCurve(mesh.TopologyEdges.EdgeLine(e)));
        }
        boundaries3d = Curve.JoinCurves(boundaries3d).ToArray();

        var bbox = BoundingBox.Empty;
        foreach (var boundary in boundaries3d)
          bbox.Union(boundary.GetBoundingBox(accurate: true));

        var plane = new Plane(new Point3d(0.0, 0.0, bbox.Min.Z), Vector3d.ZAxis);
        var polylines2d = mesh.GetOutlines(plane);
        var polylines3d = new List<Polyline>();

        var silhouette = boundaries3d;
        foreach (var polyline2d in polylines2d)
        {
          var polyline3d = new Polyline
          (
            polyline2d.Select
            (
              pt =>
              (
                Silhouette: silhouette.
                Select(c => Intersection.CurveLine(c, new Line(new Point3d(pt.X, pt.Y, bbox.Max.Z), new Point3d(pt.X, pt.Y, bbox.Min.Z)), 0.1, 0.01)).
                SelectMany(x => x).
                OrderByDescending(x => x.PointA.Z).
                FirstOrDefault(),
                Outline: pt
              )
            ).
            Select(x => x.Silhouette?.PointA ?? x.Outline)
          );
          polylines3d.Add(polyline3d ?? polyline2d);
        }

        boundaries3d = polylines3d.Select(x => new PolylineCurve(x)).ToArray();
        boundaries2d = polylines2d.Select(x => new PolylineCurve(x)).ToArray();
        return true;
      }

      boundaries2d = default;
      boundaries3d = default;
      return false;
    }

    static bool TryGetMeshPoints(Mesh mesh, out IList<Point3d> points)
    {
      if (mesh.Faces.Count > 0)
      {
        var bbox = mesh.GetBoundingBox(accurate: true);

        var vi = new HashSet<Point3d>(mesh.Vertices.Count);
        for (int e = 0; e < mesh.TopologyEdges.Count; ++e)
        {
          var faces = mesh.TopologyEdges.GetConnectedFaces(e);
          if (faces.Length != 2) continue;

          var face0 = mesh.FaceNormals[faces[0]];
          var face1 = mesh.FaceNormals[faces[1]];
          if (face0.Z <= 0.1 && face1.Z <= 0.1) continue;

          var edge = mesh.TopologyEdges.EdgeLine(e);
          {
            var pt = edge.From;
            var ray = new Line(new Point3d(pt.X, pt.Y, pt.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z));
            if (Intersection.MeshLineSorted(mesh, ray, out var _).Length <= 1) vi.Add(pt);
          }
          {
            var pt = edge.To;
            var ray = new Line(new Point3d(pt.X, pt.Y, pt.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z));
            if (Intersection.MeshLineSorted(mesh, ray, out var _).Length <= 1) vi.Add(pt);
          }
        }

        points = vi.ToArray();
        return points.Count > 0;
      }

      points = default;
      return false;
    }

    static bool TryGetMeshCreases(Mesh mesh, out IList<Line> creases)
    {
      var count = mesh.TopologyEdges.Count;
      if (count > 0)
      {
        var bbox = mesh.GetBoundingBox(accurate: true);

        creases = new List<Line>(count);
        for (int e = 0; e < count; ++e)
        {
          var faces = mesh.TopologyEdges.GetConnectedFaces(e);
          switch (faces.Length)
          {
            case 1:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              if (face0.Z <= 0.0) continue;
            }
            break;
            case 2:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              var face1 = mesh.FaceNormals[faces[1]];
              if (face0.Z <= 0.0 && face1.Z <= 0.0) continue;
              var angle = Vector3d.VectorAngle(face0, face1);
              if (angle < RhinoMath.ToRadians(1.0)) continue;
            }
            break;
            default: continue;
          }

          var edge = mesh.TopologyEdges.EdgeLine(e);
          {
            var pt = edge.From;
            var ray = new Line(new Point3d(pt.X, pt.Y, pt.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z));
            if (Intersection.MeshLineSorted(mesh, ray, out var _).Length > 1) continue;
          }
          {
            var pt = edge.To;
            var ray = new Line(new Point3d(pt.X, pt.Y, pt.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z));
            if (Intersection.MeshLineSorted(mesh, ray, out var _).Length > 1) continue;
          }
          creases.Add(mesh.TopologyEdges.EdgeLine(e));
        }

        return true;
      }

      creases = default;
      return false;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Toposolid>
      (
        doc.Value, _Toposolid_, toposolid =>
        {
          // Input
          if (!Params.GetData(DA, "Mesh", out Grasshopper.Kernel.Types.GH_Mesh gh_mesh)) return null;
          var mesh = gh_mesh.Value.DuplicateShallow() as Mesh;
          mesh.FaceNormals.ComputeFaceNormals();

          if (!TryGetMeshBoundaries(mesh, out var boundary2d, out var boundary3d)) return null;
          if (!TryGetMeshPoints(mesh, out var points)) return null;
          if (!TryGetMeshCreases(mesh, out var creases)) return null;

          var boundaryElevation = Interval.Unset;
          {
            var tol = GeometryTolerance.Model;
            var normal = default(Vector3d);
            var maxArea = 0.0; var maxIndex = 0;
            for (int index = 0; index < boundary2d.Count; ++index)
            {
              var loop = boundary2d[index];
              if (loop is null) return null;
              if
              (
                loop.IsShort(tol.ShortCurveTolerance) ||
                !loop.IsClosed ||
                !loop.TryGetPlane(out var plane, tol.VertexTolerance) ||
                plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
              )
                throw new RuntimeArgumentException(nameof(mesh), "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary3d[index]);

              boundaryElevation = Interval.FromUnion(boundaryElevation, new Interval(plane.OriginZ, plane.OriginZ));
              boundary2d[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

              using (var properties = AreaMassProperties.Compute(loop, tol.VertexTolerance))
              {
                if (properties is null) return null;
                if (properties.Area > maxArea)
                {
                  normal = plane.Normal;
                  maxArea = properties.Area;
                  maxIndex = index;

                  var orientation = loop.ClosedCurveOrientation(Plane.WorldXY);
                  if (orientation == CurveOrientation.CounterClockwise)
                    normal.Reverse();
                }
              }
            }

            if (!boundaryElevation.IsValid) return null;
          }

          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.HostObjectType type, doc, ARDB.BuiltInCategory.OST_Toposolid)) return null;

          if (!(type.Value is ARDB.ToposolidType toposolidType))
            throw new RuntimeArgumentException(nameof(type), $"Type '{type.Nomen}' is not a valid toposolid type.");

          if (!Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation)) return null;

          // Solve optional Base
          ERDB.ElevationElementReference.SolveBase
          (
            doc.Value, GeometryEncoder.ToInternalLength(boundaryElevation.Mid),
            0.0, ref baseElevation
          );

          // Compute
          var bbox = mesh.GetBoundingBox(accurate: true);
          var box = new Box(bbox);
          box.RepositionBasePlane(new Point3d(0.0, 0.0, bbox.Min.Z));

          toposolid = Reconstruct
          (
            toposolid,
            doc.Value,
            box,
            boundary2d,
            boundary3d,
            points,
            creases,
            toposolidType,
            baseElevation.Value
          );
          DA.SetData(_Toposolid_, toposolid);
          return toposolid;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.Toposolid toposolid,
      Plane plane,
      IList<Curve> boundaries,
      ARDB.ToposolidType type,
      ARDB.Level baseLevel,
      double baseOffset
    )
    {
      if (toposolid is null) return false;

      if (!(toposolid.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis)))
        return false;

      if (toposolid.GetTypeId() != type.Id)
      {
        if (ARDB.Element.IsValidType(toposolid.Document, new ARDB.ElementId[] { toposolid.Id }, type.Id))
        {
          if (toposolid.ChangeTypeId(type.Id) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            toposolid = toposolid.Document.GetElement(id) as ARDB.Toposolid;
        }
        else return false;
      }

      toposolid.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(baseLevel.Id);
      toposolid.get_Parameter(ARDB.BuiltInParameter.TOPOSOLID_HEIGHTABOVELEVEL_PARAM).Update(baseOffset);

      return true;
    }

    ARDB.Toposolid Create
    (
      ARDB.Document document,
      Plane plane,
      IList<Curve> boundary,
      ARDB.ToposolidType type,
      ARDB.Level baseLevel
    )
    {
      // We create a Level here to obtain a toposolid with a <not associated> `SketchPlane`
      var level = ARDB.Level.Create(document, plane.OriginZ);

      var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);
      var toposolid = ARDB.Toposolid.Create(document, curveLoops, type.Id, level.Id);

      toposolid.get_Parameter(ARDB.BuiltInParameter.LEVEL_PARAM).Update(baseLevel.Id);
      document.Delete(level.Id);

      return toposolid;
    }

    ARDB.Toposolid Reconstruct
    (
      ARDB.Toposolid toposolid,
      ARDB.Document doc,
      Box box,
      IList<Curve> boundary2d,
      IList<Curve> boundary3d,
      IList<Point3d> points,
      IList<Line> creases,
      ARDB.ToposolidType type,
      ERDB.ElevationElementReference baseElevation
    )
    {
      if (!baseElevation.IsLevelConstraint(out var baseLevel, out var baseOffset)) return default;

      if (!Reuse(ref toposolid, box.Plane, boundary2d, type, baseLevel, baseOffset ?? 0.0))
      {
        toposolid = toposolid.ReplaceElement
        (
          Create(doc, box.Plane, boundary2d, type, baseLevel),
          ExcludeUniqueProperties
        );
      }

      if (toposolid is object)
      {
        toposolid.get_Parameter(ARDB.BuiltInParameter.TOPOSOLID_ATTR_THICKNESS_PARAM).Update(box.Z.Length);

        var host = new Types.Toposolid(toposolid);
        host.SetSlabShape(boundary2d, boundary3d, points ?? Array.Empty<Point3d>(), creases ?? Array.Empty<Line>(), out var skipedPoints, out var skipedCreases);

        foreach (var point in skipedPoints)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Point projection is outside boundary.", new Point(point));

        foreach (var crease in skipedCreases)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some invalid creases were skipped.", new LineCurve(crease));
      }

      return toposolid;
    }
  }
#endif
}
