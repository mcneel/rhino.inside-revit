using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Grids
{
  using Convert.Geometry;
  using ElementTracking;
  using Grasshopper.Kernel.Parameters;
  using RhinoInside.Revit.External.DB.Extensions;
  using RhinoInside.Revit.GH.Exceptions;

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
        new Parameters.Level()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template Level",
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
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;
      if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.GridType type, doc, ARDB.ElementTypeGroup.GridType)) return;
      Params.TryGetData(DA, "Template", out ARDB.Grid template);

      // Previous Output
      Params.ReadTrackedElement(_Grid_, doc.Value, out ARDB.Grid grid);

      StartTransaction(doc.Value);
      {
        var untracked = Existing(_Grid_, doc.Value, ref grid, name, categoryId: ARDB.BuiltInCategory.OST_Grids);

        grid = Reconstruct(grid, doc.Value, curve, type, name, template);

        Params.WriteTrackedElement(_Grid_, doc.Value, untracked ? default : grid);
        DA.SetData(_Grid_, grid);
      }
    }

    bool Reuse(ARDB.Grid grid, Curve curve, ARDB.GridType type, ARDB.Grid template)
    {
      if (grid is null) return false;

      var tol = GeometryObjectTolerance.Internal;
      var gridCurve = grid.Curve.CreateReversed();
      var newCurve = curve.ToCurve();
      if (!gridCurve.IsAlmostEqualTo(newCurve, tol.VertexTolerance))
        return false;

      if (type is object && grid.GetTypeId() != type.Id) grid.ChangeTypeId(type.Id);
      grid.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.Grid Create(ARDB.Document doc, Curve curve, ARDB.GridType type, ARDB.Grid template)
    {
      var grid = default(ARDB.Grid);

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
        throw new RuntimeArgumentException("Curve", "Curve must be a horizontal line or arc curve.");
      }

      if (type is object && type.Id != grid.GetTypeId())
        grid.ChangeTypeId(type.Id);

      return grid;
    }

    ARDB.Grid Reconstruct(ARDB.Grid grid, ARDB.Document doc, Curve curve, ARDB.GridType type, string name, ARDB.Grid template)
    {
      if (!Reuse(grid, curve, type, template))
      {
        var newGrid = Create(doc, curve, type, template);
        grid.ReplaceElement(newGrid, ExcludeUniqueProperties);

        if (grid is object)
        {
          name = name ?? grid.Name;
          grid.Document.Delete(grid.Id);
        }

        grid = newGrid;
      }

      if (name is object && grid.Name != name)
        grid.Name = name;

      return grid;
    }
  }
}
