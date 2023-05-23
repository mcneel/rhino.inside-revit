using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.7")]
  public class AddRoomTag : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("3B95EFF0-6BB7-413D-8FBE-AB8895E804E2");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddRoomTag() : base
    (
      name: "Tag Room",
      nickname: "R-Tag",
      description: "Given a point, it adds an room tag to the given view",
      category: "Revit",
      subCategory: "Annotate"
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
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.RoomElement()
        {
          Name = "Room",
          NickName = "R",
          Description = "Room to tag.",
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
      if (!Params.GetData(DA, "Room", out Types.RoomElement room, x => x.IsValid)) return;

      ReconstructElement<ARDB.Architecture.RoomTag>
      (
        room.ReferenceDocument, _Tag_, roomTag =>
        {
          // Input
          if (!Params.TryGetData(DA, "View", out Types.View view, x => room.ReferenceDocument.IsEquivalent(x.Document))) return null;
          if (view is null && room.IsLinked)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "View cannot be null if Room is in an RVT Link.");
            return null;
          }
          if (!Params.TryGetData(DA, "Head Location", out Point3d? headLocation)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out ARDB.Architecture.RoomTagType type, Types.Document.FromValue(room.ReferenceDocument), ARDB.BuiltInCategory.OST_RoomTags)) return null;

          // Snap Point to the 'Room' 'Elevation'
          var target = (room.Value.Location as ARDB.LocationPoint).Point;
          target = room.GetReferenceTransform().OfPoint(target);

          var head = headLocation?.ToXYZ();
          head = new ARDB.XYZ(head?.X ?? target.X, head?.Y ?? target.Y, target.Z);

          // Compute
          roomTag = Reconstruct(roomTag, view?.Value, room.Value, room.GetReference(), target, head, type);

          DA.SetData(_Tag_, roomTag);
          return roomTag;
        }
      );
    }

    bool Reuse(ARDB.Architecture.RoomTag roomTag, ARDB.View view, ARDB.Architecture.Room room, ARDB.XYZ target, ARDB.Architecture.RoomTagType type)
    {
      if (roomTag is null) return false;
      if (view is object && !roomTag.View.IsEquivalent(view)) return false;
      if (!roomTag.Room.IsEquivalent(room)) return false;
      if (roomTag.GetTypeId() != type.Id) roomTag.ChangeTypeId(type.Id);
      if (roomTag.Location is ARDB.LocationPoint areaTagLocation)
      {
        var position = areaTagLocation.Point;
        if (!target.AlmostEqualPoints(position))
        {
          var pinned = roomTag.Pinned;
          roomTag.Pinned = false;
          areaTagLocation.Move(target - position);
          roomTag.Pinned = pinned;
        }
      }

      return true;
    }

    ARDB.Architecture.RoomTag Reconstruct
    (
      ARDB.Architecture.RoomTag roomTag,
      ARDB.View view,
      ARDB.Architecture.Room room,
      ARDB.Reference roomReference,
      ARDB.XYZ target,
      ARDB.XYZ head,
      ARDB.Architecture.RoomTagType type
    )
    {
      if (!Reuse(roomTag, view, room, target, type))
      {
        roomTag = type.Document.Create.NewRoomTag
        (
          roomReference.ToLinkElementId(),
          new ARDB.UV(target.X, target.Y),
          view?.Id
        );
        roomTag.ChangeTypeId(type.Id);
      }

      if (!roomTag.TagHeadPosition.AlmostEqualPoints(head))
      {
        var pinned = roomTag.Pinned;
        roomTag.Pinned = false;
        roomTag.TagHeadPosition = head;
        roomTag.Pinned = pinned;
      }

      return roomTag;
    }
  }
}
