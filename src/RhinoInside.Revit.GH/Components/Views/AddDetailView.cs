using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.12")]
  public class AddDetalView : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("8484E108-408A-4835-AC21-537D4FB121C8");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public AddDetalView() : base
    (
      name: "Add Detail View",
      nickname: "DetailView",
      description: "Given a name, it adds a section view to the active Revit document",
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
          Description = $"Section view camera frame.{Environment.NewLine}Plane, Rectangle and Box is also accepted.",
        }
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
          Description = "Section view type",
          Optional = true,
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.DetailView()
        {
          Name = "Template",
          NickName = "T",
          Description = $"Template section view (only parameters are copied)",
          Optional = true
        }, ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.DetailView()
        {
          Name = _View_,
          NickName = _View_.Substring(0, 1),
          Description = $"Output {_View_}",
        }
      )
    };

    protected const string _View_ = "View";

    public static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,

      ARDB.BuiltInParameter.VIEW_NAME,

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

      ReconstructElement<ARDB.ViewSection>
      (
        doc.Value, _View_, viewSection =>
        {
          // Input
          if (!Params.TryGetData(DA, "Frame", out Types.ViewFrame frame, x => x.IsValid)) return null;
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Parameters.ViewFamilyType.GetDataOrDefault(this, DA, "Type", out Types.ViewFamilyType type, doc, ARDB.ElementTypeGroup.ViewTypeDetailView)) return null;
          Params.TryGetData(DA, "Template", out ARDB.ViewSection template);

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_View_, out var untracked, ref viewSection, doc.Value, name, ARDB.ViewType.DraftingView.ToString()))
            viewSection = Reconstruct(viewSection, frame.ToBoundingBoxXYZ(), type.Value, name, template);

          DA.SetData(_View_, viewSection);
          return untracked ? null : viewSection;
        }
      );
    }

    bool Reuse(ARDB.ViewSection view, ARDB.ViewFamilyType type)
    {
      if (view is null) return false;
      if (type.Id != view.GetTypeId()) view.ChangeTypeId(type.Id);

      return true;
    }

    ARDB.ViewSection Create(ARDB.ViewFamilyType type)
    {
      var transform = ARDB.Transform.Identity;
      transform.Origin = new ARDB.XYZ(0.0, 1.0, 0.0);
      transform.BasisX = new ARDB.XYZ(1.0, 0.0, 0.0);
      transform.BasisY = new ARDB.XYZ(0.0, -1.0, 0.0);
      transform.BasisZ = transform.BasisX.CrossProduct(transform.BasisY);

      var box = new ARDB.BoundingBoxXYZ()
      {
        Transform = transform,
        Min = new ARDB.XYZ(0.0, 0.0, 0.0),
        Max = new ARDB.XYZ(1.0, 1.0, 1.0)
      };

      var view = ARDB.ViewSection.CreateDetail(type.Document, type.Id, box);
      view.Document.Regenerate();

      box.Min = new ARDB.XYZ(-100.0, -100.0, -1.0);
      box.Max = new ARDB.XYZ(+100.0, +100.0, 0.0);
      view.CropBox = box;

      return view;
    }

    ARDB.ViewSection Reconstruct(ARDB.ViewSection view, ARDB.BoundingBoxXYZ box, ARDB.ViewFamilyType type, string name, ARDB.ViewSection template)
    {
      var (min, max, transform, bounds) = box;
      using (var orientation = new ARDB.ViewOrientation3D(transform.Origin, transform.BasisY, -transform.BasisZ))
      {
        if (!Reuse(view, type))
          view = Create(type);

        view.SetSavedOrientation(orientation);
        view.CropBox = box;
        view.CropBoxVisible =
        view.CropBoxActive =
          bounds[BoundingBoxXYZExtension.AxisX, BoundingBoxXYZExtension.BoundsMin] ||
          bounds[BoundingBoxXYZExtension.AxisX, BoundingBoxXYZExtension.BoundsMax] ||
          bounds[BoundingBoxXYZExtension.AxisY, BoundingBoxXYZExtension.BoundsMin] ||
          bounds[BoundingBoxXYZExtension.AxisY, BoundingBoxXYZExtension.BoundsMax];

        view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING).Update(bounds[BoundingBoxXYZExtension.AxisZ, BoundingBoxXYZExtension.BoundsMin] ? 1 : 0);

        if (max.Z - min.Z < 0.02)
        {
          view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING).Update(1);
          view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_FAR).Update(-Math.Min(0.0, view.CropBox.Max.Z - 0.02));
          view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING).Update(0);
        }
        else
        {
          view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING).Update(1);
          view.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_OFFSET_FAR).Update(-min.Z);
        }

        view.CopyParametersFrom(template, ExcludeUniqueProperties);
        if (name is object) view?.get_Parameter(ARDB.BuiltInParameter.VIEW_NAME).Update(name);

        return view;
      }
    }
  }
}
