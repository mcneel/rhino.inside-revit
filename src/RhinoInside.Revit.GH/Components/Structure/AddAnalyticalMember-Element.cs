using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using RhinoInside.Revit.GH.Parameters;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
#if REVIT_2023
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalMember;
#else
  using ARDB_AnalyticalMember = ARDB.Structure.AnalyticalModelStick;
#endif

  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class AddAnalyticalMemberByElement : AddAnalyticalMember
  {
    public override Guid ComponentGuid => new Guid("C9512B48-977F-48B5-91F1-C3C55AC12F3F");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AddAnalyticalMemberByElement() : base
    (
      name: "Add Analytical Member (Element)",
      nickname: "AM-Element",
      description: "Given an element, it extract its analytical member to the active Revit document",
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
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB_AnalyticalMember>
      (
        doc.Value, _AnalyticalMember_, analyticalMember =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return null;

          //Compute
          bool isAnalyticalMember = false;
          switch (element)
          {
            case Types.FamilyInstance familyInstance:

              switch (familyInstance.Value.StructuralType)
              {
                case ARDB.Structure.StructuralType.Beam:
                case ARDB.Structure.StructuralType.Brace:
                case ARDB.Structure.StructuralType.Column:
                  isAnalyticalMember = true;
                  break;

                //case ARDB.Structure.StructuralType.Footing:
                //case ARDB.Structure.StructuralType.UnknownFraming:
                //case ARDB.Structure.StructuralType.NonStructural:
                //default:
                //  this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"This element is non structural: {element.Id}");
                //  break;
              }
              break;

            default:
              //this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"The element is not valid to create an analytical member: {element.Id}");
              break;
          }

          // Compute
          if (isAnalyticalMember)
          {
            analyticalMember = Reconstruct
            (
              analyticalMember,
              doc.Value,
              element.Curve.ToCurve()
            );
          }
          
          DA.SetData(_AnalyticalMember_, analyticalMember);
          return analyticalMember;
        }
      );
#endif
    }
  }
}
