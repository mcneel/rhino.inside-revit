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

    public HostObjectFaces()
    : base("HostObject.Faces", "HostObject.Faces", "Obtains a set of types that are owned by Family", "Revit", "Host")
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
      {
        var bottom = DB.HostObjectUtils.GetBottomFaces(host).Select(reference => new Types.Face(doc, reference));
        DA.SetDataList("Bottom", bottom);
      }

      {
        var top = DB.HostObjectUtils.GetTopFaces(host).Select(reference => new Types.Face(doc, reference));
        DA.SetDataList("Top", top);
      }

      {
        var interior = DB.HostObjectUtils.GetSideFaces(host, DB.ShellLayerType.Interior).Select(reference => new Types.Face(doc, reference));
        DA.SetDataList("Interior", interior);
      }

      {
        var exterior = DB.HostObjectUtils.GetSideFaces(host, DB.ShellLayerType.Exterior).Select(reference => new Types.Face(doc, reference));
        DA.SetDataList("Exterior", exterior);
      }
    }
  }
}
