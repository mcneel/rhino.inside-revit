using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Grids
{
  using System.Linq;
  using Convert.Geometry;
  using Grasshopper.Kernel.Parameters;
  using External.DB.Extensions;
  using GH.Exceptions;

  [ComponentVersion(introduced: "1.0", updated: "1.6")]
  public class GridByCurve : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("CEC2B3DF-C6BA-414F-BECE-E3DAEE2A3F2C");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public GridByCurve() : base
    (
      name: "Add Grid",
      nickname: "Grid",
      description: "Given its Axis, it adds a Grid element to the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
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
        new Parameters.ElevationInterval
        {
          Name = "Elevation",
          NickName= "E",
          Description = "Grid extents interval along z-axis",
          Optional = true
        }, ParamRelevance.Secondary
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
        new Parameters.Grid()
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
        new Parameters.Grid()
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
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Grid>
      (
        doc.Value, _Grid_, (grid) =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!Parameters.ElevationInterval.TryGetData(this, DA, "Elevation", out var elevation, doc)) return null;

          var tol = GeometryObjectTolerance.Model;
          if
          (
            !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance)) ||
            !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
            axisPlane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
          )
            throw new RuntimeArgumentException("Curve", "Curve must be a horizontal line or arc curve.", curve);

          if (!elevation.HasValue)
            elevation = new Interval(axisPlane.Origin.Z - 12.0 * Revit.ModelUnits, axisPlane.Origin.Z + 12.0 * Revit.ModelUnits);

          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.GridType type, doc, ARDB.ElementTypeGroup.GridType)) return null;
          Params.TryGetData(DA, "Template", out ARDB.Grid template);

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_Grid_, out var untracked, ref grid, doc.Value, name, categoryId: ARDB.BuiltInCategory.OST_Grids))
            grid = Reconstruct(grid, doc.Value, curve, elevation.Value, type, name, template);

          DA.SetData(_Grid_, grid);
          return untracked ? null : grid;
        }
      );
    }

    bool Reuse(ARDB.Grid grid, Curve curve, ARDB.GridType type, ARDB.Grid template)
    {
      if (grid is null) return false;

      var tol = GeometryObjectTolerance.Internal;
      var gridCurve = grid.Curve;
      var newCurve = grid.IsCurved ? curve.ToCurve().CreateReversed() : curve.ToCurve();

      if (!gridCurve.IsAlmostEqualTo(newCurve, tol.VertexTolerance))
      {
        if (!gridCurve.IsSameKindAs(newCurve)) return false;
        if (gridCurve is ARDB.Arc gridArc && newCurve is ARDB.Arc newArc)
        {
          // I do not found any way to update the radius ??
          if (Math.Abs(gridArc.Radius - newArc.Radius) > tol.VertexTolerance)
            return false;
        }

        // Makes Grid cross maximum number of views.
        // This increases our chances of obtaining a valid view for this Grid.
        grid.Maximize3DExtents();

        var view = default(ARDB.View);
        using (var collector = new ARDB.FilteredElementCollector(grid.Document).OfClass(typeof(ARDB.View)))
        {
          var views = collector.Cast<ARDB.View>().
            Where(x => !x.IsTemplate && !x.IsAssemblyView).
            Where
            (
              x =>
              {
                switch (x)
                {
                  case ARDB.View3D view3D: return true;
                  case ARDB.ViewPlan viewPlan: return viewPlan.GetUnderlayOrientation() == ARDB.UnderlayOrientation.LookingDown;
                }

                return false;
              }
            ).
            OrderByDescending(x => x.ViewType);

          view = views.FirstOrDefault();
        }

        if (view is null) return false;

        var curves = grid.GetCurvesInView(ARDB.DatumExtentType.Model, view);

        curves[0].TryGetLocation(out var origin0, out var basisX0, out var basisY0);
        newCurve.TryGetLocation(out var origin, out var _, out var _);

        // Move newCurve to same plane as current curve
        var elevationDelta = origin0.Z - origin.Z;
        newCurve = newCurve.CreateTransformed(ARDB.Transform.CreateTranslation(ARDB.XYZ.BasisZ * elevationDelta));
        newCurve.TryGetLocation(out var origin1, out var basisX1, out var basisY1);

        var pinned = grid.Pinned;
        grid.Pinned = false;

        grid.Location.Move(origin1 - origin0);
        using (var axis = ARDB.Line.CreateUnbound(origin1, ARDB.XYZ.BasisZ))
          grid.Location.Rotate(axis, basisX0.AngleOnPlaneTo(basisX1, ARDB.XYZ.BasisZ));

        grid.SetCurveInView(ARDB.DatumExtentType.Model, view, newCurve);
        grid.Pinned = pinned;
      }

      if (type is object && grid.GetTypeId() != type.Id) grid.ChangeTypeId(type.Id);
      grid.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.Grid Create(ARDB.Document doc, Curve curve, ARDB.GridType type, ARDB.Grid template)
    {
      var grid = default(ARDB.Grid);
      {
        var tol = GeometryObjectTolerance.Model;
        if (curve.TryGetLine(out var line, tol.VertexTolerance))
        {
          grid = ARDB.Grid.Create(doc, line.ToLine());
        }
        else if (curve.TryGetArc(out var arc, tol.VertexTolerance))
        {
          grid = ARDB.Grid.Create(doc, arc.ToArc());
        }
        else
        {
          throw new RuntimeArgumentException("Curve", "Curve must be a horizontal line or arc curve.", curve);
        }

        grid.CopyParametersFrom(template, ExcludeUniqueProperties);
      }

      if (type is object) grid.ChangeTypeId(type.Id);

      return grid;
    }

    ARDB.Grid Reconstruct(ARDB.Grid grid, ARDB.Document doc, Curve curve, Interval elevation, ARDB.GridType type, string name, ARDB.Grid template)
    {
      if (!Reuse(grid, curve, type, template))
      {
        // Avoids conflict in case we are going to assign same name...
        if (grid is object)
          grid.Name = grid.UniqueId;

        grid = grid.ReplaceElement
        (
          Create(doc, curve, type, template),
          ExcludeUniqueProperties
        );
      }

      if (name is object && grid.Name != name)
        grid.Name = name;

      grid.SetVerticalExtents(elevation.Min / Revit.ModelUnits, elevation.Max / Revit.ModelUnits);

      return grid;
    }
  }
}
