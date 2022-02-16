using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Sheets
{
  using RhinoInside.Revit.External.DB.Extensions;

  [ComponentVersion(introduced: "1.2.4", updated: "1.5")]
  public class SheetByNumber_Placeholder : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("16f18871-6fe8-4dfb-a4f5-47826d582442");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public SheetByNumber_Placeholder() : base
    (
      name: "Add Sheet (Placeholder)",
      nickname: "Placeholder",
      description: "Create a new placeholder sheet in Revit with given number and name",
      category: "Revit",
      subCategory: "View"
    )
    { }

    const string _Sheet_ = "Sheet";
    public static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.SHEET_NUMBER,
      ARDB.BuiltInParameter.SHEET_NAME,
    };

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
          Name = "Sheet Number",
          NickName = "NUM",
          Description = $"Sheet Number"
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Sheet Name",
          NickName = "N",
          Description = $"Sheet Name",
          Optional = true,
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ViewFamilyType()
        {
          Name = "Type",
          NickName = "T",
          Description = "View Type",
          Optional = true,
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.ViewSheet()
        {
          Name = "Template",
          NickName = "T",
          Description = $"Template sheet (only sheet parameters are copied)",
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
        new Parameters.ViewSheet()
        {
          Name = _Sheet_,
          NickName = _Sheet_.Substring(0, 1),
          Description = $"Output {_Sheet_}",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ViewSheet>
      (
        doc.Value, _Sheet_, (sheet) =>
        {
          // Input
          if (!Params.TryGetData(DA, "Sheet Number", out string number, x => !string.IsNullOrEmpty(x))) return null;
          if (!Params.TryGetData(DA, "Sheet Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out ARDB.ViewFamilyType type, doc, ARDB.ElementTypeGroup.ViewTypeSheet)) return null;
          Params.TryGetData(DA, "Template", out ARDB.ViewSheet template);

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_Sheet_, out var untracked, ref sheet, doc.Value, number))
            sheet = Reconstruct(sheet, doc.Value, number, name, type, template);

          DA.SetData(_Sheet_, sheet);
          return untracked ? null : sheet;
        }
      );
    }

    ARDB.ViewSheet Reconstruct
    (
      ARDB.ViewSheet sheet,
      ARDB.Document doc,
      string number, string name,
      ARDB.ViewFamilyType type,
      ARDB.ViewSheet template
    )
    {
      sheet = sheet ?? ARDB.ViewSheet.CreatePlaceholder(doc);
      sheet.CopyParametersFrom(template, ExcludeUniqueProperties);

      if (number is object) sheet?.get_Parameter(ARDB.BuiltInParameter.SHEET_NUMBER).Update(number);
      if (name is object) sheet?.get_Parameter(ARDB.BuiltInParameter.SHEET_NAME).Update(name);
      if (type is object && sheet.GetTypeId() != type.Id) sheet.ChangeTypeId(type.Id);

      return sheet;
    }
  }
}
