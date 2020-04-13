using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class InstanceExtension
  {
    public static void SetTransform(this Instance element, XYZ newOrigin, XYZ newBasisX, XYZ newBasisY)
    {
      var current = element.GetTransform();
      var BasisZ = newBasisX.CrossProduct(newBasisY);
      {
        if (!current.BasisZ.IsParallelTo(BasisZ))
        {
          var axisDirection = current.BasisZ.CrossProduct(BasisZ);
          double angle = current.BasisZ.AngleTo(BasisZ);

          using (var axis = Line.CreateUnbound(current.Origin, axisDirection))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);

          current = element.GetTransform();
        }

        if (!current.BasisX.IsAlmostEqualTo(newBasisX))
        {
          double angle = current.BasisX.AngleOnPlaneTo(newBasisX, BasisZ);
          using (var axis = Line.CreateUnbound(current.Origin, BasisZ))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);
        }

        {
          var trans = newOrigin - current.Origin;
          if (!trans.IsZeroLength())
            ElementTransformUtils.MoveElement(element.Document, element.Id, trans);
        }
      }
    }
  }
}
