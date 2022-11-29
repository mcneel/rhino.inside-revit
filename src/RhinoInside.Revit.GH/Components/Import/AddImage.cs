using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.10")]
  public class AddImage : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("506D5C19-5054-4428-A857-A4D7E8DB8AD8");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public AddImage() : base
    (
      name: "Add Image",
      nickname: "Image",
      description: "Given the path, it adds an image to the given View",
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
        new Param_String
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the image",
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

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.ImageInstance>
      (
        view.Document, _Output_, imgInstance =>
        {
          // Input
          if (!view.IsGraphicalView()) throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);
          if (!Params.GetData(DA, "Path", out string path)) return null;
          if (!Params.GetData(DA, "Point", out Point3d? pt)) return null;

          // Compute
          imgInstance = Reconstruct(imgInstance, view, GeometryEncoder.ToXYZ(pt.Value), path);

          DA.SetData(_Output_, imgInstance);
          return imgInstance;
        }
      );
    }

    bool Reuse(ARDB.ImageInstance img, ARDB.View view, ARDB.XYZ pt, string path)
    {
      if (img is null) return false;
      if (img.OwnerViewId != view.Id) return false;

      ARDB.ImageType imgType = view.Document.GetElement(img.GetTypeId()) as ARDB.ImageType;
      if (imgType.Path != path) return false;

      if (img.GetLocation(ARDB.BoxPlacement.Center) != pt)
        img.SetLocation(pt, ARDB.BoxPlacement.Center);

      return true;
    }

    ARDB.ImageInstance Reconstruct(ARDB.ImageInstance img, ARDB.View view, ARDB.XYZ pt, string path)
    {
      if (!Reuse(img, view, pt, path))
      {
        ARDB.ImageType imageType = ARDB.ImageType.Create(view.Document,
                                                         new ARDB.ImageTypeOptions(path, false, ARDB.ImageTypeSource.Import));
        img = ARDB.ImageInstance.Create(view.Document,
                                        view,
                                        imageType.Id,
                                        new ARDB.ImagePlacementOptions(pt, ARDB.BoxPlacement.Center));
      }
      return img;
    }
  }
}
