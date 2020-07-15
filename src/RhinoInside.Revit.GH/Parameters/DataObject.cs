using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  class DataObject<T> : GH_Param<Types.DataObject<T>>
  {
    public override Guid ComponentGuid => new Guid("F25FAC7B-B338-4E12-A974-F2238E3B83C2");

    protected override Bitmap Icon => (Bitmap) Properties.Resources.ResourceManager.GetObject(typeof(T).Name);

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public DataObject() : base(
      name: "RevitAPIDataObject",
      nickname: "RevitAPIDataObject",
      description: "Wraps Types.DataObject",
      category: string.Empty,
      subcategory: string.Empty,
      access: GH_ParamAccess.item)
    { }
  }
}
