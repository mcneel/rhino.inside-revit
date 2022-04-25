using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  [ComponentVersion(introduced: "1.7")]
  public class AddSpaceTag : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F3EB3A21-CF8C-440D-A912-CFC84F204957");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public AddSpaceTag() : base
    (
      name: "Add Space Tag",
      nickname: "SpaceTag",
      description: "Given a point, it adds an space tag to the given view",
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
        new Parameters.SpaceElement()
        {
          Name = "Space",
          NickName = "S",
          Description = "Space to tag.",
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
          Description = "Space Tag type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_MEPSpaceTags
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
      if (!Params.GetData(DA, "Space", out ARDB.Mechanical.Space space)) return;

      ReconstructElement<ARDB.Mechanical.SpaceTag>
      (
        space.Document, _Tag_, (spaceTag) =>
        {
          // Input
          if (!Params.TryGetData(DA, "View", out ARDB.View view)) return null;
          if (!Params.TryGetData(DA, "Head Location", out Point3d? headLocation)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out ARDB.Mechanical.SpaceTagType type, Types.Document.FromValue(space.Document), ARDB.BuiltInCategory.OST_MEPSpaceTags)) return null;

          // Snap Point to the 'Space' 'Elevation'
          var source = (space.Location as ARDB.LocationPoint).Point;
          var target = headLocation?.ToXYZ();
          target = new ARDB.XYZ(target?.X ?? source.X, target?.Y ?? source.Y, source.Z);

          // Compute
          spaceTag = Reconstruct(spaceTag, view, space, target, type);

          DA.SetData(_Tag_, spaceTag);
          return spaceTag;
        }
      );
    }

    bool Reuse(ARDB.Mechanical.SpaceTag spaceTag, ARDB.View view, ARDB.XYZ point, ARDB.Mechanical.SpaceTagType type)
    {
      if (spaceTag is null) return false;
      if (view is object && !spaceTag.View.IsEquivalent(view)) return false;
      if (type.Id != spaceTag.GetTypeId()) spaceTag.ChangeTypeId(type.Id);
      if (spaceTag.Location is ARDB.LocationPoint areaTagLocation)
      {
        var target = point;
        var position = areaTagLocation.Point;
        if (!target.IsAlmostEqualTo(position))
        {
          var pinned = spaceTag.Pinned;
          spaceTag.Pinned = false;
          areaTagLocation.Move(target - position);
          spaceTag.Pinned = pinned;
        }
      }

      return true;
    }

    ARDB.Mechanical.SpaceTag Reconstruct(ARDB.Mechanical.SpaceTag spaceTag, ARDB.View view, ARDB.Mechanical.Space space, ARDB.XYZ headPosition, ARDB.Mechanical.SpaceTagType type)
    {
      var areaLocation = (space.Location as ARDB.LocationPoint).Point;
      if (!Reuse(spaceTag, view, areaLocation, type))
        spaceTag = space.Document.Create.NewSpaceTag
        (
          space,
          new ARDB.UV(areaLocation.X, areaLocation.Y),
          view
        );

      if (!spaceTag.TagHeadPosition.IsAlmostEqualTo(headPosition))
      {
        var pinned = spaceTag.Pinned;
        spaceTag.Pinned = false;
        spaceTag.TagHeadPosition = headPosition;
        spaceTag.Pinned = pinned;
      }

      return spaceTag;
    }
  }
}
