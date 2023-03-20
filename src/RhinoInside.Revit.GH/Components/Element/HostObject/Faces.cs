using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.HostObjects
{
  public class HostObjectFaces : Component
  {
    public override Guid ComponentGuid => new Guid("032AD3F7-9E55-44B6-BE79-3DBF67D98F14");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override string IconTag => "F";

    public HostObjectFaces() : base
    (
      name: "Host Faces",
      nickname: "Faces",
      description: "Obtains the faces of a Host element",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.HostObject(), "Host", "H", "Host object to query for its faces", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GeometryFace(), "Top", "T", string.Empty, GH_ParamAccess.list);
      manager.AddParameter(new Parameters.GeometryFace(), "Bottom", "B", string.Empty, GH_ParamAccess.list);
      manager.AddParameter(new Parameters.GeometryFace(), "Exterior", "E", string.Empty, GH_ParamAccess.list);
      manager.AddParameter(new Parameters.GeometryFace(), "Interior", "I", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.HostObject host = null;
      if (!DA.GetData("Host", ref host) || host is null)
        return;

      if (host.Value is ARDB.RoofBase || host.Value is ARDB.CeilingAndFloor)
      {
        try
        {
          var top = ARDB.HostObjectUtils.GetTopFaces(host.Value).
                    Select(host.GetGeometryObjectFromReference<Types.GeometryFace>);
          DA.SetDataList("Top", top);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }

        try
        {
          var bottom = ARDB.HostObjectUtils.GetBottomFaces(host.Value).
                       Select(host.GetGeometryObjectFromReference<Types.GeometryFace>);
          DA.SetDataList("Bottom", bottom);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      }

      if (host.Value is ARDB.Wall || host.Value is ARDB.FaceWall)
      {
        try
        {
          var exterior = ARDB.HostObjectUtils.GetSideFaces(host.Value, ARDB.ShellLayerType.Exterior).
                         Select(host.GetGeometryObjectFromReference<Types.GeometryFace>);
          DA.SetDataList("Exterior", exterior);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }

        try
        {
          var interior = ARDB.HostObjectUtils.GetSideFaces(host.Value, ARDB.ShellLayerType.Interior).
                         Select(host.GetGeometryObjectFromReference<Types.GeometryFace>);
          DA.SetDataList("Interior", interior);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      }
    }
  }
}
