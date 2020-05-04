using System;
using Grasshopper.Kernel;

using RhinoInside.Revit.GH.Types;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class AnalysisComponent : Component
  {
    protected AnalysisComponent(string name, string nickname, string description, string category, string subCategory)
      : base(name, nickname, description, category, subCategory) { }

    protected void PipeHostParameter(IGH_DataAccess DA, DB.Element srcElement, DB.BuiltInParameter srcParam, string paramName)
    {
      var param = srcElement.get_Parameter(srcParam);
      if (param != null)
      {
        switch(param.StorageType)
        {
          case DB.StorageType.None: break;

          case DB.StorageType.String:
            DA.SetData(paramName, param.AsString());
            break;

          case DB.StorageType.Integer:
            DA.SetData(paramName, param.AsInteger());
            break;

          case DB.StorageType.Double:
            DA.SetData(paramName, param.AsDoubleInRhinoUnits());
            break;

          case DB.StorageType.ElementId:
            DA.SetData(
              paramName,
              Types.Element.FromElementId(doc: srcElement.Document, Id: param.AsElementId())
              );
            break;
        }
      }
    }

    protected void PipeHostParameter<T>(IGH_DataAccess DA, DB.Element srcElement, DB.BuiltInParameter srcParam, string paramName) where T: GH_Enumerate, new()
    {

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
