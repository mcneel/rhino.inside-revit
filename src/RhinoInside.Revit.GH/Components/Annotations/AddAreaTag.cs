using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.7", updated: "1.8")]
  public class AddAreaTag : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("FF951E5D-9316-4E68-8E19-86C8CCF9A3DF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddAreaTag() : base
    (
      name: "Tag Area",
      nickname: "A-Tag",
      description: "Given a point, it adds an area tag to the given Area Plan",
      category: "Revit",
      subCategory: "Annotate"
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
        }, ParamRelevance.Primary
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
      if (!Params.GetData(DA, "Area", out Types.AreaElement area, x => x.IsValid)) return;

      ReconstructElement<ARDB.AreaTag>
      (
        area.ReferenceDocument, _Tag_, areaTag =>
        {
          if (area.IsLinked)
          {
            // I'm unable to found API to tag linked areas.
            // So we trait linked areas as invalid to tag.
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tags to linked Areas are currently not supported in this Revit version.");
            return null;
          }

          // Input
          if (!Params.TryGetData(DA, "Area Plan", out Types.AreaPlan view, x => area.ReferenceDocument.IsEquivalent(x.Document))) return null;
          if (view is null)
          {
            if (area.IsLinked)
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Area Plan cannot be null if Area is in an RVT Link.");
              return null;
            }
            else
            {
              using (var collector = new ARDB.FilteredElementCollector(area.ReferenceDocument).OfClass(typeof(ARDB.ViewPlan)))
              {
                using (var areaScheme = area.Value.AreaScheme)
                {
                  view = collector.Cast<ARDB.ViewPlan>().
                    Where(x => x.ViewType == ARDB.ViewType.AreaPlan && !x.IsTemplate && areaScheme.IsEquivalent(x.AreaScheme)).
                    Select(x => new Types.AreaPlan(x)).FirstOrDefault();

                  if (view is null)
                  {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No good default Area Plan its been found.");
                    return null;
                  }
                }
              }
            }
          }
          if (!Params.TryGetData(DA, "Head Location", out Point3d? headLocation)) return null;
          if (!Parameters.FamilySymbol.GetDataOrDefault(this, DA, "Type", out ARDB.AreaTagType type, Types.Document.FromValue(area.ReferenceDocument), ARDB.BuiltInCategory.OST_AreaTags)) return null;

          // Snap Point to the 'Area' 'Elevation'
          var target = (area.Value.Location as ARDB.LocationPoint).Point;
          target = area.GetReferenceTransform().OfPoint(target);

          var head = headLocation?.ToXYZ();
          head = new ARDB.XYZ(head?.X ?? target.X, head?.Y ?? target.Y, target.Z);

          // Compute
          areaTag = Reconstruct(areaTag, view?.Value, area.Value, target, head, type);

          DA.SetData(_Tag_, areaTag);
          return areaTag;
        }
      );
    }

    bool Reuse(ARDB.AreaTag areaTag, ARDB.ViewPlan view, ARDB.Area area, ARDB.XYZ target, ARDB.AreaTagType type)
    {
      if (areaTag is null) return false;
      if (view is object && !areaTag.View.IsEquivalent(view)) return false;
      if (!areaTag.Area.IsEquivalent(area)) return false;
      if (areaTag.GetTypeId() != type.Id) areaTag.ChangeTypeId(type.Id);
      if (areaTag.Location is ARDB.LocationPoint areaTagLocation)
      {
        var position = areaTagLocation.Point;
        if (!target.AlmostEqualPoints(position))
        {
          var pinned = areaTag.Pinned;
          areaTag.Pinned = false;
          areaTagLocation.Move(target - position);
          areaTag.Pinned = pinned;
        }
      }

      return true;
    }

    ARDB.AreaTag Reconstruct
    (
      ARDB.AreaTag areaTag,
      ARDB.ViewPlan view,
      ARDB.Area area,
      ARDB.XYZ target,
      ARDB.XYZ head,
      ARDB.AreaTagType type
    )
    {
      if (!Reuse(areaTag, view, area, target, type))
      {
        areaTag = type.Document.Create.NewAreaTag(view, area, new ARDB.UV(target.X, target.Y));
        areaTag.ChangeTypeId(type.Id);
      }

      if (!areaTag.TagHeadPosition.AlmostEqualPoints(head))
      {
        var pinned = areaTag.Pinned;
        areaTag.Pinned = false;
        areaTag.TagHeadPosition = head;
        areaTag.Pinned = pinned;
      }

      return areaTag;
    }
  }
}
