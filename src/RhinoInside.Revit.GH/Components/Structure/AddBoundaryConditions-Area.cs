using System;
using System.Linq;
using Autodesk.Revit.DB.Structure;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
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
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;
      Parameters.Document.GetStructuralSettings(this, DA, "Document", out var hasSymbols);
      if (!hasSymbols) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Document does not have symbols for Boundary Conditions representation.");

      ReconstructElement<ARDB.Structure.BoundaryConditions>
      (
        doc.Value, _BoundaryConditions_, boundaryConditions =>
        {
          if (!Params.GetData(DA, "Face", out Types.GeometryFace face)) return null;

          boundaryConditions = Reconstruct
          (
            boundaryConditions,
            doc.Value,
            face
          );

          DA.SetData(_BoundaryConditions_, boundaryConditions);
          //DA.SetData(_BoundaryConditions_, new AreaBoundaryConditions(boundaryConditions));
          return boundaryConditions;
        }
      );
    }

    bool Reuse(ARDB.Structure.BoundaryConditions bConditions, Types.GeometryFace face)
    {
      if (bConditions is null) return false;

      var loopA = bConditions.GetLoops().First();
      var loopB = GeometryEncoder.ToCurveLoop( face.PolySurface.Faces.First().OuterLoop.To3dCurve() );

      for (int i = 0; i < loopA.Count(); i++)
      {
        if (!(loopA.ElementAt(i).IsSameKindAs(loopB.ElementAt(i)) &&
          loopA.ElementAt(i).AlmostEquals(loopB.ElementAt(i))))
        {
          return false;
        }
      }

      return true;
    }

    ARDB.Structure.BoundaryConditions Create(ARDB.Document doc, Types.GeometryFace face)
    {
      var bConditions = doc.Create.NewAreaBoundaryConditions(face.GetReference(),
                                                             TranslationRotationValue.Fixed, 0.0,
                                                             TranslationRotationValue.Fixed, 0.0,
                                                             TranslationRotationValue.Fixed, 0.0);
      return bConditions;
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
