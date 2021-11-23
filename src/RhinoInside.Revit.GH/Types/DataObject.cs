using System;

using Grasshopper.Kernel.Types;

using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class DataObject<T> : GH_Goo<T>
  {
    public override bool IsValid => Value != null;
    public override string TypeName => $"Revit {typeof(T).Name}";
    public override string TypeDescription => $"Represents a {TypeName}";

    public ARDB.Document Document { get; private set; } = default;

    public DataObject() { }
    public DataObject(T apiObject) : base(apiObject) {}
    public DataObject(T apiObject, ARDB.Document sourceDoc) : base(apiObject)
    {
      Document = sourceDoc;
    }

    public override IGH_Goo Duplicate() => throw new NotImplementedException();

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(T)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public override string ToString()
    {
      return IsValid ?
             TypeName :
             $"Invalid {TypeName}";
    }
  }
}
