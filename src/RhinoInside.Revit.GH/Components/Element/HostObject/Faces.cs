using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Hosts
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
      ARDB.HostObject host = null;
      if (!DA.GetData("Host", ref host) || host is null)
        return;

      var doc = host.Document;
      if (host is ARDB.RoofBase || host is ARDB.CeilingAndFloor)
      {
        try
        {
          var bottom = ARDB.HostObjectUtils.GetBottomFaces(host).
            Where(x => host.GetGeometryObjectFromReference(x) is ARDB.Face).
            Select(reference => new Types.Face(doc, reference));
          DA.SetDataList("Bottom", bottom);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }

        try
        {
          var top = ARDB.HostObjectUtils.GetTopFaces(host).
            Where(x => host.GetGeometryObjectFromReference(x) is ARDB.Face).
            Select(reference => new Types.Face(doc, reference));
          DA.SetDataList("Top", top);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      }

      if (host is ARDB.Wall || host is ARDB.FaceWall)
      {
        try
        {
          var interior = ARDB.HostObjectUtils.GetSideFaces(host, ARDB.ShellLayerType.Interior).
            Where(x => host.GetGeometryObjectFromReference(x) is ARDB.Face).
            Select(reference => new Types.Face(doc, reference));
          DA.SetDataList("Interior", interior);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }

        try
        {
          var exterior = ARDB.HostObjectUtils.GetSideFaces(host, ARDB.ShellLayerType.Exterior).
            Where(x => host.GetGeometryObjectFromReference(x) is ARDB.Face).
            Select(reference => new Types.Face(doc, reference));
          DA.SetDataList("Exterior", exterior);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      }
    }
  }
}
