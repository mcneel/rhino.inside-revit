using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class BeamByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("26411AA6-8187-49DF-A908-A292A07918F1");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public BeamByCurve() : base
    (
      "Add Beam", "Beam",
      "Given its Axis, it adds a Beam element to the active Revit document",
      "Revit", "Build"
    )
    { }

    public override void OnStarted(DB.Document document)
    {
      base.OnStarted(document);

      // Reset all previous beams joins
      if (PreviousStructure(document) is Types.IGH_ElementId[] previous)
      {
        var beamsToUnjoin = previous.OfType<Types.Element>().
                            Select(x => document.GetElement(x.Id)).
                            OfType<DB.FamilyInstance>().
                            Where(x => x.Pinned);

        foreach (var unjoinedBeam in beamsToUnjoin)
        {
          if (DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(unjoinedBeam, 0))
          {
            DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(unjoinedBeam, 0);
            DB.Structure.StructuralFramingUtils.AllowJoinAtEnd(unjoinedBeam, 0);
          }

          if (DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(unjoinedBeam, 1))
          {
            DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(unjoinedBeam, 1);
            DB.Structure.StructuralFramingUtils.AllowJoinAtEnd(unjoinedBeam, 1);
          }
        }
      }
    }

    void ReconstructBeamByCurve
    (
      DB.Document doc,

      [Description("New Beam")]
      ref DB.FamilyInstance beam,

      Rhino.Geometry.Curve curve,
      Optional<DB.FamilySymbol> type,
      Optional<DB.Level> level
    )
    {
      if
      (
        curve.IsClosed ||
        !curve.IsPlanar(Revit.VertexTolerance * Revit.ModelUnits) ||
        curve.GetNextDiscontinuity(Rhino.Geometry.Continuity.C1_continuous, curve.Domain.Min, curve.Domain.Max, out var _)
      )
        ThrowArgumentException(nameof(curve), "Curve must be a C1 continuous planar non closed curve.");

      SolveOptionalLevel(doc, curve, ref level, out var _);

      var centerLine = curve.ToCurve();

      if (type.HasValue)
        ChangeElementTypeId(ref beam, type.Value.Id);

      // Try to update Beam
      if (beam is object && beam.Location is DB.LocationCurve locationCurve && centerLine.IsSameKindAs(locationCurve.Curve))
      {
        var referenceLevel = beam.get_Parameter(DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
        var updateLevel = referenceLevel.AsElementId() != level.Value.Id;

        if (!updateLevel || !referenceLevel.IsReadOnly)
        {
          if (updateLevel)
            referenceLevel.Set(level.Value.Id);

          locationCurve.Curve = centerLine;
          return;
        }
      }

      // Reconstruct Beam
      {
        SolveOptionalType(doc, ref type, DB.BuiltInCategory.OST_StructuralFraming, nameof(type));

        var newBeam = doc.Create.NewFamilyInstance
        (
          centerLine,
          type.Value,
          level.Value,
          DB.Structure.StructuralType.Beam
        );

        if (beam is object && DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, 0))
          DB.Structure.StructuralFramingUtils.AllowJoinAtEnd(newBeam, 0);
        else
          DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(newBeam, 0);

        if (beam is object && DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, 1))
          DB.Structure.StructuralFramingUtils.AllowJoinAtEnd(newBeam, 1);
        else
          DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(newBeam, 1);

        newBeam.get_Parameter(DB.BuiltInParameter.Y_JUSTIFICATION)?.Set((int) DB.Structure.YJustification.Origin);
        newBeam.get_Parameter(DB.BuiltInParameter.Z_JUSTIFICATION)?.Set((int) DB.Structure.ZJustification.Origin);

        newBeam.Document.Regenerate();
        newBeam.get_Parameter(DB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Set(0.0);

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
        };

        ReplaceElement(ref beam, newBeam, parametersMask);
      }
    }
  }
}
