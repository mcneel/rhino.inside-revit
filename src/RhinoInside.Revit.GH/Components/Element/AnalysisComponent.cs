using Grasshopper.Kernel;

using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class AnalysisComponent : Component
  {
    protected AnalysisComponent(string name, string nickname, string description, string category, string subCategory)
      : base(name, nickname, description, category, subCategory) { }

    protected void PipeHostParameter(IGH_DataAccess DA, ARDB.Element srcElement, ARDB.BuiltInParameter srcParam, string paramName)
    {
      DA.SetData(paramName, srcElement?.get_Parameter(srcParam).AsGoo());
    }
  }
}
