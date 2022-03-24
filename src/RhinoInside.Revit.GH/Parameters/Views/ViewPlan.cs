using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.7")]
  public class FloorPlan : Element<Types.FloorPlan, ARDB.ViewPlan>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("1BDE7F9F-2769-416B-AAA7-85FBACA3BC73");

    public FloorPlan() : base("Floor Plan", "Floor Plan", "Contains a collection of Revit floor plan views", "Params", "Revit") { }

    protected override Types.FloorPlan InstantiateT() => new Types.FloorPlan();
  }

  [ComponentVersion(introduced: "1.7")]
  public class CeilingPlan : Element<Types.CeilingPlan, ARDB.ViewPlan>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("33E34BD8-4E56-4227-9B32-C076346D8FC8");

    public CeilingPlan() : base("Ceiling Plan", "Ceiling View", "Contains a collection of Revit ceiling plan views", "Params", "Revit") { }

    protected override Types.CeilingPlan InstantiateT() => new Types.CeilingPlan();
  }

  [ComponentVersion(introduced: "1.7")]
  public class AreaPlan : Element<Types.AreaPlan, ARDB.ViewPlan>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("D0D3D169-9CAF-48E4-982A-E2AF58B362D4");

    public AreaPlan() : base("Area View", "Area View", "Contains a collection of Revit area plan views", "Params", "Revit") { }

    protected override Types.AreaPlan InstantiateT() => new Types.AreaPlan();
  }
}
