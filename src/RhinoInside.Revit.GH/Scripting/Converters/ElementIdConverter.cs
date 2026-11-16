using System;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;

namespace RhinoInside.Revit.GH.Scripting.Converters
{
  public sealed class ElementIdConverter : ParamValueConverter
  {
    public static ParamConverterIdentity Identity { get; }
      = new ParamConverterIdentity(new Guid("5716bd31-2735-4be5-88ac-bcc0304be776"), "ElementId", "mcneel.rhino3dinrevit.rhino");

    public ElementIdConverter()
      : base(Identity, new ParamType(typeof(Autodesk.Revit.DB.ElementId)))
    {
      Image = default;
      Category = "Revit";
      Description = "Converts DB.Element to DB.ElementId";
    }

    public override bool Cast(ConvertDirection direction, object data, out object target)
    {
      target = default;

      if (ConvertDirection.Incoming == direction
            && data is Autodesk.Revit.DB.Element element)
      {
        target = element.Id;
        return true;
      }

      return false;
    }
  }
}
