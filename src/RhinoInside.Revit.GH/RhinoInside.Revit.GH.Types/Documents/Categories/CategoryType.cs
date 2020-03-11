using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;

using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Documents.Categories
{
  [
    Guid("195B9D7E-D4B0-4335-A442-3C2FA40794A2"),
    DisplayName("Category Type"),
    Description("Represents a Revit parameter category type."),
    Exposure(GH_Exposure.septenary),
  ]
  public class CategoryType : GH_Enum<DB.CategoryType>
  {
    public CategoryType() : base(DB.CategoryType.Invalid) { }
    public CategoryType(DB.CategoryType value) : base(value) { }

    public override Array GetEnumValues() =>
      Enum.GetValues(typeof(DB.CategoryType)).
      Cast<DB.CategoryType>().
      Where(x => x != DB.CategoryType.Invalid).
      ToArray();

    public override string ToString()
    {
      if (Value == DB.CategoryType.AnalyticalModel)
        return "Analytical";

      return base.ToString();
    }
  }
}
