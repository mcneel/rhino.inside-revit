using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  public class AnalyzeCurtainGridLine : Component
  {
    public override Guid ComponentGuid => new Guid("FACE5E7D-174F-41DA-853E-CDC4B094F57C");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "DCL";

    public AnalyzeCurtainGridLine() : base
    (
      name: "Deconstruct Curtain Line",
      nickname: "D-CLine",
      description: "Deconstruct given curtain grid line",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

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
        param: new Parameters.Mullion(),
        name: "Attached Mullions",
        nickname: "ACGM",
        description: "All curtain grid mullions attached to the curtain grid line",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.GraphicalElement(),
        name: "Attached Panels",
        nickname: "ACGP",
        description: "All curtain grid panels attached to the curtain grid line",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      ARDB.CurtainGridLine gridLine = default;
      if (!DA.GetData("Curtain Grid Line", ref gridLine))
        return;

      DA.SetData("Curve", gridLine.FullCurve.ToCurve());
      DA.SetDataList("Segments", gridLine.AllSegmentCurves.ToCurveMany());
      DA.SetDataList("Existing Segments", gridLine.ExistingSegmentCurves.ToCurveMany());
      DA.SetDataList("Skipped Segments", gridLine.SkippedSegmentCurves.ToCurveMany());

      // find attached mullions
      double tolerance = gridLine.Document.Application.VertexTolerance;
      var attachedMullions = new List<Types.Element>();
      
      // collect familyinstances and filter for DB.Mullion
      var dependentMullionIds = gridLine.GetDependentElements(CompoundElementFilter.ElementClassFilter(typeof(ARDB.Mullion)));

      // for each DB.Mullion that is dependent on this DB.CurtainGridLine
      foreach (var mullion in dependentMullionIds.Select(gridLine.Document.GetElement).OfType<ARDB.Mullion>())
      {
        if (mullion.LocationCurve is ARDB.Curve locationCurve)
        {
          // check the distance of the DB.Mullion curve start and end, to the DB.CurtainGridLine axis curve
          var mstart = locationCurve.GetEndPoint(0);
          var mend = locationCurve.GetEndPoint(1);

          // if the distance is less than EPSILON, the DB.Mullion axis and DB.CurtainGridLine axis are almost overlapping
          if (gridLine.FullCurve.Distance(mstart) < tolerance && gridLine.FullCurve.Distance(mend) < tolerance)
            attachedMullions.Add(Types.Mullion.FromElement(mullion));
        }
      }
      DA.SetDataList("Attached Mullions", attachedMullions.OfType<Types.Mullion>());

      // filter attached panels
      // panels can be a mix of DB.Panel and DB.FamilyInstance
      // no need to filter for .OfType<DB.Panel>() like with DB.Mullion
      // but make sure to remove all the DB.FamilyInstance that are actually DB.Mullion
      var dependentPanels = gridLine.GetDependentElements
      (
        CompoundElementFilter.ElementClassFilter(typeof(ARDB.FamilyInstance)).
        ThatExcludes(dependentMullionIds.ToArray())
      ).
        Select(x => Types.Panel.FromElementId(gridLine.Document, x));

      DA.SetDataList("Attached Panels", dependentPanels.OfType<Types.Panel>());
    }
  }
}
