using System;
using System.Linq;
using System.Runtime.InteropServices;
using Rhino;
using Rhino.DocObjects;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using GH.ElementTracking;
  using GH.Kernel.Attributes;

  public class BeamByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("26411AA6-8187-49DF-A908-A292A07918F1");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public BeamByCurve() : base
    (
      name: "Add Beam",
      nickname: "Beam",
      description: "Given its Axis, it adds a Beam element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
    )
    { }

    public override void OnStarted(ARDB.Document document)
    {
      base.OnStarted(document);

      // Reset all previous beams joins
      var beams = Params.TrackedElements<ARDB.FamilyInstance>("Beam", document);
      var pinnedBeams = beams.Where(x => x.Pinned);

      foreach (var beam in pinnedBeams)
      {
        if (beam.StructuralType != ARDB.Structure.StructuralType.NonStructural)
        {
          if (ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, 0))
          {
            ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(beam, 0);
            ARDB.Structure.StructuralFramingUtils.AllowJoinAtEnd(beam, 0);
          }

          if (ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, 1))
          {
            ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(beam, 1);
            ARDB.Structure.StructuralFramingUtils.AllowJoinAtEnd(beam, 1);
          }
        }
      }
    }

    void ReconstructBeamByCurve
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New Beam")]
      ref ARDB.FamilyInstance beam,

      Rhino.Geometry.Curve curve,
      Optional<ARDB.FamilySymbol> type,
      Optional<ARDB.Level> level
    )
    {
      if
      (
        curve.IsClosed ||
        !curve.IsPlanar(GeometryObjectTolerance.Model.VertexTolerance) ||
        curve.GetNextDiscontinuity(Rhino.Geometry.Continuity.C1_continuous, curve.Domain.Min, curve.Domain.Max, out var _)
      )
        ThrowArgumentException(nameof(curve), "Curve must be a C1 continuous planar non closed curve.");

      SolveOptionalLevel(document, curve, ref level, out var _);

      var centerLine = curve.ToCurve();

      if (type.HasValue)
        ChangeElementTypeId(ref beam, type.Value.Id);

      // Try to update Beam
      if (beam is object && beam.Location is ARDB.LocationCurve locationCurve && centerLine.IsSameKindAs(locationCurve.Curve))
      {
        var referenceLevel = beam.get_Parameter(ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
        var updateLevel = referenceLevel.AsElementId() != level.Value.Id;

        if (!updateLevel || !referenceLevel.IsReadOnly)
        {
          if (updateLevel)
          	referenceLevel.Update(level.Value.Id);

          if(!locationCurve.Curve.IsAlmostEqualTo(centerLine))
            locationCurve.Curve = centerLine;

          return;
        }
      }

      // Reconstruct Beam
      {
        SolveOptionalType(document, ref type, ARDB.BuiltInCategory.OST_StructuralFraming, nameof(type));

        var newBeam = document.Create.NewFamilyInstance
        (
          centerLine,
          type.Value,
          level.Value,
          ARDB.Structure.StructuralType.Beam
        );
        
        if (beam.StructuralType != ARDB.Structure.StructuralType.NonStructural)
        {
          if (beam is object && ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, 0))
            ARDB.Structure.StructuralFramingUtils.AllowJoinAtEnd(newBeam, 0);
          else
            ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(newBeam, 0);

          if (beam is object && ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, 1))
            ARDB.Structure.StructuralFramingUtils.AllowJoinAtEnd(newBeam, 1);
          else
            ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(newBeam, 1);
        }

        newBeam.get_Parameter(ARDB.BuiltInParameter.Y_JUSTIFICATION)?.Update((int) ARDB.Structure.YJustification.Origin);
        newBeam.get_Parameter(ARDB.BuiltInParameter.Z_JUSTIFICATION)?.Update((int) ARDB.Structure.ZJustification.Origin);

        newBeam.Document.Regenerate();
        newBeam.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(0.0);

        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
          ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
        };

        ReplaceElement(ref beam, newBeam, parametersMask);
      }
    }
  }
}
