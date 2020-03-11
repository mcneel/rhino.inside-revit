using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Components.Documents.Families
{
  public class FamilySave : Component
  {
    public override Guid ComponentGuid => new Guid("C2B9B045-8FD2-4124-9294-D9BA809B44B1");
    protected override string IconTag => "S";

    public FamilySave()
    : base("Family.Save", "Family.Save", "Saves the Family to a given file path.", "Revit", "Family")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.Families.Family(), "Family", "F", "Family to save", GH_ParamAccess.item);

      var path = new Grasshopper.Kernel.Parameters.Param_FilePath();
      path.FileFilter = "Family File (*.rfa)|*.rfa";
      manager[manager.AddParameter(path, "Path", "P", string.Empty, GH_ParamAccess.item)].Optional = true;

      manager.AddBooleanParameter("OverrideFile", "O", "Override file on disk", GH_ParamAccess.item, false);
      manager.AddBooleanParameter("Compact", "C", "Compact the file", GH_ParamAccess.item, false);
      manager.AddIntegerParameter("Backups", "B", "The maximum number of backups to keep on disk", GH_ParamAccess.item, -1);
      manager[manager.AddParameter(new Parameters.Elements.View.View(), "PreviewView", "PreviewView", "The view that will be used to generate the file preview", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.Families.Family(), "Family", "F", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Family family = null;
      if (!DA.GetData("Family", ref family))
        return;

      var filePath = string.Empty;
      DA.GetData("Path", ref filePath);

      var overrideFile = false;
      if (!DA.GetData("OverrideFile", ref overrideFile))
        return;

      var compact = false;
      if (!DA.GetData("Compact", ref compact))
        return;

      var backups = -1;
      if (!DA.GetData("Backups", ref backups))
        return;

      if (Revit.ActiveDBDocument.EditFamily(family) is DB.Document familyDoc) using (familyDoc)
      {
        var view = default(DB.View);
        if (DA.GetData("PreviewView", ref view))
        {
          if (!view.Document.Equals(familyDoc))
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"View '{view.Title}' is not a valid view in document {familyDoc.Title}");
            return;
          }
        }

        try
        {
          if (string.IsNullOrEmpty(filePath))
          {
            if (overrideFile)
            {
              using (var saveOptions = new DB.SaveOptions() { Compact = compact })
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
            bool isFolder = filePath.Last() == Path.DirectorySeparatorChar;
            if (isFolder)
              filePath = Path.Combine(filePath, familyDoc.Title);

            if (Path.IsPathRooted(filePath) && filePath.Contains(Path.DirectorySeparatorChar))
            {
              if (!Path.HasExtension(filePath))
                filePath += ".rfa";

              using (var saveAsOptions = new DB.SaveAsOptions() { OverwriteExistingFile = overrideFile, Compact = compact })
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
        catch(Autodesk.Revit.Exceptions.InvalidOperationException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
        finally { familyDoc.Close(false); }
      }
    }
  }
}
