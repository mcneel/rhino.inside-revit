using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class AnalysisComponent : Component
  {
    protected AnalysisComponent(string name, string nickname, string description, string category, string subCategory)
      : base(name, nickname, description, category, subCategory) { }

    protected void PipeHostParameter(IGH_DataAccess DA, DB.Element srcElement, DB.BuiltInParameter srcParam, string paramName)
    {
      DA.SetData(paramName, srcElement?.get_Parameter(srcParam).AsGoo());
    }

    protected void PipeHostParameter<T>(IGH_DataAccess DA, DB.Element srcElement, DB.BuiltInParameter srcParam, string paramName) where T: Types.GH_Enumerate, new()
    {
      if (srcElement is null)
      {
        DA.SetData(paramName, null);
        return;
      }

      var param = srcElement.get_Parameter(srcParam);
      if (param != null && param.StorageType == DB.StorageType.Integer)
      {
        var enumType = new T();
        enumType.Value = param.AsInteger();
        DA.SetData(paramName, enumType);
      }
    }
  }
}
