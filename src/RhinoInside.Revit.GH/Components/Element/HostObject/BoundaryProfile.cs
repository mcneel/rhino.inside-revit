using System;
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Components.Hosts
{
  using External.DB.Extensions;
  using Convert.Geometry;

  [ComponentVersion(introduced: "1.0", updated: "1.9")]
  public class HostObjectBoundaryProfile : TransactionalComponent
  {
    public override Guid ComponentGuid => new Guid("7CE0BD56-A2AC-4D49-A39B-7B34FE897265");
    protected override string IconTag => "B";

    public HostObjectBoundaryProfile() : base
    (
      name: "Host Boundary Profile",
      nickname: "BoundProf",
      description: "Get the boundary profile of the given host element",
      category: "Revit",
      subCategory: "Host"
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
          Description = "Host to acces boundary profile",
        }
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Profile",
          NickName = "PC",
          Description = "Sketch profile curves",
          Optional = true,
          Access = GH_ParamAccess.list
        },
#if REVIT_2022
        ParamRelevance.Primary
#else
        ParamRelevance.Occasional
#endif
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
        new Param_Plane()
        {
          Name = "Plane",
          NickName = "P",
          Description = "Sketch plane",
        },ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Profile",
          NickName = "PC",
          Description = "Sketch profile curves",
          Access = GH_ParamAccess.list
        },ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Host", out Types.HostObject host, x => x.IsValid)) return;
      if (Params.GetDataList(DA, "Profile", out IList<Curve> profiles))
      {
#if REVIT_2022
        // TODO: Compare with current profiles, maybe no transaction is necessary

        using (var scope = new ARDB.SketchEditScope(host.Document, NickName))
        {
          if (scope.IsSketchEditingSupportedForSketchBasedElement(host.Value))
          {
            var sketch = host.Value.GetSketch();
            var sketchId = sketch?.Id ?? ARDB.ElementId.InvalidElementId;

            if (scope.IsElementWithoutSketch(host.Value))
            {
              sketch = scope.StartWithNewSketch(host.Value);
            }
            else if (scope.IsSketchEditingSupported(sketchId))
            {
              scope.Start(sketchId);
            }
            else sketch = null;

            if (sketch is object)
            {
              using (var tx = NewTransaction(host.Document))
              {
                tx.Start();

                var tol = GeometryTolerance.Model;
                var modelCurves = sketch.GetProfileCurveElements();
                foreach (var modelCurve in modelCurves)
                  sketch.Document.Delete(modelCurve.Select(x => x.Id).ToArray());

                foreach (var profile in profiles)
                {
                  var loop = Curve.ProjectToPlane(profile, sketch.SketchPlane.GetPlane().ToPlane());

                  var segments = loop.TryGetPolyCurve(out var polyCurve, tol.AngleTolerance) ?
                    polyCurve.DuplicateSegments() :
                    new Curve[] { loop };

                  foreach (var segment in segments)
                  {
                    var curve = segment.ToCurve();
                    sketch.Document.Create.NewModelCurve(curve, sketch.SketchPlane);
                  }
                }

                // Commit Scope
                {
                  EventHandler<ARUI.Events.DialogBoxShowingEventArgs> DialogBoxShowing = null;
                  try
                  {
                    Revit.ActiveUIApplication.DialogBoxShowing += DialogBoxShowing = (sender, args) =>
                    {
                      if (args.DialogId == "TaskDialog_Sketch_Edits_Discarded")
                        args.OverrideResult(1001 /*IDYES*/);
                    };

                    if (CommitTransaction(host.Document, tx) == ARDB.TransactionStatus.Committed)
                    {
                      scope.Commit(CreateFailuresPreprocessor());
                      host.InvalidateGraphics();
                    }
                    else scope.Cancel();
                  }
                  catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return; }
                  finally { Revit.ActiveUIApplication.DialogBoxShowing -= DialogBoxShowing; }
                }
              }
            }
            else
            {
              if (sketchId == ARDB.ElementId.InvalidElementId)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Host element is not sketch based. {{{host.Id}}}");
              else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Host element sketch is not editable. {{{host.Id}}}");
            }
          }
          else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Host sketch does not support editing. {{{host.Id}}}");
        }
#else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Edit Boundary Profile is only supported on Revit 2022 or above.");
        return;
#endif
      }

      {
        Params.TrySetData(DA, "Host", () => host);

        if (host is Types.ISketchAccess access && access.Sketch is Types.Sketch sketch)
        {
          Params.TrySetData(DA, "Plane", () => sketch.ProfilesPlane);
          Params.TrySetDataList(DA, "Profile", () => sketch.Profiles);
        }
      }
    }
  }
}
