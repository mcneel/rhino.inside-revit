using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Host
{
  public class HostObjectInserts : Component
  {
    public override Guid ComponentGuid => new Guid("70CCF7A6-856C-4D24-A82B-BC1D4FC63078");
    protected override string IconTag => "I";

    public HostObjectInserts() : base
    (
      name: "Host Inserts",
      nickname: "Inserts",
      description: "Obtains a set of types that are owned by Family",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.HostObject(), "Host", "H", "Host object to query for its inserts", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Inserts", "INS", "Embedded inserts", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.GraphicalElement(), "Shared", "SHI", "Embedded shared inserts", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Element(), "Shadows", "S", "Embedded shadows", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Element(), "Openings", "O", "Embedded rectangular openings", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.HostObject(), "Walls", "W", "Embedded walls", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.HostObject host = null;
      if (!DA.GetData("Host", ref host) || host is null)
        return;

      var doc = host.Document;

      var inserts = new HashSet<DB.ElementId>(host.FindInserts(false, false, false, false));
      DA.SetDataList("Inserts", inserts.Select(x => Types.Element.FromElementId(doc, x)));

      var shared = new HashSet<DB.ElementId>(host.FindInserts(false, false, false, true));
      shared.ExceptWith(inserts);
      DA.SetDataList("Shared", shared.Select(x => Types.Element.FromElementId(doc, x)));

      var openings = new HashSet<DB.ElementId>(host.FindInserts(true, false, false, false));
      openings.ExceptWith(inserts);
      DA.SetDataList("Openings", openings.Select(x => Types.Element.FromElementId(doc, x)));

      var shadows = new HashSet<DB.ElementId>(host.FindInserts(false, true, false, false));
      shadows.ExceptWith(inserts);
      DA.SetDataList("Shadows", shadows.Select(x => Types.Element.FromElementId(doc, x)));

      var walls = new HashSet<DB.ElementId>(host.FindInserts(false, false, true, false));
      walls.ExceptWith(inserts);
      DA.SetDataList("Walls", walls.Select(x => Types.Element.FromElementId(doc, x)));
    }
  }
}
