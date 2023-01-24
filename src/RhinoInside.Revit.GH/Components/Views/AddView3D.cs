using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using External.DB.Extensions;
  using Grasshopper.Kernel.Parameters;

  [ComponentVersion(introduced: "1.0", updated: "1.12")]
  public class AddView3D : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F7B775C9-05E0-40F7-85E9-5CC2EF79731E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddView3D() : base
    (
      name: "Add 3D View",
      nickname: "3D View",
      description: "Given a camera frame, it adds a 3D View to the active Revit document",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.ViewFrame
        {
          Name = "Frame",
          NickName = "F",
          Description = $"View camera frame.{Environment.NewLine}Plane, Rectangle and Box is also accepted.",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Perspective",
          NickName = "P",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Name",
          NickName = "N",
          Description = "View name",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.ViewFamilyType()
        {
          Name = "Type",
          NickName = "T",
          Description = "3D view type",
          Optional = true,
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.View3D()
        {
          Name = "Template",
          NickName = "T",
          Description = $"Template 3D view (only parameters are copied)",
          Optional = true
        }, ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.View3D()
        {
          Name = _View_,
          NickName = _View_.Substring(0, 1),
          Description = $"Output {_View_}",
        }
      )
    };

    public override void AddedToDocument(GH_Document document)
    {
      // V 1.12
      if (Params.Input<IGH_Param>("Plane") is IGH_Param plane) plane.Name = "Frame";
      if (Params.Output<IGH_Param>("View3D") is IGH_Param view3D) view3D.Name = "View";

      base.AddedToDocument(document);
    }

    protected const string _View_ = "View";

    public static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,

      ARDB.BuiltInParameter.VIEW_NAME,
      ARDB.BuiltInParameter.VIEWER_PERSPECTIVE,

      ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_NEAR,
      ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_FAR,
      ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_BOTTOM,
      ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_TOP,
      ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_LEFT,
      ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_RIGHT,

      ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_NEAR,
      ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR,
      ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_BOTTOM,
      ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_TOP,
      ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_LEFT,
      ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_RIGHT,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.View3D>
      (
        doc.Value, _View_, view =>
        {
          // Input
          if (!Params.TryGetData(DA, "Frame", out Types.ViewFrame frame, x => x.IsValid)) return null;
          if (!Params.TryGetData(DA, "Perspective", out bool? perspective)) return null;
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Parameters.ViewFamilyType.GetDataOrDefault(this, DA, "Type", out Types.ViewFamilyType type, doc, ARDB.ElementTypeGroup.ViewType3D)) return null;
          Params.TryGetData(DA, "Template", out ARDB.View3D template);

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_View_, out var untracked, ref view, doc.Value, name, ARDB.ViewType.DraftingView.ToString()))
            view = Reconstruct(view, frame.ToBoundingBoxXYZ(), perspective ?? frame?.Value?.IsParallelProjection is false, type.Value, name, template);

          DA.SetData(_View_, view);
          return untracked ? null : view;
        }
      );
    }

    bool Reuse(ARDB.View3D view, bool perspective, ARDB.ViewFamilyType type)
    {
      if (view is null) return false;
      if (type.Id != view.GetTypeId()) view.ChangeTypeId(type.Id);
      view.get_Parameter(ARDB.BuiltInParameter.VIEWER_PERSPECTIVE).Update(perspective);

      return true;
    }

    ARDB.View3D Create(ARDB.ViewFamilyType type, bool perspective)
    {
      return perspective ?
        ARDB.View3D.CreatePerspective(type.Document, type.Id) :
        ARDB.View3D.CreateIsometric(type.Document, type.Id);
    }

    ARDB.View3D Reconstruct(ARDB.View3D view, ARDB.BoundingBoxXYZ box, bool perspective, ARDB.ViewFamilyType type, string name, ARDB.View3D template)
    {
      var (min, max, transform, bounds) = box;
      using (var orientation = new ARDB.ViewOrientation3D(transform.Origin, transform.BasisY, -transform.BasisZ))
      {
        if (!Reuse(view, perspective, type))
          view = Create(type, perspective);

        if (view.IsLocked) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "View is locked and cannot be reoriented.");
        else view.SetSavedOrientation(orientation);

        view.CropBox = box;
        view.CropBoxVisible =
        view.CropBoxActive =
          bounds[BoundingBoxXYZExtension.AxisX, BoundingBoxXYZExtension.BoundsMin] ||
          bounds[BoundingBoxXYZExtension.AxisX, BoundingBoxXYZExtension.BoundsMax] ||
          bounds[BoundingBoxXYZExtension.AxisY, BoundingBoxXYZExtension.BoundsMin] ||
          bounds[BoundingBoxXYZExtension.AxisY, BoundingBoxXYZExtension.BoundsMax];

        view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR).Update(true);
        view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_FAR).Update(-min.Z);

        view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR).Update(bounds[BoundingBoxXYZExtension.AxisZ, BoundingBoxXYZExtension.BoundsMin]);
        view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_NEAR).Update(bounds[BoundingBoxXYZExtension.AxisZ, BoundingBoxXYZExtension.BoundsMax]);

        view.CopyParametersFrom(template, ExcludeUniqueProperties);
        if (name is object) view?.get_Parameter(ARDB.BuiltInParameter.VIEW_NAME).Update(name);

        return view;
      }
    }
  }
}
