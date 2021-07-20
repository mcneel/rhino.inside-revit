using System;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyLoad : TransactionalComponent
  {
    public override Guid ComponentGuid => new Guid("0E244846-95AE-4B0E-8218-CB24FD4D34D1");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "L";

    public FamilyLoad() : base
    (
      name: "Load Component Family",
      nickname: "Load",
      description: "Loads a family into the document",
      category: "Revit",
      subCategory: "Family"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_FilePath()
        {
          Name = "Path",
          NickName = "P",
          FileFilter = "Family File (*.rfa)|*.rfa"
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Overwrite",
          NickName = "O",
          Description = "Overwrite Family",
        }.
        SetDefaultVale(false),
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Overwrite Parameters",
          NickName = "OP",
          Description = "Overwrite Parameters",
        }.
        SetDefaultVale(false),
        ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Family()
        {
          Name = "Family",
          NickName = "F",
        }
      )
    };

    public override void VariableParameterMaintenance()
    {
      if (Params.Input<IGH_Param>("Override Family") is IGH_Param overrideFamily)
        overrideFamily.Name = "Overwrite";

      if (Params.Input<IGH_Param>("Override Parameters") is IGH_Param overrideParameters)
        overrideParameters.Name = "Overwrite Parameters";

      base.VariableParameterMaintenance();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      if (!Params.GetData(DA, "Path", out string filePath)) return;
      if (!Params.TryGetData(DA, "Overwrite", out bool? overwrite)) return;
      if (!overwrite.HasValue) overwrite = false;
      if (!Params.TryGetData(DA, "Overwrite Parameters", out bool? overwriteParameters)) return;
      if (!overwriteParameters.HasValue) overwriteParameters = overwrite;

      using (var transaction = NewTransaction(doc))
      {
        transaction.Start(Name);

        if (doc.LoadFamily(filePath, new FamilyLoadOptions(overwrite == true, overwriteParameters == true), out var family))
        {
          CommitTransaction(doc, transaction);
        }
        else
        {
          var name = Path.GetFileNameWithoutExtension(filePath);
          doc.TryGetFamily(name, out family);

          if (family is object && overwrite != true)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Family '{name}' already loaded!");
        }

        DA.SetData("Family", family);
      }
    }
  }
}
