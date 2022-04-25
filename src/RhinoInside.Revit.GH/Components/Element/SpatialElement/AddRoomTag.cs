using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.SpatialElement
{
  [ComponentVersion(introduced: "1.7")]
  public class AddRoomTag : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("3B95EFF0-6BB7-413D-8FBE-AB8895E804E2");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddRoomTag() : base
    (
      name: "Add Room Tag",
      nickname: "RoomTag",
      description: "Given a point, it adds an room tag to the given view",
      category: "Revit",
      subCategory: "Topology"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "The view where the tag will be added.",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.RoomElement()
        {
          Name = "Room",
          NickName = "R",
          Description = "Room to tag.",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Head Location",
          NickName = "HL",
          Description = "The location of the tag's head.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Type",
          NickName = "T",
          Description = "Room Tag type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_RoomTags
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Tag_,
          NickName = _Tag_.Substring(0, 1),
          Description = $"Output {_Tag_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Tag_ = "Tag";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Room", out ARDB.Architecture.Room room)) return;

      ReconstructElement<ARDB.Architecture.RoomTag>
      (
        room.Document, _Tag_, (roomTag) =>
        {
          // Input
          if (!Params.TryGetData(DA, "View", out ARDB.View view)) return null;
          if (!Params.TryGetData(DA, "Head Location", out Point3d? headLocation)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out ARDB.Architecture.RoomTagType type, Types.Document.FromValue(room.Document), ARDB.BuiltInCategory.OST_RoomTags)) return null;

          //if (viewPlan is null)
          //{
          //  using (var collector = new ARDB.FilteredElementCollector(room.Document).OfClass(typeof(ARDB.ViewPlan)))
          //    viewPlan = collector.Cast<ARDB.ViewPlan>().Where(x => x.AreaScheme.IsEquivalent(room.AreaScheme)).FirstOrDefault();
          //}

          // Snap Point to the 'Area' 'Elevation'
          var source = (room.Location as ARDB.LocationPoint).Point;
          var target = headLocation?.ToXYZ();
          target = new ARDB.XYZ(target?.X ?? source.X, target?.Y ?? source.Y, source.Z);

          // Compute
          roomTag = Reconstruct(roomTag, view, room, target, type);

          DA.SetData(_Tag_, roomTag);
          return roomTag;
        }
      );
    }

    bool Reuse(ARDB.Architecture.RoomTag roomTag, ARDB.View view, ARDB.XYZ point, ARDB.Architecture.RoomTagType type)
    {
      if (roomTag is null) return false;
      if (view is object && !roomTag.View.IsEquivalent(view)) return false;
      if (type.Id != roomTag.GetTypeId()) roomTag.ChangeTypeId(type.Id);
      if (roomTag.Location is ARDB.LocationPoint areaTagLocation)
      {
        var target = point;
        var position = areaTagLocation.Point;
        if (!target.IsAlmostEqualTo(position))
        {
          var pinned = roomTag.Pinned;
          roomTag.Pinned = false;
          areaTagLocation.Move(target - position);
          roomTag.Pinned = pinned;
        }
      }

      return true;
    }

    ARDB.Architecture.RoomTag Reconstruct(ARDB.Architecture.RoomTag roomTag, ARDB.View view, ARDB.Architecture.Room room, ARDB.XYZ headPosition, ARDB.Architecture.RoomTagType type)
    {
      var areaLocation = (room.Location as ARDB.LocationPoint).Point;
      if (!Reuse(roomTag, view, areaLocation, type))
        roomTag = room.Document.Create.NewRoomTag
        (
          new ARDB.LinkElementId(room.Id),
          new ARDB.UV(areaLocation.X, areaLocation.Y),
          view?.Id
        );

      if (!roomTag.TagHeadPosition.IsAlmostEqualTo(headPosition))
      {
        var pinned = roomTag.Pinned;
        roomTag.Pinned = false;
        roomTag.TagHeadPosition = headPosition;
        roomTag.Pinned = pinned;
      }

      return roomTag;
    }
  }
}
