using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;

using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Documents.Params
{
  [
    Guid("3D9979B4-65C8-447F-BCEA-3705249DF3B6"),
    DisplayName("Parameter Group"),
    Description("Represents a Revit parameter group."),
    Exposure(GH_Exposure.quarternary),
  ]
  public class BuiltInParameterGroup : GH_Enum<DB.BuiltInParameterGroup>
  {
    public BuiltInParameterGroup() : base(DB.BuiltInParameterGroup.INVALID) { }
    public override string ToString()
    {
      try { return DB.LabelUtils.GetLabelFor(Value); }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

      return base.ToString();
    }

    public override Array GetEnumValues() =>
      Enum.GetValues(typeof(DB.BuiltInParameterGroup)).
      Cast<DB.BuiltInParameterGroup>().
      Where(x => x != DB.BuiltInParameterGroup.INVALID).
      OrderBy(x => DB.LabelUtils.GetLabelFor(x)).
      ToArray();
  }
}
