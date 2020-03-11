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
  public class FamilyVoidByBrep : Component
  {
    public override Guid ComponentGuid => new Guid("F0887AD5-8ACB-4806-BB12-7596BCEDFFED");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override string IconTag => "V";

    public FamilyVoidByBrep()
    : base("FamilyVoid.ByBrep", "FamilyVoid.ByBrep", string.Empty, "Revit", "Family")
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

      var cutting = true;
      if (DA.GetData("Void", ref cutting))
        brep.SetUserString(DB.BuiltInParameter.ELEMENT_IS_CUTTING.ToString(), cutting ? "1" : null);

      DA.SetData("Brep", brep);
    }
  }
}
