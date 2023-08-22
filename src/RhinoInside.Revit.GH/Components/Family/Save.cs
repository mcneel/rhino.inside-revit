using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Families
{
  using External.DB.Extensions;

  [ComponentVersion(introduced:"1.0", updated: "1.13")]
  public class FamilySave : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("C2B9B045-8FD2-4124-9294-D9BA809B44B1");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "S";

    public FamilySave() : base
    (
      name: "Save Component Family",
      nickname: "Save",
      description: "Saves the Family to a given file path.",
      category: "Revit",
      subCategory: "Component"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Family()
        {
          Name = "Family",
          NickName = "F",
          Description = "Family to save"
        }
      ),
      new ParamDefinition
      (
        new Param_FilePath()
        {
          Name = "Path",
          NickName = "P",
          Description = $"Path where to save the Family.{OS.NewLine}If relative or missing a temporary location will be used.",
          FileFilter = "Family Files (*.rfa)|*.rfa"
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Overwrite",
          NickName = "O",
          Description = "Overwrite file on disk",
        }.SetDefaultVale(false)
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Compact",
          NickName = "C",
          Description = "Compact the file",
        }.SetDefaultVale(false), ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Integer()
        {
          Name = "Backups",
          NickName = "B",
          Description = "The maximum number of backups to keep on disk",
        }.SetDefaultVale(-1), ParamRelevance.Secondary
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
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Path",
          NickName = "P",
          Description = "Path where Family is saved."
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Written",
          NickName = "W",
          Description = "Wheter or not file is being written to disk",
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Family", out Types.Family family, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Path", out string filePath, x => !string.IsNullOrWhiteSpace(x))) return;
      if (!Params.TryGetData(DA, "Overwrite", out bool? overwrite)) return;
      if (!Params.TryGetData(DA, "Compact", out bool? compact)) return;
      if (!Params.TryGetData(DA, "Backups", out int? backups)) return;

      if (filePath is null)
        filePath = family.Nomen;

      if (filePath.Last() == Path.DirectorySeparatorChar)
        filePath = Path.Combine(filePath, family.Nomen);

      if (!Path.HasExtension(filePath))
        filePath += ".rfa";

      if (!Path.IsPathRooted(filePath))
      {
        filePath = Path.Combine(Types.Document.FromValue(family.Document).SwapFolder.Directory.FullName, "Families", filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
      }

      var exist = File.Exists(filePath);
      if (overwrite is true || !exist)
      {
        if (family.Document.EditFamily(family.Value) is ARDB.Document familyDoc) using (familyDoc)
        {
          try
          {
            using (var saveAsOptions = new ARDB.SaveAsOptions() { OverwriteExistingFile = true, Compact = compact is true })
            {
              if (backups > -1)
                saveAsOptions.MaximumBackups = backups.Value;

              familyDoc.SaveAs(filePath, saveAsOptions);
              Params.TrySetData(DA, "Family", () => family);
              Params.TrySetData(DA, "Path", () => filePath);
              Params.TrySetData(DA, "Written", () => true);
            }
          }
          catch (Autodesk.Revit.Exceptions.InvalidOperationException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
          finally
          {
            familyDoc.Release();
          }
        }
      }
      else
      {
        if (exist)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"File '{filePath}' already exist");

        Params.TrySetData(DA, "Family", () => family);
        Params.TrySetData(DA, "Path", () => filePath);
        Params.TrySetData(DA, "Written", () => false);
      }
    }
  }
}
