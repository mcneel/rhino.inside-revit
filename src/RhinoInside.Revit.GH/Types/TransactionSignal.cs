using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  [
    ComponentGuid("9C008EDE-6DB1-4460-9CD3-B6D03C3BEEE3"),
    Name("Transaction Signal"),
    Description("Represents a Revit Transaction Signal."),
  ]
  public class TransactionSignal : GH_Enum<DBX.TransactionSignal>
  {
    public static new ReadOnlyDictionary<int, string> NamedValues { get; } = new ReadOnlyDictionary<int, string>
    (
      new Dictionary<int, string>
      {
        { (int) DBX.TransactionSignal.Frozen,     "Frozen"    },
        { (int) DBX.TransactionSignal.Effective,  "Effective" },
        { (int) DBX.TransactionSignal.Simulated,  "Simulated" },
      }
    );

    public override bool CastFrom(object source)
    {
      if (source is GH_Boolean boolean)
        source = boolean.Value;

      if (source is bool booleanValue)
      {
        Value = booleanValue ? DBX.TransactionSignal.Effective : DBX.TransactionSignal.Frozen;
        return true;
      }

      return base.CastFrom(source);
    }
  }
}
