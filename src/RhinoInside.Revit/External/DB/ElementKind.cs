using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhinoInside.Revit.External.DB
{
  [Flags]
  public enum ElementKind
  {
    None      = 0,
    System    = 1,
    Component = 2,
    Direct    = 4
  }
}
