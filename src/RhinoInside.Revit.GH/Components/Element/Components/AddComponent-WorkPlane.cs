using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.13")]
  public class AddComponentWorkPlane : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("08586F77-2844-4C0A-925A-200A091CF707");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddComponentWorkPlane() : base
    (
      name: "Add Component (Work Plane)",
      nickname: "WP-Component",
      description: "Given a Work Plane, it adds a work plane-based component to the active Revit document",
      category: "Revit",
      subCategory: "Component"
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
        new Param_Plane()
        {
          Name = "Location",
          NickName = "L",
          Description = "Component location.",
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
        new Parameters.Level()
        {
          Name = "Schedule Level",
          NickName = "SL",
          Description = "Schedule Level.",
        }.SetDefaultVale(new Types.Level()), ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.GeometryObject()
        {
          Name = "Work Plane",
          NickName = "WP",
          Description = $"Work Plane.{OS.NewLine}Face references are also accepted.",
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
        }.SetDefaultVale(0.0), ParamRelevance.Secondary
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
        }, ParamRelevance.Secondary
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
      ARDB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;
      if (!Params.GetData(DA, "Location", out Plane? location, x => x.IsValid)) return;
      if (!Params.GetData(DA, "Type", out Types.FamilySymbol type, x => x.IsValid)) return;
      if (!Parameters.Level.GetDataOrDefault(this, DA, "Schedule Level", out Types.Level level, doc, location.Value.Origin.Z)) return;
      if (!Params.TryGetData(DA, "Work Plane", out Types.GeometryObject workPlane)) return;
      if (!Params.TryGetData(DA, "Offset from Host", out double? offsetFromHost)) return;

      type.AssertPlacementType(ARDB.FamilyPlacementType.WorkPlaneBased);

      var tol = GeometryTolerance.Model;
      var origin = location.Value.Origin.ToXYZ();
      var basisX = (ERDB.UnitXYZ) location.Value.XAxis.ToXYZ();
      var basisY = (ERDB.UnitXYZ) location.Value.YAxis.ToXYZ();

      var reference = default(ARDB.Reference);
      bool associatedWorkPlane = true;

      ReconstructElement<ARDB.SketchPlane>
      (
        doc.Value, _WorkPlane_, sketchPlane =>
        {
          if (workPlane is null)
          {
            if (sketchPlane is null)
              sketchPlane = ARDB.SketchPlane.Create(doc.Value, ARDB.Plane.CreateByOriginAndBasis(origin, basisX, basisY));
            else
              sketchPlane.SetLocation(origin, basisX, basisY);

            reference = ARDB.Reference.ParseFromStableRepresentation(sketchPlane.Document, sketchPlane.UniqueId);

            associatedWorkPlane = false;
            DA.SetData(_WorkPlane_, Types.GraphicalElement.FromElement(sketchPlane));
            return sketchPlane;
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

            return null;
          }
        }
      );

      ReconstructElement<ARDB.FamilyInstance>
      (
        doc.Value, _Component_, component =>
        {
          // Compute
          component = reference is object ? Reconstruct
          (
            component,
            doc.Value,
            origin,
            basisX,
            type.Value,
            level?.Value,
            reference
          ) : default;

          if (component is object)
          {
            DA.SetData(_Component_, component);

            Params.TrySetData(DA, "Face", () => Types.GeometryFace.FromReference(component.Document, component.HostFace) as Types.GeometryFace);

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

          return component;
        }
      );
    }

    bool Reuse
    (
      ARDB.FamilyInstance component,
      ARDB.XYZ origin,
      ERDB.UnitXYZ basisX,
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
            return dependents.Contains(component.Id);
          }

          if (!hostElement.IsEquivalent(component.Host))
            return false;
          break;
      }

      component.SetLocation(origin, basisX);
      return true;
    }

    ARDB.FamilyInstance Create
    (
      ARDB.Document doc,
      ARDB.XYZ point,
      ARDB.XYZ basisX,
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
            case ARDB.SketchPlane sketchPlane:
              return doc.Create().NewFamilyInstance(point, type, basisX, sketchPlane, ARDB.Structure.StructuralType.NonStructural);

            case ARDB.Grid grid:
              if (!grid.IsCurved)
                return doc.Create().NewFamilyInstance(point, type, basisX, grid.GetSketchPlane(ensureSketchPlane: true), ARDB.Structure.StructuralType.NonStructural);
              break;
          }
          
          return doc.Create().NewFamilyInstance(reference, point, basisX, type);

        case ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE:
          return doc.Create().NewFamilyInstance(reference, point, basisX, type);
      }

      throw new Exceptions.RuntimeArgumentException("Host", $"Input Host is not valid for the Work Plane-Based type '{type.Name}'.");
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance component,
      ARDB.Document doc,
      ARDB.XYZ origin,
      ERDB.UnitXYZ basisX,
      ARDB.FamilySymbol type,
      ARDB.Level level,
      ARDB.Reference reference
    )
    {
      if (!Reuse(component, origin, basisX, type, reference))
      {
        component = component.ReplaceElement
        (
          Create(doc, origin, basisX, type, reference),
          ExcludeUniqueProperties
        );
      }

      using (var scheduleLevel = component.get_Parameter(ARDB.BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM))
      {
        if(scheduleLevel?.IsReadOnly is false)
          scheduleLevel.Update(level?.Id ?? ElementIdExtension.Invalid);
      }

      return component;
    }
  }
}
