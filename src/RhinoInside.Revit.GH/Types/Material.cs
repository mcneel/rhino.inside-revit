using System;
using RhinoInside.Revit.Convert.System.Drawing;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Material")]
  public class Material : Element
  {
    protected override Type ScriptVariableType => typeof(DB.Material);
    public static explicit operator DB.Material(Material value) => value?.Value;
    public new DB.Material Value => base.Value as DB.Material;

    public Material() { }
    public Material(DB.Material material) : base(material) { }

    #region Identity Data
    public string MaterialClass
    {
      get => Value?.MaterialClass;
      set
      {
        if (value is object && Value?.MaterialClass != value)
          Value.MaterialClass = value;
      }
    }

    public string MaterialCategory
    {
      get => Value?.MaterialCategory;
      set
      {
        if (value is object && Value?.MaterialCategory != value)
          Value.MaterialCategory = value;
      }
    }
    #endregion

    #region Materials and finshed

    public bool? UseRenderAppearanceForShading
    {
      get => Value?.UseRenderAppearanceForShading;
      set
      {
        if (value is object && Value?.UseRenderAppearanceForShading != value)
          Value.UseRenderAppearanceForShading = value.Value;
      }
    }

    public System.Drawing.Color? Color
    {
      get => Value?.Color.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          var materialColor = material.Color;
          var valueColor = value.Value;

          if (materialColor.Red != valueColor.R || materialColor.Green != valueColor.G || materialColor.Blue != valueColor.B)
            material.Color = valueColor.ToColor();
        }
      }
    }

    public double? Transparency
    {
      get => Value?.Transparency / 100.0;
      set
      {
        if (value is object && Value is DB.Material material)
        {
          var intValue = (int) Math.Round(value.Value * 100.0);
          if (material.Transparency != intValue)
            material.Transparency = intValue;
        }
      }
    }

    public double? Shininess
    {
      get => Value?.Shininess / 128.0;
      set
      {
        if (value is object && Value is DB.Material material)
        {
          var intValue = (int) Math.Round(value.Value * 128.0);
          if (material.Shininess != intValue)
            material.Shininess = intValue;
        }
      }
    }    

    public double? Smoothness
    {
      get => Value?.Smoothness / 100.0;
      set
      {
        if (value is object && Value is DB.Material material)
        {
          var intValue = (int) Math.Round(value.Value * 100.0);
          if (material.Smoothness != intValue)
            material.Smoothness = intValue;
        }
      }
    }

#if REVIT_2019
    public FillPatternElement SurfaceForegroundPattern
    {
      get => new FillPatternElement(Document, SurfaceForegroundPatternId);
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.SurfaceForegroundPatternId)
        {
          AssertValidDocument(value.Document, nameof(SurfaceForegroundPattern));
          material.SurfaceForegroundPatternId = value.Id;
        }
      }
    }

    public DB.ElementId SurfaceForegroundPatternId
    {
      get => Value?.SurfaceForegroundPatternId;
      set
      {
        if(value is object && Value is DB.Material material && value != material.SurfaceForegroundPatternId)
          material.SurfaceForegroundPatternId = value;
      }
    }

    public System.Drawing.Color? SurfaceForegroundPatternColor
    {
      get => Value?.SurfaceForegroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.SurfaceForegroundPatternColor)
              material.SurfaceForegroundPatternColor = color;
          }
        }
      }
    }

    public FillPatternElement SurfaceBackgroundPattern
    {
      get => new FillPatternElement(Document, SurfaceBackgroundPatternId);
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.SurfaceBackgroundPatternId)
        {
          AssertValidDocument(value.Document, nameof(SurfaceBackgroundPattern));
          material.SurfaceBackgroundPatternId = value.Id;
        }
      }
    }

    public DB.ElementId SurfaceBackgroundPatternId
    {
      get => Value?.SurfaceBackgroundPatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.SurfaceBackgroundPatternId)
          material.SurfaceBackgroundPatternId = value;
      }
    }

    public System.Drawing.Color? SurfaceBackgroundPatternColor
    {
      get => Value?.SurfaceBackgroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.SurfaceBackgroundPatternColor)
              material.SurfaceBackgroundPatternColor = color;
          }
        }
      }
    }

    public FillPatternElement CutForegroundPattern
    {
      get => new FillPatternElement(Document, CutForegroundPatternId);
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.CutForegroundPatternId)
        {
          AssertValidDocument(value.Document, nameof(CutForegroundPattern));
          material.CutForegroundPatternId = value.Id;
        }
      }
    }

    public DB.ElementId CutForegroundPatternId
    {
      get => Value?.CutForegroundPatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.CutForegroundPatternId)
          material.CutForegroundPatternId = value;
      }
    }

    public System.Drawing.Color? CutForegroundPatternColor
    {
      get => Value?.CutForegroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.CutForegroundPatternColor)
              material.CutForegroundPatternColor = color;
          }
        }
      }
    }

    public FillPatternElement CutBackgroundPattern
    {
      get => new FillPatternElement(Document, CutBackgroundPatternId);
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.CutBackgroundPatternId)
        {
          AssertValidDocument(value.Document, nameof(CutBackgroundPattern));
          material.CutBackgroundPatternId = value.Id;
        }
      }
    }

    public DB.ElementId CutBackgroundPatternId
    {
      get => Value?.CutBackgroundPatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.CutBackgroundPatternId)
          material.CutBackgroundPatternId = value;
      }
    }

    public System.Drawing.Color? CutBackgroundPatternColor
    {
      get => Value?.CutBackgroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.CutBackgroundPatternColor)
              material.CutBackgroundPatternColor = color;
          }
        }
      }
    }
#else
    public FillPatternElement SurfaceForegroundPattern
    {
      get => new FillPatternElement(Document, SurfaceForegroundPatternId);
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.SurfacePatternId)
        {
          AssertValidDocument(value.Document, nameof(SurfaceForegroundPattern));
          material.SurfacePatternId = value.Id;
        }
      }
    }

    public DB.ElementId SurfaceForegroundPatternId
    {
      get => Value?.SurfacePatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.SurfacePatternId)
          material.SurfacePatternId = value;
      }
    }

    public System.Drawing.Color? SurfaceForegroundPatternColor
    {
      get => Value?.SurfacePatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.SurfacePatternColor)
              material.SurfacePatternColor = color;
          }
        }
      }
    }

    public FillPatternElement SurfaceBackgroundPattern
    {
      get => default;
      set { }
    }

    public DB.ElementId SurfaceBackgroundPatternId
    {
      get => default;
      set { }
    }

    public System.Drawing.Color? SurfaceBackgroundPatternColor
    {
      get => default;
      set { }
    }

    public FillPatternElement CutForegroundPattern
    {
      get => new FillPatternElement(Document, CutForegroundPatternId);
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.CutPatternId)
        {
          AssertValidDocument(value.Document, nameof(CutForegroundPattern));
          material.CutPatternId = value.Id;
        }
      }
    }

    public DB.ElementId CutForegroundPatternId
    {
      get => Value?.CutPatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.CutPatternId)
          material.CutPatternId = value;
      }
    }

    public System.Drawing.Color? CutForegroundPatternColor
    {
      get => Value?.CutPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.CutPatternColor)
              material.CutPatternColor = color;
          }
        }
      }
    }

    public FillPatternElement CutBackgroundPattern
    {
      get => default;
      set { }
    }

    public DB.ElementId CutBackgroundPatternId
    {
      get => default;
      set { }
    }

    public System.Drawing.Color? CutBackgroundPatternColor
    {
      get => default;
      set { }
    }
#endif
    #endregion
  }
}
