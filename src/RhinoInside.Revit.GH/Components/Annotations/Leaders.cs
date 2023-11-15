using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.12")]
  public class AnnotationLeaders : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("71F014DE-5EE8-4FC8-97E6-89E60CE9B772");
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    protected override string IconTag => string.Empty;

    public AnnotationLeaders() : base
    (
      name: "Annotation Leaders",
      nickname: "Leaders",
      description: string.Empty,
      category: "Revit",
      subCategory: "Annotate"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Annotation()
        {
          Name = "Annotation",
          NickName = "A",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Leader",
          NickName = "L",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Shoulder Locations",
          NickName = "SL",
          Description = "Annotation shoulder locations",
          Optional= true,
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "End Locations",
          NickName = "EL",
          Description = "Annotation end locations",
          Optional= true,
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Text Locations",
          NickName = "TL",
          Description = "Annotation text locations",
          Optional= true,
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Annotation()
        {
          Name = "Annotation",
          NickName = "A",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Curves",
          NickName = "C",
          Description = "Leader curves",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Leader",
          NickName = "L",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Head Locations",
          NickName = "HL",
          Description = "Annotation end locations",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Shoulder Locations",
          NickName = "SL",
          Description = "Annotation shoulder locations",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "End Locations",
          NickName = "EL",
          Description = "Annotation end locations",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Text Locations",
          NickName = "TL",
          Description = "Annotation text locations",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Annotation", out Types.IGH_Annotation annotation, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Annotation", () => annotation);

      if (annotation is Types.IAnnotationLeadersAccess leaderElement)
      {
        if (Params.GetData(DA, "Leader", out bool? hasLeader))
        {
          StartTransaction(annotation.Document);
          leaderElement.HasLeader = hasLeader;
        }

        if (!(hasLeader is false))
        {
          if (Params.GetDataList(DA, "End Locations", out IList<Point3d?> endPositions))
          {
            StartTransaction(annotation.Document);

            if (annotation.Value is Autodesk.Revit.DB.TextNote note)
            {
              var location = annotation.Location;
              note.RemoveLeaders();
              foreach (var point in endPositions)
              {
                if (!point.HasValue) continue;
                location.ClosestParameter(point.Value, out var u, out var v);

                if (note.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.ARC_LEADER_PARAM).AsBoolean())
                {
                  if (u < 0.0)
                    note.AddLeader(Autodesk.Revit.DB.TextNoteLeaderTypes.TNLT_ARC_L);
                  else
                    note.AddLeader(Autodesk.Revit.DB.TextNoteLeaderTypes.TNLT_ARC_R);
                }
                else
                {
                  if (u < 0.0)
                    note.AddLeader(Autodesk.Revit.DB.TextNoteLeaderTypes.TNLT_STRAIGHT_L);
                  else
                    note.AddLeader(Autodesk.Revit.DB.TextNoteLeaderTypes.TNLT_STRAIGHT_R);
                }
              }
            }

            foreach (var (leader, end) in leaderElement.Leaders.ZipOrLast(endPositions))
            {
              if (end is null) continue;
              if (leader is null) continue;
              leaderElement.HasLeader = true;
              leader.EndPosition = end.Value;
            }
          }

          if (Params.GetDataList(DA, "Shoulder Locations", out IList<Point3d?> shoulderPositions))
          {
            StartTransaction(annotation.Document);
            foreach (var (leader, shoulder) in leaderElement.Leaders.ZipOrLast(shoulderPositions))
            {
              if (shoulder is null) continue;
              if (leader is null) continue;
              leader.ElbowPosition = shoulder.Value;
            }
          }

          Params.TrySetData(DA, "Leader", () => leaderElement.HasLeader);
        }

        if (Params.GetDataList(DA, "Text Locations", out IList<Point3d?> textPositions))
        {
          StartTransaction(annotation.Document);
          foreach (var (segment, text) in leaderElement.Leaders.ZipOrLast(textPositions))
          {
            if (text is null) continue;
            if (segment is null) continue;
            if (segment.IsTextPositionAdjustable)
              segment.TextPosition = text.Value;
            else
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"´Text Position can´t be adjusted. {{{annotation.Id.ToString("D")}}}");
          }
        }

        // Regenerate is necessary here to obtain correct elbow positions.
        annotation.Document.Regenerate();
        var leaders = leaderElement.Leaders;

        Params.TrySetDataList
        (
          DA, "Head Locations",
          () => leaders.Select(x => x.HeadPosition)
        );

        if (leaderElement.HasLeader is true)
        {
          Params.TrySetDataList
          (
            DA, "Curves",
            () => leaders.Select(x => x.LeaderCurve)
          );

          Params.TrySetDataList
          (
            DA, "Shoulder Locations",
            () => leaders.Select(x => x.HasElbow ? x.ElbowPosition : default(Point3d?))
          );

          Params.TrySetDataList
          (
            DA, "End Locations",
            () => leaders.Select(x => x.EndPosition)
          );
        }

        Params.TrySetDataList
        (
          DA, "Text Locations",
          () => leaders.Select(x => x.TextPosition)
        );
      }
    }
  }
}
