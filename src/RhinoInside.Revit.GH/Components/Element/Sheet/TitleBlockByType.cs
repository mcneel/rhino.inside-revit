using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.TitleBlocks
{
  using Convert.Geometry;
  using Exceptions;
  using External.DB.Extensions;
  using GH.ElementTracking;

  [ComponentVersion(introduced: "1.2.4")]
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
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Location",
          NickName = "L",
          Description = $"Location where to place the {_TitleBlock_} on given Sheet",
          Optional = true,
        },
        ParamRelevance.Occasional
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
      var OST_TitleBlocks = new ARDB.ElementId(ARDB.BuiltInCategory.OST_TitleBlocks);

      // Input
      if (!Params.TryGetData(DA, "Sheet", out Types.ViewSheet sheet, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Type", out Types.FamilySymbol type,
        x => x.IsValid && x.Document.Equals(sheet.Document) && x.Category.Id == OST_TitleBlocks)) return;
      if (!Params.TryGetData(DA, "Location", out Rhino.Geometry.Point3d? location, x => x.IsValid)) return;

      if (type is null)
      {
        type = Types.FamilySymbol.FromElementId<Types.FamilySymbol>
        (
          sheet.Document,
          sheet.Document.GetDefaultFamilyTypeId(OST_TitleBlocks)
        );

        if (!type.IsValid)
          throw new RuntimeArgumentException(nameof(type), "No default title block type has been found.");
      }

      // Previous Output
      Params.ReadTrackedElement(_TitleBlock_, sheet.Document, out ARDB.FamilyInstance titleBlock);

      StartTransaction(sheet.Document);
      {
        titleBlock = Reconstruct
        (
          titleBlock, location.HasValue? location.Value.ToXYZ() : ARDB.XYZ.Zero,
          type.Value,
          sheet.Value
        );

        Params.WriteTrackedElement(_TitleBlock_, sheet.Document, titleBlock);
        DA.SetData(_TitleBlock_, titleBlock);
      }
    }

    bool Reuse(ARDB.XYZ location, ARDB.FamilyInstance titleBlock, ARDB.FamilySymbol type)
    {
      if (titleBlock is null) return false;
      if (titleBlock.GetTypeId() != type.Id)
        titleBlock.ChangeTypeId(type.Id);

      titleBlock.GetLocation(out var _, out var basisX, out var basisY);
      titleBlock.SetLocation(location, basisX, basisY);

      return true;
    }

    ARDB.FamilyInstance Create(ARDB.XYZ location, ARDB.FamilySymbol type, ARDB.ViewSheet sheet)
    {
      var titleBlock = default(ARDB.FamilyInstance);

      if (titleBlock is null)
        titleBlock = sheet.Document.Create.NewFamilyInstance(location, type, sheet);

      return titleBlock;
    }

    ARDB.FamilyInstance Reconstruct(ARDB.FamilyInstance titleBlock, ARDB.XYZ location, ARDB.FamilySymbol type, ARDB.ViewSheet sheet)
    {
      if (!Reuse(location, titleBlock, type))
      {
        titleBlock = titleBlock.ReplaceElement
        (
          Create(location, type, sheet),
          ExcludeUniqueProperties
        );
      }

      return titleBlock;
    }
  }
}
