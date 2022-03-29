using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using System.Windows.Forms;
  using External.DB;
  using External.DB.Extensions;
  using GH_IO.Serialization;
  using Grasshopper.Kernel.Parameters;
  using Grasshopper.Kernel.Types;
  using Rhino.Geometry;

  [ComponentVersion(introduced: "1.0", updated: "1.7")]
  public class ViewExportImage : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("4A962A0C-46A0-4A5F-B727-6747B715A975");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "IMG";

    public ViewExportImage() : base
    (
      name: "Export View Image",
      nickname: "ViewImage",
      description: "Exports a view into a raster image file",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>
      (
        name: "View",
        nickname: "V",
        description: "View to capture"
      ),
      ParamDefinition.Create<Param_FilePath>
      (
        name: "Folder",
        nickname: "F",
        description:  "Folder to store captures",
        optional:  true,
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_String>
      (
        name: "File Name",
        nickname: "FN",
        description:  "Capture file name",
        optional:  true,
        relevance: ParamRelevance.Secondary
      ),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ImageFileType>>
      (
        name: "File Type",
        nickname: "FT",
        description:  "The file type used for export",
        defaultValue: ARDB.ImageFileType.PNG,
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Boolean>
      (
        name: "Overwrite",
        nickname: "O",
        description:  "Overwrite file",
        defaultValue: false
      ),
      ParamDefinition.Create<Param_Integer>
      (
        name: "Pixel Size",
        nickname: "S",
        description:  "The pixel size of an image in specified direction",
        defaultValue: 1024,
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.Param_Enum<Types.FitDirectionType>>
      (
        name: "Fit Direction",
        nickname: "D",
        description:  "The fit direction",
        defaultValue: ARDB.FitDirectionType.Horizontal,
        relevance: ParamRelevance.Occasional
      ),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ImageResolution>>
      (
        name: "Resolution",
        nickname: "R",
        description:  "The image resolution in dots per inch",
        defaultValue: ARDB.ImageResolution.DPI_72,
        relevance: ParamRelevance.Occasional
      ),
      ParamDefinition.Create<Param_Interval2D>
      (
        name: "Crop Extents in View near-plane coordinate system.",
        nickname: "CE",
        description:  "Crop extents",
        optional: true,
        relevance: ParamRelevance.Secondary
      ),
      ParamDefinition.Create<Parameters.View>
      (
        name: "Template",
        nickname: "T",
        description:  "View template to apply on each capture",
        optional: true,
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.ElementFilter>
      (
        name: "Filter",
        nickname: "F",
        description:  "Filter that determines set of elements that will be captured",
        optional: true,
        relevance: ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>
      (
        name: "View",
        nickname: "V",
        description: "Captured view",
        relevance: ParamRelevance.Occasional
      ),
      ParamDefinition.Create<Param_String>
      (
        name: "Image File",
        nickname: "I",
        description: "Captured image file"
      )
    };

    protected override void BeforeSolveInstance()
    {
      if (Params.Input<IGH_Param>("Folder") is IGH_Param Folder)
      {
        Folder.Description = Folder.DataType != GH_ParamData.@void ?
          "Default folder" : $"Default is under '{Core.SwapFolder}'";
      }

      base.BeforeSolveInstance();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!view.Value.IsGraphicalView())
        throw new Exceptions.RuntimeArgumentException("View", $"'{view.FullName}' is not a graphical view", view);

      if (!Params.TryGetData(DA, "Folder", out string folder)) return;
      if (!Params.TryGetData(DA, "File Name", out string fileName)) return;

      if (fileName is object && fileName.Any(x => Path.GetInvalidFileNameChars().Contains(x)))
        throw new Exceptions.RuntimeArgumentException("File Name", $"'{fileName}' is not a valid file name", fileName);

      if (string.IsNullOrEmpty(folder))
        folder = Path.Combine(Core.SwapFolder, view.Document.GetFingerprintGUID().ToString(), InstanceGuid.ToString());

      if (!Params.TryGetData(DA, "File Type", out ARDB.ImageFileType? fileType)) return;
      if (!Params.TryGetData(DA, "Overwrite", out bool? overwrite)) return;
      if (!Params.TryGetData(DA, "Pixel Size", out int? pixelSize)) return;
      if (!Params.TryGetData(DA, "Fit Direction", out ARDB.FitDirectionType? fitDirection)) return;
      if (!Params.TryGetData(DA, "Resolution", out ARDB.ImageResolution? resolution)) return;
      if (!Params.TryGetData(DA, "Crop Extents", out UVInterval? cropExtents)) return;
      if (!Params.TryGetData(DA, "Template", out Types.View template)) return;
      if (!Params.TryGetData(DA, "Filter", out Types.ElementFilter filter)) return;

      using (var uiDoc = new Autodesk.Revit.UI.UIDocument(view.Document))
      {
        // Clear selection temporary
        var selectedIds = uiDoc.Selection.GetElementIds();
        if (selectedIds.Count > 0)
          uiDoc.Selection.SetElementIds(new ARDB.ElementId[] { });

        try
        {
          // We extract ModelToProjection transform here before starting transaction
          // because ARDB.CustomExporter may be not reliable while in a Transacion.
          var modelToProjection = view.GetModelToProjectionTransform();

          using (view.Document.RollBackScope())
          {
            if (template is object)
              view.Value.ViewTemplateId = template.Id;

            var elementIds = default(ICollection<ARDB.ElementId>);
            var elementFilter = filter?.Value;
            if (elementFilter is object)
            {
              using (var collector = new ARDB.FilteredElementCollector(view.Document, view.Id))
              {
                elementIds = collector.ToElements().
                  Where(x => !elementFilter.PassesFilter(x) && x.CanBeHidden(view.Value)).
                  Select(x => x.Id).ToList();
              }
            }

            if (elementFilter is object && elementIds?.Count > 0)
              view.Value.HideElements(elementIds);

            view.Document.Regenerate();

            // By default adjust Crop to visible in view elements projected-box
            if (!view.Value.CropBoxActive && !cropExtents.HasValue)
              cropExtents = view.GetElementsBoundingRectangle(modelToProjection, new Types.ElementFilter(elementFilter));

            // Adjust Crop Box
            if (cropExtents.HasValue)
            {
              var cropBox = view.Value.CropBox;
              cropBox.Min = new ARDB.XYZ(cropExtents.Value.U.Min / Revit.ModelUnits, cropExtents.Value.V.Min / Revit.ModelUnits, cropBox.Min.Z);
              cropBox.Max = new ARDB.XYZ(cropExtents.Value.U.Max / Revit.ModelUnits, cropExtents.Value.V.Max / Revit.ModelUnits, cropBox.Max.Z);

              view.Value.CropBox = cropBox;
              view.Value.CropBoxActive = true;
              view.Document.Regenerate();
            }
            else
            {
              var cropBox = view.Value.CropBox;
              cropExtents = new UVInterval
              (
                new Interval(cropBox.Min.X * Revit.ModelUnits, cropBox.Max.X * Revit.ModelUnits),
                new Interval(cropBox.Min.Y * Revit.ModelUnits, cropBox.Max.Y * Revit.ModelUnits)
              );
            }

            var viewName = ARDB.ImageExportOptions.GetFileName(view.Document, view.Id);
            var options = new ARDB.ImageExportOptions()
            {
              ZoomType =                pixelSize.HasValue ? ARDB.ZoomFitType.FitToPage : ARDB.ZoomFitType.Zoom,
              FitDirection =            fitDirection ??
                                        (cropExtents.Value.U.Length > cropExtents.Value.V.Length ?
                                        ARDB.FitDirectionType.Horizontal : ARDB.FitDirectionType.Vertical),
              PixelSize =               pixelSize.GetValueOrDefault(1048),
              ImageResolution =         resolution ?? ARDB.ImageResolution.DPI_72,
              ShadowViewsFileType =     fileType ?? ARDB.ImageFileType.PNG,
              HLRandWFViewsFileType =   fileType ?? ARDB.ImageFileType.PNG,
              ExportRange =             ARDB.ExportRange.SetOfViews,
              FilePath =                folder + Path.DirectorySeparatorChar
            };

            options.SetViewsAndSheets(new ARDB.ElementId[] { view.Id });

            var fileExtension = ".png";
            switch (options.ShadowViewsFileType)
            {
              case ARDB.ImageFileType.BMP:          fileExtension = ".bmp"; break;
              case ARDB.ImageFileType.JPEGLossless: fileExtension = ".jpg"; break;
              case ARDB.ImageFileType.JPEGMedium:   fileExtension = ".jpg"; break;
              case ARDB.ImageFileType.JPEGSmallest: fileExtension = ".jpg"; break;
              case ARDB.ImageFileType.PNG:          fileExtension = ".png"; break;
              case ARDB.ImageFileType.TARGA:        fileExtension = ".tga"; break;
              case ARDB.ImageFileType.TIFF:         fileExtension = ".tif"; break;
            }

            var filename = Path.Combine(options.FilePath, viewName) + fileExtension;
            var imageFile = Path.Combine
            (
              options.FilePath,
              fileName is null ? 
              $"{InstanceGuid:B} {DA.ParameterTargetPath(0)}({DA.ParameterTargetIndex(0)})" :
              fileName
            ) + fileExtension;

            if (overwrite != true && File.Exists(imageFile))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"File '{imageFile.TripleDotPath(64)}' already exists.");
            }
            else if
            (
              TransparentBackground &&
              (
                options.ShadowViewsFileType == ARDB.ImageFileType.BMP ||
                options.ShadowViewsFileType == ARDB.ImageFileType.PNG ||
                options.ShadowViewsFileType == ARDB.ImageFileType.TIFF
              )
            )
            {
              Directory.CreateDirectory(folder);

              var whiteImage = default(Bitmap);
              {
                var White = new ARDB.Color(255, 255, 255);
                view.Value.SetBackground(ARDB.ViewDisplayBackground.CreateGradient(White, White, White));
                view.Document.ExportImage(options);

                using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                  whiteImage = Image.FromStream(stream) as Bitmap;
              }

              var blackImage = default(Bitmap);
              {
                var Black = new ARDB.Color(0, 0, 0);
                view.Value.SetBackground(ARDB.ViewDisplayBackground.CreateGradient(Black, Black, Black));
                view.Document.ExportImage(options);

                using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                  blackImage = Image.FromStream(stream) as Bitmap;
              }

              var size = blackImage.Size;
              var rectangle = new Rectangle(System.Drawing.Point.Empty, size);

              using (var targetImage = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb))
              {
                var White = Color.FromArgb(0xFF, Color.White);

                var blackScanLine = new int[size.Width];
                var whiteScanLine = new int[size.Width];
                var targetScanLine = new int[size.Width];

                var blackScanData = blackImage.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var whiteScanData = whiteImage.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var targetScanData = targetImage.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                for (int y = 0; y < size.Height; ++y)
                {
                  Marshal.Copy(blackScanData.Scan0 + (blackScanData.Stride * y), blackScanLine, 0, size.Width);
                  Marshal.Copy(whiteScanData.Scan0 + (whiteScanData.Stride * y), whiteScanLine, 0, size.Width);

                  for (int x = 0; x < size.Width; ++x)
                  {
                    var b = Color.FromArgb(blackScanLine[x]);
                    var w = Color.FromArgb(whiteScanLine[x]);
                    var t = w;

                    if (b != w)
                    {
                      if (w == White) t = Color.FromArgb(0x00, w);
                      else
                      {
                        // Little hack to "recover" transparency
                        var hsv = new Rhino.Display.ColorHSV(w);

                        if (hsv.S == 0)
                        {
                          var alpha = (int) Math.Round((1.0 - hsv.V) * 0xFF);
                          hsv.V = 0.0;
                          t = Color.FromArgb(alpha, hsv);
                        }
                        else
                        {
                          var alpha = (int) Math.Round(hsv.S * 0xFF);
                          hsv.S = 1.0;
                          t = Color.FromArgb(alpha, hsv);
                        }
                      }
                    }

                    targetScanLine[x] = t.ToArgb();
                  }

                  Marshal.Copy(targetScanLine, 0, targetScanData.Scan0 + (targetScanData.Stride * y), size.Width);
                }

                targetImage.UnlockBits(targetScanData);
                whiteImage.UnlockBits(whiteScanData);
                blackImage.UnlockBits(blackScanData);

                whiteImage.Dispose();
                blackImage.Dispose();

                targetImage.Save(filename, ImageFormat.Png);
                FileExtension.MoveFile(filename, imageFile, overwrite: true);
              }
            }
            else
            {
              Directory.CreateDirectory(folder);
              view.Document.ExportImage(options);
              FileExtension.MoveFile(filename, imageFile, overwrite: true);
            }

            DA.SetData("Image File", imageFile);
          }
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }
        finally
        {
          if (selectedIds.Count > 0)
            uiDoc.Selection.SetElementIds(selectedIds);
        }
      }
    }

    #region IO
    protected bool TransparentBackground { get; set; } = false;

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      bool transparentBackground = false;
      reader.TryGetBoolean("TransparentBackground", ref transparentBackground);
      TransparentBackground = transparentBackground;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (TransparentBackground)
        writer.SetBoolean("TransparentBackground", TransparentBackground);

      return true;
    }
    #endregion

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      Menu_AppendItem
      (
        menu, "Transparent Background",
        (sender, arg) =>
        {
          RecordUndoEvent("Set: Transparent Background");
          TransparentBackground = !TransparentBackground;
          ExpireSolution(true);
        },
        enabled: true,
        TransparentBackground
      );
    }
    #endregion
  }
}
