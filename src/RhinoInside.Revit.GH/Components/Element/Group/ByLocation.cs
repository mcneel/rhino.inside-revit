using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;
  using External.DB.Extensions;
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
      ARDB.Document document,

      [Description("New Group Element")]
      ref ARDB.Group group,

      [Description("Location where to place the group.")]
      Rhino.Geometry.Plane location,
      ARDB.GroupType type,
      Optional<ARDB.Level> level
    )
    {
      if (!location.IsValid)
        ThrowArgumentException(nameof(location), "Should be a valid plane.");

      if (!type.Category.Id.TryGetBuiltInCategory(out var bic) || bic != ARDB.BuiltInCategory.OST_IOSModelGroups)
        ThrowArgumentException(nameof(type), $"'{type.Name}' is not a Model Group Type.");

      SolveOptionalLevel(document, location.Origin, ref level, out var bbox);

      ChangeElementTypeId(ref group, type.Id);

      var newLocation = location.Origin.ToXYZ();
      if
      (
        group is ARDB.Group &&
        group.Location is ARDB.LocationPoint locationPoint &&
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

        var parametersMask = new ARDB.BuiltInParameter[]
        {
          ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
          ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
          ARDB.BuiltInParameter.GROUP_LEVEL,
          ARDB.BuiltInParameter.GROUP_OFFSET_FROM_LEVEL,
        };

        ReplaceElement(ref group, newGroup, parametersMask);
      }

      if (group is ARDB.Group)
      {
        using (var levelParam = group.get_Parameter(ARDB.BuiltInParameter.GROUP_LEVEL))
        using (var offsetFromLevel = group.get_Parameter(ARDB.BuiltInParameter.GROUP_OFFSET_FROM_LEVEL))
        {
          var oldOffset = offsetFromLevel.AsDouble();
          var newOffset = newLocation.Z - level.Value.GetHeight();
          if (levelParam.AsElementId() != level.Value.Id || !Rhino.RhinoMath.EpsilonEquals(oldOffset, newOffset, Rhino.RhinoMath.SqrtEpsilon))
          {
            var groupType = group.GroupType;
            var oldGroups = new HashSet<ARDB.ElementId>(groupType.Groups.Cast<ARDB.Group>().Select(x => x.Id));

            levelParam.Set(level.Value.Id);
            offsetFromLevel.Set(newOffset);
            document.Regenerate();

            var newGroups = new HashSet<ARDB.ElementId>(groupType.Groups.Cast<ARDB.Group>().Select(x => x.Id));
            newGroups.ExceptWith(oldGroups);

            if(newGroups.FirstOrDefault() is ARDB.ElementId newGroupId)
              group = newGroupId.IsValid() ? document.GetElement(newGroupId) as ARDB.Group : default;
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
