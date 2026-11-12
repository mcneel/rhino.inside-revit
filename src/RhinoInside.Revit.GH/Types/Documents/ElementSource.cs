using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Element Source")]
  public interface IGH_ElementSource : IGH_Goo
  {
    public Document SourceDocument { get; }
    public RevitLinkInstance SourceInstance { get; }
  }
}
