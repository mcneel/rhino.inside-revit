using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.27")]
  public class AddAreaBoundaryConditions : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("8C0FF24B-B5D7-40AA-BD4E-1DFF9326DE86");

    public override GH_Exposure Exposure => GH_Exposure.senary;

    public AddAreaBoundaryConditions() : base
    (
      name: "Add Boundary Conditions (Area)",
      nickname: "BC-Area",
      description: "Given a referenced face, this component adds area boundary conditions to the analytical model.",
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
        new Parameters.GeometryFace()
        {
          Name = "Face",
          NickName = "F",
          Description = "Face to reference the boundary condition",
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.BoundaryConditions()
        {
          Name = _BoundaryConditions_,
          NickName = _BoundaryConditions_.Substring(0, 1),
          Description = $"Output {_BoundaryConditions_}",
        }
      )
    };

    const string _BoundaryConditions_ = "Boundary Conditions";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.BOUNDARY_DIRECTION_X,
      ARDB.BuiltInParameter.BOUNDARY_DIRECTION_Y,
      ARDB.BuiltInParameter.BOUNDARY_DIRECTION_Z,
      ARDB.BuiltInParameter.LOAD_USE_LOCAL_COORDINATE_SYSTEM
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;
      if (!Parameters.BoundaryConditions.TryGetStructuralSettings(doc, out var settings) || !settings.BoundaryConditionFamilySymbolFixed.IsValid())
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Document does not have symbols for Boundary Conditions representation.");
        return;
      }

      ReconstructElement<ARDB.Structure.BoundaryConditions>
      (
        doc.Value, _BoundaryConditions_, boundaryConditions =>
        {
          if (!Params.GetData(DA, "Face", out Types.GeometryFace face)) return null;

          if (Types.Element.FromReference(face.Document, face.GetReference()) is Types.AnalyticalElement)
          {
            boundaryConditions = Reconstruct
            (
              boundaryConditions,
              doc.Value,
              face
            );

            DA.SetData(_BoundaryConditions_, Types.AreaBoundaryConditions.FromElement(boundaryConditions));
          }
          else
          {
            boundaryConditions = null;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input 'Face' is not suitable for Boundary Conditions element creation.");
          }

          return boundaryConditions;
        }
      );
    }

    bool Reuse(ARDB.Structure.BoundaryConditions bConditions, Types.GeometryFace face)
    {
      if (bConditions is null) return false;

      var loopA = bConditions.GetLoops().First();
      var loopB = GeometryEncoder.ToCurveLoop( face.TrimmedSurface.Faces.First().OuterLoop.To3dCurve() );

      if (loopA.NumberOfCurves() != loopB.NumberOfCurves()) return false;
      foreach (var (curveA, curveB) in loopA.Zip(loopB, (First, Second) => (First, Second)))
      {
        if (!curveA.AlmostEquals(curveB))
          return false;
      }

      return true;
    }

    ARDB.Structure.BoundaryConditions Create(ARDB.Document doc, Types.GeometryFace face)
    {
#if REVIT_2027
      return ARDB.Structure.BoundaryConditions.CreateAreaBoundaryConditions(doc,
#else
      return doc.Create.NewAreaBoundaryConditions(
#endif
        face.GetReference(),
        ARDB.Structure.TranslationRotationValue.Fixed, 0.0,
        ARDB.Structure.TranslationRotationValue.Fixed, 0.0,
        ARDB.Structure.TranslationRotationValue.Fixed, 0.0);
    }

    ARDB.Structure.BoundaryConditions Reconstruct
    (
      ARDB.Structure.BoundaryConditions bConditions,
      ARDB.Document doc,
      Types.GeometryFace surface
    )
    {
      if (!Reuse(bConditions, surface))
      {
        bConditions = bConditions.ReplaceElement
        (
          Create(doc, surface),
          ExcludeUniqueProperties
        );
      }

      return bConditions;
    }
  }
}
