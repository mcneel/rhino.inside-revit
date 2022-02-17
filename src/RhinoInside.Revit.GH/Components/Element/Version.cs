using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Elements
{
  [ComponentVersion(introduced: "1.6")]
  public class ElementVersion : Component
  {
    public override Guid ComponentGuid => new Guid("3848C899-CC07-4397-855D-961E907C09D9");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;


    public ElementVersion()
    : base("Element Version", "Version", string.Empty, "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", string.Empty, GH_ParamAccess.item);
      manager.AddParameter(new Param_Boolean(), "Editable", "ED", "Identifies if the element is read-only or can possibly be modified", GH_ParamAccess.item);
      manager.AddParameter(new Param_Guid(), "Created", "C", "Document episode when Element was created", GH_ParamAccess.item);
      manager.AddParameter(new Param_Guid(), "Updated", "U", "Document episode when Element was updated last time", GH_ParamAccess.item);
    }

#if !REVIT_2021
    protected override void BeforeSolveInstance()
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'Updated' output is only supported on Revit 2021 or above.");
      base.BeforeSolveInstance();
    }
#endif

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      DA.SetData("Editable", element.IsEditable);

      var (created, updated) = element.Version;
      DA.SetData("Created", created);
      DA.SetData("Updated", updated);
    }
  }
}
