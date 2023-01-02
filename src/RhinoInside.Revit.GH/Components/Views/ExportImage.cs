using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using Convert.System.Drawing;
  using External.DB;
  using External.DB.Extensions;
  using External.UI.Selection;

  [ComponentVersion(introduced: "1.0", updated: "1.11")]
  public class ViewExportImage : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("4A962A0C-46A0-4A5F-B727-6747B715A975");
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    protected override string IconTag => "IMG";

    public ViewExportImage() : base
    (
      name: "Export View Image",
      nickname: "View Image",
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
        relevance: ParamRelevance.Secondary
      ),
      ParamDefinition.Create<Param_String>
      (
        name: "File Name",
        nickname: "FN",
        description:  "Capture file name",
        optional:  true,
        relevance: ParamRelevance.Quarternary
      ),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ImageFileType>>
      (
        name: "File Type",
        nickname: "FT",
        description:  "The file type used for export",
        optional: true,
        relevance: ParamRelevance.Tertiary
      ),
      ParamDefinition.Create<Param_Boolean>
      (
        name: "Overwrite",
        nickname: "O",
        description:  "Overwrite file",
        defaultValue: false,
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Integer>
      (
        name: "Pixel Size",
        nickname: "S",
        description:  "The pixel size of an image in specified direction",
        optional: true,
        relevance: ParamRelevance.Quarternary
      ),
      ParamDefinition.Create<Parameters.Param_Enum<Types.FitDirectionType>>
      (
        name: "Fit Direction",
        nickname: "D",
        description:  "The fit direction",
        optional: true,
        relevance: ParamRelevance.Occasional
      ),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ImageResolution>>
      (
        name: "Resolution",
        nickname: "R",
        description:  "The image resolution in dots per inch",
        optional: true,
        relevance: ParamRelevance.Occasional
      ),
      ParamDefinition.Create<Param_Interval2D>
      (
        name: "Crop Extents",
        nickname: "CE",
        description:  "Crop extents in View near-plane coordinate system.",
        optional: true,
        relevance: ParamRelevance.Secondary
      ),
      ParamDefinition.Create<Parameters.View>
      (
        name: "Template",
        nickname: "T",
        description:  "View template to apply on each capture",
        optional: true,
        relevance: ParamRelevance.Tertiary
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
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_String>
      (
        name: "Image File",
        nickname: "I",
        description: "Captured image file",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Interval2D>
      (
        name: "Crop Extents",
        nickname: "CE",
        description:  "Crop extents in View near-plane coordinate system.",
        relevance: ParamRelevance.Secondary
      ),
      ParamDefinition.Create<Param_Surface>
      (
        name: "Outline",
        nickname: "O",
        description:  "Exported outline in World coordinate system.",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_OGLShader>
      (
        name: "Texture",
        nickname: "T",
        description:  "Display material.",
        relevance: ParamRelevance.Occasional
      ),
    };

    static readonly TemporaryDirectory Textures = new TemporaryDirectory(Path.Combine(Core.SwapFolder, "Textures"));

    DirectoryInfo VolatileDirectory;
    void CreateVolatileDirectory(string folder)
    {
      if (VolatileDirectory is null)
      {
        VolatileDirectory = new DirectoryInfo(Path.Combine(folder, InstanceGuid.ToString()));
        VolatileDirectory.Create();
        VolatileDirectory.Attributes |= FileAttributes.System;

        var desktopINI = new FileInfo(Path.Combine(VolatileDirectory.FullName, "Desktop.ini"));
        desktopINI.Delete();
        using (var writer = File.CreateText(desktopINI.FullName))
        {
          writer.WriteLine("[.ShellClassInfo]");
          writer.WriteLine($"LocalizedResourceName={NickName}");
          writer.WriteLine($"InfoTip=Results of '{Name}' Grasshopper component.");
          writer.WriteLine("FolderType=Pictures");
        }
        desktopINI.Attributes |= FileAttributes.Hidden;
      }
    }

    string VolatileTextureFileName(IGH_DataAccess DA, int param) => $"-{DA.ParameterTargetPath(param)}({DA.ParameterTargetIndex(param)})";

    public override void RemovedFromDocument(GH_Document document)
    {
      Textures.Delete(this);

      base.RemovedFromDocument(document);
    }

    protected override void BeforeSolveInstance()
    {
      if (Params.Input<IGH_Param>("Folder") is IGH_Param Folder)
      {
        Folder.Description = Folder.DataType != GH_ParamData.@void ?
          "Folder to store resulting images." : $"Default is under \"{Textures.Directory.FullName}\"";
      }

      Textures.Delete(this);

      if (Params.Input<IGH_Param>("Overwrite") is null && VolatileDirectory is object)
      {
        VolatileDirectory.Refresh();
        if (VolatileDirectory.Exists)
        {
          foreach (var branch in VolatileDirectory.EnumerateDirectories("{*}", SearchOption.TopDirectoryOnly))
          {
            var branchName = branch.Name.Substring(1, branch.Name.Length - 2);
            if (!branchName.Split(';').All(x => int.TryParse(x, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int _)))
              continue;

            foreach (var item in branch.EnumerateFiles("* - *.???", SearchOption.AllDirectories))
            {
              var attributes = item.Attributes;
              if (!attributes.HasFlag(FileAttributes.Temporary))
                continue;

              if (attributes.HasFlag(FileAttributes.System))
                continue;

              if (attributes.HasFlag(FileAttributes.ReadOnly))
                continue;

              var itemName = item.Name.Substring(0, item.Name.IndexOf(' '));
              if (!int.TryParse(itemName, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int _))
                continue;

              if
              (
                !item.Extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) &&
                !item.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) &&
                !item.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
                !item.Extension.Equals(".tga", StringComparison.OrdinalIgnoreCase) &&
                !item.Extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
              )
                continue;

              try { item.Delete(); } catch { }
            }

            try { branch.Delete(); } catch { }
          }
        }
      }

      VolatileDirectory = default;

      base.BeforeSolveInstance();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view)) return;
      else Params.TrySetData(DA, "View", () => view);
      {
        if (!(view.Value.IsGraphicalView() || (view.Value is ARDB.ViewSchedule schedule && !schedule.IsInternalKeynoteSchedule && !schedule.IsTitleblockRevisionSchedule)))
          throw new Exceptions.RuntimeArgumentException("View", $"'{view.FullName}' is not a graphical view", view);
      }

      if (!Params.TryGetData(DA, "Folder", out string folder)) return;
      {
        if (folder is object && folder.Any(x => Path.GetInvalidPathChars().Contains(x)))
          throw new Exceptions.RuntimeArgumentException("Folder", $"'{folder}' is not a valid folder name", folder);

        if (folder is object && !Path.IsPathRooted(folder))
          throw new Exceptions.RuntimeArgumentException("Folder", $"'{folder}' is not a valid absolute path", folder);
      }

      if (!Params.TryGetData(DA, "File Name", out string fileName)) return;
      {
        if (fileName is object && fileName.Any(x => Path.GetInvalidFileNameChars().Contains(x)))
          throw new Exceptions.RuntimeArgumentException("File Name", $"'{fileName}' is not a valid file name", fileName);
      }

      var fileExtension = ".png";
      if (!Params.TryGetData(DA, "File Type", out ARDB.ImageFileType? fileType)) return;
      {
        fileType = fileType ?? ARDB.ImageFileType.PNG;
        switch (fileType)
        {
          case ARDB.ImageFileType.BMP:          fileExtension = ".bmp"; break;
          case ARDB.ImageFileType.JPEGLossless: fileExtension = ".jpg"; break;
          case ARDB.ImageFileType.JPEGMedium:   fileExtension = ".jpg"; break;
          case ARDB.ImageFileType.JPEGSmallest: fileExtension = ".jpg"; break;
          case ARDB.ImageFileType.PNG:          fileExtension = ".png"; break;
          case ARDB.ImageFileType.TARGA:        fileExtension = ".tga"; break;
          case ARDB.ImageFileType.TIFF:         fileExtension = ".tif"; break;
        }
      }

      if (!Params.TryGetData(DA, "Overwrite", out bool? overwrite)) return;
      if (!Params.TryGetData(DA, "Pixel Size", out int? pixelSize)) return;
      if (!Params.TryGetData(DA, "Fit Direction", out ARDB.FitDirectionType? fitDirection)) return;
      if (!Params.TryGetData(DA, "Resolution", out ARDB.ImageResolution? resolution)) return;
      if (!Params.TryGetData(DA, "Crop Extents", out UVInterval? cropExtents)) return;
      if (!Params.TryGetData(DA, "Template", out Types.View template)) return;
      if (!Params.TryGetData(DA, "Filter", out Types.ElementFilter filter)) return;

      var viewName = ARDB.ImageExportOptions.GetFileName(view.Document, view.Id);

      var imageFileIsVolatile = false;
      if (string.IsNullOrEmpty(fileName))
      {
        if (string.IsNullOrEmpty(folder))
        {
          fileName = Textures.PrefixOf(this) + VolatileTextureFileName(DA, 0);
          imageFileIsVolatile = true;
        }
        else
        {
          CreateVolatileDirectory(folder);
          fileName = $"{InstanceGuid}\\{DA.ParameterTargetPath(0)}\\{DA.ParameterTargetIndex(0)}{viewName}";
        }
      }

      if (string.IsNullOrEmpty(folder))
        folder = Textures.Directory.FullName;

      var app = view.Document.Application;
      var appBackgroundColor = app.BackgroundColor;

      var imageFile = new FileInfo(Path.Combine(folder, fileName + fileExtension));
      var opacityFile = new FileInfo(Path.Combine(Textures.Directory.FullName, Textures.PrefixOf(this) + VolatileTextureFileName(DA, 0) + "-Opacity.png"));

      overwrite = overwrite ?? imageFileIsVolatile;
      try
      {
        using (new NoSelectionScope(view.Document))
        {
          // We extract ModelToProjection transform here before starting transaction
          // because ARDB.CustomExporter may be not reliable while in a Transacion.
          var modelToProjection = view.GetModelToProjectionTransform();

          using (view.Document.RollBackScope())
          {
            if (template is object)
              view.Value.ViewTemplateId = template.Id;

            if (overwrite.Value == false && imageFile.Exists)
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"File '{imageFile.FullName.TripleDotPath(64)}' already exists.");
            }
            else
            {
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
            }

            if (view.Value is ARDB.ViewSchedule schedule)
            {
              var sheet = ARDB.ViewSheet.Create(view.Document, ElementIdExtension.InvalidElementId);
              ARDB.ScheduleSheetInstance.Create(view.Document, sheet.Id, view.Id, XYZExtension.Zero);

              view = Types.View.FromElement(sheet) as Types.View;
              viewName = ARDB.ImageExportOptions.GetFileName(view.Document, view.Id);
            }
            else
            {
              // By default adjust Crop to visible in view elements projected-box
              if (!view.Value.CropBoxActive && !cropExtents.HasValue)
                cropExtents = view.GetElementsBoundingRectangle(modelToProjection, filter);

              // Adjust Crop Box
              if (cropExtents.HasValue)
              {
                if (cropExtents.Value.IsValid)
                {
                  var cropBox = view.Value.CropBox;
                  cropBox.Min = new ARDB.XYZ(cropExtents.Value.U.Min / Revit.ModelUnits, cropExtents.Value.V.Min / Revit.ModelUnits, cropBox.Min.Z);
                  cropBox.Max = new ARDB.XYZ(cropExtents.Value.U.Max / Revit.ModelUnits, cropExtents.Value.V.Max / Revit.ModelUnits, cropBox.Max.Z);

                  view.Value.CropBox = cropBox;
                  view.Value.CropBoxActive = true;
                  view.Document.Regenerate();
                }
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
            }

            cropExtents = cropExtents ?? view.GetOutline(ActiveSpace.ModelSpace);

            if (cropExtents.Value.IsValid)
            {
              Params.TrySetData(DA, "Crop Extents", () => cropExtents.Value);
              Params.TrySetData(DA, "Outline", () => view.Surface);

              if (!(overwrite.Value == false && imageFile.Exists))
              {
                var options = new ARDB.ImageExportOptions()
                {
                  ZoomType = pixelSize.HasValue ? ARDB.ZoomFitType.FitToPage : ARDB.ZoomFitType.Zoom,
                  FitDirection = fitDirection ??
                                (cropExtents.Value.U.Length > cropExtents.Value.V.Length ?
                                ARDB.FitDirectionType.Horizontal : ARDB.FitDirectionType.Vertical),
                  PixelSize = pixelSize.GetValueOrDefault(512),
                  Zoom = 100,
                  ImageResolution = resolution ?? ARDB.ImageResolution.DPI_150,
                  ShadowViewsFileType = fileType ?? ARDB.ImageFileType.PNG,
                  HLRandWFViewsFileType = fileType ?? ARDB.ImageFileType.PNG,
                  ExportRange = ARDB.ExportRange.SetOfViews,
                  FilePath = folder + Path.DirectorySeparatorChar
                };

                options.SetViewsAndSheets(new ARDB.ElementId[] { view.Id });
                Directory.CreateDirectory(options.FilePath);
                var sourceFile = new FileInfo(Path.Combine(options.FilePath, viewName + fileExtension));

                var canSetBackground = view.Value is ARDB.View3D || view.Value is ARDB.ViewSection;
                var viewStyleIsRealistic =
                  view.Value.DisplayStyle == ARDB.DisplayStyle.Realistic ||
                  view.Value.DisplayStyle == ARDB.DisplayStyle.RealisticWithEdges ||
                  view.Value.DisplayStyle == ARDB.DisplayStyle.Rendering;

                if
                (
                  !viewStyleIsRealistic &&
                  TransparentBackground &&
                  (
                    options.ShadowViewsFileType == ARDB.ImageFileType.BMP ||
                    options.ShadowViewsFileType == ARDB.ImageFileType.PNG ||
                    options.ShadowViewsFileType == ARDB.ImageFileType.TIFF
                  )
                )
                {
                  var viewBackground = view.Value.GetBackground();
                  try
                  {
                    var whiteImage = default(Bitmap);
                    {
                      if (!app.BackgroundColor.IsEquivalent(ColorExtension.White))
                      {
                        if (canSetBackground)
                          view.Value.SetBackground(ARDB.ViewDisplayBackground.CreateGradient(ColorExtension.White, ColorExtension.White, ColorExtension.White));

                        app.BackgroundColor = ColorExtension.White;
                      }

                      view.Document.ExportImage(options);

                      using (var stream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                        whiteImage = Image.FromStream(stream) as Bitmap;
                    }

                    var blackImage = default(Bitmap);
                    {
                      if (!app.BackgroundColor.IsEquivalent(ColorExtension.Black))
                      {
                        if (canSetBackground)
                          view.Value.SetBackground(ARDB.ViewDisplayBackground.CreateGradient(ColorExtension.Black, ColorExtension.Black, ColorExtension.Black));

                        app.BackgroundColor = ColorExtension.Black;
                      }

                      view.Document.ExportImage(options);

                      using (var stream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                        blackImage = Image.FromStream(stream) as Bitmap;
                    }

                    sourceFile.Delete();

                    var rectangle = new Rectangle(System.Drawing.Point.Empty, blackImage.Size);
                    var _Texture_ = Params.IndexOfOutputParam("Texture");
                    using (var opacityImage = _Texture_ >= 0 ? new Bitmap(rectangle.Width, rectangle.Height, PixelFormat.Format8bppIndexed) : null)
                    using (var bitmapImage = new Bitmap(rectangle.Width, rectangle.Height, PixelFormat.Format32bppArgb))
                    {
                      var White = Color.FromArgb(0xFF, Color.White);

                      var blackScanLine = new int[rectangle.Width];
                      var whiteScanLine = new int[rectangle.Width];
                      var targetScanLine = new int[rectangle.Width];
                      var opacityScanLine = new byte[rectangle.Width];

                      var blackScanData = blackImage.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                      var whiteScanData = whiteImage.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                      var targetScanData = bitmapImage.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                      var opacityScanData = opacityImage?.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                      var backgroundColor = appBackgroundColor.ToColor();
                      for (int y = 0; y < rectangle.Height; ++y)
                      {
                        Marshal.Copy(blackScanData.Scan0 + (blackScanData.Stride * y), blackScanLine, 0, rectangle.Width);
                        Marshal.Copy(whiteScanData.Scan0 + (whiteScanData.Stride * y), whiteScanLine, 0, rectangle.Width);

                        for (int x = 0; x < rectangle.Width; ++x)
                        {
                          var b = Color.FromArgb(blackScanLine[x]);
                          var w = Color.FromArgb(whiteScanLine[x]);
                          var t = w;
                          byte alpha = byte.MaxValue;

                          if (b != w)
                          {
                            if (w == White)
                            {
                              alpha = 0;
                              t = Color.FromArgb(0x00, backgroundColor);
                            }
                            else
                            {
                              // Little hack to "recover" transparency
                              var hsv = new ColorHSV(w);

                              if (hsv.S == 0)
                              {
                                alpha = (byte) Math.Round((1.0 - hsv.V) * byte.MaxValue);
                                hsv.V = 0.0;
                                t = Color.FromArgb(alpha, hsv);
                              }
                              else
                              {
                                alpha = (byte) Math.Round(hsv.S * byte.MaxValue);
                                hsv.S = 1.0;
                                t = Color.FromArgb(alpha, hsv);
                              }
                            }
                          }

                          targetScanLine[x] = t.ToArgb();
                          opacityScanLine[x] = alpha;
                        }

                        Marshal.Copy(targetScanLine, 0, targetScanData.Scan0 + (targetScanData.Stride * y), rectangle.Width);
                        if (opacityScanData is object)
                          Marshal.Copy(opacityScanLine, 0, opacityScanData.Scan0 + (opacityScanData.Stride * y), rectangle.Width);
                      }

                      opacityImage?.UnlockBits(targetScanData);
                      bitmapImage.UnlockBits(targetScanData);
                      whiteImage.UnlockBits(whiteScanData);
                      blackImage.UnlockBits(blackScanData);

                      whiteImage.Dispose();
                      blackImage.Dispose();

                      switch (options.ShadowViewsFileType)
                      {
                        case ARDB.ImageFileType.BMP: bitmapImage.Save(imageFile.FullName, ImageFormat.Bmp); break;
                        case ARDB.ImageFileType.PNG: bitmapImage.Save(imageFile.FullName, ImageFormat.Png); break;
                        case ARDB.ImageFileType.TIFF: bitmapImage.Save(imageFile.FullName, ImageFormat.Tiff); break;
                      }

                      if (opacityImage is object)
                      {
                        var palette = opacityImage.Palette;
                        for (int i = 0; i < byte.MaxValue; ++i)
                          palette.Entries[i] = Color.FromArgb(i, i, i);

                        opacityImage.Palette = palette;
                        opacityImage.Save(opacityFile.FullName, ImageFormat.Png);
                      }
                    }
                  }
                  finally
                  {
                    if (canSetBackground)
                      view.Value.SetBackground(viewBackground);

                    if (!app.BackgroundColor.IsEquivalent(appBackgroundColor))
                      app.BackgroundColor = appBackgroundColor;
                  }
                }
                else
                {
                  view.Document.ExportImage(options);
                  sourceFile.MoveTo(imageFile.FullName, overwrite: true);
                }
              }
            }
            else imageFile.Delete();
          }
        }
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      imageFile.Refresh();
      if (imageFile.Exists)
      {
        Params.TrySetData(DA, "Image File", () => imageFile.FullName);
        Params.TrySetData
        (
          DA,
          "Texture",
          () =>
          {
            var material = new DisplayMaterial(appBackgroundColor.ToColor(), transparency: 0.0)
            {
              Specular = Color.Black
            };

            // Bitmap Texture
            imageFile.Refresh();
            if (imageFile.Exists)
            {
              var contrast = byte.MaxValue - (byte) Math.Round(material.Diffuse.GetBrightness() * byte.MaxValue);
              material.Diffuse = Color.FromArgb(contrast, contrast, contrast);

              var textureFile = Textures.CopyFrom(this, imageFile, VolatileTextureFileName(DA, 0));
              if (overwrite == false)
                textureFile.Attributes &= ~FileAttributes.Temporary;

              var texture = new Texture() { FileReference = Rhino.FileIO.FileReference.CreateFromFullPath(textureFile.FullName) };
              material.SetBitmapTexture(texture, front: true);
            }

            // Opacity Texture
            opacityFile.Refresh();
            if (opacityFile.Exists)
            {
              var textureFile = Textures.MoveFrom(this, opacityFile, VolatileTextureFileName(DA, 0) + "-Opacity");
              if (overwrite == false)
                textureFile.Attributes &= ~FileAttributes.Temporary;

              var texture = new Texture() { FileReference = Rhino.FileIO.FileReference.CreateFromFullPath(textureFile.FullName) };
              material.SetTransparencyTexture(texture, front: true);
            }

            return new GH_Material() { Value = material };
          }
        );
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
