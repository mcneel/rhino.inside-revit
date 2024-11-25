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
  public abstract class AddElementTag : ElementTrackerComponent
  {
    protected AddElementTag(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
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
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to tag.",
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

    protected abstract ARDB.TagMode TagMode { get; }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.IndependentTag>
      (
        view.Document, _Tag_, independentTag =>
        {
          // Input
          if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return null;
          if (!Params.TryGetData(DA, "Head Location", out Point3d? headLocation)) return null;

          if (!headLocation.HasValue)
            headLocation = element.Location.Origin;

          // Compute
          var headPosition = headLocation.Value.ToXYZ();
          var leaderEnd = element.Location.Origin.ToXYZ();
          independentTag = Reconstruct
          (
            independentTag,
            view,
            element.GetReference(),
            headPosition,
            leaderEnd,
            !headPosition.AlmostEqualPoints(leaderEnd, view.Document.Application.ShortCurveTolerance),
            ARDB.TagOrientation.Horizontal
          );

          DA.SetData(_Tag_, independentTag);
          return independentTag;
        }
      );
    }

    bool Reuse
    (
      ARDB.IndependentTag independetTag,
      ARDB.View view,
      ARDB.Reference reference,
      ARDB.XYZ point
    )
    {
      if (independetTag is null) return false;
      if (independetTag.OwnerViewId != view.Id) return false;
      var taggedIds = independetTag.GetTaggedElementIds();
      if (taggedIds.Count != 1 || !taggedIds.Contains(reference.ToLinkElementId())) return false;
      if (independetTag.Location is ARDB.LocationPoint independentTagLocation)
      {
        var target = point;
        var position = independentTagLocation.Point;
        if (!target.AlmostEqualPoints(position))
        {
          var pinned = independetTag.Pinned;
          independetTag.Pinned = false;
          independentTagLocation.Move(target - position);
          independetTag.Pinned = pinned;
        }
      }

      return true;
    }

    ARDB.IndependentTag Create
    (
      ARDB.View view,
      ARDB.Reference reference,
      ARDB.XYZ point,
      bool leader,
      ARDB.TagOrientation orientation
    )
    {
      try
      {
#if REVIT_2018
        return ARDB.IndependentTag.Create(view.Document, view.Id, reference, leader, TagMode, orientation, point);
#else
        var element = view.Document.GetElement(reference.ElementId, reference.LinkedElementId);
        return view.Document.Create.NewTag(view, element, leader, TagMode, orientation, point);
#endif
      }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException)
      {
        if (view.Document.GetElement(reference.ElementId, reference.LinkedElementId) is ARDB.Element element)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"There is no tag loaded for '{element.Category.Name}'.");
          return null;
        }

        throw;
      }
    }

    ARDB.IndependentTag Reconstruct
    (
      ARDB.IndependentTag independentTag,
      ARDB.View view,
      ARDB.Reference reference,
      ARDB.XYZ headPosition,
      ARDB.XYZ leaderEnd,
      bool leader,
      ARDB.TagOrientation orientation
    )
    {
      if (!Reuse(independentTag, view, reference, headPosition))
        independentTag = Create(view, reference, headPosition, leader, orientation);

      if (independentTag is object)
      {
        independentTag.get_Parameter(ARDB.BuiltInParameter.LEADER_LINE).Update(leader);

        if (view.ViewType == ARDB.ViewType.ThreeD)
        {
          independentTag.get_Parameter(ARDB.BuiltInParameter.TAG_LEADER_TYPE).Update(1);
          if (independentTag.GetTaggedReferences().FirstOrDefault() is ARDB.Reference referenceTagged)
          {
            if (!independentTag.GetLeaderEnd(referenceTagged).AlmostEqualPoints(leaderEnd))
              independentTag.SetLeaderEnd(referenceTagged, leaderEnd);
          }
        }

        if (!independentTag.TagHeadPosition.AlmostEqualPoints(headPosition))
        {
          var pinned = independentTag.Pinned;
          independentTag.Pinned = false;
          independentTag.TagHeadPosition = headPosition;
          independentTag.Pinned = pinned;
        }
      }

      return independentTag;
    }
  }

  [ComponentVersion(introduced: "1.8")]
  public class AddElementTagByCategory : AddElementTag
  {
    public override Guid ComponentGuid => new Guid("689D4059-7371-424B-91E5-298732C5E387");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    protected override ARDB.TagMode TagMode => ARDB.TagMode.TM_ADDBY_CATEGORY;

    public AddElementTagByCategory() : base
    (
      name: "Tag By Category",
      nickname: "C-Tag",
      description: "Given a point, it adds a category tag to the given View",
      category: "Revit",
      subCategory: "Annotate"
    )
    { }
  }

  [ComponentVersion(introduced: "1.8")]
  public class AddElementTagByMutliCategory : AddElementTag
  {
    public override Guid ComponentGuid => new Guid("E6E4A2EE-E48E-4630-91FD-D268EAD1FDDF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    protected override ARDB.TagMode TagMode => ARDB.TagMode.TM_ADDBY_MULTICATEGORY;

    public AddElementTagByMutliCategory() : base
    (
      name: "Multi-Category Tag",
      nickname: "MC-Tag",
      description: "Given a point, it adds a multi-category tag to the given View",
      category: "Revit",
      subCategory: "Annotate"
    )
    { }
  }

  [ComponentVersion(introduced: "1.8")]
  public class AddElementTagByMaterial : AddElementTag
  {
    public override Guid ComponentGuid => new Guid("11424062-B5DA-4BA9-8B98-4886829EC67F");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    protected override ARDB.TagMode TagMode => ARDB.TagMode.TM_ADDBY_MATERIAL;

    public AddElementTagByMaterial() : base
    (
      name: "Material Tag",
      nickname: "M-Tag",
      description: "Given a point, it adds a material tag to the given View",
      category: "Revit",
      subCategory: "Annotate"
    )
    { }
  }
}
