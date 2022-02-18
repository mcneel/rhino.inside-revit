using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.TitleBlocks
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.2.4", updated: "1.5")]
  public class TitleBlockByType : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F2F3D866-5A62-40C0-A85B-C417183E0A52");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public TitleBlockByType() : base
    (
      name: "Add Title Block",
      nickname: "Title Block",
      description: "Create a Revit Title Block on a sheet view",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.ViewSheet()
        {
          Name = "Sheet",
          NickName = "S",
          Description = $"Sheet where to place the {_TitleBlock_}",
        }
      ),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Location",
          NickName = "L",
          Description = $"Location where to place the {_TitleBlock_} on given Sheet",
          Optional = true,
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = $"{_TitleBlock_} type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_TitleBlocks
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = _TitleBlock_,
          NickName = _TitleBlock_.Substring(0, 1),
          Description = $"Output {_TitleBlock_}",
        }
      ),
    };

    const string _TitleBlock_ = "Title Block";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties = default;

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Sheet", out Types.ViewSheet sheet, x => x.IsValid)) return;

      ReconstructElement<ARDB.FamilyInstance>
      (
        sheet.Document, _TitleBlock_, (titleBlock) =>
        {
          // Input
          if (!Params.TryGetData(DA, "Location", out Plane? location, x => x.IsValid)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.FamilySymbol type, Types.Document.FromValue(sheet.Document), ARDB.BuiltInCategory.OST_TitleBlocks)) return null;

          // Compute
          StartTransaction(sheet.Document);
          {
            titleBlock = Reconstruct
            (
              titleBlock,
              location.HasValue ? location.Value : Plane.WorldXY,
              type.Value,
              sheet.Value
            );
          }

          DA.SetData(_TitleBlock_, titleBlock);
          return titleBlock;
        }
      );
    }

    bool Reuse(ARDB.FamilyInstance titleBlock, ARDB.FamilySymbol type)
    {
      if (titleBlock is null) return false;
      if (titleBlock.GetTypeId() != type.Id) titleBlock.ChangeTypeId(type.Id);

      return true;
    }

    ARDB.FamilyInstance Reconstruct
    (
      ARDB.FamilyInstance titleBlock,
      Plane location, ARDB.FamilySymbol type, ARDB.ViewSheet sheet
    )
    {
      if (!Reuse(titleBlock, type))
      {
        titleBlock = titleBlock.ReplaceElement
        (
          sheet.Document.Create.NewFamilyInstance(location.Origin.ToXYZ(), type, sheet),
          ExcludeUniqueProperties
        );
      }

      var newOrigin = location.Origin.ToXYZ();
      var newBasisX = location.XAxis.ToXYZ();
      var newBasisY = location.YAxis.ToXYZ();
      titleBlock.GetLocation(out var origin, out var basisX, out var basisY);

      if
      (
        !origin.IsAlmostEqualTo(newOrigin) ||
        !basisX.IsAlmostEqualTo(newBasisX) ||
        !basisY.IsAlmostEqualTo(newBasisY)
      )
      {
        var pinned = titleBlock.Pinned;
        try
        {
          titleBlock.Pinned = false;
          titleBlock.SetLocation(newOrigin, newBasisX, newBasisY);
        }
        finally { titleBlock.Pinned = pinned; }
      }

      return titleBlock;
    }
  }
}
