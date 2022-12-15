using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.11")]
  public class AddImageType : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("09BD0AA8-52D7-4C2B-B7B1-C66304C939AF");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

    protected override string IconTag => string.Empty;

    public AddImageType() : base
    (
      name: "Add Image Type",
      nickname: "ImageType",
      description: "Given the path, it adds an image type to the given View",
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
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the image file",
          FileFilter =  @"All Image Files (*.bmp, *.jpg, *.jpeg, *.png, *.tif, *.tiff)|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|" +
                        @"Bitmap Files (*.bmp)|*.bmp|" +
                        @"JPEG Files (*.jpg, *.jpeg)|*.jpg;*.jpeg|" +
                        @"Portable Network Graphics (*.png)|*.png|" +
                        @"TIFF Files (*.tif, *.tiff)|*.tif;*.tiff"
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Type";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ImageType>
      (
        doc.Value, _Output_, type =>
        {
          // Input
          if (!Params.GetData(DA, "Path", out string path)) return null;

          // Compute
          type = Reconstruct(type, doc.Value, path);

          DA.SetData(_Output_, type);
          return type;
        }
      );
    }

#if REVIT_2020
    static ARDB.ImageTypeOptions CreateImageTypeOptions(string path)
    {
#if REVIT_2021
      return new ARDB.ImageTypeOptions(path, useRelativePath: false, ARDB.ImageTypeSource.Import);
#else
      return new ARDB.ImageTypeOptions(path, useRelativePath: false);
#endif
    }
#else
    static string CreateImageTypeOptions(string path) => path;
#endif

    bool Reuse(ARDB.ImageType imageType, ARDB.Document doc, string path)
    {
      if (imageType is null) return false;
      if (string.Compare(imageType.Path, path, StringComparison.OrdinalIgnoreCase) != 0)
        imageType.ReloadFrom(CreateImageTypeOptions(path));

      return true;
    }

    ARDB.ImageType Reconstruct(ARDB.ImageType imageType, ARDB.Document doc, string path)
    {
      if (!Reuse(imageType, doc, path))
        imageType = ARDB.ImageType.Create(doc, CreateImageTypeOptions(path));

      return imageType;
    }
  }
}
