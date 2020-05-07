using System;
using System.Linq;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyzeInstanceSpace : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("6AC37380-D14F-46BF-835C-611DB8C38E3B");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "AIS";

    public AnalyzeInstanceSpace() : base(
      name: "Analyze Instance Space",
      nickname: "A-IS",
      description: "Analyze family instance space e.g. spatial elements surrounding the given instance",
      category: "Revit",
      subCategory: "Analyze"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.GraphicalElement(),
        name: "Family Instance",
        nickname: "FI",
        description: "Family Instance",
        access: GH_ParamAccess.item
        );
      // optional phase parameter to grab room/space data
      // TODO: replace with custom phase parameter (with picker like levels)
      manager[
        manager.AddParameter(
          param: new Parameters.Element(),
          name: "Phase",
          nickname: "PH",
          description: "Phase to query surrounding spatial elements from",
          access: GH_ParamAccess.item
          )].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.SpatialElement(),
        name: "FromRoom",
        nickname: "FR",
        description: "Room that given instance is originating from, if family instance supports from and to properties e.g. Doors, Windows",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.SpatialElement(),
        name: "ToRoom",
        nickname: "TR",
        description: "Room that given instance is ending at, if family instance supports from and to properties e.g. Doors, Windows",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.SpatialElement(),
        name: "Room",
        nickname: "R",
        description: "Room that spatially contains given instance",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.SpatialElement(),
        name: "Space",
        nickname: "S",
        description: "Space that spatially contains given instance",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input family instance
      DB.FamilyInstance famInst = default;
      if (!DA.GetData("Family Instance", ref famInst))
        return;

      // grab input phase if provided
      DB.Phase phase = default;
      DA.GetData("Phase", ref phase);

      if (phase is null)
      {
        DA.SetData("FromRoom", famInst.FromRoom);
        DA.SetData("ToRoom", famInst.ToRoom);
        DA.SetData("Room", famInst.Room);
        DA.SetData("Space", famInst.Space);
      }
      else
      {
        DA.SetData("FromRoom", famInst.get_FromRoom(phase));
        DA.SetData("ToRoom", famInst.get_ToRoom(phase));
        DA.SetData("Room", famInst.get_Room(phase));
        DA.SetData("Space", famInst.get_Space(phase));
      }
    }
  }
}
