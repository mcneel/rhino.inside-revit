using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Groups
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced:"1.0", updated:"1.37")]
  public class GroupByLocation : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("DF634530-634D-43F8-9C42-73F4A8D62C1E");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public GroupByLocation() : base
    (
      name: "Add Model Group",
      nickname: "M-Group",
      description: "Given its location, it reconstructs a Model Group into the active Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(Parameters.Document.AddTarget(), ParamRelevance.Occasional),
      new ParamDefinition
       (
        new Param_Plane
        {
          Name = "Location",
          NickName = "L",
          Description = "Location where to place the group.",
        }
      ),
      new ParamDefinition
       (
        new Parameters.ElementType
        {
          Name = "Type",
          NickName = "T",
          Description = "Model group type",
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_IOSModelGroups
        }
      ),
      new ParamDefinition
      (
        new Parameters.LevelConstraint
        {
          Name = "Base",
          NickName = "BA",
          Description = $"Base of the {_Group_.ToLowerInvariant()}.{OS.NewLine}This input accepts a 'Level Constraint', an 'Elevation' or a 'Number' as an offset from the 'Location'.",
          Optional = true,
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Group()
        {
          Name = _Group_,
          NickName = _Group_.Substring(0, 1),
          Description = $"Output {_Group_}",
        }
      )
    };

    const string _Group_ = "Group";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.GROUP_LEVEL,
      ARDB.BuiltInParameter.GROUP_OFFSET_FROM_LEVEL,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Group>
      (
        doc.Value, _Group_, group =>
        {
          // Input
          if (!Params.GetData(DA, "Location", out Plane? location, x => x.IsValid)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.ElementType type, doc, ARDB.ElementTypeGroup.ModelGroupType)) return null;
          if (!Params.TryGetData(DA, "Base", out ERDB.ElevationElementReference? baseElevation)) return null;

          if (type.Value.Category.ToBuiltInCategory() != ARDB.BuiltInCategory.OST_IOSModelGroups)
            throw new Exceptions.RuntimeArgumentException(nameof(type), $"'{type.DisplayName}' is not a Model Group Type.");

          ERDB.ElevationElementReference.SolveBase
          (
            doc.Value, location.Value.OriginZ / Revit.ModelUnits,
            0.0,
            ref baseElevation
          );

          // Compute
          group = Reconstruct
          (
            group,
            doc.Value,
            location.Value,
            type.Value as ARDB.GroupType,
            baseElevation.Value
          );

          DA.SetData(_Group_, group);
          return group;
        }
      );
    }

    bool Reuse(ref ARDB.Group group, ARDB.XYZ point, ARDB.GroupType type)
    {
      if (group is null) return false;

      if (group.GetTypeId() != type.Id)
      {
        if (!ARDB.Element.IsValidType(group.Document, new ARDB.ElementId[] { group.Id }, type.Id))
          return false;

        group.GroupType = type;
      }

      return true;
    }

    ARDB.Group Create(ARDB.Document document, ARDB.XYZ point, ARDB.GroupType type)
    {
      using (var create = document.Create())
        return create.PlaceGroup(point, type);
    }

    ARDB.Group Reconstruct(ARDB.Group group, ARDB.Document doc, Plane location, ARDB.GroupType type, ERDB.ElevationElementReference baseElevation)
    {
      if (!baseElevation.IsLevelConstraint(out var level, out var newOffset))
        return null;

      var origin = location.Origin.ToXYZ();
      origin = new ARDB.XYZ(origin.X, origin.Y, level.GetElevation() + newOffset.Value);

      if (!Reuse(ref group, origin, type))
      {
        group = group.ReplaceElement
        (
          Create(doc, origin, type),
          ExcludeUniqueProperties
        );
      }

      if (group is ARDB.Group)
      {
        using (var levelParam = group.get_Parameter(ARDB.BuiltInParameter.GROUP_LEVEL))
        using (var offsetFromLevel = group.get_Parameter(ARDB.BuiltInParameter.GROUP_OFFSET_FROM_LEVEL))
        {
          var oldOffset = offsetFromLevel.AsDouble();
          if (levelParam.AsElementId() != level.Id || !Rhino.RhinoMath.EpsilonEquals(oldOffset, newOffset.Value, Rhino.RhinoMath.SqrtEpsilon))
          {
            var groupType = group.GroupType;
            var oldGroups = new HashSet<ARDB.ElementId>(groupType.Groups.Cast<ARDB.Group>().Select(x => x.Id));

            levelParam.Set(level.Id);
            offsetFromLevel.Set(newOffset.Value);
            doc.Regenerate();

            var newGroups = new HashSet<ARDB.ElementId>(groupType.Groups.Cast<ARDB.Group>().Select(x => x.Id));
            newGroups.ExceptWith(oldGroups);

            if (newGroups.FirstOrDefault() is ARDB.ElementId newGroupId)
              group = newGroupId.IsValid() ? doc.GetElement(newGroupId) as ARDB.Group : default;
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

      return group;
    }
  }
}
