using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.12")]
  public class ImageTypePath : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("D4873F18-3B73-4E5C-8C34-0DF7D32BE127");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => string.Empty;

    public ImageTypePath() : base
    (
      name: "Image Type Path",
      nickname: "ImageType",
      description: "Get-Set accessor for image type file path",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Image Type",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Reload",
          NickName = "R",
          Description = "Reload",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the image file",
          Optional = true,
          FileFilter =  @"All Image Files (*.bmp, *.jpg, *.jpeg, *.png, *.tif, *.tiff)|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|" +
                        @"Bitmap Files (*.bmp)|*.bmp|" +
                        @"JPEG Files (*.jpg, *.jpeg)|*.jpg;*.jpeg|" +
                        @"Portable Network Graphics (*.png)|*.png|" +
                        @"TIFF Files (*.tif, *.tiff)|*.tif;*.tiff"
        }, ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Image Type",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Can Reload",
          NickName = "CR",
          Description = "Whether or not the Image Type can be reloaded from file",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Path",
          NickName = "P",
          Description = "Path to the image file",
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Type", out ARDB.ImageType type, x => x is object)) return;
      else Params.TrySetData(DA, "Type", () => type);

      Params.TryGetData(DA, "Path", out string path);

      if (Params.TryGetData(DA, "Reload", out bool? reload) && reload is true)
      {
        StartTransaction(type.Document);
        if (path is null) type.Reload();
        else type.ReloadFrom(CreateImageTypeOptions(path));
      }

      Params.TrySetData(DA, "Path", () => type.Path);
      Params.TrySetData(DA, "Can Reload", () => type.CanReload());
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
  }
}
