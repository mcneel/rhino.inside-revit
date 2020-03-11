using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;

using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Documents.Params
{
  public class StorageType : GH_Enum<DB.StorageType> { }
}
