using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using RhinoInside.Revit.GH.Parameters;

namespace RhinoInside.Revit.GH.Components.Structure
{

  #if REVIT_2023
    using ARDB_Structure_AnalyticalMember = ARDB.Structure.AnalyticalMember;
#else
      using ARDB_Structure_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
#endif

  [ComponentVersion(introduced: "1.27")]
  public class AddAnalyticalMember : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("88AD5522-B3AD-4A67-AB96-3D90249BA215");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.secondary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalMember() : base
    (
      name: "Add Analytical Member",
      nickname: "A-Member",
      description: "Given its location curve, it adds an analytical member to the active Revit document",
      category: "Revit",
      subCategory: "Structure"
    )
    { }
    
    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      #if REVIT_2023
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
          Description = "Analytical member location",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Enum<Types.AnalyticalStructuralRole>
        {
          Name = "Structural Role",
          NickName = "R",
          Description = "Analytical member structural role",
          Access = GH_ParamAccess.item,
          Optional = true,
        }.SetDefaultVale(ARDB.Structure.AnalyticalStructuralRole.StructuralRoleMember)
      )
#endif
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
#if REVIT_2023
      new ParamDefinition
      (
        new Parameters.AnalyticalMember()
        {
          Name = _AnalyticalMember_,
          NickName = _AnalyticalMember_.Substring(0, 1),
          Description = $"Output {_AnalyticalMember_}",
        }
      )
#endif
    };

    const string _AnalyticalMember_ = "Analytical Member";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
#if REVIT_2023
      ARDB.BuiltInParameter.STRUCTURAL_SECTION_SHAPE,
      ARDB.BuiltInParameter.STRUCTURAL_ANALYZES_AS,
      ARDB.BuiltInParameter.ANALYTICAL_ELEMENT_STRUCTURAL_ROLE,
      ARDB.BuiltInParameter.ANALYTICAL_MEMBER_ROTATION,
      ARDB.BuiltInParameter.ANALYTICAL_MEMBER_SECTION_TYPE
#endif
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB_Structure_AnalyticalMember>
      (
        doc.Value, _AnalyticalMember_, analyticalMember =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;
          if (!Params.GetData(DA, "Structural Role", out Types.AnalyticalStructuralRole structuralRole)) return null;

          // Compute
          analyticalMember = Reconstruct
          (
            analyticalMember,
            doc.Value,
            curve,
            structuralRole.Value
          );

          DA.SetData(_AnalyticalMember_, analyticalMember);
          return analyticalMember;
        }
      );
#endif
    }
#if REVIT_2023
    bool Reuse
    (
      ARDB_Structure_AnalyticalMember analyticalMember,
      Curve curve,
      ARDB.Structure.AnalyticalStructuralRole structuralRole
    )
    {
      if (analyticalMember is null) return false;

      if ( analyticalMember.StructuralRole != structuralRole)
        analyticalMember.StructuralRole = structuralRole;

      using (var loc = analyticalMember.GetCurve() as ARDB.Curve)
      {
        if (!loc.IsSameKindAs(curve.ToCurve()))
          return false;

        if (!loc.AlmostEquals(curve.ToCurve(), analyticalMember.Document.Application.VertexTolerance))
          analyticalMember.SetCurve(curve.ToCurve());
      }

      return true;
    }

    ARDB_Structure_AnalyticalMember Create(ARDB.Document doc, ARDB.Curve curve, ARDB.Structure.AnalyticalStructuralRole structuralRole)
    {
      ARDB_Structure_AnalyticalMember analyticalMember = ARDB_Structure_AnalyticalMember.Create(doc, curve);
      analyticalMember.StructuralRole = structuralRole;
      return analyticalMember;
    }

    ARDB_Structure_AnalyticalMember Reconstruct
    (
      ARDB_Structure_AnalyticalMember analyticalMember,
      ARDB.Document doc,
      Curve curve,
      ARDB.Structure.AnalyticalStructuralRole structuralRole
    )
    {
      if (!Reuse(analyticalMember, curve, structuralRole))
      {
        analyticalMember = analyticalMember.ReplaceElement
        (
          Create(doc, curve.ToCurve(), structuralRole),
          ExcludeUniqueProperties
        );
        analyticalMember.Document.Regenerate();
      }

      return analyticalMember;
    }
#endif
  }
}
