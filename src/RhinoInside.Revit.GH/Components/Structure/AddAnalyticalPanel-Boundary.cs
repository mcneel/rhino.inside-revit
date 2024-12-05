using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Exceptions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalOpening;
#else
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalModelSurface;
#endif

  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class AddAnalyticalPanelByBoundary : BaseAnalyticalComponent
  {
    public override Guid ComponentGuid => new Guid("BA2D1733-0A7A-463C-BDDC-4262405F4FE6");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalPanelByBoundary() : base
    (
      name: "Add Analytical Panel (Boundary)",
      nickname: "AP-Boundary",
      description: "Given its boundary, it adds an analytical panel to the active Revit document",
      category: "Revit",
      subCategory: "Structure"
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
        new Param_Surface()
        {
          Name = "Boundary",
          NickName = "B",
          Description = "Analytical panel boundary surface",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Openings",
          NickName = "O",
          Description = "Create analytical openings",
        }.SetDefaultVale(true), ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalPanel()
        {
          Name = _AnalyticalPanel_,
          NickName = "P",
          Description = $"Output {_AnalyticalPanel_}",
        }
      ),
      new ParamDefinition
      (
        new Parameters.AnalyticalOpening()
        {
          Name = "Analytical Openings",
          NickName = "O",
          Description = "Output openings",
        }, ParamRelevance.Secondary
      )
    };

    const string _AnalyticalPanel_ = "Analytical Panel";

//    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
//    {
//#if REVIT_2023
//      ARDB.BuiltInParameter.STRUCTURAL_ANALYZES_AS,
//      ARDB.BuiltInParameter.ANALYTICAL_ELEMENT_STRUCTURAL_ROLE,
//      ARDB.BuiltInParameter.ANALYTICAL_PANEL_THICKNESS
//#endif
//    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      var panel = ReconstructElement<ARDB_AnalyticalPanel>
      (
        doc.Value, _AnalyticalPanel_, analyticalPanel =>
        {
          // Input
          if (!Params.GetData(DA, "Boundary", out Brep boundary)) return null;
          if (!Params.TryGetData(DA, "Openings", out bool? openings)) return null;

          var tol = GeometryTolerance.Model;
          if (boundary.Faces.Count != 1)
            throw new RuntimeArgumentException("Boundary", "Boundary surface should have only one face.", boundary);

          if (!boundary.Faces[0].TryGetPlane(out var _, tol.VertexTolerance))
            throw new RuntimeArgumentException("Boundary", "Boundary surface should be planar.", boundary);

          var loops = openings ?? true ?
            boundary.Loops.Select(x => x.To3dCurve()).ToArray() :
            boundary.Loops.Where(x => x.LoopType == BrepLoopType.Outer).Select(x => x.To3dCurve()).ToArray();

          var boundaryPlane = default(Plane);
          var maxArea = 0.0;
          for (int index = 0; index < loops.Length; ++index)
          {
            var loop = loops[index];
            var plane = default(Plane);
            if(loop is null || loop.IsShort(tol.ShortCurveTolerance))
              throw new RuntimeArgumentException("Boundary", $"Loop {index} is too short.\nTolerance is {tol.ShortCurveTolerance}", loop);

            if (!loop.IsClosed(tol.VertexTolerance) || !loop.TryGetPlane(out plane, tol.VertexTolerance))
              throw new RuntimeArgumentException("Boundary", $"Loop {index} should be closed and planar.\nTolerance is {tol.VertexTolerance}", loop);

            loops[index] = loop.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? loop;

            using (var properties = AreaMassProperties.Compute(loop, tol.VertexTolerance))
            {
              if (properties is null)
                throw new RuntimeArgumentException("Boundary", "Failed to compute loop Area.", loop);

              if (properties.Area > maxArea)
              {
                maxArea = properties.Area;
                var orientation = loop.ClosedCurveOrientation(plane);

                if (orientation == CurveOrientation.CounterClockwise)
                  plane.Flip();

                boundaryPlane = plane;
              }
              else if (plane.Normal.IsParallelTo(boundaryPlane.Normal) == 0 || Math.Abs(plane.DistanceTo(boundaryPlane.Origin)) > GeometryTolerance.Internal.DefaultTolerance)
              {
                throw new RuntimeArgumentException("Boundary", "Loops should be a list of coplanar curves.", loops);
              }
            }
          }

          // Compute
          analyticalPanel = Reconstruct
          (
            analyticalPanel,
            doc.Value,
            loops
          );

          DA.SetData(_AnalyticalPanel_, analyticalPanel);
          return analyticalPanel;
        }
      );

      Params.TrySetDataList(DA, "Analytical Openings", () => panel.GetAnalyticalOpeningsIds().Select(x => new Types.AnalyticalOpening(doc.Value.GetElement(x) as ARDB_AnalyticalOpening)));
#endif
    }

//#if REVIT_2023
//    bool Reuse
//    (
//      ARDB_AnalyticalPanel analyticalPanel,
//      IList<Curve> boundary
//    )
//    {
//      if (analyticalPanel is null) return false;
//      if (analyticalPanel.GetOuterContour() is null) return false;
//      if (boundary.Count < 1) return false;

//      var curveLoop = boundary.ConvertAll(x => x.ToBoundedCurveLoop());
//      analyticalPanel.SetOuterContour(curveLoop[0]);

//      var openingIds = analyticalPanel.GetAnalyticalOpeningsIds().OrderBy(x => x.ToValue()).ToArray();
//      int o = 1;
//      for (; o < curveLoop.Length; ++o)
//        Create(analyticalPanel, curveLoop[o], o - 1 < openingIds.Length ? openingIds[o - 1] : null);

//      analyticalPanel.Document.Delete(openingIds.Skip(o - 1).ToArray());

//      return true;
//    }

//    ARDB_AnalyticalOpening Create(ARDB_AnalyticalPanel panel, ARDB.CurveLoop loop, ARDB.ElementId openingId)
//    {
//      ARDB_AnalyticalOpening opening = null;
//      if (openingId is object)
//      {
//        opening = panel.Document.GetElement(openingId) as ARDB_AnalyticalOpening;
//        opening.SetOuterContour(loop);
//      }
//      else
//      {
//        opening = ARDB_AnalyticalOpening.Create(panel.Document, loop, panel.Id);
//      }

//      return opening;
//    }

//    ARDB_AnalyticalPanel Create(ARDB.Document doc, IList<Curve> boundary)
//    {
//      if (boundary.Count < 1) return null;

//      var curveLoop = boundary.ConvertAll(x => x.ToBoundedCurveLoop());

//      if (curveLoop is null)
//        throw new ArgumentException("Failed to convert boundary curves to CurveLoop.", nameof(boundary));

//      var panel = ARDB_AnalyticalPanel.Create(doc, curveLoop[0]);

//      for (int b = 1; b < boundary.Count; ++b)
//        ARDB_AnalyticalOpening.Create(doc, curveLoop[b], panel.Id);

//      return panel;
//    }

//    protected ARDB_AnalyticalPanel Reconstruct
//    (
//      ARDB_AnalyticalPanel analyticalPanel,
//      ARDB.Document doc,
//      IList<Curve> boundary
//    )
//    {
//      if (!Reuse(analyticalPanel, boundary))
//      {
//        analyticalPanel = analyticalPanel.ReplaceElement
//        (
//          Create(doc, boundary),
//          ExcludeUniqueProperties
//        );
//      }

//      return analyticalPanel;
//    }
//#endif
  }
}
