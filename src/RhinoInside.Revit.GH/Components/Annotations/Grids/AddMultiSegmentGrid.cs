using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Annotations.Grids
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using GH.Exceptions;

  [ComponentVersion(introduced: "1.21")]
  public class AddMultiSegmentGrid : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("03EB988D-9AA9-49E8-BD38-4ED5172E6340");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    public AddMultiSegmentGrid() : base
    (
      name: "Add Multi-Grid",
      nickname: "M-Grid",
      description: "Given its Axis, it adds a Multi-Segment Grid element to the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.SketchPlane()
        {
          Name = "Work Plane",
          NickName = "WP",
          Description = "Work Plane element",
        }
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Curve",
          NickName = "C",
          Description = "Grid curve",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ProjectElevation
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the grid.{OS.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Curve'.",
          Optional = true,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.ProjectElevation
        {
          Name = "Top",
          NickName = "TO",
          Description = $"Top of the grid.{OS.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Curve'",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Grid Name",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Grid Type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Grids
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template grid",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Grid_,
          NickName = _Grid_.Substring(0, 1),
          Description = $"Output {_Grid_}",
        }
      ),
    };

    const string _Grid_ = "Grid";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.DATUM_TEXT
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Work Plane", out Types.SketchPlane sketchPlane)) return;

      ReconstructElement<ARDB.MultiSegmentGrid>
      (
        sketchPlane.Document, _Grid_, grid =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation)) return null;
          if (!Params.TryGetData(DA, "Top", out ERDB.ElevationElementReference? topElevation)) return null;
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.GridType type, Types.Document.FromValue(sketchPlane.Document), ARDB.ElementTypeGroup.GridType)) return null;
          Params.TryGetData(DA, "Template", out ARDB.MultiSegmentGrid template);

          var extents = new Interval();
          // Validation & Defaults
          {
            var tol = GeometryTolerance.Model;
            var axisPlane = sketchPlane.Location;

            curve = Curve.ProjectToPlane(curve, axisPlane);
            if (curve is null) throw new RuntimeArgumentException("Curve", "Failed to project Curve on to Work Plane.", curve);

            curve = curve.ToArcsAndLines(tol.VertexTolerance, 10.0 * tol.AngleTolerance, tol.ShortCurveTolerance, 0.0);
            if (curve is null) throw new RuntimeArgumentException("Curve", "Failed to convert Curve on to a series of arcs and lines.", curve);

            extents = new Interval
            (
              GeometryEncoder.ToInternalLength(axisPlane.OriginZ),
              GeometryEncoder.ToInternalLength(axisPlane.OriginZ)
            );

            if (!baseElevation.HasValue) extents.T0 -= 12.0;
            else if (baseElevation.Value.IsOffset(out var baseOffset)) extents.T0 += baseOffset;
            else if (baseElevation.Value.IsElevation(out var elevation)) extents.T0 = elevation;
            else if (baseElevation.Value.IsUnlimited()) extents.T0 = double.NegativeInfinity;

            if (!topElevation.HasValue) extents.T1 += 12.0;
            else if (topElevation.Value.IsOffset(out var topOffset)) extents.T1 += topOffset;
            else if (topElevation.Value.IsElevation(out var elevation)) extents.T1 = elevation;
            else if (topElevation.Value.IsUnlimited()) extents.T1 = double.PositiveInfinity;
          }

          // Compute
          if (CanReconstruct(_Grid_, out var untracked, ref grid, sketchPlane.Document, name, categoryId: ARDB.BuiltInCategory.OST_Grids))
            grid = Reconstruct(grid, sketchPlane.Value, curve, extents, type, name, template);

          DA.SetData(_Grid_, grid);
          return untracked ? null : grid;
        }
      );
    }

    bool Reuse(ARDB.MultiSegmentGrid grid, Curve curve, ARDB.GridType type, ARDB.MultiSegmentGrid template)
    {
      if (grid is null) return false;

      curve.CombineShortSegments(GeometryDecoder.Tolerance.ShortCurveTolerance);
      if (!(grid.GetSketch() is ARDB.Sketch sketch && Types.Sketch.SetProfile(sketch, new Curve[] { curve }, Vector3d.ZAxis)))
        return false;

      if (type is object && grid.GetTypeId() != type.Id) grid.ChangeTypeId(type.Id);
      grid.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.MultiSegmentGrid Create(ARDB.SketchPlane sketchPlane, Curve curve, ARDB.GridType type, ARDB.MultiSegmentGrid template)
    {
      using (var loop = curve.ToBoundedCurveLoop())
      {
        if (!ARDB.MultiSegmentGrid.IsValidCurveLoop(loop))
          throw new RuntimeArgumentException("Curve", "The Curve should be an open loop consisting of lines and arcs.", curve);

        var grid = sketchPlane.Document.GetElement(ARDB.MultiSegmentGrid.Create(sketchPlane.Document, type.Id, loop, sketchPlane.Id)) as ARDB.MultiSegmentGrid;
        grid.CopyParametersFrom(template, ExcludeUniqueProperties);

        return grid;
      }
    }

    ARDB.MultiSegmentGrid Reconstruct(ARDB.MultiSegmentGrid grid, ARDB.SketchPlane sketchPlane, Curve curve, Interval extents, ARDB.GridType type, string name, ARDB.MultiSegmentGrid template)
    {
      if (!Reuse(grid, curve, type, template))
      {
        var previousGrid = grid;
        grid = grid.ReplaceElement
        (
          Create(sketchPlane, curve, type, template),
          ExcludeUniqueProperties
        );

        // Avoids conflict in case we are going to assign same name...
        if (previousGrid.IsValid())
        {
          if (name is null) name = previousGrid.Name;
          previousGrid.Document.Delete(previousGrid.Id);
        }
      }

      if (name is object && grid.Name != name)
        grid.Name = name;

      var tol = GeometryTolerance.Internal;
      foreach (var child in grid.GetGridIds().Select(x => grid.Document.GetElement(x) as ARDB.Grid))
      {
        using (var outline = child.GetExtents())
        {
          if (!tol.DefaultTolerance.Equals(extents.T0, outline.MinimumPoint.Z) || !tol.DefaultTolerance.Equals(extents.T1, outline.MaximumPoint.Z))
          {
            child.SetVerticalExtents
            (
              double.IsInfinity(extents.T0) ? outline.MinimumPoint.Z : extents.T0,
              double.IsInfinity(extents.T1) ? outline.MaximumPoint.Z : extents.T1
            );
          }
        }
      }

      return grid;
    }
  }
}
