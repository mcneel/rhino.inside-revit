using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.GH.ElementTracking;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Exceptions;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Sheet
{
  public class AddTitleBlock : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F2F3D866-5A62-40C0-A85B-C417183E0A52");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public AddTitleBlock() : base
    (
      name: "Add Title Block",
      nickname: "Title Block",
      description: "Create a Revit title block on a sheet view",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Location",
          NickName = "L",
          Description = "Sheet where to place the title block",
          Optional = true,
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.ViewSheet()
        {
          Name = "Sheet",
          NickName = "S",
          Description = "Sheet where to place the title block",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Title block type",
          Optional = true,
          SelectedBuiltInCategory = DB.BuiltInCategory.OST_TitleBlocks
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
    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties = default;

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var OST_TitleBlocks = new DB.ElementId(DB.BuiltInCategory.OST_TitleBlocks);

      // Input
      if (!Params.TryGetData(DA, "Location", out Rhino.Geometry.Point3d? location, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Sheet", out Types.ViewSheet sheet, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Type", out Types.FamilySymbol type,
        x => x.IsValid && x.Document.Equals(sheet.Document) && x.Category.Id == OST_TitleBlocks)) return;

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
      Params.ReadTrackedElement(_TitleBlock_, sheet.Document, out DB.FamilyInstance titleBlock);

      StartTransaction(sheet.Document);
      {
        titleBlock = Reconstruct
        (
          titleBlock, location.HasValue? location.Value.ToXYZ() : DB.XYZ.Zero,
          type.Value,
          sheet.Value
        );

        Params.WriteTrackedElement(_TitleBlock_, sheet.Document, titleBlock);
        DA.SetData(_TitleBlock_, titleBlock);
      }
    }

    bool Reuse(DB.XYZ location, DB.FamilyInstance titleBlock, DB.FamilySymbol type)
    {
      if (titleBlock is null) return false;
      if (titleBlock.GetTypeId() != type.Id)
        titleBlock.ChangeTypeId(type.Id);

      titleBlock.GetLocation(out var _, out var basisX, out var basisY);
      titleBlock.SetLocation(location, basisX, basisY);

      return true;
    }

    DB.FamilyInstance Create(DB.XYZ location, DB.FamilySymbol type, DB.ViewSheet sheet)
    {
      var titleBlock = default(DB.FamilyInstance);

      if (titleBlock is null)
        titleBlock = sheet.Document.Create.NewFamilyInstance(location, type, sheet);

      return titleBlock;
    }

    DB.FamilyInstance Reconstruct(DB.FamilyInstance titleBlock, DB.XYZ location, DB.FamilySymbol type, DB.ViewSheet sheet)
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
