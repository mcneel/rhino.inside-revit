using System;
using System.Linq;
using System.Drawing.Imaging;
using System.IO;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ElementTypes
{
  using External.DB.Extensions;

  public class ElementTypeExportImage : Component
  {
    public override Guid ComponentGuid => new Guid("3A5F6AF7-5449-406F-B47E-8A55800D0BEE");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "IMG";

    public ElementTypeExportImage() : base
    (
      name: "Export Type Image",
      nickname: "TypImage",
      description: "Exports a ElementType preview into an image file",
      category: "Revit",
      subCategory: "Type"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "ElementType to query for its prview", GH_ParamAccess.item);

      var folderPath = new Grasshopper.Kernel.Parameters.Param_FilePath();
      manager[manager.AddParameter(folderPath, "Folder", "F", $"Default is {Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}\\%ProjectTitle%", GH_ParamAccess.item)].Optional = true;

      manager.AddBooleanParameter("Overwrite", "O", "Overwrite file", GH_ParamAccess.item, false);

      var fileType = new Parameters.Param_Enum<Types.ImageFileType>();
      fileType.PersistentData.Append(new Types.ImageFileType(ARDB.ImageFileType.PNG));
      manager.AddParameter(fileType, "File Type", "FT", "The file type used for export", GH_ParamAccess.item);

      var imageResolution = new Parameters.Param_Enum<Types.ImageResolution>();
      imageResolution.PersistentData.Append(new Types.ImageResolution());
      manager.AddParameter(imageResolution, "Resolution", "R", "The image resolution in dots per inch", GH_ParamAccess.item);

      manager.AddIntegerParameter("Pixel Size X", "PSX", "The pixel size of an image in X direction", GH_ParamAccess.item, 256);
      manager.AddIntegerParameter("Pixel Size Y", "PSY", "The pixel size of an image in Y direction", GH_ParamAccess.item, 256);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      var imageFilePath = new Grasshopper.Kernel.Parameters.Param_FilePath();
      manager.AddParameter(imageFilePath, "Image File", "I", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementType = default(ARDB.ElementType);
      if (!DA.GetData("Type", ref elementType))
        return;

      var folder = default(string);
      DA.GetData("Folder", ref folder);

      if (string.IsNullOrEmpty(folder))
        folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), elementType.Document.GetTitle());

      Directory.CreateDirectory(folder);

      var overrideFile = default(bool);
      if (!DA.GetData("Overwrite", ref overrideFile))
        return;

      var fileType = default(ARDB.ImageFileType);
      if (!DA.GetData("File Type", ref fileType))
        return;

      var resolution = default(ARDB.ImageResolution);
      if (!DA.GetData("Resolution", ref resolution))
        return;

      var pixelSizeX = default(int);
      if (!DA.GetData("Pixel Size X", ref pixelSizeX))
        return;

      var pixelSizeY = default(int);
      if (!DA.GetData("Pixel Size Y", ref pixelSizeY))
        return;

      var size = new System.Drawing.Size(pixelSizeX, pixelSizeY);

      var elementTypeName = $"{elementType.Category?.Name} - {elementType.FamilyName} - {elementType.Name}";

      var filePath = Path.Combine(folder, elementTypeName);

      if (!overrideFile && File.Exists(filePath))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"File '{filePath}' already exists.");
      }
      else if (elementType.GetPreviewImage(size) is System.Drawing.Bitmap bitmap)
      {
        switch (resolution)
        {
          case ARDB.ImageResolution.DPI_72:  bitmap.SetResolution(72,  72); break;
          case ARDB.ImageResolution.DPI_150: bitmap.SetResolution(150, 150); break;
          case ARDB.ImageResolution.DPI_300: bitmap.SetResolution(300, 300); break;
          case ARDB.ImageResolution.DPI_600: bitmap.SetResolution(600, 600); break;
        }

        switch (fileType)
        {
          case ARDB.ImageFileType.BMP:
            filePath += ".bmp";
            bitmap.Save(filePath, ImageFormat.Bmp);
            break;
          case ARDB.ImageFileType.JPEGLossless:
          {
            filePath += ".jpg";
            var codec = ImageCodecInfo.GetImageDecoders().Where(x => x.FormatID == ImageFormat.Jpeg.Guid).FirstOrDefault();
            using (var parameters = new EncoderParameters(1))
            {
              parameters.Param[0] = new EncoderParameter(Encoder.Quality, 100);
              bitmap.Save(filePath, codec, parameters);
            }
          }
          break;
          case ARDB.ImageFileType.JPEGMedium:
          {
            filePath += ".jpg";
            var codec = ImageCodecInfo.GetImageDecoders().Where(x => x.FormatID == ImageFormat.Jpeg.Guid).FirstOrDefault();
            using (var parameters = new EncoderParameters(1))
            {
              parameters.Param[0] = new EncoderParameter(Encoder.Quality, 50);
              bitmap.Save(filePath, codec, parameters);
            }
          }
          break;
          case ARDB.ImageFileType.JPEGSmallest:
          {
            filePath += ".jpg";
            var codec = ImageCodecInfo.GetImageDecoders().Where(x => x.FormatID == ImageFormat.Jpeg.Guid).FirstOrDefault();
            using (var parameters = new EncoderParameters(1))
            {
              parameters.Param[0] = new EncoderParameter(Encoder.Quality, 1);
              bitmap.Save(filePath, codec, parameters);
            }
          }
          break;
          case ARDB.ImageFileType.PNG:
            filePath += ".png";
            bitmap.Save(filePath, ImageFormat.Png);
            break;
          case ARDB.ImageFileType.TARGA:
            filePath += ".tga";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unsuported file format.");
            filePath = string.Empty;
            break;
          case ARDB.ImageFileType.TIFF:
            filePath += ".tif";
            bitmap.Save(filePath, ImageFormat.Tiff);
            break;
        }
      }
      else
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to export image for '{elementType.Name}' type.");
        filePath = default;
      }

      DA.SetData("Image File", filePath);
    }
  }
}
