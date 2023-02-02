namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Annotation")]
  public interface IGH_Annotation : IGH_GraphicalElement
  {
    GeometryObject[] References { get; }
  }
}
