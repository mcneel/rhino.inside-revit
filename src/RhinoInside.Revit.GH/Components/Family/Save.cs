using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  using External.DB.Extensions;

  public class FamilySave : Component
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
      subCategory: "Family"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Family(), "Family", "F", "Family to save", GH_ParamAccess.item);

      var path = new Grasshopper.Kernel.Parameters.Param_FilePath();
      path.FileFilter = "Family File (*.rfa)|*.rfa";
      manager[manager.AddParameter(path, "Path", "P", string.Empty, GH_ParamAccess.item)].Optional = true;

      manager.AddBooleanParameter("Overwrite", "O", "Overwrite file on disk", GH_ParamAccess.item, false);
      manager.AddBooleanParameter("Compact", "C", "Compact the file", GH_ParamAccess.item, false);
      manager.AddIntegerParameter("Backups", "B", "The maximum number of backups to keep on disk", GH_ParamAccess.item, -1);
      manager[manager.AddParameter(new Parameters.View(), "Preview View", "PV", "The view that will be used to generate the file preview", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Family(), "Family", "F", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      ARDB.Family family = null;
      if (!DA.GetData("Family", ref family))
        return;

      var filePath = string.Empty;
      DA.GetData("Path", ref filePath);

      var overwrite = false;
      if (!DA.GetData("Overwrite", ref overwrite))
        return;

      var compact = false;
      if (!DA.GetData("Compact", ref compact))
        return;

      var backups = -1;
      if (!DA.GetData("Backups", ref backups))
        return;

      if (family.Document.EditFamily(family) is ARDB.Document familyDoc) using (familyDoc)
      {
        var view = default(ARDB.View);
        if (DA.GetData("Preview View", ref view))
        {
          if (!view.Document.Equals(familyDoc))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"View '{view.Title}' is not a valid view in document {familyDoc.GetFileName()}"); return;
        }

        try
        {
          if (string.IsNullOrEmpty(filePath))
          {
            if (overwrite)
            {
              using (var saveOptions = new ARDB.SaveOptions() { Compact = compact })
              {
                if (view is object)
                  saveOptions.PreviewViewId = view.Id;

                familyDoc.Save(saveOptions);
                DA.SetData("Family", family);
              }
            }
            else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to collect data from 'Path'.");
          }
          else
          {
            if (filePath.Last() == Path.DirectorySeparatorChar)
              filePath = Path.Combine(filePath, familyDoc.Title);

            if (Path.IsPathRooted(filePath) && filePath.Contains(Path.DirectorySeparatorChar))
            {
              if (!Path.HasExtension(filePath))
                filePath += ".rfa";

              using (var saveAsOptions = new ARDB.SaveAsOptions() { OverwriteExistingFile = overwrite, Compact = compact })
              {
                if (backups > -1)
                  saveAsOptions.MaximumBackups = backups;

                if (view is object)
                  saveAsOptions.PreviewViewId = view.Id;

                familyDoc.SaveAs(filePath, saveAsOptions);
                DA.SetData("Family", family);
              }
            }
            else
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path should be absolute.");
            }
          }
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
        finally
        {
          familyDoc.Release();
        }
      }
    }
  }
}
