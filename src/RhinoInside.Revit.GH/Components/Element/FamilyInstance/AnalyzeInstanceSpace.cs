using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.SpatialElements
{
  public class AnalyzeInstanceSpace : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("6AC37380-D14F-46BF-835C-611DB8C38E3B");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AnalyzeInstanceSpace() : base
    (
      name: "Analyze Instance Space",
      nickname: "A-IS",
      description: "Analyze spatial elements surrounding the given instance",
      category: "Revit",
      subCategory: "Room & Area"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.FamilyInstance(),
        name: "Component",
        nickname: "C",
        description: "Component Instance",
        access: GH_ParamAccess.item
      );
      // optional phase parameter to grab room/space data
      manager
      [
        manager.AddParameter
        (
          param: new Parameters.Phase(),
          name: "Phase",
          nickname: "P",
          description: "Phase to query surrounding spatial elements from",
          access: GH_ParamAccess.item
        )
      ].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.SpatialElement(),
        name: "FromRoom",
        nickname: "FR",
        description: "Room that given instance is originating from, if family instance supports from and to properties e.g. Doors, Windows",
        access: GH_ParamAccess.item
      );
      manager.AddParameter
      (
        param: new Parameters.SpatialElement(),
        name: "ToRoom",
        nickname: "TR",
        description: "Room that given instance is ending at, if family instance supports from and to properties e.g. Doors, Windows",
        access: GH_ParamAccess.item
      );
      manager.AddParameter
      (
        param: new Parameters.SpatialElement(),
        name: "Room",
        nickname: "R",
        description: "Room that spatially contains given instance",
        access: GH_ParamAccess.item
      );
      manager.AddParameter
      (
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
      if (!Params.GetData(DA, "Component", out Types.FamilyInstance instance, x => x.IsValid))
        return;

      // grab input phase if provided
      if (!Params.TryGetData(DA, "Phase", out Types.Phase phase, x => x.IsValid))
        return;

      if (phase is null)
      {
        DA.SetData("FromRoom", instance.Value.FromRoom);
        DA.SetData("ToRoom", instance.Value.ToRoom);
        DA.SetData("Room", instance.Value.Room);
        DA.SetData("Space", instance.Value.Space);
      }
      else
      {
        DA.SetData("FromRoom", instance.Value.get_FromRoom(phase.Value));
        DA.SetData("ToRoom", instance.Value.get_ToRoom(phase.Value));
        DA.SetData("Room", instance.Value.get_Room(phase.Value));
        DA.SetData("Space", instance.Value.get_Space(phase.Value));
      }
    }
  }
}
