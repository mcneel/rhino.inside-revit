using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyGeometryVoidByBrep : Component
  {
    public override Guid ComponentGuid => new Guid("F0887AD5-8ACB-4806-BB12-7596BCEDFFED");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => "V";

    public FamilyGeometryVoidByBrep()
    : base("Component Family Void", "FamVoid", string.Empty, "Revit", "Family")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", string.Empty, GH_ParamAccess.item);
      manager[manager.AddBooleanParameter("Void", "V", string.Empty, GH_ParamAccess.item, true)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var brep = default(Rhino.Geometry.Brep);
      if (!DA.GetData("Brep", ref brep))
        return;

      brep = brep.DuplicateBrep();

      var cutting = default(bool);
      if (DA.GetData("Void", ref cutting))
        brep.TrySetUserString(DB.BuiltInParameter.ELEMENT_IS_CUTTING.ToString(), cutting, false);

      DA.SetData("Brep", brep);
    }
  }
}
