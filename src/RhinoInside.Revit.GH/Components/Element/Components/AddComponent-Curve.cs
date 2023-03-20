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
  using External.DB.Extensions;
  using GH.Exceptions;

  [ComponentVersion(introduced: "1.13")]
  public class AddComponentCurve : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("5A6D9A20-B05F-4CAF-AB75-500CEE23B7CD");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "C";

    public AddComponentCurve() : base
    (
      name: "Add Component (Curve)",
      nickname: "C-Component",
      description: "Given a Curve, it adds a curve based component to the active Revit document",
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
          Description = "Driving curve.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Component type.",
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_GenericModel
        }
      ),
      new ParamDefinition
      (
        new Parameters.GeometryObject()
        {
          Name = "Work Plane",
          NickName = "WP",
          Description = $"Level or Face reference.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Offset from Host",
          NickName = "O",
          Description = "Signed distance from 'Work Plane'.",
        }.SetDefaultVale(0.0), ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = _Component_,
          NickName = _Component_.Substring(0, 1),
          Description = $"Output {_Component_}",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GeometryFace()
        {
          Name = "Face",
          NickName = "F",
          Description = $"Work Plane face",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _WorkPlane_,
          NickName = "WP",
          Description = $"Work Plane element",
        }
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Offset from Host",
          NickName = "O",
          Description = "Signed distance from 'Work Plane'.",
        }, ParamRelevance.Primary
      ),
    };

    const string _Component_ = "Component";
    const string _WorkPlane_ = "Work Plane";

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;
      if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Type", out Types.FamilySymbol type)) return;
      if (!Params.TryGetData(DA, "Work Plane", out Types.GeometryObject workPlane)) return;
      if (!Params.TryGetData(DA, "Offset from Host", out double? offsetFromHost)) return;

      type.AssertPlacementType(ARDB.FamilyPlacementType.CurveBased);

      var tol = GeometryTolerance.Model;

      if (curve.IsShort(tol.ShortCurveTolerance))
        throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

      if (curve.IsClosed(tol.VertexTolerance))
        throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

      if (!curve.IsLinear(tol.VertexTolerance))
        throw new RuntimeArgumentException("Curve", $"Curve should be a line like curve.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

      var line = ARDB.Line.CreateBound(curve.PointAtStart.ToXYZ(), curve.PointAtEnd.ToXYZ());
      line.TryGetLocation(out var origin, out var basisX, out var basisY);

      var reference = default(ARDB.Reference);
      bool associatedWorkPlane = true;
      var notAssociatedElementId = default(ARDB.ElementId);

      ReconstructElement<ARDB.FamilyInstance>
      (
        doc.Value, _Component_, component =>
        {
          ReconstructElement<ARDB.SketchPlane>
          (
            doc.Value, _WorkPlane_, sketchPlane =>
            {
              if (workPlane is null)
              {
                if (sketchPlane is null || component is null || sketchPlane.GetDependentElements(ERDB.CompoundElementFilter.ExclusionFilter(component.Id, true)).Count == 0)
                {
                  var extents = new Interval(-1.0 * Revit.ModelUnits, +1.0 * Revit.ModelUnits);
                  var surface = new PlaneSurface(new Plane(origin.ToPoint3d(), basisX.Direction.ToVector3d(), basisY.Direction.ToVector3d()), extents, extents);
                  var directShape = ARDB.DirectShape.CreateElement(doc.Value, new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel));
                  directShape.SetShape(surface.ToShape());
                  directShape.Document.Regenerate();

                  using (var geometry = directShape.get_Geometry(new ARDB.Options() { ComputeReferences = true }))
                    reference = geometry.GetFaceReferences(directShape).FirstOrDefault();

                  sketchPlane = ARDB.SketchPlane.Create(doc.Value, reference);
                  notAssociatedElementId = directShape.Id;
                }
                else
                {
                  sketchPlane.SetLocation(origin, basisX, basisY);
                  reference = ARDB.Reference.ParseFromStableRepresentation(sketchPlane.Document, sketchPlane.UniqueId);
                }

                associatedWorkPlane = false;
                DA.SetData(_WorkPlane_, Types.GraphicalElement.FromElement(sketchPlane));
              }
              else
              {
                reference = workPlane.GetReference();

                if
                (
                  !(workPlane is Types.GeometryFace) &&
                  workPlane.CastTo(out Types.GraphicalElement graphicalElement) &&
                  !(graphicalElement is Types.DatumPlane)
                )
                {
                  if (graphicalElement is Types.CurveElement curveElement)
                  {
                    reference = curveElement.SketchPlane.GetReference();
                  }
                  else if (graphicalElement.Value.get_Geometry(new ARDB.Options() { ComputeReferences = true }) is ARDB.GeometryElement geometry)
                  {
                    // Find a Face on 'Work Plane' reference.
                    if
                    (
                      geometry.Project
                      (
                        graphicalElement.Value,
                        workPlane.WorldToGeometryTransform.ToTransform().OfPoint(origin),
                        out var faceTransform,
                        out ARDB.Face face,
                        out reference
                      ) is ARDB.IntersectionResult projected
                    )
                    {
                      var faceNormal = (ERDB.UnitXYZ) faceTransform.OfVector(face.ComputeNormal(projected.UVPoint));
                      if (faceNormal.IsParallelTo(basisX)) basisX = faceNormal.Right();
                      reference = graphicalElement.GetAbsoluteReference(reference);

                      if (offsetFromHost.HasValue) origin = projected.XYZPoint;
                      else
                      {
                        var facePlane = new ERDB.PlaneEquation(projected.XYZPoint, faceNormal);
                        offsetFromHost = facePlane.SignedDistanceTo(origin) * Revit.ModelUnits;
                        origin = facePlane.Project(origin);
                      }
                    }
                  }
                }

                if (reference is null)
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Input reference is not a valid Work Plane. {{{workPlane.Id.ToString("D")}}}");
              }

              if (reference is object)
              {
                if (Types.GeometryFace.FromReference(doc.Value, reference) is Types.GeometryFace face)
                {
                  var brep = face.PolySurface;
                  if
                  (
                    brep is object &&
                    brep.Surfaces.Count == 1 &&
                    brep.Surfaces[0].TryGetPlane(out var facePlane/*, tol.VertexTolerance*/)
                  )
                  {
                    var start = facePlane.ClosestPoint(line.GetEndPoint(0).ToPoint3d()).ToXYZ();
                    var end = facePlane.ClosestPoint(line.GetEndPoint(1).ToPoint3d()).ToXYZ();
                    line = ARDB.Line.CreateBound(start, end);
                  }
                  else throw new RuntimeArgumentException("Work Plane", $"'Work Plane' face should be planar.\nTolerance is {/*tol.VertexTolerance*/0.0} {GH_Format.RhinoUnitSymbol()}", brep);
                }

                // Compute
                component = Reconstruct
                (
                  component,
                  doc.Value,
                  line,
                  type.Value,
                  reference
                );
              }
              else component = null;

              if (component is object)
              {
                DA.SetData(_Component_, component);

                Params.TrySetData(DA, "Face", () => component.HostFace is object ? Types.GeometryFace.FromReference(component.Document, component.HostFace) as Types.GeometryFace:default);

                if (associatedWorkPlane) Params.TrySetData
                (
                  DA, "Work Plane", () => component.HostFace is ARDB.Reference referemce ?
                    Types.GraphicalElement.FromReference(component.Document, referemce) :
                    Types.GraphicalElement.FromElement(component.Host)
                );

                if (offsetFromHost.HasValue)
                  component.get_Parameter(ARDB.BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.Set(offsetFromHost.Value / Revit.ModelUnits);

                Params.TrySetData(DA, "Offset from Host", () => component.get_Parameter(ARDB.BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsGoo());
              }

              return sketchPlane;
            }
          );

          return component;
        }
      );

      if (notAssociatedElementId.IsValid())
        doc.Value.Delete(notAssociatedElementId);
    }

    bool Reuse
    (
      ARDB.FamilyInstance component,
      ARDB.Line line,
      ARDB.FamilySymbol type,
      ARDB.Reference reference
    )
    {
      if (component is null) return false;
      if (type.Id != component.GetTypeId()) component.ChangeTypeId(type.Id);

      switch (reference.ElementReferenceType)
      {
        case ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE:
          if (!component.Document.AreEquivalentReferences(reference, component.HostFace)) return false;
          break;

        case ARDB.ElementReferenceType.REFERENCE_TYPE_NONE:
          var hostElement = component.Document.GetElement(reference);
          if (hostElement is ARDB.SketchPlane sketchPlane)
          {
            var dependents = sketchPlane.GetDependentElements(ERDB.CompoundElementFilter.ExclusionFilter(component.Id, inverted: true));
            if (!dependents.Contains(component.Id)) return false;
          }
          else if (!hostElement.IsEquivalent(component.Host)) return false;
          break;
      }

      if (component.Location is ARDB.LocationCurve locationCurve)
      {
        if (!locationCurve.Curve.AlmostEquals(line, GeometryTolerance.Internal.VertexTolerance))
        {
          if (component.Host is ARDB.Level level)
          {
            var plane = new ERDB.PlaneEquation(ERDB.UnitXYZ.BasisZ, -level.ProjectElevation);
            line = ARDB.Line.CreateBound(plane.Project(line.Origin), plane.Project(line.Origin + line.Direction * line.Length));
          }
          else
          {
            line.TryGetLocation(out var origin, out var basisX, out var basisY);
            component.SetLocation(origin, basisX);
          }

          locationCurve.Curve = line;
        }
      }

      return true;
    }

    ARDB.FamilyInstance Create
    (
      ARDB.Document doc,
      ARDB.Line line,
      ARDB.FamilySymbol type,
      ARDB.Reference reference
    )
    {
      if (!type.IsActive) type.Activate();
      switch (reference.ElementReferenceType)
      {
        case ARDB.ElementReferenceType.REFERENCE_TYPE_NONE:
          switch (doc.GetElement(reference))
          {
            case ARDB.Level level:
            {
              var list = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>(1)
              {
                new Autodesk.Revit.Creation.FamilyInstanceCreationData(line, type, level, ARDB.Structure.StructuralType.NonStructural)
              };

              var ids = doc.Create().NewFamilyInstances2(list);
              return doc.GetElement(ids.First()) as ARDB.FamilyInstance;
            }
          }

          return doc.Create().NewFamilyInstance(reference, line, type);

        case ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE:
          return doc.Create().NewFamilyInstance(reference, line, type);
      }

      throw new Exceptions.RuntimeArgumentException("Host", $"Input Host is not valid for the Curve Based type '{type.Name}'.");
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance component,
      ARDB.Document doc,
      ARDB.Line line,
      ARDB.FamilySymbol type,
      ARDB.Reference reference
    )
    {
      if (!Reuse(component, line, type, reference))
      {
        component = component.ReplaceElement
        (
          Create(doc, line, type, reference),
          ExcludeUniqueProperties
        );
      }

      return component;
    }
  }
}
