using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalMember;
#else
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
#endif

  public abstract class BaseAnalyticalComponent : ElementTrackerComponent
  {
    protected BaseAnalyticalComponent(string name, string nickname, string description, string category, string subCategory)
      : base(name, nickname, description, category, subCategory)
    { }

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
  }
}
