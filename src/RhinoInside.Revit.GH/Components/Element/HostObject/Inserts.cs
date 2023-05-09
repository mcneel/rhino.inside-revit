using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.HostObjects
{
  [ComponentVersion(introduced: "1.0", updated: "1.13")]
  public class HostObjectInserts : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("70CCF7A6-856C-4D24-A82B-BC1D4FC63078");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override string IconTag => "I";

    public HostObjectInserts() : base
    (
      name: "Hosted Elements",
      nickname: "Hosted",
      description: "Obtains a set of elements hosted by the input Host",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.HostObject()
        {
          Name = "Host",
          NickName = "H",
          Description = "Host object to query for its inserts",
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.HostObject()
        {
          Name = "Host",
          NickName = "H",
          Description = "Accessed Host element",
        },ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Inserts",
          NickName = "INS",
          Description = "Embedded inserts",
          Access = GH_ParamAccess.list
        },ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Shared",
          NickName = "SHI",
          Description = "Embedded shared inserts",
          Access = GH_ParamAccess.list
        },ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Shadows",
          NickName = "S",
          Description = "Embedded shadows",
          Access = GH_ParamAccess.list
        },ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Opening()
        {
          Name = "Openings",
          NickName = "O",
          Description = "Embedded openings",
          Access = GH_ParamAccess.list
        },ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Wall()
        {
          Name = "Walls",
          NickName = "W",
          Description = "Embedded walls",
          Access = GH_ParamAccess.list
        },ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Host", out Types.HostObject host, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Host", () => host);

      var doc = host.Document;

      var inserts = new HashSet<ARDB.ElementId>(host.Value.FindInserts(false, false, false, false));
      Params.TrySetDataList(DA, "Inserts", () => inserts.Select(x => host.GetElement<Types.GraphicalElement>(x)));

      Params.TrySetDataList
      (
        DA, "Shared",
        () =>
        {
          var shared = new HashSet<ARDB.ElementId>(host.Value.FindInserts(false, false, false, true));
          shared.ExceptWith(inserts);

          return shared.Select(x => host.GetElement<Types.GraphicalElement>(x));
        }
      );

      Params.TrySetDataList
      (
        DA, "Openings",
        () =>
        {
          var openings = new HashSet<ARDB.ElementId>(host.Value.FindInserts(true, false, false, false));
          openings.ExceptWith(inserts);

          foreach (var opening in host.Value.GetDependentElements(new ARDB.ElementClassFilter(typeof(ARDB.Opening))))
            openings.Add(opening);

          return openings.Select(x => host.GetElement<Types.GraphicalElement>(x));
        }
      );

      Params.TrySetDataList
      (
        DA, "Shadows",
        () =>
        {
          var shadows = new HashSet<ARDB.ElementId>(host.Value.FindInserts(false, true, false, false));
          shadows.ExceptWith(inserts);

          return shadows.Select(x => host.GetElement<Types.GraphicalElement>(x));
        }
      );

      Params.TrySetDataList
      (
        DA, "Walls",
        () =>
        {
          var walls = new HashSet<ARDB.ElementId>(host.Value.FindInserts(false, false, true, false));
          walls.ExceptWith(inserts);

          return walls.Select(x => host.GetElement<Types.GraphicalElement>(x));
        }
      );
    }
  }
}
