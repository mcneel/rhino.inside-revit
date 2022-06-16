using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.7", updated: "1.8")]
  public class AddAreaTag : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("FF951E5D-9316-4E68-8E19-86C8CCF9A3DF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddAreaTag() : base
    (
      name: "Tag Area",
      nickname: "TagArea",
      description: "Given a point, it adds an area tag to the given Area Plan",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.AreaPlan()
        {
          Name = "Area Plan",
          NickName = "AP",
          Description = "The Area Plan where the tag will be added.",
        }
      ),
      new ParamDefinition
      (
        new Parameters.AreaElement()
        {
          Name = "Area",
          NickName = "A",
          Description = "Area to tag.",
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
          Description = "Area Tag type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_AreaTags
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
      if (!Params.GetData(DA, "Area", out ARDB.Area area)) return;

      ReconstructElement<ARDB.AreaTag>
      (
        area.Document, _Tag_, areaTag =>
        {
          // Input
          if (!Params.TryGetData(DA, "Area Plan", out ARDB.ViewPlan viewPlan)) return null;
          if (!Params.TryGetData(DA, "Head Location", out Point3d? headLocation)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out ARDB.AreaTagType type, Types.Document.FromValue(area.Document), ARDB.BuiltInCategory.OST_AreaTags)) return null;

          // Snap Point to the 'Area' 'Elevation'
          var source = (area.Location as ARDB.LocationPoint).Point;
          var target = headLocation?.ToXYZ();
          target = new ARDB.XYZ(target?.X ?? source.X, target?.Y ?? source.Y, source.Z);

          // Compute
          areaTag = Reconstruct(areaTag, viewPlan, area, target, type);

          DA.SetData(_Tag_, areaTag);
          return areaTag;
        }
      );
    }

    bool Reuse(ARDB.AreaTag areaTag, ARDB.ViewPlan view, ARDB.XYZ point, ARDB.AreaTagType type)
    {
      if (areaTag is null) return false;
      if (view is object && !areaTag.View.IsEquivalent(view)) return false;
      if (areaTag.GetTypeId() != type.Id) areaTag.ChangeTypeId(type.Id);
      if (areaTag.Location is ARDB.LocationPoint areaTagLocation)
      {
        var target = point;
        var position = areaTagLocation.Point;
        if (!target.IsAlmostEqualTo(position))
        {
          var pinned = areaTag.Pinned;
          areaTag.Pinned = false;
          areaTagLocation.Move(target - position);
          areaTag.Pinned = pinned;
        }
      }

      return true;
    }

    ARDB.AreaTag Reconstruct(ARDB.AreaTag areaTag, ARDB.ViewPlan view, ARDB.Area area, ARDB.XYZ headPosition, ARDB.AreaTagType type)
    {
      var areaLocation = (area.Location as ARDB.LocationPoint).Point;
      if (!Reuse(areaTag, view, areaLocation, type))
        areaTag = area.Document.Create.NewAreaTag(view, area, new ARDB.UV(areaLocation.X, areaLocation.Y));

      if (!areaTag.TagHeadPosition.IsAlmostEqualTo(headPosition))
      {
        var pinned = areaTag.Pinned;
        areaTag.Pinned = false;
        areaTag.TagHeadPosition = headPosition;
        areaTag.Pinned = pinned;
      }

      return areaTag;
    }
  }
}
