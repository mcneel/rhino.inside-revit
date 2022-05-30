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
  public class AddSpotElevation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("00c729f1-75be-4b13-8ab5-aefa4462f335");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddSpotElevation() : base
    (
      name: "Add Spot Elevation",
      nickname: "SpotEle",
      description: "Given a point, it adds a spot elevation to the given View",
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
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to place a specific spot elevation",
        }
      ),
      new ParamDefinition
      (
        new Param_Line
        {
          Name = "Leader",
          NickName = "L",
          Description = "Line to place the leader's spot elevation",
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
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Spot Elevation";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.SpotDimension>
      (
        view.Document, _Output_, spot =>
        {
          // Input
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.GetData(DA, "Leader", out Line? line)) return null;
          if (!Params.GetData(DA, "Reference", out ARDB.Element element)) return null;

          if
          (
            view.ViewType is ARDB.ViewType.Schedule ||
            view.ViewType is ARDB.ViewType.ColumnSchedule ||
            view.ViewType is ARDB.ViewType.PanelSchedule
          )
            throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);

          // Compute
          spot = Reconstruct(spot, view, point.Value.ToXYZ(), line.Value.ToLine(), element);

          DA.SetData(_Output_, spot);
          return spot;
        }
      );
    }

    static bool Contains(ARDB.ReferenceArray references, ARDB.ElementId value)
    {
      foreach (var reference in references.Cast<ARDB.Reference>())
        if (reference.ElementId == value)
          return true;

      return false;
    }

    bool Reuse(ARDB.SpotDimension spot, ARDB.View view, ARDB.XYZ point, ARDB.Line leader, ARDB.Element element)
    {
      if (spot is null) return false;
      if (spot.OwnerViewId != view.Id) return false;

      // Point
      if (!spot.Origin.AlmostEquals(point, GeometryObjectTolerance.Internal.VertexTolerance)) return false;

      // Leader
      if (!leader.GetEndPoint(0).AlmostEquals(spot.LeaderEndPosition, GeometryObjectTolerance.Internal.VertexTolerance) &&
          !leader.GetEndPoint(1).AlmostEquals(spot.LeaderEndPosition, GeometryObjectTolerance.Internal.VertexTolerance))
      {
        return false;
      }
#if REVIT_2021
      if (!leader.GetEndPoint(0).AlmostEquals(spot.LeaderShoulderPosition, GeometryObjectTolerance.Internal.VertexTolerance) &&
          !leader.GetEndPoint(1).AlmostEquals(spot.LeaderShoulderPosition, GeometryObjectTolerance.Internal.VertexTolerance))
      {
        return false;
      }
#endif
      // Elements
      if (!Contains(spot.References, element.Id)) return false;

      return true;
    }

    ARDB.SpotDimension Create(ARDB.View view, ARDB.XYZ point, ARDB.Line leader, ARDB.Element element)
    {
      var reference = GetReferences(new List<ARDB.Element> { element }, point);
      if (point.DistanceTo(leader.GetEndPoint(0)) < point.DistanceTo(leader.GetEndPoint(1)))
        return view.Document.Create.NewSpotElevation(view, reference[0], point, leader.GetEndPoint(0), leader.GetEndPoint(1), point, true);
      else
        return view.Document.Create.NewSpotElevation(view, reference[0], point, leader.GetEndPoint(1), leader.GetEndPoint(0), point, true);
    }

    static IList<ARDB.Reference> GetReferences(IList<ARDB.Element> elements, ARDB.XYZ point)
    {
      var referenceArray = new List<ARDB.Reference>(elements.Count);
      foreach (var element in elements)
      {
        var reference = default(ARDB.Reference);
        switch (element)
        {
          case null: break;
          case ARDB.FamilyInstance instance:
            reference = instance.GetReferences(ARDB.FamilyInstanceReferenceType.CenterLeftRight).FirstOrDefault();
            break;

          case ARDB.ModelLine modelLine:
            reference = modelLine.GeometryCurve.Reference;
            break;

          default:
            using (var options = new ARDB.Options() { ComputeReferences = true, IncludeNonVisibleObjects = true })
            {
              var geometry = element.get_Geometry(options);

              var edges = geometry.OfType<ARDB.Solid>().
                SelectMany(y => y.Edges.Cast<ARDB.Edge>()).
                ToList();

              var edge = default(ARDB.Edge);
              var distance = Double.MaxValue;
              foreach (var e in edges)
              {
                e.AsCurve().ToCurve().ClosestPoint(point.ToPoint3d(), out double t);
                var d = point.ToPoint3d().DistanceTo(e.AsCurve().ToCurve().PointAt(t));
                if (d < distance)
                {
                  edge = e;
                  distance = d;
                }
              }

              reference = edge.Reference;

            }
            break;
        }

        if (reference is object)
          referenceArray.Add(reference);
      }
      return referenceArray;

    }

    ARDB.SpotDimension Reconstruct(ARDB.SpotDimension spot, ARDB.View view, ARDB.XYZ point, ARDB.Line leader, ARDB.Element element)
    {
      if (!Reuse(spot, view, point, leader, element))
        spot = Create(view, point, leader, element);

      return spot;
    }
  }
}


