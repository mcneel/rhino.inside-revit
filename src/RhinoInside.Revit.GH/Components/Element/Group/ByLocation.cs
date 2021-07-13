using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using System.Runtime.InteropServices;
  using Kernel.Attributes;

  public class GroupByLocation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("DF634530-634D-43F8-9C42-73F4A8D62C1E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public GroupByLocation() : base
    (
      name: "Add Model Group",
      nickname: "ModelGroup",
      description: "Given its location, it reconstructs a Model Group into the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    void ReconstructGroupByLocation
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New Group Element")]
      ref DB.Group group,

      [Description("Location where to place the group.")]
      Rhino.Geometry.Plane location,
      DB.GroupType type,
      Optional<DB.Level> level
    )
    {
      if (!location.IsValid)
        ThrowArgumentException(nameof(location), "Should be a valid plane.");

      SolveOptionalLevel(document, location.Origin, ref level, out var bbox);

      ChangeElementTypeId(ref group, type.Id);

      var newLocation = location.Origin.ToXYZ();
      if
      (
        group is DB.Group &&
        group.Location is DB.LocationPoint locationPoint &&
        locationPoint.Point.Z == newLocation.Z
      )
      {
        if (!newLocation.IsAlmostEqualTo(locationPoint.Point))
        {
          group.Pinned = false;
          locationPoint.Point = newLocation;
          group.Pinned = true;
        }
      }
      else
      {
        var newGroup = document.IsFamilyDocument ?
                       document.FamilyCreate.PlaceGroup(newLocation, type) :
                       document.Create.PlaceGroup(newLocation, type);

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.GROUP_LEVEL,
          DB.BuiltInParameter.GROUP_OFFSET_FROM_LEVEL,
        };

        ReplaceElement(ref group, newGroup, parametersMask);
      }

      if (group is DB.Group)
      {
        using (var levelParam = group.get_Parameter(DB.BuiltInParameter.GROUP_LEVEL))
        using (var offsetFromLevel = group.get_Parameter(DB.BuiltInParameter.GROUP_OFFSET_FROM_LEVEL))
        {
          var oldOffset = offsetFromLevel.AsDouble();
          var newOffset = newLocation.Z - level.Value.GetHeight();
          if (levelParam.AsElementId() != level.Value.Id || !Rhino.RhinoMath.EpsilonEquals(oldOffset, newOffset, Rhino.RhinoMath.SqrtEpsilon))
          {
            var groupType = group.GroupType;
            var oldGroups = new HashSet<DB.ElementId>(groupType.Groups.Cast<DB.Group>().Select(x => x.Id));

            levelParam.Set(level.Value.Id);
            offsetFromLevel.Set(newOffset);
            document.Regenerate();

            var newGroups = new HashSet<DB.ElementId>(groupType.Groups.Cast<DB.Group>().Select(x => x.Id));
            newGroups.ExceptWith(oldGroups);

            if(newGroups.FirstOrDefault() is DB.ElementId newGroupId)
              group = newGroupId.IsValid() ? document.GetElement(newGroupId) as DB.Group : default;
          }
        }

        if (Types.Element.FromElement(group) is Types.Group goo)
        {
          var pinned = goo.Pinned;
          goo.Pinned = false;
          goo.Location = location;
          goo.Pinned = pinned;
        }
      }
    }
  }
}
