using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhinoInside.Revit.External.DB
{
  public class TextureData { }

  public class TwoDMapData : TextureData
  {
    public double OffsetU;
    public double OffsetV;
    public double SizeU;
    public double SizeV;
    public bool RepeatU;
    public bool RepeatV;
    public double Angle;
  }

  public class UnifiedBitmapData : TextureData
  {
    public TwoDMapData TwoDMap;
    public string SourceFile;
    public bool Invert;
    public double Brightness;
  }
}
