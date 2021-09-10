using System;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using RhinoInside.Revit.GH.ElementTracking;
using RhinoInside.Revit.External.DB.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Sheet
{
  public class SheetByNumber : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("704d9c1b-fc56-4407-87cf-720047ae5875");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public SheetByNumber() : base
    (
      name: "Add Sheet",
      nickname: "Sheet",
      description: "Create a new sheet in Revit with given number and name",
      category: "Revit",
      subCategory: "View"
    )
    { }

    static readonly (string name, string nickname, string tip) _Sheet_
    = (name: "Sheet", nickname: "S", tip: "Output Sheet");

    static readonly (string name, string nickname, string tip) _TitleBlock_
      = (name: "Title Block", nickname: "TB", tip: "Title Block placed on the sheet");

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
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Number",
          NickName = "NO",
          Description = $"{_Sheet_.name} Number"
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = $"{_Sheet_.name} Name",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = $"{_TitleBlock_.name} Type",
          NickName = $"{_TitleBlock_.nickname}T",
          Description = $"{_TitleBlock_.name} type to use for Sheet",
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Placeholder",
          NickName = "PH",
          Description = $"Whether sheet is a placeholder",
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Indexed",
          NickName = "IDX",
          Description = $"Whether sheet appears on sheet lists",
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Parameters.AssemblyInstance()
        {
          Name = "Assembly",
          NickName = "A",
          Description = "Assembly to create sheet for",
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Sheet()
        {
          Name = "Template",
          NickName = "T",
          Description = $"Template sheet (only sheet parameters are copied)",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Sheet()
        {
          Name = _Sheet_.name,
          NickName = _Sheet_.nickname,
          Description = _Sheet_.tip,
        }
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = _TitleBlock_.name,
          NickName = _TitleBlock_.nickname,
          Description = _TitleBlock_.tip,
        }
      ),
    };

    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      DB.BuiltInParameter.SHEET_NUMBER,
    };

    class SheetHandler
    {
      // required
      public string Number { get; }

      // use name that is set
      // OR, name from template
      // OR, default
      string _name = null;
      public string Name
      {
        get => _name ?? Template?.Name;
        set => _name = value;
      }

      public bool? IsPlaceHolder { get; set; }
      public bool? IsIndexed { get; set; }

      public DB.ElementType TitleblockType { get; set; }

      public DB.AssemblyInstance Assembly { get; set; }

      public DB.ViewSheet Template { get; set; }

      public SheetHandler(string number)
      {
        Number = number;
      }

      public DB.ViewSheet CreateSheet(DB.Document doc)
      {
        var tblockId = TitleblockType?.Id ?? DB.ElementId.InvalidElementId;
        if (Assembly is DB.AssemblyInstance assm)
        {
          return DB.AssemblyViewUtils.CreateSheet(assm.Document, assm.Id, tblockId);
        }
        else
        {
          if (IsPlaceHolder.HasValue && IsPlaceHolder.Value)
            return DB.ViewSheet.CreatePlaceholder(doc);
          else
            return DB.ViewSheet.Create(doc, tblockId);
        }
      }

      public bool CanUpdateSheet(DB.ViewSheet sheet)
      {
        bool canUpdate = true;

        // - if titleblock on existing does not match the titleblock provided (or not),
        //   on the inputs, do not reuse so Revit places tblock by default at proper location
        //   and whether sheet has titleblock or not matches the input
        var tblocks = new DB.FilteredElementCollector(sheet.Document, sheet.Id)
                            .OfCategory(DB.BuiltInCategory.OST_TitleBlocks)
                            .ToElements();
        if (TitleblockType is DB.ElementType tblockType)
        {
          if (tblocks.Count == 0 || tblocks.Count > 1)
            canUpdate = false;
          else
          {
            var tblock = tblocks.First();
            if (!tblock.GetTypeId().Equals(tblockType.Id))
              canUpdate = false;
          }
        }
        else if (tblocks.Any())
          canUpdate = false;

        // - if sheet placeholder state is different from input, do not reuse.
        //   sheets can not be converted to placeholder vice versa
        if (IsPlaceHolder.HasValue && (IsPlaceHolder.Value != sheet.IsPlaceholder))
          canUpdate = false;

        return canUpdate;
      }

      public void UpdateSheet(DB.ViewSheet sheet)
      {
        // we are not duplicating sheets. that is a very complex process
        // involving various view types, some can only exist on a single sheet
        // let's just copy the parameters from template instead
        if (Template is DB.ViewSheet template)
          sheet.CopyParametersFrom(template, ExcludeUniqueProperties);

        sheet.SheetNumber = Number;

        if (Name is object)
          sheet.Name = Name;

        if (IsIndexed.HasValue)
          sheet.UpdateParameterValue(DB.BuiltInParameter.SHEET_SCHEDULED, IsIndexed.Value);
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // active document
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      // sheet input data
      if (!Params.TryGetData(DA, "Number", out string number, x => !string.IsNullOrEmpty(x))) return;
      // Note:
      // I decided for name to be a required field, although revit automatically names the sheets (e.g. Unnamed in English)
      // This matches the behaviour of Add View components since a name is required for a view and has to be unique
      // Sheets are a type of revit view but the naming is a little less stringent in the API
      // Having name optional, would cause a problem detecting if a sheet can be reused:
      //   If component creates a set of sheets without a name input, it would generate the sheets
      //   with revit's default name. If later a name input is connected, the component can correctly
      //   update the sheet names. But if the name input would be disconnected, the component has no way of
      //   knowing the previous or default name for the sheet and will leave the names as they are
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;

      Params.TryGetData(DA, "Placeholder", out bool? placeholder);
      Params.TryGetData(DA, "Indexed", out bool? indexed);
      Params.TryGetData(DA, $"{_TitleBlock_.name} Type", out DB.ElementType tblockType);

      Params.TryGetData(DA, "Assembly", out DB.AssemblyInstance assembly);
      Params.TryGetData(DA, "Template", out DB.ViewSheet template);

      // find any tracked sheet
      Params.ReadTrackedElement(_Sheet_.name, doc.Value, out DB.ViewSheet sheet);

      // update, or create
      StartTransaction(doc.Value);
      {
        sheet = Reconstruct(sheet, doc.Value, new SheetHandler(number)
        {
          Name = name,
          IsPlaceHolder = placeholder,
          IsIndexed = indexed,
          TitleblockType = tblockType,
          Assembly = assembly,
          Template = template
        });

        Params.WriteTrackedElement(_Sheet_.name, doc.Value, sheet);
        DA.SetData(_Sheet_.name, sheet);

        var titleblock = new DB.FilteredElementCollector(sheet.Document, sheet.Id)
                               .OfCategory(DB.BuiltInCategory.OST_TitleBlocks)
                               .ToElements()
                               .FirstOrDefault();
        DA.SetData(_TitleBlock_.name, titleblock);
      }
    }

    bool Reuse(DB.ViewSheet sheet, SheetHandler data)
    {
      if (!data.CanUpdateSheet(sheet))
      {
        // let's change the sheet number so other sheets can be created with same id
        sheet.SheetNumber = sheet.UniqueId;
        return false;
      }
      else
      {
        data.UpdateSheet(sheet);
        return true;
      }
    }

    DB.ViewSheet Create(DB.Document doc, SheetHandler data)
    {
      var sheet = data.CreateSheet(doc);
      data.UpdateSheet(sheet);
      return sheet;
    }

    DB.ViewSheet Reconstruct(DB.ViewSheet sheet, DB.Document doc, SheetHandler data)
    {
      if (sheet is null || !Reuse(sheet, data))
        sheet = sheet.ReplaceElement
        (
          Create(doc, data),
          ExcludeUniqueProperties
        );

      return sheet;
    }
  }
}
