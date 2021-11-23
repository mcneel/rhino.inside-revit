using System;
using System.IO;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using External.DB.Extensions;

  public class ViewExportImage : Component
  {
    public override Guid ComponentGuid => new Guid("4A962A0C-46A0-4A5F-B727-6747B715A975");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "IMG";

    public ViewExportImage() : base
    (
      "Export View Image", "ViewImage",
      "Exports a view into an image file",
      "Revit", "View"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.View(), "View", "V", string.Empty, GH_ParamAccess.item);

      var folderPath = new Grasshopper.Kernel.Parameters.Param_FilePath();
      manager[manager.AddParameter(folderPath, "Folder", "F", $"Default is {Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}\\%ProjectTitle%", GH_ParamAccess.item)].Optional = true;

      manager.AddBooleanParameter("Overwrite", "O", "Overwrite file", GH_ParamAccess.item, false);

      var fileType = new Parameters.Param_Enum<Types.ImageFileType>();
      fileType.PersistentData.Append(new Types.ImageFileType(ARDB.ImageFileType.PNG));
      manager.AddParameter(fileType, "File Type", "FT", "The file type used for export", GH_ParamAccess.item);

      var imageResolution = new Parameters.Param_Enum<Types.ImageResolution>();
      imageResolution.PersistentData.Append(new Types.ImageResolution());
      manager.AddParameter(imageResolution, "Resolution", "R", "The image resolution in dots per inch", GH_ParamAccess.item);

      var fitDirectionType = new Parameters.Param_Enum<Types.FitDirectionType>();
      fitDirectionType.PersistentData.Append(new Types.FitDirectionType());
      manager.AddParameter(fitDirectionType, "Fit Direction", "D", "The fit direction", GH_ParamAccess.item);

      manager.AddIntegerParameter("Pixel Size", "PS", "The pixel size of an image in specified direction", GH_ParamAccess.item, 1024);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      var imageFilePath = new Grasshopper.Kernel.Parameters.Param_FilePath();
      manager.AddParameter(imageFilePath, "Image File", "I", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(ARDB.View);
      if (!DA.GetData("View", ref view))
        return;

      var folder = default(string);
      DA.GetData("Folder", ref folder);

      if (string.IsNullOrEmpty(folder))
        folder = Path.Combine(Core.SwapFolder,view.Document.GetFingerprintGUID().ToString(), InstanceGuid.ToString());

      Directory.CreateDirectory(folder);

      var overwrite = default(bool);
      if (!DA.GetData("Overwrite", ref overwrite))
        return;

      var fileType = default(ARDB.ImageFileType);
      if (!DA.GetData("File Type", ref fileType))
        return;

      var resolution = default(ARDB.ImageResolution);
      if (!DA.GetData("Resolution", ref resolution))
        return;

      var fitDirection = default(ARDB.FitDirectionType);
      if (!DA.GetData("Fit Direction", ref fitDirection))
        return;

      var pixelSize = default(int);
      if (!DA.GetData("Pixel Size", ref pixelSize))
        return;

      using (var uiDoc = new Autodesk.Revit.UI.UIDocument(view.Document))
      {
        var selectedIds = uiDoc.Selection.GetElementIds();
        if (selectedIds.Count > 0)
          uiDoc.Selection.SetElementIds(new ARDB.ElementId[] { });

        try
        {
          var viewName = ARDB.ImageExportOptions.GetFileName(view.Document, view.Id);
          var options = new ARDB.ImageExportOptions()
          {
            ZoomType = ARDB.ZoomFitType.FitToPage,
            FitDirection = fitDirection, // DB.FitDirectionType.Horizontal,
            PixelSize = pixelSize,    // 2048,
            ImageResolution = resolution,   // DB.ImageResolution.DPI_72,
            ShadowViewsFileType = fileType,     // DB.ImageFileType.PNG,
            HLRandWFViewsFileType = fileType,     // DB.ImageFileType.PNG,
            ExportRange = ARDB.ExportRange.SetOfViews,
            FilePath = folder + Path.DirectorySeparatorChar
          };

          options.SetViewsAndSheets(new ARDB.ElementId[] { view.Id });

          var filePath = Path.Combine(options.FilePath, viewName);
          switch (options.ShadowViewsFileType)
          {
            case ARDB.ImageFileType.BMP: filePath += ".bmp"; break;
            case ARDB.ImageFileType.JPEGLossless: filePath += ".jpg"; break;
            case ARDB.ImageFileType.JPEGMedium: filePath += ".jpg"; break;
            case ARDB.ImageFileType.JPEGSmallest: filePath += ".jpg"; break;
            case ARDB.ImageFileType.PNG: filePath += ".png"; break;
            case ARDB.ImageFileType.TARGA: filePath += ".tga"; break;
            case ARDB.ImageFileType.TIFF: filePath += ".tif"; break;
          }

          if (!overwrite && File.Exists(filePath))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"File '{filePath.TripleDotPath(64)}' already exists.");
          else
            view.Document.ExportImage(options);

          DA.SetData("Image File", filePath);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }
        finally
        {
          if (selectedIds.Count > 0)
            uiDoc.Selection.SetElementIds(selectedIds);
        }
      }
    }
  }
}
