using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class HostObjectFaces : Component
  {
    public override Guid ComponentGuid => new Guid("032AD3F7-9E55-44B6-BE79-3DBF67D98F14");
    protected override string IconTag => "F";

    public HostObjectFaces() : base
    (
      name: "Host Faces",
      nickname: "Faces",
      description: "Obtains a set of types that are owned by Family",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.HostObject(), "Host", "H", "Host object to query for its faces", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Face(), "Bottom", "B", string.Empty, GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Face(), "Top", "T", string.Empty, GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Face(), "Interior", "I", string.Empty, GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Face(), "Exterior", "E", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.HostObject host = null;
      if (!DA.GetData("Host", ref host) || host is null)
        return;

      var doc = host.Document;
      try
      {
        var bottom = DB.HostObjectUtils.GetBottomFaces(host).Select(reference => new Types.Face(doc, reference));
        DA.SetDataList("Bottom", bottom);
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      try
      {
        var top = DB.HostObjectUtils.GetTopFaces(host).Select(reference => new Types.Face(doc, reference));
        DA.SetDataList("Top", top);
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      try
      {
        var interior = DB.HostObjectUtils.GetSideFaces(host, DB.ShellLayerType.Interior).Select(reference => new Types.Face(doc, reference));
        DA.SetDataList("Interior", interior);
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      try
      {
        var exterior = DB.HostObjectUtils.GetSideFaces(host, DB.ShellLayerType.Exterior).Select(reference => new Types.Face(doc, reference));
        DA.SetDataList("Exterior", exterior);
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }
    }
  }
}
