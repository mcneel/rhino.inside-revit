using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ClusterViewsByType : Component, IGH_VariableParameterComponent
  {
    public override Guid ComponentGuid => new Guid("f6b99fe2-19e1-4840-96c1-13873a0aece8");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "BV";

    public ClusterViewsByType() : base
    (
      name: "Cluster Views By Family",
      nickname: "BV",
      description: "Split a list of views into separate clusters by their family",
      category: "Revit",
      subCategory: "View"
    )
    { }

    class ComponentAttributes : GH_ComponentAttributes
    {
      public ComponentAttributes(IGH_Component component) : base(component) { }

      public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        bool ctrl = Control.ModifierKeys == Keys.Control;
        bool shift = Control.ModifierKeys == Keys.Shift;

        if (e.Button == MouseButtons.Left && (ctrl || shift))
        {
          if (!Owner.Params.Input[0].VolatileData.IsEmpty)
          {
            if (Owner is ClusterViewsByType collectViews)
            {
              sender.ActiveInteraction = null;
              if (shift)
                collectViews.OutputParamsHandler(new OutputParamsExpand());
              else if (ctrl)
                collectViews.OutputParamsHandler(new OutputParamsCollapse());

              return GH_ObjectResponse.Handled;
            }
          }
        }

        return GH_ObjectResponse.Ignore;
      }
    }

    public override void CreateAttributes() => m_attributes = new ComponentAttributes(this);

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.View(), "Views", "Views", "List of views to be split into branches based on type", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      OutputParamsHandler(new OutputParamsRegister
      {
        Manager = manager
      });
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var views = new List<DB.View>();
      if (!DA.GetDataList("Views", views))
        return;

      OutputParamsHandler(new OutputParamsSetData
      {
        DA = DA,
        Views = views
      });
    }

    class OutputParamsHandleContext { }
    class OutputParamsRegister : OutputParamsHandleContext
    {
      public GH_OutputParamManager Manager { get; set; }
    }
    class OutputParamsSetData : OutputParamsHandleContext
    {
      public IGH_DataAccess DA { get; set; }
      public List<DB.View> Views { get; set; }
    }

    class OutputParamsExpand : OutputParamsHandleContext
    {
      public Dictionary<string, IList<IGH_Param>> Connections { get; set; } = new Dictionary<string, IList<IGH_Param>>();
    }
    class OutputParamsCollapse : OutputParamsHandleContext { }

    readonly List<IGH_Param> _params = new List<IGH_Param>();

    void OutputParamsHandler(OutputParamsHandleContext ctx)
    {
      if (ctx is OutputParamsExpand opPreExp)
      {
        RecordUndoEvent("Show All Outputs");

        // if expanding to show all params, remove whatever is left on component
        // but collect the connections first. Then re-register the params and
        // apply the connections
        opPreExp.Connections.Clear();
        foreach (var param in Params.Output.ToArray())
        {
          if (param.Recipients.Count > 0)
            opPreExp.Connections.Add(param.Name, param.Recipients.ToArray());

          Params.UnregisterOutputParameter(param);
        }

        foreach (var param in _params)
        {
          Params.RegisterOutputParam(param);
          if (opPreExp.Connections.TryGetValue(param.Name, out var recipients))
          {
            foreach (var recipient in recipients)
              recipient.AddSource(param);
          }
        }

        Params.OnParametersChanged();
        ExpireSolution(true);
      }

      else if (ctx is OutputParamsCollapse)
      {
        RecordUndoEvent("Remove Unconnected Outputs");

        foreach (var param in Params.Output.ToArray())
        {
          if (Params.Output.Find(p => p.Name == param.Name) is IGH_Param output)
          {
            if (output.Recipients.Count <= 0)
              Params.UnregisterOutputParameter(output);
          }
        }

        Params.OnParametersChanged();
        OnDisplayExpired(false);
      }

      else
      {
        foreach (DB.ViewType value in Enum.GetValues(typeof(DB.ViewType)))
        {
          var paramName = $"{value}";
          var paramNickname = paramName.Substring(0, 1);
          var paramTip = $"Views of type \"{paramName}\"";

          var param = default(IGH_Param);
          var paramSetter = default(Func<IEnumerable<DB.View>, IEnumerable<Types.Element>>);
          switch (value)
          {
            case DB.ViewType.Undefined:
            case DB.ViewType.Internal:
            case DB.ViewType.ProjectBrowser:
            case DB.ViewType.SystemBrowser:
              continue;

            case DB.ViewType.DrawingSheet:
              param = new Parameters.ViewSheet();
              paramSetter = vs => vs.OfType<DB.ViewSheet>().Select(x => Types.ViewSheet.FromElement(x));
              break;

            default:
              param = new Parameters.View();
              paramSetter = vs => vs.Where(v => v.ViewType == value).Select(x => Types.View.FromElement(x));
              break;
          }

          param.Name = paramName;
          param.NickName = paramNickname;
          param.Description = paramTip;
          param.Access = GH_ParamAccess.list;

          // register at start
          if (ctx is OutputParamsRegister opReg)
          {
            _params.Add(param);
            opReg.Manager.AddParameter(param);
          }
          // set data
          else if (ctx is OutputParamsSetData opSet)
          {
            if (Params.Output.FindIndex(p => p.Name == paramName) is int pindex
                  && pindex >= 0)
              opSet.DA.SetDataList(pindex, paramSetter(opSet.Views));
          }
        }
      }
    }

    bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
    bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Output;
    IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
    bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => true;
    void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
  }
}
