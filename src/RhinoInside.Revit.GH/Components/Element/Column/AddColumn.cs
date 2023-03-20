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

  [ComponentVersion(introduced: "1.13")]
  public class AddColumn : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("BE2C26C7-9617-4E3F-A961-C66E076BA37B");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "I";

    public AddColumn() : base
    (
      name: "Add Column",
      nickname: "Column",
      description: "Given its Location, it adds a column element to the active Revit document",
      category: "Revit",
      subCategory: "Host"
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
          Description = "Column location.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Column type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Columns
        }
      ),
      new ParamDefinition
      (
        new Parameters.LevelConstraint()
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the {_Column_.ToLowerInvariant()}.{Environment.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Location'.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.LevelConstraint()
        {
          Name = "Top",
          NickName = "TO",
          Description = $"Top of the {_Column_.ToLowerInvariant()}.{Environment.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Location'.",
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
          Name = _Column_,
          NickName = _Column_.Substring(0, 1),
          Description = $"Output {_Column_}",
        }
      )
    };

    const string _Column_ = "Column";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM,
      //ARDB.BuiltInParameter.INSTANCE_MOVE_BASE_WITH_GRIDS,
      //ARDB.BuiltInParameter.INSTANCE_MOVE_TOP_WITH_GRIDS,
      ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM,
      ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM,
      ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM,
      ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM,
      ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.FamilyInstance>
      (
        doc.Value, _Column_, column =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!Params.TryGetData(DA, "Location", out Plane? location, x => x.IsValid)) return null;

          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out Types.FamilySymbol type, doc, ARDB.BuiltInCategory.OST_Columns)) return null;
          type.AssertPlacementType(ARDB.FamilyPlacementType.TwoLevelsBased);

          if (!Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation)) return null;
          if (!Params.TryGetData(DA, "Top", out ERDB.ElevationElementReference? topElevation)) return null;

          // Solve missing Base & Top
          ERDB.ElevationElementReference.SolveBaseAndTop
          (
            doc.Value, location.Value.OriginZ / Revit.ModelUnits,
            0.0, 10.0,
            ref baseElevation, ref topElevation
          );

          // If there are no Levels!!
          if (!baseElevation.Value.IsLevelConstraint(out var baseLevel, out var baseOffset))
            return null;

          if (!topElevation.Value.IsLevelConstraint(out var topLevel, out var topOffset))
          {
            topLevel = baseLevel;
            topOffset = topElevation.Value.Offset;
          }

          var basePlane = new Plane(new Point3d(0.0, 0.0, (baseLevel.ProjectElevation + baseOffset.Value) * Revit.ModelUnits), Vector3d.ZAxis);
          var topPlane  = new Plane(new Point3d(0.0, 0.0, (topLevel.ProjectElevation  + topOffset.Value)  * Revit.ModelUnits), Vector3d.ZAxis);

          var axis = new Line(location.Value.Origin, location.Value.ZAxis);
          if (!Rhino.Geometry.Intersect.Intersection.LinePlane(axis, basePlane, out var t0) || ! Rhino.Geometry.Intersect.Intersection.LinePlane(axis, topPlane, out var t1))
            throw new RuntimeArgumentException("Location", $"Location can't be a vertical plane.", location.Value);

          var line = ARDB.Line.CreateBound(axis.PointAt(t0).ToXYZ(), axis.PointAt(t1).ToXYZ());

          // Compute
          column = Reconstruct(column, doc.Value, line, (ERDB.UnitXYZ) location.Value.XAxis.ToXYZ(), type.Value, baseLevel ?? topLevel, topLevel ?? baseLevel);

          DA.SetData(_Column_, column);
          return column;
        }
      );
    }

    bool Reuse
    (
      ARDB.FamilyInstance column,
      ARDB.FamilySymbol type
    )
    {
      if (column is null) return false;
      if (type.Id != column.GetTypeId()) column.ChangeTypeId(type.Id);

      return true;
    }

    ARDB.FamilyInstance Create(ARDB.Document doc, ARDB.Curve curve, ARDB.FamilySymbol type, ARDB.Level level)
    {
      if (!type.IsActive) type.Activate();

      var list = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>(1)
      {
        new Autodesk.Revit.Creation.FamilyInstanceCreationData
        (
          curve: curve,
          symbol: type,
          level: level,
          structuralType: ARDB.Structure.StructuralType.Column
        )
      };

      var ids = doc.Create().NewFamilyInstances2(list);
      if (ids.Count == 1)
      {
        var instance = doc.GetElement(ids.First()) as ARDB.FamilyInstance;

        // We turn analytical model off by default
        instance.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
        return instance;
      }

      throw new Exceptions.RuntimeArgumentException("Type", $"Type '{type.FamilyName} : {type.Name}' is not a valid column type.");
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance column,
      ARDB.Document doc,
      ARDB.Line line,
      ERDB.UnitXYZ right,
      ARDB.FamilySymbol type,
      ARDB.Level baseLevel,
      ARDB.Level topLevel
    )
    {
      if (!Reuse(column, type))
      {
        column = column.ReplaceElement
        (
          Create(doc, line, type, baseLevel),
          ExcludeUniqueProperties
        );

        column.Document.Regenerate();
      }

      var lineDirection = (ERDB.UnitXYZ) line.Direction;
      var angle = right.AngleOnPlaneTo(lineDirection.Right(), lineDirection);

      column.get_Parameter(ARDB.BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM).Update(2);
      //column.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVE_BASE_WITH_GRIDS)?.Update(false);
      //column.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVE_TOP_WITH_GRIDS)?.Update(false);
      column.get_Parameter(ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).Update(baseLevel.Id);
      column.get_Parameter(ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Update(topLevel.Id);
      column.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(angle);

      if (column.Location is ARDB.LocationCurve locationCurve)
      {
        if (!locationCurve.Curve.AlmostEquals(line, GeometryTolerance.Internal.VertexTolerance))
        {
          line.TryGetLocation(out var origin, out var basisX, out var basisY);
          column.SetLocation(origin, basisX, basisY);

          locationCurve.Curve = line;

          var startPoint = line.GetEndPoint(ERDB.CurveEnd.Start);
          var endPoint = line.GetEndPoint(ERDB.CurveEnd.End);
          column.get_Parameter(ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Update(Math.Min(startPoint.Z, endPoint.Z) - baseLevel.ProjectElevation);
          column.get_Parameter(ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Update(Math.Max(startPoint.Z, endPoint.Z) - topLevel.ProjectElevation);
        }
      }

      return column;
    }
  }
}
