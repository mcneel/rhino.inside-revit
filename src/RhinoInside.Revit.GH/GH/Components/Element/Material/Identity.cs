using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class MaterialIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("06E0CF55-B10C-433A-B6F7-AAF3885055DB");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ID";

    public MaterialIdentity() : base
    (
      "Material.Identity", "Material.Identity",
      "Query material identity information",
      "Revit", "Material"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Material(), "Material", "Material", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Class", "Class", "Material class", GH_ParamAccess.item);
      manager.AddTextParameter("Name", "Name", "Material name", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var material = default(DB.Material);
      if (!DA.GetData("Material", ref material))
        return;

      DA.SetData("Class", material.MaterialClass);
      DA.SetData("Name", material.Name);
    }
  }
}
