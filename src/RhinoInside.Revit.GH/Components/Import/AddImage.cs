using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Import
{
  [ComponentVersion(introduced: "1.11")]
  public class AddImage : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("506D5C19-5054-4428-A857-A4D7E8DB8AD8");
#if REVIT_2020
    public override GH_Exposure Exposure => GH_Exposure.quinary;
#else
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.hidden;
    public override bool SDKCompliancy(int exeVersion, int exeServiceRelease) => false;
#endif
    protected override string IconTag => string.Empty;

    public AddImage() : base
    (
      name: "Add Image",
      nickname: "Image",
      description: "Given the point, it adds an image to the given View",
      category: "Revit",
      subCategory: "View"
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
          Description = "View to add a specific dimension"
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element Type for the image",
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_RasterImages,
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to place the image",
        }
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Rotation",
          NickName = "R",
          Description = "Base rotation",
          Optional = true,
          AngleParameter = true,
        }, ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Image";

    protected override void BeforeSolveInstance()
    {
#if !REVIT_2020
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{Name}' component is only supported on Revit 2020 or above.");
#endif
      base.BeforeSolveInstance();
    }
    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view)) return;

#if REVIT_2020
      ReconstructElement<ARDB.ImageInstance>
      (
        view.Document, _Output_, image =>
        {
          // Input
          if (!ARDB.ImageInstance.IsValidView(view.Value)) throw new Exceptions.RuntimeArgumentException("View", $"View '{view.Nomen}' does not support raster image creation", view);
          if (!Params.GetData(DA, "Type", out ARDB.ImageType type)) return null;
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.TryGetData(DA, "Rotation", out double? rotation)) return null;

          if (rotation.HasValue && Params.Input<Param_Number>("Rotation")?.UseDegrees == true)
            rotation = Rhino.RhinoMath.ToRadians(rotation.Value);

          var viewPlane = view.Location;
          if (view.Value.ViewType != ARDB.ViewType.ThreeD)
            point = viewPlane.ClosestPoint(point.Value);

          // Compute
          image = Reconstruct
          (
            image,
            view.Value,
            point.Value.ToXYZ(),
            rotation ?? 0.0,
            type
          );

          DA.SetData(_Output_, image);
          return image;
        }
      );
#endif
    }

#if REVIT_2020
    bool Reuse(ARDB.ImageInstance image, ARDB.View view, ARDB.XYZ point, ARDB.ImageType type)
    {
      if (image is null) return false;
      if (image.OwnerViewId != view.Id) return false;

      // Looks like images can't change its type.
      if (image.GetTypeId() != type.Id) return false;

      if (!image.GetLocation(ARDB.BoxPlacement.Center).AlmostEqualPoints(point))
        image.SetLocation(point, ARDB.BoxPlacement.Center);

      return true;
    }

    ARDB.ImageInstance Reconstruct(ARDB.ImageInstance image, ARDB.View view, ARDB.XYZ point, double rotation, ARDB.ImageType type)
    {
      if (!Reuse(image, view, point, type))
      {
        image = ARDB.ImageInstance.Create
        (
          view.Document,
          view,
          type.Id,
          new ARDB.ImagePlacementOptions(point, ARDB.BoxPlacement.Center)
        );
      }

      var baseDirection = image.GetLocation(ARDB.BoxPlacement.BottomRight) - image.GetLocation(ARDB.BoxPlacement.BottomLeft);
      var currentRotation = baseDirection.AngleOnPlaneTo(view.RightDirection, view.ViewDirection);
      if (!GeometryTolerance.Internal.AlmostEqualAngles(currentRotation, rotation))
      {
        var pinned = image.Pinned;
        image.Pinned = false;
        using (var axis = ARDB.Line.CreateUnbound(image.GetLocation(ARDB.BoxPlacement.Center), view.ViewDirection))
          ARDB.ElementTransformUtils.RotateElement(image.Document, image.Id, axis, rotation + currentRotation);
        image.Pinned = pinned;
      }

      return image;
    }
#endif
  }
}
