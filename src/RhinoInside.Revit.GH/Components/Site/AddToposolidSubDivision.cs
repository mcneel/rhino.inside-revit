using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;
  using RhinoInside.Revit.GH.Exceptions;

#if REVIT_2024
  [ComponentVersion(introduced: "1.16"), ComponentRevitAPIVersion(min: "2024.0")]
  public class AddToposolidSubDivision : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("1C148123-7E04-42EB-8B51-A6C2B731AD21");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.tertiary);

    public AddToposolidSubDivision() : base
    (
      name: "Add Toposolid Sub-Division",
      nickname: "T-Subdivision",
      description: "Given its outline curve, it adds a Toposolid subdivision element to the active Revit document",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Toposolid()
        {
          Name = "Toposolid",
          NickName = "T",
          Description = "Host Toposolid",
        }
      ),
      new ParamDefinition
       (
        new Param_Curve
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Toposolid boundary profile",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Toposolid()
        {
          Name = _Subdivision_,
          NickName = _Subdivision_.Substring(0, 1),
          Description = $"Output {_Subdivision_}",
        }
      )
    };

    const string _Subdivision_ = "Sub-Division";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Toposolid", out ARDB.Toposolid host)) return;

      ReconstructElement<ARDB.Toposolid>
      (
        host.Document, _Subdivision_, subdivision =>
        {
          // Input
          if (!Params.GetDataList(DA, "Boundary", out IList<Curve> boundary)) return null;

          var boundaryElevation = Interval.Unset;
          {
            var tol = GeometryTolerance.Model;
            var normal = default(Vector3d);
            var maxArea = 0.0; var maxIndex = 0;
            for (int index = 0; index < boundary.Count; ++index)
            {
              var loop = boundary[index];
              if (loop is null) return null;
              if
              (
                loop.IsShort(tol.ShortCurveTolerance) ||
                !loop.IsClosed ||
                !loop.TryGetPlane(out var plane, tol.VertexTolerance) ||
                plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
              )
                throw new RuntimeArgumentException(nameof(boundary), "Boundary loop curves should be a set of valid horizontal, coplanar and closed curves.", boundary);

              boundaryElevation = Interval.FromUnion(boundaryElevation, new Interval(plane.OriginZ, plane.OriginZ));
              boundary[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

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

          // Compute
          subdivision = Reconstruct(subdivision, host, boundary);
          DA.SetData(_Subdivision_, subdivision);
          return subdivision;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.Toposolid subdivision,
      IList<Curve> boundaries
    )
    {
      if (subdivision is null) return false;

      if (!(subdivision.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, boundaries, Vector3d.ZAxis)))
        return false;

      return true;
    }

    ARDB.Toposolid Create
    (
      ARDB.Toposolid host,
      IList<Curve> boundary
    )
    {
      var curveLoops = boundary.ConvertAll(GeometryEncoder.ToCurveLoop);
      var subdivision = host.CreateSubDivision(host.Document, curveLoops);

      return subdivision;
    }

    ARDB.Toposolid Reconstruct
    (
      ARDB.Toposolid subdivision,
      ARDB.Toposolid host,
      IList<Curve> boundary
    )
    {
      if (!Reuse(ref subdivision, boundary))
      {
        subdivision = subdivision.ReplaceElement
        (
          Create(host, boundary),
          ExcludeUniqueProperties
        );
      }

      return subdivision;
    }
  }
#endif
}
