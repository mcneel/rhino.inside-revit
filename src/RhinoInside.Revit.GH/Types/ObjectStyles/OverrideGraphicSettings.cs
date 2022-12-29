using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.System.Drawing;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Graphic Overrides")]
  public class OverrideGraphicSettings : ValueObject, ICloneable
  {
    #region DocumentObject
    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.OverrideGraphicSettings settings)
        {
          var description = new List<string>();

          var InvalidPenNumber = ARDB.OverrideGraphicSettings.InvalidPenNumber;

          if (settings.DetailLevel != default)                    description.Add($"Detail Level = {settings.DetailLevel}");
          if (settings.Halftone != default)                       description.Add($"Halftone = {settings.Halftone}");

          if (settings.ProjectionLinePatternId.IsValid())         description.Add($"Projection Lines : Pattern = {(Document.GetElement(settings.ProjectionLinePatternId)?.Name ?? "<Solid>")}");
          if (settings.ProjectionLineColor.IsValid)               description.Add($"Projection Lines : Color = {GH_Format.FormatColour(settings.ProjectionLineColor.ToColor())}");
          if (settings.ProjectionLineWeight != InvalidPenNumber)  description.Add($"Projection Lines : Weight = {settings.ProjectionLineWeight}");

#if REVIT_2019
          if (!settings.IsSurfaceForegroundPatternVisible)        description.Add($"Surface Patterns : Foreground Visible = {settings.IsSurfaceForegroundPatternVisible}");
          if (settings.SurfaceForegroundPatternId.IsValid())      description.Add($"Surface Patterns : Foreground Pattern = {(Document.GetElement(settings.SurfaceForegroundPatternId)?.Name ?? "<Solid fill>")}");
          if (settings.SurfaceForegroundPatternColor.IsValid)     description.Add($"Surface Patterns : Foreground Color = {GH_Format.FormatColour(settings.SurfaceForegroundPatternColor.ToColor())}");
          if (!settings.IsSurfaceBackgroundPatternVisible)        description.Add($"Surface Patterns : Background Visible = {settings.IsSurfaceBackgroundPatternVisible}");
          if (settings.SurfaceBackgroundPatternId.IsValid())      description.Add($"Surface Patterns : Background Pattern = {(Document.GetElement(settings.SurfaceBackgroundPatternId)?.Name ?? "<Solid fill>")}");
          if (settings.SurfaceBackgroundPatternColor.IsValid)     description.Add($"Surface Patterns : Background Color = {GH_Format.FormatColour(settings.SurfaceBackgroundPatternColor.ToColor())}");
#else
          if (!settings.IsProjectionFillPatternVisible)           description.Add($"Surface Patterns : Foreground Visible = {settings.IsProjectionFillPatternVisible}");
          if (settings.ProjectionFillPatternId.IsValid())         description.Add($"Surface Patterns : Foreground Pattern = {(Document.GetElement(settings.ProjectionFillPatternId)?.Name ?? "<Solid fill>")}");
          if (settings.ProjectionFillColor.IsValid)               description.Add($"Surface Patterns : Foreground Color = {GH_Format.FormatColour(settings.ProjectionFillColor.ToColor())}");
#endif
          if (settings.Transparency != default)                   description.Add($"Surface : Transparency = {settings.Transparency}%");

          if (settings.CutLinePatternId.IsValid())                description.Add($"Cut Lines : Pattern = {(Document.GetElement(settings.CutLinePatternId)?.Name ?? "<Solid>")}");
          if (settings.CutLineColor.IsValid)                      description.Add($"Cut Lines : Color = {GH_Format.FormatColour(settings.CutLineColor.ToColor())}");
          if (settings.CutLineWeight != InvalidPenNumber)         description.Add($"Cut Lines : Weight = {settings.CutLineWeight}");

#if REVIT_2019
          if (!settings.IsCutForegroundPatternVisible)            description.Add($"Cut Patterns : Foreground Visible = {settings.IsCutForegroundPatternVisible}");
          if (settings.CutForegroundPatternId.IsValid())          description.Add($"Cut Patterns : Foreground Pattern = {(Document.GetElement(settings.CutForegroundPatternId)?.Name ?? "<Solid fill>")}");
          if (settings.CutForegroundPatternColor.IsValid)         description.Add($"Cut Patterns : Foreground Color = {GH_Format.FormatColour(settings.CutForegroundPatternColor.ToColor())}");
          if (!settings.IsCutBackgroundPatternVisible)            description.Add($"Cut Patterns : Background Visible = {settings.IsCutBackgroundPatternVisible}");
          if (settings.CutBackgroundPatternId.IsValid())          description.Add($"Cut Patterns : Background Pattern = {(Document.GetElement(settings.CutBackgroundPatternId)?.Name ?? "<Solid fill>")}");
          if (settings.CutBackgroundPatternColor.IsValid)         description.Add($"Cut Patterns : Background Color = {GH_Format.FormatColour(settings.CutBackgroundPatternColor.ToColor())}");
#else
          if (!settings.IsCutFillPatternVisible)                  description.Add($"Cut Patterns : Foreground Visible = {settings.IsCutFillPatternVisible}");
          if (settings.CutFillPatternId.IsValid())                description.Add($"Cut Patterns : Foreground Pattern = {(Document.GetElement(settings.CutFillPatternId)?.Name ?? "<Solid fill>")}");
          if (settings.CutFillColor.IsValid)                      description.Add($"Cut Patterns : Foreground Color = {GH_Format.FormatColour(settings.CutFillColor.ToColor())}");
#endif

          if (description.Count > 0)
            return Environment.NewLine + string.Join(Environment.NewLine, description.Select(x => $"  {x}"));

          return "<Default>";
        }

        return "<None>";
      }
    }
#endregion

    public new ARDB.OverrideGraphicSettings Value => base.Value as ARDB.OverrideGraphicSettings;

    object ICloneable.Clone() => new OverrideGraphicSettings(this);

    public OverrideGraphicSettings() : base() { }
    public OverrideGraphicSettings(OverrideGraphicSettings value) : base(value?.Document, value is null ? null : new ARDB.OverrideGraphicSettings(value.Value)) { }
    public OverrideGraphicSettings(ARDB.Document doc, ARDB.OverrideGraphicSettings value) : base(doc, value) { }
    public OverrideGraphicSettings(ARDB.Document doc) : this(doc, doc is null ? null : new ARDB.OverrideGraphicSettings()) { }
  }
}
