using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;
using CR = Autodesk.Revit.Creation;

namespace RhinoInside.Revit.GH.Components.Element.Sheet
{
  public class SheetTitleBlock : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("f2f3d866-5a62-40c0-a85b-c417183e0a52");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "STB";

    public SheetTitleBlock() : base(
      name: "Sheet Title Block",
      nickname: "STB",
      description: "Get-Set accessor to sheet title block(s)",
      category: "Revit",
      subCategory: "View"
    )
    { }

    static readonly (string name, string nickname, string tip) _Sheet_
      = (name: "Sheet", nickname: "S", tip: "Output Sheet");

    static readonly (string name, string nickname, string tip) _TitleBlockType_
      = (name: "Title Block Type", nickname: "TBT", tip: "Title Block type to use for Title Block");

    static readonly (string name, string nickname, string tip) _TitleBlock_
      = (name: "Title Block", nickname: "TB", tip: "Title Block placed on the sheet");

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.ViewSheet()
        {
          Name = _Sheet_.name,
          NickName = _Sheet_.nickname,
          Description = _Sheet_.tip,
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = _TitleBlockType_.name,
          NickName = _TitleBlockType_.nickname,
          Description = _TitleBlockType_.tip,
          Optional = true,
          SelectedBuiltInCategory = DB.BuiltInCategory.OST_TitleBlocks
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ViewSheet()
        {
          Name = _Sheet_.name,
          NickName = _Sheet_.nickname,
          Description = _Sheet_.tip,
        }
      ),
      new ParamDefinition
      (
        new Parameters.FamilyInstance()
        {
          Name = _TitleBlock_.name,
          NickName = _TitleBlock_.nickname,
          Description = _TitleBlock_.tip,
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var sheet = default(DB.ViewSheet);
      if (!DA.GetData(_Sheet_.name, ref sheet))
        return;

      IList<DB.Element> titleblocks =
        new DB.FilteredElementCollector(sheet.Document, sheet.Id)
              .OfCategory(DB.BuiltInCategory.OST_TitleBlocks)
              .WhereElementIsNotElementType()
              .ToElements();

      var _TBType_ = Params.IndexOfInputParam(_TitleBlockType_.name);
      if (_TBType_ >= 0 && Params.Input[_TBType_].DataType != GH_ParamData.@void)
      {
        var tbSymbol = default(DB.FamilySymbol);
        if (DA.GetData(_TitleBlockType_.name, ref tbSymbol))
        {
          StartTransaction(sheet.Document);
          if (titleblocks.Any())
            sheet.Document.Delete(titleblocks.Select(t => t.Id).ToList());

          DB.FamilyInstance titleblock =
            sheet.Document.Create.NewFamilyInstance(new DB.XYZ(), tbSymbol, sheet);

          DA.SetDataList(_TitleBlock_.name, new List<DB.FamilyInstance> { titleblock });
        }
      }
      else
        DA.SetDataList(_TitleBlock_.name, titleblocks.ToList());

      DA.SetData(_Sheet_.name, sheet);
    }
  }
}
