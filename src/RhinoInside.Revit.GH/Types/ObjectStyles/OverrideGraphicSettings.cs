using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Numerical;
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

          if (settings.ProjectionLinePatternId.IsValid())         description.Add($"Projection Lines : Pattern = {(Document?.GetElement(settings.ProjectionLinePatternId)?.Name ?? "<Solid>")}");
          if (settings.ProjectionLineColor.IsValid)               description.Add($"Projection Lines : Color = {GH_Format.FormatColour(settings.ProjectionLineColor.ToColor())}");
          if (settings.ProjectionLineWeight != InvalidPenNumber)  description.Add($"Projection Lines : Weight = {settings.ProjectionLineWeight}");

          if (!settings.IsSurfaceForegroundPatternVisible())      description.Add($"Surface Patterns : Foreground Visible = {settings.IsSurfaceForegroundPatternVisible()}");
          if (settings.SurfaceForegroundPatternId().IsValid())    description.Add($"Surface Patterns : Foreground Pattern = {(Document?.GetElement(settings.SurfaceForegroundPatternId())?.Name ?? "<Solid fill>")}");
          if (settings.SurfaceForegroundPatternColor().IsValid)   description.Add($"Surface Patterns : Foreground Color = {GH_Format.FormatColour(settings.SurfaceForegroundPatternColor().ToColor())}");
#if REVIT_2019
          if (!settings.IsSurfaceBackgroundPatternVisible())      description.Add($"Surface Patterns : Background Visible = {settings.IsSurfaceBackgroundPatternVisible()}");
          if (settings.SurfaceBackgroundPatternId().IsValid())    description.Add($"Surface Patterns : Background Pattern = {(Document?.GetElement(settings.SurfaceBackgroundPatternId())?.Name ?? "<Solid fill>")}");
          if (settings.SurfaceBackgroundPatternColor().IsValid)   description.Add($"Surface Patterns : Background Color = {GH_Format.FormatColour(settings.SurfaceBackgroundPatternColor().ToColor())}");
#endif
          if (settings.Transparency != default)                   description.Add($"Surface : Transparency = {settings.Transparency}%");

          if (settings.CutLinePatternId.IsValid())                description.Add($"Cut Lines : Pattern = {(Document.GetElement(settings.CutLinePatternId)?.Name ?? "<Solid>")}");
          if (settings.CutLineColor.IsValid)                      description.Add($"Cut Lines : Color = {GH_Format.FormatColour(settings.CutLineColor.ToColor())}");
          if (settings.CutLineWeight != InvalidPenNumber)         description.Add($"Cut Lines : Weight = {settings.CutLineWeight}");

          if (!settings.IsCutForegroundPatternVisible())          description.Add($"Cut Patterns : Foreground Visible = {settings.IsCutForegroundPatternVisible()}");
          if (settings.CutForegroundPatternId().IsValid())        description.Add($"Cut Patterns : Foreground Pattern = {(Document?.GetElement(settings.CutForegroundPatternId())?.Name ?? "<Solid fill>")}");
          if (settings.CutForegroundPatternColor().IsValid)       description.Add($"Cut Patterns : Foreground Color = {GH_Format.FormatColour(settings.CutForegroundPatternColor().ToColor())}");
#if REVIT_2019
          if (!settings.IsCutBackgroundPatternVisible())          description.Add($"Cut Patterns : Background Visible = {settings.IsCutBackgroundPatternVisible()}");
          if (settings.CutBackgroundPatternId().IsValid())        description.Add($"Cut Patterns : Background Pattern = {(Document?.GetElement(settings.CutBackgroundPatternId())?.Name ?? "<Solid fill>")}");
          if (settings.CutBackgroundPatternColor().IsValid)       description.Add($"Cut Patterns : Background Color = {GH_Format.FormatColour(settings.CutBackgroundPatternColor().ToColor())}");
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
    internal OverrideGraphicSettings(ARDB.Document doc, OverrideGraphicSettings other) : base(doc, new ARDB.OverrideGraphicSettings(other.Value))
    {
      if (other.Document.IsEquivalent(Document)) return;

      if (other.Document is null)
      {
        if (Value.ProjectionLinePatternId.ToBuiltInLinePattern() != ERDB.BuiltInLinePattern.Solid) Value.SetProjectionLinePatternId(ElementIdExtension.Invalid);
        if (Value.CutLinePatternId.ToBuiltInLinePattern() != ERDB.BuiltInLinePattern.Solid) Value.SetCutLinePatternId(ElementIdExtension.Invalid);

        using (var collector = new ARDB.FilteredElementCollector(Document))
        {
          var solidPatternId = collector.OfClass(typeof(ARDB.FillPatternElement)).
            Cast<ARDB.FillPatternElement>().
            FirstOrDefault
            (
              x =>
              {
                using (var pattern = x.GetFillPattern())
                  return pattern.Target == ARDB.FillPatternTarget.Drafting && pattern.IsSolidFill;
              }
            )?.
            Id ?? ElementIdExtension.Invalid;

          Value.SetSurfaceForegroundPatternId(Value.SurfaceForegroundPatternId() == ElementIdExtension.Default ? solidPatternId : ElementIdExtension.Invalid);
          Value.SetSurfaceBackgroundPatternId(Value.SurfaceBackgroundPatternId() == ElementIdExtension.Default ? solidPatternId : ElementIdExtension.Invalid);
          Value.SetCutForegroundPatternId(Value.CutForegroundPatternId() == ElementIdExtension.Default ? solidPatternId : ElementIdExtension.Invalid);
          Value.SetCutBackgroundPatternId(Value.CutBackgroundPatternId() == ElementIdExtension.Default ? solidPatternId : ElementIdExtension.Invalid);
        }
      }
      else
      {
        Value.SetProjectionLinePatternId(Document.LookupElement(other.Document, other.Value.ProjectionLinePatternId));
        Value.SetCutLinePatternId(Document.LookupElement(other.Document, other.Value.CutLinePatternId));

        Value.SetSurfaceForegroundPatternId(Document.LookupElement(other.Document, other.Value.SurfaceForegroundPatternId()));
        Value.SetSurfaceBackgroundPatternId(Document.LookupElement(other.Document, other.Value.SurfaceBackgroundPatternId()));
        Value.SetCutForegroundPatternId(Document.LookupElement(other.Document, other.Value.CutForegroundPatternId()));
        Value.SetCutBackgroundPatternId(Document.LookupElement(other.Document, other.Value.CutBackgroundPatternId()));
      }
    }

    public override bool CastFrom(object source)
    {
      if (GH_Convert.ToColor(source, out var color, GH_Conversion.Both))
      {
        base.Value = new ARDB.OverrideGraphicSettings().
          SetProjectionLineColor(color.ToColor()).
          SetSurfaceForegroundPatternId(ElementIdExtension.Default).
          SetSurfaceForegroundPatternColor(color.ToColor()).
          SetSurfaceTransparency((int) Math.Round((1.0 - (color.A / 255.0)) * 100.0)).
          SetCutForegroundPatternId(ElementIdExtension.Default).
          SetCutForegroundPatternColor(color.ToColor());

        return true;
      }

      if (GH_Convert.ToDouble(source, out var transparency, GH_Conversion.Primary))
      {
        base.Value = new ARDB.OverrideGraphicSettings().SetSurfaceTransparency((int) Math.Round(Arithmetic.Clamp(transparency, 0, 1) * 100.0));
        return true;
      }

      if (GH_Convert.ToBoolean(source, out var reset, GH_Conversion.Primary))
      {
        if (reset)
        {
          var highlight = System.Drawing.Color.FromArgb(180, 255, 128, 0);
          base.Value = new ARDB.OverrideGraphicSettings().
            SetProjectionLineColor(highlight.ToColor()).
            SetSurfaceForegroundPatternId(ElementIdExtension.Default).
            SetSurfaceForegroundPatternColor(highlight.ToColor()).
            SetSurfaceTransparency((int) Math.Round((1.0 - (highlight.A / 255.0)) * 100.0)).
            SetCutForegroundPatternId(ElementIdExtension.Default).
            SetCutForegroundPatternColor(highlight.ToColor());
        }
        else
        {
          base.Value = new ARDB.OverrideGraphicSettings();
        }

        return true;
      }

      return base.CastFrom(source);
    }
  }
}
