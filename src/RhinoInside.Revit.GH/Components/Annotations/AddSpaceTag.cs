using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.7")]
  public class AddSpaceTag : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F3EB3A21-CF8C-440D-A912-CFC84F204957");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddSpaceTag() : base
    (
      name: "Tag Space",
      nickname: "TagSpace",
      description: "Given a point, it adds an space tag to the given view",
      category: "Revit",
      subCategory: "Annotation"
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
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.SpaceElement()
        {
          Name = "Space",
          NickName = "S",
          Description = "Space to tag.",
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
      if (!Params.GetData(DA, "Space", out Types.SpaceElement space, x => x.IsValid)) return;

      ReconstructElement<ARDB.Mechanical.SpaceTag>
      (
        space.ReferenceDocument, _Tag_, spaceTag =>
        {
          if (space.IsLinked)
          {
            // I'm unable to found API to tag linked spaces.
            // So we trait linked spaces as invalid to tag.
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tags to linked Spaces are currently not supported in this Revit version.");
            return null;
          }

          // Input
          if (!Params.TryGetData(DA, "View", out Types.View view, x => space.ReferenceDocument.IsEquivalent(x.Document))) return null;
          if (view is null && space.IsLinked)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "View cannot be null if Space is in an RVT Link.");
            return null;
          }
          if (!Params.TryGetData(DA, "Head Location", out Point3d? headLocation)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out ARDB.Mechanical.SpaceTagType type, Types.Document.FromValue(space.ReferenceDocument), ARDB.BuiltInCategory.OST_MEPSpaceTags)) return null;

          // Snap Point to the 'Space' 'Elevation'
          var target = (space.Value.Location as ARDB.LocationPoint).Point;
          target = space.GetReferenceTransform().OfPoint(target);

          var head = headLocation?.ToXYZ();
          head = new ARDB.XYZ(head?.X ?? target.X, head?.Y ?? target.Y, target.Z);

          // Compute
          spaceTag = Reconstruct(spaceTag, view?.Value, space.Value, target, head, type);

          DA.SetData(_Tag_, spaceTag);
          return spaceTag;
        }
      );
    }

    bool Reuse(ARDB.Mechanical.SpaceTag spaceTag, ARDB.View view, ARDB.Mechanical.Space space, ARDB.XYZ target, ARDB.Mechanical.SpaceTagType type)
    {
      if (spaceTag is null) return false;
      if (view is object && !spaceTag.View.IsEquivalent(view)) return false;
      if (!spaceTag.Space.IsEquivalent(space)) return false;
      if (spaceTag.GetTypeId() != type.Id) spaceTag.ChangeTypeId(type.Id);
      if (spaceTag.Location is ARDB.LocationPoint areaTagLocation)
      {
        var position = areaTagLocation.Point;
        if (!target.AlmostEqualPoints(position))
        {
          var pinned = spaceTag.Pinned;
          spaceTag.Pinned = false;
          areaTagLocation.Move(target - position);
          spaceTag.Pinned = pinned;
        }
      }

      return true;
    }

    ARDB.Mechanical.SpaceTag Reconstruct
    (
      ARDB.Mechanical.SpaceTag spaceTag,
      ARDB.View view,
      ARDB.Mechanical.Space space,
      ARDB.XYZ target,
      ARDB.XYZ head,
      ARDB.Mechanical.SpaceTagType type
    )
    {
      if (!Reuse(spaceTag, view, space, target, type))
      {
        spaceTag = type.Document.Create.NewSpaceTag
        (
          space,
          new ARDB.UV(target.X, target.Y),
          view
        );
        spaceTag.ChangeTypeId(type.Id);
      }

      if (!spaceTag.TagHeadPosition.AlmostEqualPoints(head))
      {
        var pinned = spaceTag.Pinned;
        spaceTag.Pinned = false;
        spaceTag.TagHeadPosition = head;
        spaceTag.Pinned = pinned;
      }

      return spaceTag;
    }
  }
}
