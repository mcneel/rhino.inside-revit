using System;
using System.Globalization;
using System.IO;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Convert.System.Drawing;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("View")]
  public interface IGH_View : IGH_Element { }

  [Kernel.Attributes.Name("View")]
  public class View : Element, IGH_View
  {
    protected override Type ValueType => typeof(ARDB.View);
    public static explicit operator ARDB.View(View value) => value?.Value;
    public new ARDB.View Value => base.Value as ARDB.View;

    public View() { }
    public View(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public View(ARDB.View view) : base(view) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
      {
        var surface = DisplaySurface;
        target = surface is object ? (Q) (object) new GH_Surface(surface) : default;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Box)))
      {
        target = (Q) (object) new GH_Box(Box);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        target = (Q) (object) new GH_Plane(Location);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Interval2D)))
      {
        var outline = Outline;
        target = outline is object ? (Q) (object) new GH_Interval2D(new UVInterval(outline[0], outline[1])) : default;

        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Material)))
      {
        var material = new GH_Material { Value = DisplayMaterial };
        target = (Q) (object) material;

        return true;
      }

      return false;
    }

    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.View view && !view.IsTemplate && ViewType is ViewType viewType)
        {
          FormattableString formatable = $"{viewType} : {view.Name}";
          return formatable.ToString(CultureInfo.CurrentUICulture);
        }

        return base.DisplayName;
      }
    }

    public ViewType ViewType => Value is ARDB.View view ?
      new ViewType(view.ViewType) : default;

    public Interval[] Outline
    {
      get
      {
        if (Value is ARDB.View view)
        {
          var outline = view.Outline;
          var modelUnits = Revit.ModelUnits;
          return !outline.IsUnset() ?
          new Interval[]
          {
            new Interval(outline.Min.U * modelUnits, outline.Max.U * modelUnits),
            new Interval(outline.Min.V * modelUnits, outline.Max.V * modelUnits)
          } :
          new Interval[] { NaN.Interval, NaN.Interval };
        }

        return default;
      }
    }

    public Plane Location => Value is ARDB.View view ? new Plane
    (
      view.Origin.ToPoint3d(),
      view.RightDirection.ToVector3d(),
      view.UpDirection.ToVector3d()
    ) : NaN.Plane;

    public Box Box => Value?.get_BoundingBox(default).ToBox() ?? NaN.Box;

    public Surface DisplaySurface 
    {
      get
      {
        if (Value is ARDB.View view)
        {
          var box = view.get_BoundingBox(default).ToBox();
          var outline = Outline;
          var scale = view.Scale == 0 ? 1 : view.Scale;
          return new PlaneSurface
          (
            plane: box.Plane,
            xExtents: new Interval(outline[0].T0 * scale, outline[0].T1 * scale),
            yExtents: new Interval(outline[1].T0 * scale, outline[1].T1 * scale)
          );
        }

        return default;
      }
    }

    public DisplayMaterial DisplayMaterial
    {
      get
      {
        if (Value is ARDB.View view)
        {
          var swapFolder = Path.Combine(Core.SwapFolder, view.Document.GetFingerprintGUID().ToString());
          Directory.CreateDirectory(swapFolder);

          var rect = view.GetOutlineRectangle().ToRectangle();
          var fitDirection = rect.Width > rect.Height ?
            ARDB.FitDirectionType.Horizontal :
            ARDB.FitDirectionType.Vertical;
          var pixelSize = Math.Max(rect.Width, rect.Height);
          if (pixelSize == 0) return default;
          pixelSize = Math.Min(4096, pixelSize);

          using (var uiDoc = new Autodesk.Revit.UI.UIDocument(view.Document))
          {
            var selectedIds = uiDoc.Selection.GetElementIds();
            if (selectedIds.Count > 0)
              uiDoc.Selection.SetElementIds(new ARDB.ElementId[] { });

            try
            {
              var options = new ARDB.ImageExportOptions()
              {
                ZoomType = ARDB.ZoomFitType.FitToPage,
                FitDirection = fitDirection,
                PixelSize = pixelSize,
                ImageResolution = ARDB.ImageResolution.DPI_72,
                ShadowViewsFileType = ARDB.ImageFileType.PNG,
                HLRandWFViewsFileType = ARDB.ImageFileType.PNG,
                ExportRange = ARDB.ExportRange.SetOfViews,
                FilePath = swapFolder + Path.DirectorySeparatorChar
              };
              options.SetViewsAndSheets(new ARDB.ElementId[] { view.Id });
              view.Document.ExportImage(options);

              var viewName = ARDB.ImageExportOptions.GetFileName(view.Document, view.Id);
              var filename = Path.Combine(options.FilePath, viewName) + ".png";
              var texturename = Path.Combine(options.FilePath, view.UniqueId) + ".png";

              // We rename texture file to avoid conflicts
              // between Rhino and Revit accessing the same file
              FileExtension.MoveFile(filename, texturename, overwrite: true);

              var material = new DisplayMaterial(System.Drawing.Color.White, transparency: 0.0);
              material.SetBitmapTexture(texturename, front: true);
              return material;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { }
            finally
            {
              if (selectedIds.Count > 0)
                uiDoc.Selection.SetElementIds(selectedIds);
            }
          }
        }

        return default;
      }
    }
  }
}
