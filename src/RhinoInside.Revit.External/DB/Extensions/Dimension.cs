using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class DimensionExtension
  {
    public static bool GetHasLeader(this Dimension dimension)
    {
#if REVIT_2021
      return dimension.HasLeader;
#else
      return dimension.get_Parameter(BuiltInParameter.DIM_LEADER).AsBoolean();      
#endif
    }

    public static void SetHasLeader(this Dimension dimension, bool value)
    {
#if REVIT_2021
      dimension.HasLeader = value;
#else
      dimension.get_Parameter(BuiltInParameter.DIM_LEADER).Set(value);
#endif
    }
  }
}
