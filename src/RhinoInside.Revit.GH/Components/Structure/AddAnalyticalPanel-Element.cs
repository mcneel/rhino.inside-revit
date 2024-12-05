using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using RhinoInside.Revit.GH.Exceptions;

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
  public class AddAnalyticalPanelByElement : BaseAnalyticalComponent
  {
    public override Guid ComponentGuid => new Guid("F2228146-1A4B-42BB-AA90-5EBE35F70160");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalPanelByElement() : base
    (
      name: "Add Analytical Panel (Element)",
      nickname: "AP-Element",
      description: "Given an element, it extract its analytical panel to the active Revit document",
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
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Graphical element",
          Access = GH_ParamAccess.item
        }
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
          NickName = _AnalyticalPanel_.Substring(0, 1),
          Description = $"Output {_AnalyticalPanel_}",
        }
      )
    };

    const string _AnalyticalPanel_ = "Analytical Panel";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB_AnalyticalPanel>
      (
        doc.Value, _AnalyticalPanel_, analyticalPanel =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return null;

          // Check input
          bool isAnalyticalPanel = false;
          Brep boundary = null;
          switch (element)
          {
            case Types.Wall wall:
              isAnalyticalPanel = true;
              boundary = wall.TrimmedSurface;
              break;

            case Types.Floor floor:
              isAnalyticalPanel = true;
              boundary = floor.Sketch.TrimmedSurface;
              break;

            default:
              this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Element with id {element.Id} is not supported for creating an analytical element");
              break;
          }

          if (!isAnalyticalPanel) return null;

          if (boundary.Faces.Count != 1)
            throw new RuntimeArgumentException("Boundary", "Boundary surface should have only one face.", boundary);

          if (!boundary.Faces[0].TryGetPlane(out var _, tol.VertexTolerance))
            throw new RuntimeArgumentException("Boundary", "Boundary surface should be planar.", boundary);

          var loops = boundary.Loops.Where(x => x.LoopType == BrepLoopType.Outer).Select(x => x.To3dCurve()).ToArray();

          var boundaryPlane = default(Rhino.Geometry.Plane);
          var maxArea = 0.0;
          for (int index = 0; index < loops.Length; ++index)
          {
            var loop = loops[index];
            var plane = default(Rhino.Geometry.Plane);
            if (loop is null || loop.IsShort(tol.ShortCurveTolerance))
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
#endif
    }
  }
}
