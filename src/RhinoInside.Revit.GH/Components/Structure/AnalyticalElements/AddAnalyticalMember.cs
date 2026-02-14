using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalMember;
#else
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
#endif

  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class AddAnalyticalMember : AddAnalyticalElement
  {
    public override Guid ComponentGuid => new Guid("88AD5522-B3AD-4A67-AB96-3D90249BA215");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
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
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalMember()
        {
          Name = _AnalyticalMember_,
          NickName = _AnalyticalMember_.Substring(0, 1),
          Description = $"Output {_AnalyticalMember_}",
        }
      )
    };

    const string _AnalyticalMember_ = "Analytical Member";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
#if REVIT_2023
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB_AnalyticalMember>
      (
        doc.Value, _AnalyticalMember_, analyticalMember =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;

          // Compute
          analyticalMember = Reconstruct
          (
            analyticalMember,
            doc.Value,
            curve.ToCurve()
          );

          DA.SetData(_AnalyticalMember_, analyticalMember);
          return analyticalMember;
        }
      );
#endif
    }
  }
}
