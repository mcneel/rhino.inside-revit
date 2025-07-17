using System;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using RhinoInside.Revit.Convert.System.Collections.Generic;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalMember;
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalPanel;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalOpening;
#else
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
  using ARDB_AnalyticalPanel = ARDB.Structure.AnalyticalModelSurface;
  using ARDB_AnalyticalOpening = ARDB.Structure.AnalyticalModelSurface;
#endif

  public abstract class AddAnalyticalElement : ElementTrackerComponent
  {
    protected AddAnalyticalElement(string name, string nickname, string description, string category, string subCategory)
      : base(name, nickname, description, category, subCategory)
    { }

    #region Analytical Member

#if REVIT_2023
    static readonly ARDB.BuiltInParameter[] ExcludeMemberUniqueProperties =
    {
      ARDB.BuiltInParameter.STRUCTURAL_SECTION_SHAPE,
      ARDB.BuiltInParameter.STRUCTURAL_ANALYZES_AS,
      ARDB.BuiltInParameter.ANALYTICAL_ELEMENT_STRUCTURAL_ROLE,
      ARDB.BuiltInParameter.ANALYTICAL_MEMBER_ROTATION,
      ARDB.BuiltInParameter.ANALYTICAL_MEMBER_SECTION_TYPE
    };

    bool Reuse
    (
      ARDB_AnalyticalMember analyticalMember,
      ARDB.Curve curve
    )
    {
      if (analyticalMember is null) return false;

      using (var loc = analyticalMember.GetCurve())
      {
        if (!loc.IsSameKindAs(curve))
          return false;

        if (!loc.AlmostEquals(curve, analyticalMember.Document.Application.VertexTolerance))
          analyticalMember.SetCurve(curve);
      }

      return true;
    }

    ARDB_AnalyticalMember Create(ARDB.Document doc, ARDB.Curve curve)
    {
      return ARDB_AnalyticalMember.Create(doc, curve);
    }

    protected ARDB_AnalyticalMember Reconstruct
    (
      ARDB_AnalyticalMember analyticalMember,
      ARDB.Document doc,
      ARDB.Curve curve
    )
    {
      if (!Reuse(analyticalMember, curve))
      {
        analyticalMember = analyticalMember.ReplaceElement
        (
          Create(doc, curve),
          ExcludeMemberUniqueProperties
        );
        analyticalMember.Document.Regenerate();
      }

      return analyticalMember;
    }
#endif
    #endregion

    #region Analytical Panel

#if REVIT_2023

    static readonly ARDB.BuiltInParameter[] ExcludePanelUniqueProperties =
    {
          ARDB.BuiltInParameter.STRUCTURAL_ANALYZES_AS,
          ARDB.BuiltInParameter.ANALYTICAL_ELEMENT_STRUCTURAL_ROLE,
          ARDB.BuiltInParameter.ANALYTICAL_PANEL_THICKNESS
    };

    bool Reuse
    (
      ARDB_AnalyticalPanel analyticalPanel,
      IList<Curve> boundary
    )
    {
      if (analyticalPanel is null) return false;
      if (analyticalPanel.GetOuterContour() is null) return false;
      if (boundary.Count < 1) return false;

      var curveLoop = boundary.ConvertAll(x => x.ToBoundedCurveLoop());
      analyticalPanel.SetOuterContour(curveLoop[0]);

      var openingIds = analyticalPanel.GetAnalyticalOpeningsIds().OrderBy(x => x.ToValue()).ToArray();
      int o = 1;
      for (; o < curveLoop.Length; ++o)
        Create(analyticalPanel, curveLoop[o], o - 1 < openingIds.Length ? openingIds[o - 1] : null);

      analyticalPanel.Document.Delete(openingIds.Skip(o - 1).ToArray());

      return true;
    }

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

    ARDB_AnalyticalOpening Create(ARDB_AnalyticalPanel panel, ARDB.CurveLoop loop, ARDB.ElementId openingId)
    {
      ARDB_AnalyticalOpening opening = null;
      if (openingId is object)
      {
        opening = panel.Document.GetElement(openingId) as ARDB_AnalyticalOpening;
        opening.SetOuterContour(loop);
      }
      else
      {
        opening = ARDB_AnalyticalOpening.Create(panel.Document, loop, panel.Id);
      }

      return opening;
    }

    ARDB_AnalyticalPanel Create(ARDB.Document doc, IList<Curve> boundary)
    {
      if (boundary.Count < 1) return null;

      var curveLoop = boundary.ConvertAll(x => x.ToBoundedCurveLoop());

      if (curveLoop is null)
        throw new ArgumentException("Failed to convert boundary curves to CurveLoop.", nameof(boundary));

      var panel = ARDB_AnalyticalPanel.Create(doc, curveLoop[0]);

      for (int b = 1; b < boundary.Count; ++b)
        ARDB_AnalyticalOpening.Create(doc, curveLoop[b], panel.Id);

      return panel;
    }

    protected ARDB_AnalyticalPanel Reconstruct
    (
      ARDB_AnalyticalPanel analyticalPanel,
      ARDB.Document doc,
      IList<Curve> boundary
    )
    {
      if (!Reuse(analyticalPanel, boundary))
      {
        analyticalPanel = analyticalPanel.ReplaceElement
        (
          Create(doc, boundary),
          ExcludePanelUniqueProperties
        );
      }

      return analyticalPanel;
    }

    protected ARDB_AnalyticalPanel Reconstruct
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
          ExcludePanelUniqueProperties
        );
      }

      return analyticalPanel;
    }
#endif

    #endregion

  }
}
