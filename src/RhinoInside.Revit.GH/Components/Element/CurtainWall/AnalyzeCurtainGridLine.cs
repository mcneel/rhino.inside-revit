using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;

using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.Convert.Geometry;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyseCurtainGridLine : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("FACE5E7D-174F-41DA-853E-CDC4B094F57C");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ACGL";

    public AnalyseCurtainGridLine() : base(
      name: "Analyze Curtain Grid Line",
      nickname: "A-CGL",
      description: "Analyze given curtain grid line",
      category: "Revit",
      subCategory: "Analyze"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.CurtainGridLine(),
        name: "Curtain Grid Line",
        nickname: "CGL",
        description: "Curtain Grid Line",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddCurveParameter(
        name: "Curve",
        nickname: "C",
        description: "Full curve of the curtain grid line",
        access: GH_ParamAccess.item
        );
      manager.AddCurveParameter(
        name: "Segments",
        nickname: "S",
        description: "All curve segments of the curtain grid line",
        access: GH_ParamAccess.item
        );
      manager.AddCurveParameter(
        name: "Existing Segments",
        nickname: "ES",
        description: "All existing curve segments of the curtain grid line",
        access: GH_ParamAccess.item
        );
      manager.AddCurveParameter(
        name: "Skipped Segments",
        nickname: "SS",
        description: "All skipped curve segments of the curtain grid line",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.CurtainGridMullion(),
        name: "Attached Mullions",
        nickname: "ACGM",
        description: "All curtain grid mullions attached to the curtain grid line",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Attached Panels",
        nickname: "ACGP",
        description: "All curtain grid panels attached to the curtain grid line",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      DB.CurtainGridLine gridLine = default;
      if (!DA.GetData("Curtain Grid Line", ref gridLine))
        return;


      DA.SetData("Curve", gridLine.FullCurve.ToCurve());
      DA.SetDataList("Segments", gridLine.AllSegmentCurves.ToCurves());
      DA.SetDataList("Existing Segments", gridLine.ExistingSegmentCurves.ToCurves());
      DA.SetDataList("Skipped Segments", gridLine.SkippedSegmentCurves.ToCurves());

      // find attached mullions
      const double EPSILON = 0.1;
      var attachedMullions = new List<Types.Element>();
      var famInstFilter = new DB.ElementClassFilter(typeof(DB.FamilyInstance));
      // collect familyinstances and filter for DB.Mullion
      var dependentMullions = gridLine.GetDependentElements(famInstFilter).Select(x => gridLine.Document.GetElement(x)).OfType<DB.Mullion>();
      // for each DB.Mullion that is dependent on this DB.CurtainGridLine
      foreach (DB.Mullion mullion in dependentMullions)
      {
        if (mullion.LocationCurve != null)
        {
          // check the distance of the DB.Mullion curve start and end, to the DB.CurtainGridLine axis curve
          var mcurve = mullion.LocationCurve;
          var mstart = mcurve.GetEndPoint(0);
          var mend = mcurve.GetEndPoint(1);
          // if the distance is less than EPSILON, the DB.Mullion axis and DB.CurtainGridLine axis are almost overlapping
          if (gridLine.FullCurve.Distance(mstart) < EPSILON && gridLine.FullCurve.Distance(mend) < EPSILON)
                attachedMullions.Add(Types.CurtainGridMullion.FromElement(mullion));
        }
      }
      DA.SetDataList("Attached Mullions", attachedMullions);

      // filter attached panels
      // panels can be a mix of DB.Panel and DB.FamilyInstance
      // no need to filter for .OfType<DB.Panel>() like with DB.Mullion
      // but make sure to remove all the DB.FamilyInstance that are actually DB.Mullion
      var dependentPanels = gridLine.GetDependentElements(famInstFilter).Select(x => gridLine.Document.GetElement(x)).Where(x => x as DB.Mullion is null);
      DA.SetDataList("Attached Panels", dependentPanels.Select(x => Types.Element.FromElement(x)));
    }
  }
}
