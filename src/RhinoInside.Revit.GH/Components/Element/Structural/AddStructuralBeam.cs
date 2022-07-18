using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using Exceptions;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.6")]
  public class AddStructuralBeam : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("26411AA6-8187-49DF-A908-A292A07918F1");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddStructuralBeam() : base
    (
      name: "Add Structural Beam",
      nickname: "S-Beam",
      description: "Given its Axis, it adds a beam element to the active Revit document",
      category: "Revit",
      subCategory: "Build"
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
          Description = "Structural Framing axis curve.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Structural Framing type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_StructuralFraming
        }
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Reference Level",
          NickName = "RL",
          Description = "Reference level.",
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = _Beam_,
          NickName = _Beam_.Substring(0, 1),
          Description = $"Output {_Beam_}",
        }
      )
    };

    const string _Beam_ = "Beam";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
      ARDB.BuiltInParameter.Y_JUSTIFICATION,
      ARDB.BuiltInParameter.Z_JUSTIFICATION,
      ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.FamilyInstance>
      (
        doc.Value, _Beam_, beam =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;

          if (curve.IsShort(tol.ShortCurveTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (curve.IsClosed(tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (!curve.TryGetPlane(out var plane, tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be planar and parallel to view plane.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (curve.GetNextDiscontinuity(Continuity.C1_continuous, curve.Domain.Min, curve.Domain.Max, Math.Cos(tol.AngleTolerance), Rhino.RhinoMath.SqrtEpsilon, out var _))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be C1 continuous.\nTolerance is {Rhino.RhinoMath.ToDegrees(tol.AngleTolerance):N1}°", curve);

          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out Types.FamilySymbol type, doc, ARDB.BuiltInCategory.OST_StructuralFraming)) return null;

          var bbox = curve.GetBoundingBox(accurate: true);
          if (!Parameters.Level.GetDataOrDefault(this, DA, "Reference Level", out Types.Level level, doc, bbox.Center.Z)) return null;

          // Compute
          beam = Reconstruct(beam, doc.Value, curve.ToCurve(), plane.Normal.ToXYZ(), type.Value, level.Value);

          DA.SetData(_Beam_, beam);
          return beam;
        }
      );
    }

    bool Reuse
    (
      ARDB.FamilyInstance beam,
      ARDB.XYZ normal,
      ARDB.FamilySymbol type
    )
    {
      if (beam is null) return false;
      if (type.Id != beam.GetTypeId()) beam.ChangeTypeId(type.Id);

      beam.GetLocation(out var _, out var basisX, out var basisY);
      var currentNormal = basisX.CrossProduct(basisY);

      var wasVertical = currentNormal.IsPerpendicularTo(ARDB.XYZ.BasisZ);
      var isVertical = normal.IsPerpendicularTo(ARDB.XYZ.BasisZ);
      return isVertical == wasVertical;
    }

    ARDB.FamilyInstance Create(ARDB.Document doc, ARDB.Curve curve, ARDB.FamilySymbol type)
    {
      var list = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>(1)
      {
        new Autodesk.Revit.Creation.FamilyInstanceCreationData
        (
          curve: curve,
          symbol: type,
          level: default, // No work-plane based.
          structuralType: ARDB.Structure.StructuralType.Beam
        )
      };

      var ids = doc.IsFamilyDocument ?
        doc.FamilyCreate.NewFamilyInstances2(list) :
        doc.Create.NewFamilyInstances2(list);

      var instance = doc.GetElement(ids.First()) as ARDB.FamilyInstance;

      // We turn analytical model off by default
      instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
      return instance;
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance beam,
      ARDB.Document doc,
      ARDB.Curve curve,
      ARDB.XYZ normal,
      ARDB.FamilySymbol type,
      ARDB.Level level
    )
    {
      if (!Reuse(beam, normal, type))
      {
        beam = beam.ReplaceElement
        (
          Create(doc, curve, type),
          ExcludeUniqueProperties
        );

        beam.Document.Regenerate();
      }

      if (level is object)
      {
        using (var referenceLevel = beam.get_Parameter(ARDB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM))
        {
          if (!referenceLevel.IsReadOnly) referenceLevel.Update(level.Id);
        }
      }

      if (ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, ERDB.CurveEnd.Start))
        ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(beam, ERDB.CurveEnd.Start);
      if (ARDB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(beam, ERDB.CurveEnd.End))
        ARDB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(beam, ERDB.CurveEnd.End);

      if (beam.ExtensionUtility is ARDB.IExtension extension)
      {
        if (extension.get_IsMiterLocked(ERDB.CurveEnd.Start))
          extension.set_IsMiterLocked(ERDB.CurveEnd.Start, false);
        if (extension.get_IsMiterLocked(ERDB.CurveEnd.End))
          extension.set_IsMiterLocked(ERDB.CurveEnd.End, false);

        if (extension.get_SymbolicExtended(ERDB.CurveEnd.Start))
          extension.set_SymbolicExtended(ERDB.CurveEnd.Start, false);
        if (extension.get_SymbolicExtended(ERDB.CurveEnd.End))
          extension.set_SymbolicExtended(ERDB.CurveEnd.End, false);

        if (extension.get_Extended(ERDB.CurveEnd.Start))
          extension.set_Extended(ERDB.CurveEnd.Start, false);
        if (extension.get_Extended(ERDB.CurveEnd.End))
          extension.set_Extended(ERDB.CurveEnd.End, false);
      }

      beam.get_Parameter(ARDB.BuiltInParameter.Y_JUSTIFICATION)?.Update(ARDB.Structure.YJustification.Origin);
      beam.get_Parameter(ARDB.BuiltInParameter.Z_JUSTIFICATION)?.Update(ARDB.Structure.ZJustification.Origin);
      beam.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(0.0);

      if (beam.Location is ARDB.LocationCurve locationCurve)
      {
        if (!locationCurve.Curve.AlmostEquals(curve, GeometryTolerance.Internal.VertexTolerance))
        {
          curve.TryGetLocation(out var origin, out var basisX, out var basisY);

          beam.Pinned = false;
          beam.SetLocation(origin, basisX, basisY);

          locationCurve.Curve = curve;
        }
      }

      return beam;
    }
  }
}
