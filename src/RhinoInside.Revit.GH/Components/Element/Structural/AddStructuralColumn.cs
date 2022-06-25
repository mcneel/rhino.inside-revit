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
  public class AddStructuralColumn : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("47B560AC-1E1D-4576-9F17-BCCF612974D8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddStructuralColumn() : base
    (
      name: "Add Structural Column",
      nickname: "S-Column",
      description: "Given its Axis, it adds a structural column element to the active Revit document",
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
          Description = "Structural Column axis line.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Structural Column type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_StructuralColumns
        }
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Base Level",
          NickName = "BL",
          Description = "Base level.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Top Level",
          NickName = "TL",
          Description = "Top level.",
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
      ARDB.BuiltInParameter.INSTANCE_MOVE_BASE_WITH_GRIDS,
      ARDB.BuiltInParameter.INSTANCE_MOVE_TOP_WITH_GRIDS,
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
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!curve.TryGetLine(out var line, GeometryTolerance.Model.VertexTolerance))
            throw new RuntimeArgumentException("Curve", "Curve must be line like curve.", curve);

          if (line.ToZ - line.FromZ < GeometryTolerance.Model.VertexTolerance)
            throw new RuntimeArgumentException("Curve", "Curve start point must be below curve end point.", curve);

          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out Types.FamilySymbol type, doc, ARDB.BuiltInCategory.OST_StructuralColumns)) return null;

          var bbox = curve.GetBoundingBox(accurate: true);
          if (!Parameters.Level.GetDataOrDefault(this, DA, "Base Level", out Types.Level baseLevel, doc, bbox.Min.Z)) return null;
          if (!Parameters.Level.GetDataOrDefault(this, DA, "Top Level", out Types.Level topLevel, doc, bbox.Max.Z)) return null;

          // Compute
          column = Reconstruct(column, doc.Value, line.ToLine(), type.Value, baseLevel.Value, topLevel.Value);

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

      var ids = doc.IsFamilyDocument ?
        doc.FamilyCreate.NewFamilyInstances2(list) :
        doc.Create.NewFamilyInstances2(list);

      return doc.GetElement(ids.First()) as ARDB.FamilyInstance;
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance column,
      ARDB.Document doc,
      ARDB.Curve curve,
      ARDB.FamilySymbol type,
      ARDB.Level baselevel,
      ARDB.Level topLevel
    )
    {
      if (!Reuse(column, type))
      {
        column = column.ReplaceElement
        (
          Create(doc, curve, type, baselevel),
          ExcludeUniqueProperties
        );

        // We turn off analytical model off by default
        column.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL)?.Update(false);
        column.Document.Regenerate();
      }

      column.get_Parameter(ARDB.BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM).Update(2);
      column.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVE_BASE_WITH_GRIDS)?.Update(false);
      column.get_Parameter(ARDB.BuiltInParameter.INSTANCE_MOVE_TOP_WITH_GRIDS)?.Update(false);
      column.get_Parameter(ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).Update(baselevel.Id);
      column.get_Parameter(ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Update(topLevel.Id);
      column.get_Parameter(ARDB.BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE)?.Update(0.0);

      if (column.Location is ARDB.LocationCurve locationCurve)
      {
        if (!locationCurve.Curve.AlmostEquals(curve, GeometryTolerance.Internal.VertexTolerance))
        {
          curve.TryGetLocation(out var origin, out var basisX, out var basisY);

          column.Pinned = false;
          column.SetLocation(origin, basisX, basisY);

          locationCurve.Curve = curve;

          var startPoint = curve.GetEndPoint(ERDB.CurveEnd.Start);
          var endPoint = curve.GetEndPoint(ERDB.CurveEnd.End);
          column.get_Parameter(ARDB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Update(Math.Min(startPoint.Z, endPoint.Z) - baselevel.ProjectElevation);
          column.get_Parameter(ARDB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Update(Math.Max(startPoint.Z, endPoint.Z) - topLevel.ProjectElevation);
        }
      }

      return column;
    }
  }

}
