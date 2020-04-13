using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Kernel.Attributes;

  public class GroupByLocation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("DF634530-634D-43F8-9C42-73F4A8D62C1E");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public GroupByLocation() : base
    (
      "Add Model Group", "ModelGroup",
      "Given its location, it reconstructs a Model Group into the active Revit document",
      "Revit", "Model"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Group", "G", "New Group Element", GH_ParamAccess.item);
    }

    void ReconstructGroupByLocation
    (
      DB.Document doc,
      ref DB.Group element,

      [Description("Location where to place the group. Point or plane is accepted.")]
      Rhino.Geometry.Point3d location,
      DB.GroupType type,
      Optional<DB.Level> level
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;
      location = location.ChangeUnits(scaleFactor);

      if (!location.IsValid)
        ThrowArgumentException(nameof(location), "Should be a valid point.");

      SolveOptionalLevel(doc, location, ref level, out var bbox);

      ChangeElementTypeId(ref element, type.Id);

      if
      (
        element is DB.Group &&
        element.Location is DB.LocationPoint locationPoint &&
        locationPoint.Point.Z == location.Z
      )
      {
        var newOrigin = location.ToHost();
        if (!newOrigin.IsAlmostEqualTo(locationPoint.Point))
        {
          element.Pinned = false;
          locationPoint.Point = newOrigin;
          element.Pinned = true;
        }
      }
      else
      {
        var newGroup = doc.IsFamilyDocument ?
                       doc.FamilyCreate.PlaceGroup(location.ToHost(), type) :
                       doc.Create.PlaceGroup(location.ToHost(), type);

        var parametersMask = new DB.BuiltInParameter[]
        {
          DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
          DB.BuiltInParameter.ELEM_FAMILY_PARAM,
          DB.BuiltInParameter.ELEM_TYPE_PARAM,
          DB.BuiltInParameter.GROUP_LEVEL,
          DB.BuiltInParameter.GROUP_OFFSET_FROM_LEVEL,
        };

        ReplaceElement(ref element, newGroup, parametersMask);
      }

      if (element is DB.Group)
      {
        using (var levelParam = element.get_Parameter(DB.BuiltInParameter.GROUP_LEVEL))
        using (var offsetFromLevel = element.get_Parameter(DB.BuiltInParameter.GROUP_OFFSET_FROM_LEVEL))
        {
          var oldOffset = offsetFromLevel.AsDouble();
          var newOffset = location.Z - level.Value.Elevation;
          if (levelParam.AsElementId() != level.Value.Id || !Rhino.RhinoMath.EpsilonEquals(oldOffset, newOffset, Rhino.RhinoMath.SqrtEpsilon))
          {
            var groupType = element.GroupType;
            var oldGroups = new HashSet<DB.ElementId>(groupType.Groups.Cast<DB.Group>().Select(x => x.Id));

            levelParam.Set(level.Value.Id);
            offsetFromLevel.Set(newOffset);
            doc.Regenerate();

            var newGroups = new HashSet<DB.ElementId>(groupType.Groups.Cast<DB.Group>().Select(x => x.Id));
            newGroups.ExceptWith(oldGroups);

            if(newGroups.FirstOrDefault() is DB.ElementId newGroupId)
              element = newGroupId.IsValid() ? doc.GetElement(newGroupId) as DB.Group : default;
          }
        }
      }
    }
  }
}
