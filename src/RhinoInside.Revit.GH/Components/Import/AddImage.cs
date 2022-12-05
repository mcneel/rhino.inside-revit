using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.11")]
  public class AddImage : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("506D5C19-5054-4428-A857-A4D7E8DB8AD8");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddImage() : base
    (
      name: "Add Image",
      nickname: "Image",
      description: "Given the point, it adds an image to the given View",
      category: "Revit",
      subCategory: "Element"
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
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to place the image",
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
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
#if !REVIT_2021
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{Name}' component is only supported on Revit 2021 or above.");
#endif
    }
    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

#if REVIT_2021
      ReconstructElement<ARDB.ImageInstance>
      (
        view.Document, _Output_, imgInstance =>
        {
          // Input
          if (!view.IsGraphicalView()) throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);
          if (!Params.GetData(DA, "Type", out ARDB.ImageType imageType)) return null;
          if (!Params.GetData(DA, "Point", out Point3d? pt)) return null;

          // Compute
          imgInstance = Reconstruct(imgInstance, view, GeometryEncoder.ToXYZ(pt.Value), imageType);

          DA.SetData(_Output_, imgInstance);
          return imgInstance;
        }
      );
#endif
    }

#if REVIT_2021
    bool Reuse(ARDB.ImageInstance img, ARDB.View view, ARDB.XYZ pt, ARDB.ImageType imageType)
    {
      if (img is null) return false;
      if (img.OwnerViewId != view.Id) return false;

      ARDB.ImageType currentImgType = view.Document.GetElement(img.GetTypeId()) as ARDB.ImageType;
      if (currentImgType.Id != imageType.Id) return false;

      if (img.GetLocation(ARDB.BoxPlacement.Center) != pt)
        img.SetLocation(pt, ARDB.BoxPlacement.Center);

      return true;
    }
#endif

#if REVIT_2021
    ARDB.ImageInstance Reconstruct(ARDB.ImageInstance img, ARDB.View view, ARDB.XYZ pt, ARDB.ImageType imageType)
    {
      if (!Reuse(img, view, pt, imageType))
      {
        img = ARDB.ImageInstance.Create(view.Document,
                                        view,
                                        imageType.Id,
                                        new ARDB.ImagePlacementOptions(pt, ARDB.BoxPlacement.Center));
      }
      return img;
    }
#endif
  }
}
