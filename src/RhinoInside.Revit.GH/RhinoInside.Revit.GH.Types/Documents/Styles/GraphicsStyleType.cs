using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;

using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Documents.Styles
{
  public class GraphicsStyleType : GH_Enum<DB.GraphicsStyleType>
  {
    public GraphicsStyleType() : base(DB.GraphicsStyleType.Projection) { }
    public GraphicsStyleType(DB.GraphicsStyleType value) : base(value) { }
  }
}
