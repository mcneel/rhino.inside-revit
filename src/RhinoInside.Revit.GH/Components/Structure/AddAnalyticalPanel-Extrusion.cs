using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
#else
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
#endif

  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class AddAnalyticalPanelByExtrusion : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("872CCB2C-E374-4C3F-B7A7-24686AD3911C");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalPanelByExtrusion() : base
    (
      name: "Add Analytical Panel (Extrusion)",
      nickname: "AP-Extrusion",
      description: "Given a curve, it adds an analytical panel perpendicular to the provided work plane to the active Revit document",
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
        new Param_Curve()
        {
          Name = "Curve",
          NickName = "C",
          Description = "Analytical panel curve",
        }
      ),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Plane",
          NickName = "P",
          Description = "Analytical panel work plane",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Height",
          NickName = "H",
          Description = "Analytical panel height",
          Optional = true
        }, ParamRelevance.Primary
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

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
#if REVIT_2023
      ARDB.BuiltInParameter.STRUCTURAL_ANALYZES_AS,
      ARDB.BuiltInParameter.ANALYTICAL_ELEMENT_STRUCTURAL_ROLE,
      ARDB.BuiltInParameter.ANALYTICAL_PANEL_THICKNESS
#endif
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB_AnalyticalPanel>
      (
        doc.Value, _AnalyticalPanel_, analyticalPanel =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!Params.TryGetData(DA, "Plane", out Plane? plane)) return null;
          if (!Params.TryGetData(DA, "Height", out double? height)) return null;

          var tol = GeometryTolerance.Model;
          var normal = default(ARDB.XYZ);
          if (plane.HasValue)
          {
            curve = Curve.ProjectToPlane(curve, plane.Value) ?? curve;
            normal = plane.Value.Normal.ToXYZ();
          }

          if (curve.IsShort(tol.ShortCurveTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (curve.IsClosed(tol.ShortCurveTolerance * 1.01))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          var offset = height / Revit.ModelUnits ?? 10.0;
          var analyticalCurve = curve.ToCurve();

          normal ??= analyticalCurve switch
          {
            ARDB.Line l    => l.Direction.CrossProduct(l.Direction.PerpVector()),
            ARDB.Arc a     => a.Normal,
            ARDB.Ellipse e => e.Normal,
            _ => throw new Exceptions.RuntimeArgumentException("Curve", "Curve shuld be a line, an arc or an ellipse.", curve),
          };

          // Compute
          analyticalPanel = Reconstruct
          (
            analyticalPanel,
            doc.Value,
            analyticalCurve.CreateReversed(),
            normal,
            -offset
          );

          DA.SetData(_AnalyticalPanel_, analyticalPanel);
          return analyticalPanel;
        }
      );
#endif
    }

#if REVIT_2023
    bool Reuse
    (
      ARDB_AnalyticalPanel analyticalPanel,
      ARDB.Curve curve,
      ARDB.XYZ normal,
      double offset
    )
    {
      if (analyticalPanel is null) return false;

      var curveLoop = analyticalPanel.GetOuterContour();
      if (!curveLoop.HasPlane())
        return false;

      var curves = new ARDB.Curve[] { null, null, curve, null };
      curves[0] = curve.CreateTransformed(ARDB.Transform.CreateTranslation(normal * offset)).CreateReversed();
      curves[1] = ARDB.Line.CreateBound(curves[0].GetEndPoint(1), curves[2].GetEndPoint(0));
      curves[3] = ARDB.Line.CreateBound(curves[2].GetEndPoint(1), curves[0].GetEndPoint(0));
      curveLoop = ARDB.CurveLoop.Create(curves);
      if (!curveLoop.HasPlane())
        return false;

      analyticalPanel.SetOuterContour(curveLoop);
      return true;
    }

    ARDB_AnalyticalPanel Reconstruct
    (
      ARDB_AnalyticalPanel analyticalPanel,
      ARDB.Document doc,
      ARDB.Curve curve,
      ARDB.XYZ normal,
      double offset
    )
    {
      if (!Reuse(analyticalPanel, curve, normal, offset))
      {
        analyticalPanel = analyticalPanel.ReplaceElement
        (
          ARDB_AnalyticalPanel.Create(doc, curve, normal * offset),
          ExcludeUniqueProperties
        );
      }

      return analyticalPanel;
    }
#endif
  }
}

