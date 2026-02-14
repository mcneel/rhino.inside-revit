using System;
using Autodesk.Revit.DB.Structure;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Structure
{
  [ComponentVersion(introduced: "1.27")]
  public class AddPointBoundaryConditions : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("A353B981-96D0-4CF7-9DC7-28D8297F0253");

    public override GH_Exposure Exposure => GH_Exposure.senary;

    public AddPointBoundaryConditions() : base
    (
      name: "Add Boundary Conditions (Point)",
      nickname: "BC-Point",
      description: "Given a reference point, this component adds a point boundary conditions to the analytical model.",
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
        new Parameters.GeometryPoint()
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to reference the boundary condition",
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
      ARDB.BuiltInParameter.BOUNDARY_AREA_RESTRAINT_X,
      ARDB.BuiltInParameter.BOUNDARY_AREA_RESTRAINT_Y,
      ARDB.BuiltInParameter.BOUNDARY_AREA_RESTRAINT_Z,
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
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Point", out Types.GeometryPoint point)) return null;

          // Compute
          boundaryConditions = Reconstruct
          (
            boundaryConditions,
            doc.Value,
            point
          );

          DA.SetData(_BoundaryConditions_, new PointBoundaryConditions(boundaryConditions));
          return boundaryConditions;
        }
      );
    }
    bool Reuse
    (
      ARDB.Structure.BoundaryConditions bConditions,
      Types.GeometryPoint point
    )
    {
      if (bConditions is null) return false;
      if (!bConditions.Point.AlmostEqualPoints(point.Value.Coord)) return false;

      return true;
    }

    ARDB.Structure.BoundaryConditions Create(ARDB.Document doc, Types.GeometryPoint point)
    {

      var bConditions = doc.Create.NewPointBoundaryConditions(point.GetReference(),
                                                              TranslationRotationValue.Fixed, 0.0,
                                                              TranslationRotationValue.Fixed, 0.0,
                                                              TranslationRotationValue.Fixed, 0.0,
                                                              TranslationRotationValue.Fixed, 0.0,
                                                              TranslationRotationValue.Fixed, 0.0,
                                                              TranslationRotationValue.Fixed, 0.0);
      return bConditions;
    }

    ARDB.Structure.BoundaryConditions Reconstruct
    (
      ARDB.Structure.BoundaryConditions bConditions,
      ARDB.Document doc,
      Types.GeometryPoint point
    )
    {
      if (!Reuse(bConditions, point))
      {
        bConditions = bConditions.ReplaceElement
        (
          Create(doc, point),
          ExcludeUniqueProperties
        );
      }
      return bConditions;
    }
  }
}
