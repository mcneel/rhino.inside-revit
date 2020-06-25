using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyLoad : DocumentComponent
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
      ParamDefinition.FromParam
      (
        CreateDocumentParam(),
        ParamVisibility.Voluntary
      ),
      ParamDefinition.FromParam
      (
        new Param_FilePath()
        {
          Name = "Path",
          NickName = "P",
          Access = GH_ParamAccess.item,
          FileFilter = "Family File (*.rfa)|*.rfa"
        }
      ),
      ParamDefinition.FromParam
      (
        new Param_Boolean()
        {
          Name = "Override Family",
          NickName = "OF",
          Description = "Override Family",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Binding,
        defaultValue: false
      ),
      ParamDefinition.FromParam
      (
        new Param_Boolean()
        {
          Name = "Override Parameters",
          NickName = "OP",
          Description = "Override Parameters",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Binding,
        defaultValue: false
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.FromParam
      (
        new Parameters.Family()
        {
          Name = "Family",
          NickName = "F",
          Description = string.Empty,
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var filePath = string.Empty;
      if (!DA.GetData("Path", ref filePath))
        return;

      var overrideFamily = false;
      if (!DA.GetData("Override Family", ref overrideFamily))
        return;

      var overrideParameters = false;
      if (!DA.GetData("Override Parameters", ref overrideParameters))
        return;

      using (var transaction = NewTransaction(doc))
      {
        transaction.Start(Name);

        if (doc.LoadFamily(filePath, new FamilyLoadOptions(overrideFamily, overrideParameters), out var family))
        {
          CommitTransaction(doc, transaction);
        }
        else
        {
          var name = Path.GetFileNameWithoutExtension(filePath);
          using (var collector = new DB.FilteredElementCollector(doc).OfClass(typeof(DB.Family)))
            family = collector.Cast<DB.Family>().Where(x => x.Name == name).FirstOrDefault();

          if (family is object && overrideFamily == false)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Family '{name}' already loaded!");
        }

        DA.SetData("Family", family);
      }
    }
  }
}
