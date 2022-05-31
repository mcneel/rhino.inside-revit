using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotation
{
  [ComponentVersion(introduced: "1.8")]
  public class AddSpotCoordinate : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("449b853b-423a-4007-ab6b-6f8e417a1175");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddSpotCoordinate() : base
    (
      name: "Add Spot Coordinate",
      nickname: "SpotCoor",
      description: "Given a point, it adds a spot coordinate to the given View",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific dimension"
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Reference",
          NickName = "R",
          Description = "Reference to create a specific dimension",
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to place a specific spot coordinate",
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Head Location",
          NickName = "HL",
          Description = "Location to place the leader's spot text",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element type of the given dimension",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_SpotCoordinates
        }, ParamRelevance.Secondary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Dimension()
        {
          Name = _Spot_,
          NickName = _Spot_.Substring(0, 1),
          Description = $"Output {_Spot_}"
        }
      )
    };

    const string _Spot_ = "Spot Coordinate";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.SpotDimension>
      (
        view.Document, _Spot_, spot =>
        {
          // Input
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.GetData(DA, "Reference", out ARDB.Element element)) return null;
          if (!Params.TryGetData(DA, "Head Location", out Point3d? headLocation)) return null;
          if (headLocation is null) headLocation = point;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.SpotDimensionType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.SpotCoordinateType)) return null;

          if
          (
            view.ViewType is ARDB.ViewType.Schedule ||
            view.ViewType is ARDB.ViewType.ColumnSchedule ||
            view.ViewType is ARDB.ViewType.PanelSchedule
          )
            throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);

          // Compute
          spot = Reconstruct
          (
            spot, view, element,
            point.Value.ToXYZ(),
            (headLocation.Value - Vector3d.XAxis * ((headLocation.Value.X - point.Value.X) / 3.0)).ToXYZ(),
            headLocation.Value.ToXYZ(),
            type
          );

          DA.SetData(_Spot_, spot);
          return spot;
        }
      );
    }

    bool Reuse
    (
      ARDB.SpotDimension spot, ARDB.View view,
      ARDB.Element element,
      ARDB.XYZ point, ARDB.XYZ bend, ARDB.XYZ end
    )
    {
      if (spot is null) return false;
      if (spot.OwnerViewId != view.Id) return false;

      // Reference
      if (spot.References.Size != 1) return false;

      var reference = GetReference(element, point, out var origin);
      if (reference is null) return false;

      var prevReference = spot.References.get_Item(0);

      if (prevReference.ElementReferenceType != reference.ElementReferenceType) return false;
      if (prevReference.ElementId != reference.ElementId) return false;
      if (prevReference.LinkedElementId != reference.LinkedElementId) return false;

      // Origin
      var vertexTolerance = spot.Document.Application.VertexTolerance;
      if (!spot.Origin.AlmostEquals(origin, vertexTolerance)) return false;

      // Leader
      if (point.AlmostEquals(end, vertexTolerance))
      {
        spot.get_Parameter(ARDB.BuiltInParameter.SPOT_DIM_LEADER)?.Update(false);
      }
      else
      {
#if REVIT_2021
        spot.get_Parameter(ARDB.BuiltInParameter.SPOT_DIM_LEADER)?.Update(true);

        if (spot.LeaderHasShoulder)
        {
          if (!bend.AlmostEquals(spot.LeaderShoulderPosition, vertexTolerance))
            spot.LeaderShoulderPosition = bend;
        }

        if (!end.AlmostEquals(spot.LeaderEndPosition, vertexTolerance))
          spot.LeaderEndPosition = end;
#else
        return false;
#endif
      }

      return true;
    }

    ARDB.SpotDimension Create
    (
      ARDB.View view,
      ARDB.Element element,
      ARDB.XYZ point, ARDB.XYZ bend, ARDB.XYZ end
    )
    {
      var reference = GetReference(element, point, out var origin);
      if (reference is null) return null;

      var hasLeader = !point.AlmostEquals(end, view.Document.Application.VertexTolerance);
      return view.Document.Create.NewSpotCoordinate(view, reference, origin, bend, end, point, hasLeader);
    }

    static ARDB.Reference GetReference(ARDB.Element element, ARDB.XYZ point, out ARDB.XYZ origin)
    {
      origin = default;
      var reference = default(ARDB.Reference);
      switch (element)
      {
        case null: break;
#if REVIT_2018
        case ARDB.FamilyInstance instance:
          reference = instance.GetReferences(ARDB.FamilyInstanceReferenceType.CenterLeftRight).FirstOrDefault();
          break;
#endif

        case ARDB.ModelLine modelLine:
          reference = modelLine.GeometryCurve.Reference;
          break;

        default:
          using (var options = new ARDB.Options() { ComputeReferences = true, IncludeNonVisibleObjects = true })
          {
            var geometry = element.get_Geometry(options);

            var edges = geometry.OfType<ARDB.Solid>().
              SelectMany(y => y.Edges.Cast<ARDB.Edge>());

            var closestEdge = default(ARDB.Edge);
            var minDistance = double.PositiveInfinity;
            foreach (var edge in edges.Where(x => x.Reference is object))
            {
              var projected = edge.AsCurve().Project(point);
              var distance = projected.XYZPoint.DistanceTo(point);
              if (distance < minDistance)
              {
                origin = projected.XYZPoint;
                closestEdge = edge;
                minDistance = distance;
              }
            }

            reference = closestEdge.Reference;
          }
          break;
      }

      return reference;
    }

    ARDB.SpotDimension Reconstruct
    (
      ARDB.SpotDimension spot,
      ARDB.View view,
      ARDB.Element element,
      ARDB.XYZ point, ARDB.XYZ bend, ARDB.XYZ end,
      ARDB.SpotDimensionType type
    )
    {
      if (!Reuse(spot, view, element, point, bend, end))
        spot = Create(view, element, point, bend, end);

      if (spot?.GetTypeId() != type.Id) spot.ChangeTypeId(type.Id);

      return spot;
    }
  }
}
