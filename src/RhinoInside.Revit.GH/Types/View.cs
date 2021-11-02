using System;
using System.Globalization;
using System.IO;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Render;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("View")]
  public interface IGH_View : IGH_Element { }

  [Kernel.Attributes.Name("View")]
  public class View : Element, IGH_View
  {
    protected override Type ValueType => typeof(DB.View);
    public static explicit operator DB.View(View value) => value?.Value;
    public new DB.View Value => base.Value as DB.View;

    public View() { }
    public View(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public View(DB.View view) : base(view) { }

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
        if (Value is DB.View view && !view.IsTemplate && ViewType is ViewType viewType)
        {
          FormattableString formatable = $"{viewType} : {view.Name}";
          return formatable.ToString(CultureInfo.CurrentUICulture);
        }

        return base.DisplayName;
      }
    }

    public ViewType ViewType => Value is DB.View view ?
      new ViewType(view.ViewType) : default;

    public Interval[] Outline
    {
      get
      {
        if (Value is DB.View view)
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

    public Plane Location => Value is DB.View view ? new Plane
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
        if (Value is DB.View view)
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
        if (Value is DB.View view)
        {
          var swapFolder = Path.Combine(AddIn.SwapFolder, view.Document.GetFingerprintGUID().ToString());
          Directory.CreateDirectory(swapFolder);

          var rect = view.GetOutlineRectangle();
          var fitDirection = rect.Width > rect.Height ?
            DB.FitDirectionType.Horizontal :
            DB.FitDirectionType.Vertical;
          var pixelSize = Math.Max(rect.Width, rect.Height);
          if (pixelSize == 0) return default;
          pixelSize = Math.Min(4096, pixelSize);

          using (var uiDoc = new Autodesk.Revit.UI.UIDocument(view.Document))
          {
            var selectedIds = uiDoc.Selection.GetElementIds();
            if (selectedIds.Count > 0)
              uiDoc.Selection.SetElementIds(new DB.ElementId[] { });

            try
            {
              var options = new DB.ImageExportOptions()
              {
                ZoomType = DB.ZoomFitType.FitToPage,
                FitDirection = fitDirection,
                PixelSize = pixelSize,
                ImageResolution = DB.ImageResolution.DPI_72,
                ShadowViewsFileType = DB.ImageFileType.PNG,
                HLRandWFViewsFileType = DB.ImageFileType.PNG,
                ExportRange = DB.ExportRange.SetOfViews,
                FilePath = swapFolder + Path.DirectorySeparatorChar
              };
              options.SetViewsAndSheets(new DB.ElementId[] { view.Id });
              view.Document.ExportImage(options);

              var viewName = DB.ImageExportOptions.GetFileName(view.Document, view.Id);
              var filename = Path.Combine(options.FilePath, viewName) + ".png";

              var material = new DisplayMaterial(System.Drawing.Color.White, transparency: 0.0);
              material.SetBitmapTexture(filename, front: true);
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
