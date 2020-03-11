using System;
using Grasshopper.Kernel;

namespace Grasshopper.Kernel.Components
{
  public class GH_BrepIsSolid : GH_Component
  {
    public override Guid ComponentGuid => new Guid("ACF07D2E-7204-430D-8352-13AF35E08365");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

    public GH_BrepIsSolid()
    : base("Is Solid", "Is Solid", "Test whether a Brep is solid, and it's orientation", "Surface", "Analysis")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", "Brep to check for its solid orientation", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddNumberParameter("Orientation", "O", "None=0, OutWard=1, Inward=-1", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Rhino.Geometry.Brep brep = null;
      if (!DA.GetData(0, ref brep))
        return;

      var orientation = brep?.SolidOrientation ?? Rhino.Geometry.BrepSolidOrientation.Unknown;
      if (orientation == Rhino.Geometry.BrepSolidOrientation.Unknown)
        return;

      DA.SetData(0, orientation);
    }
  }
}
