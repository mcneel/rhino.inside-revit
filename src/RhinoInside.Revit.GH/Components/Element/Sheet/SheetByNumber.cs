using System;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using RhinoInside.Revit.GH.ElementTracking;
using RhinoInside.Revit.External.DB.Extensions;

using DB = Autodesk.Revit.DB;

using RhinoInside.Revit.GH.Kernel.Attributes;

namespace RhinoInside.Revit.GH.Components.Element.Sheet
{
  [Since("v1.2")]
  public class SheetByNumber : BaseSheetByNumber<SheetHandler>
  {
    public override Guid ComponentGuid => new Guid("704d9c1b-fc56-4407-87cf-720047ae5875");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public SheetByNumber() : base
    (
      name: "Add Sheet",
      nickname: "Sheet",
      description: "Create a new sheet in Revit with given number and name"
    )
    { }

    static readonly (string name, string nickname, string tip) _Sheet_
    = (name: "Sheet", nickname: "S", tip: "Output Sheet");

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
          Description = $"{_Sheet_.name} Number"
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Sheet Name",
          NickName = "N",
          Description = $"{_Sheet_.name} Name",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Appears In Sheet List",
          NickName = "AISL",
          Description = $"Whether sheet appears on sheet lists",
          Optional = true
        }
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
        ParamRelevance.Primary
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
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // active document
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      // sheet input data
      if (!Params.TryGetData(DA, "Sheet Number", out string number, x => !string.IsNullOrEmpty(x))) return;
      // Note: see notes on SheetHandler.Name parameter
      if (!Params.TryGetData(DA, "Sheet Name", out string name, x => !string.IsNullOrEmpty(x))) return;

      Params.TryGetData(DA, "Appears In Sheet List", out bool? scheduled);

      Params.TryGetData(DA, "Template", out DB.ViewSheet template);

      // find any tracked sheet
      Params.ReadTrackedElement(_Sheet_.name, doc.Value, out DB.ViewSheet sheet);

      // update, or create
      StartTransaction(doc.Value);
      {
        sheet = Reconstruct(sheet, doc.Value, new SheetHandler(number)
        {
          Name = name,
          SheetScheduled = scheduled,
          Template = template
        });

        Params.WriteTrackedElement(_Sheet_.name, doc.Value, sheet);
        DA.SetData(_Sheet_.name, sheet);
      }
    }
  }
}
