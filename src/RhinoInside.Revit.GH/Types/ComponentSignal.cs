using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.GH.Kernel.Attributes;

namespace RhinoInside.Revit.GH.Types
{
  [
    ComponentGuid("70B49D97-E636-4470-953B-4878C04E7D64"),
    Name("Component Signal"),
    Description("Represents a Component Signal."),
  ]
  public class ComponentSignal : GH_Enum<Kernel.ComponentSignal>
  {
    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) Kernel.ComponentSignal.Frozen,  "Frozen" },
        { (int) Kernel.ComponentSignal.Active,  "Active" },
      }
    );

    public override bool CastFrom(object source)
    {
      if (source is GH_Boolean boolean)
        source = boolean.Value;

      if (source is bool booleanValue)
      {
        Value = booleanValue ? Kernel.ComponentSignal.Active : Kernel.ComponentSignal.Frozen;
        return true;
      }

      return base.CastFrom(source);
    }
  }
}
