using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyLoad : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("0E244846-95AE-4B0E-8218-CB24FD4D34D1");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "L";

    public FamilyLoad()
    : base("Load Family", "Load", "Loads a family into the document", "Revit", "Family")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      var path = new Grasshopper.Kernel.Parameters.Param_FilePath();
      path.FileFilter = "Family File (*.rfa)|*.rfa";
      manager.AddParameter(path, "Path", "P", string.Empty, GH_ParamAccess.item);

      manager.AddBooleanParameter("OverrideFamily", "O", "Override Family", GH_ParamAccess.item, false);
      manager.AddBooleanParameter("OverrideParameters", "O", "Override Parameters", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Family(), "Family", "F", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var filePath = string.Empty;
      if (!DA.GetData("Path", ref filePath))
        return;

      var overrideFamily = false;
      if (!DA.GetData("OverrideFamily", ref overrideFamily))
        return;

      var overrideParameters = false;
      if (!DA.GetData("OverrideParameters", ref overrideParameters))
        return;

      using (var transaction = new DB.Transaction(doc))
      {
        transaction.Start(Name);

        if (doc.LoadFamily(filePath, new FamilyLoadOptions(overrideFamily, overrideParameters), out var family))
        {
          transaction.Commit();
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
