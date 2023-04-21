using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.8", updated: "1.10")]
  public class AddSpotElevation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("00c729f1-75be-4b13-8ab5-aefa4462f335");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddSpotElevation() : base
    (
      name: "Add Spot Elevation",
      nickname: "E-Spot",
      description: "Given a point, it adds a spot elevation to the given View",
      category: "Revit",
      subCategory: "Annotate"
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
        new Parameters.GeometryObject()
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
          Description = "Point to place a specific spot elevation",
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
        new Parameters.DimensionType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element type of the given dimension",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_SpotElevations
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

    const string _Spot_ = "Spot Elevation";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.SpotDimension>
      (
        view.Document, _Spot_, spot =>
        {
          // Input
          if (!view.IsGraphicalView()) throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.GetData(DA, "Reference", out Types.GeometryObject geometry)) return null;
          if (!Params.TryGetData(DA, "Head Location", out Point3d? headLocation)) return null;
          if (headLocation is null) headLocation = point;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.SpotDimensionType type, Types.Document.FromValue(view.Document), ARDB.ElementTypeGroup.SpotElevationType)) return null;

          var bbox = geometry.GetBoundingBox(Transform.Identity);
          if (!bbox.Contains(point.Value))
            point = bbox.ClosestPoint(point.Value);

          // Compute
          spot = Reconstruct
          (
            spot, view, geometry.GetDefaultReference(),
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
      ARDB.Reference reference,
      ARDB.XYZ point, ARDB.XYZ bend, ARDB.XYZ end
    )
    {
      if (spot is null) return false;
      if (spot.OwnerViewId != view.Id) return false;

      // Reference
      if (spot.References.Size != 1) return false;

      if (reference is null) return false;

      var prevReference = spot.References.get_Item(0);
      if (!spot.Document.AreEquivalentReferences(prevReference, reference)) return false;

      // Origin
      if (!spot.Origin.AlmostEqualPoints(point))
      {
        spot.Pinned = false;
        spot.Location.Move(point - spot.Origin);
      }

      // Leader
      if (point.AlmostEqualPoints(end, view.Document.Application.ShortCurveTolerance))
      {
        spot.get_Parameter(ARDB.BuiltInParameter.SPOT_DIM_LEADER)?.Update(false);
      }
      else
      {
#if REVIT_2021
        spot.get_Parameter(ARDB.BuiltInParameter.SPOT_DIM_LEADER)?.Update(true);

        if (spot.LeaderHasShoulder)
        {
          if (!bend.AlmostEqualPoints(spot.LeaderShoulderPosition))
            spot.LeaderShoulderPosition = bend;
        }

        if (!end.AlmostEqualPoints(spot.LeaderEndPosition))
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
      ARDB.Reference reference,
      ARDB.XYZ point, ARDB.XYZ bend, ARDB.XYZ end
    )
    {
      if (reference is null) return null;

      var hasLeader = !point.AlmostEqualPoints(end, view.Document.Application.ShortCurveTolerance);

      if (reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_NONE)
      {
        var host = view.Document.GetElement(reference);
        if (host is ARDB.Architecture.TopographySurface topography)
        {
          var extents = new Interval(-1.0 * Revit.ModelUnits, +1.0 * Revit.ModelUnits);
          var surface = new PlaneSurface(Plane.WorldXY, extents, extents);
          var directShape = ARDB.DirectShape.CreateElement(view.Document, new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel));
          directShape.SetShape(surface.ToShape());
          directShape.Document.Regenerate();

          using (var geometry = directShape.get_Geometry(new ARDB.Options() { ComputeReferences = true }))
          {
            var faceReference = geometry.GetFaceReferences(directShape).FirstOrDefault();
            var templateSpot = view.Document.Create.NewSpotElevation(view, faceReference, XYZExtension.Zero, bend, end, point, hasLeader);

            var id = ARDB.ElementTransformUtils.CopyElement(templateSpot.Document, templateSpot.Id, point);
            templateSpot.Document.Delete(templateSpot.Id);
            directShape.Document.Delete(directShape.Id);
            return view.Document.GetElement(id.FirstOrDefault() ?? ElementIdExtension.Invalid) as ARDB.SpotDimension;
          }
        }
      }

      return view.Document.Create.NewSpotElevation(view, reference, point, bend, end, point, hasLeader);
    }

    ARDB.SpotDimension Reconstruct
    (
      ARDB.SpotDimension spot,
      ARDB.View view,
      ARDB.Reference reference,
      ARDB.XYZ point, ARDB.XYZ bend, ARDB.XYZ end,
      ARDB.SpotDimensionType type
    )
    {
      if (!Reuse(spot, view, reference, point, bend, end))
        spot = Create(view, reference, point, bend, end);

      if (spot?.GetTypeId() != type.Id) spot.ChangeTypeId(type.Id);

      return spot;
    }
  }
}


