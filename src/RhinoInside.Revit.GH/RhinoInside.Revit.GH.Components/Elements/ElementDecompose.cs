using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;
using static System.Math;
using static Rhino.RhinoMath;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  public class ElementDecompose : Component, IGH_VariableParameterComponent
  {
    public override Guid ComponentGuid => new Guid("FAD33C4B-A7C3-479B-B309-8F5363B25599");
    public ElementDecompose() : base("Element.Decompose", "Decompose", "Decomposes an Element into its parameters", "Revit", "Element") { }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      bool hasInputData = !Params.Input[0].VolatileData.IsEmpty;
      bool hasOutputParameters = Params.Output.Count > 0;

      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Get common parameters", Menu_PopulateOutputsWithCommonParameters, hasInputData, false);
      Menu_AppendItem(menu, "Get all parameters", Menu_PopulateOutputsWithAllParameters, hasInputData, false);
      Menu_AppendItem(menu, "Remove unconnected parameters", Menu_RemoveUnconnectedParameters, hasOutputParameters, false);
    }

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
            if (Owner is ElementDecompose elementDecompose)
            {
              sender.ActiveInteraction = null;
              if (shift)
                elementDecompose.Menu_PopulateOutputsWithCommonParameters(sender, e);
              else if (ctrl)
                elementDecompose.Menu_RemoveUnconnectedParameters(sender, e);

              return GH_ObjectResponse.Handled;
            }
          }
        }

        return GH_ObjectResponse.Ignore;
      }
    }

    public override void CreateAttributes() => m_attributes = new ComponentAttributes(this);

    void AddOutputParameter(IGH_Param param)
    {
      if (param.Attributes is null)
        param.Attributes = new GH_LinkedParamAttributes(param, Attributes);

      param.Access = GH_ParamAccess.item;
      Params.RegisterOutputParam(param);
    }

    void Menu_PopulateOutputsWithCommonParameters(object sender, EventArgs e)
    {
      var common = default(HashSet<Parameters.Documents.Params.ParameterParam>);
      foreach (var goo in Params.Input[0].VolatileData.AllData(true).OfType<Types.Element>())
      {
        var element = (DB.Element) goo;
        if (element is null)
          continue;

        var current = new HashSet<Parameters.Documents.Params.ParameterParam>();
        foreach (var param in element.GetOrderedParameters())
          current.Add(new Parameters.Documents.Params.ParameterParam(param));

        if (common is null)
          common = current;
        else
          common.IntersectWith(current);
      }

      RecordUndoEvent("Get Common Parameters");

      PopulateOutputParameters(common);
    }

    void Menu_PopulateOutputsWithAllParameters(object sender, EventArgs e)
    {
      var all = new HashSet<Parameters.Documents.Params.ParameterParam>();
      foreach (var goo in Params.Input[0].VolatileData.AllData(true).OfType<Types.Element>())
      {
        var element = (DB.Element) goo;
        if (element is null)
          continue;

        foreach (var param in element.GetOrderedParameters())
          all.Add(new Parameters.Documents.Params.ParameterParam(param));
      }

      RecordUndoEvent("Get All Parameters");

      PopulateOutputParameters(all);
    }

    void PopulateOutputParameters(IEnumerable<Parameters.Documents.Params.ParameterParam> parameters)
    {
      var connected = new Dictionary<Parameters.Documents.Params.ParameterParam, IList<IGH_Param>>();
      foreach (var output in Params.Output.ToArray())
      {
        if
        (
          output.Recipients.Count > 0 &&
          output is Parameters.Documents.Params.ParameterParam param
        )
          connected.Add(param, param.Recipients.ToArray());

        Params.UnregisterOutputParameter(output);
      }

      if (parameters is object)
      {
        foreach (var group in parameters.GroupBy(x => x.ParameterGroup).OrderBy(x => x.Key))
        {
          foreach (var parameter in group.OrderBy(x => x.ParameterBuiltInId))
          {
            AddOutputParameter(parameter);

            if (connected.TryGetValue(parameter, out var recipients))
            {
              foreach (var recipient in recipients)
                recipient.AddSource(parameter);
            }
          }
        }
      }

      Params.OnParametersChanged();
      ExpireSolution(true);
    }

    void Menu_RemoveUnconnectedParameters(object sender, EventArgs e)
    {
      RecordUndoEvent("Remove Unconnected Outputs");

      foreach (var output in Params.Output.ToArray())
      {
        if (output.Recipients.Count > 0)
          continue;

        Params.UnregisterOutputParameter(output);
      }

      Params.OnParametersChanged();
      OnDisplayExpired(false);
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to inspect", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager) { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Element element = null;
      if (!DA.GetData("Element", ref element))
        return;

      for (int p = 0; p < Params.Output.Count; ++p)
      {
        if (Params.Output[p] is Parameters.Documents.Params.ParameterParam param)
          DA.SetData(p, param.GetParameter(element));
      }
    }

    bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
    bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Output;
    IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
    bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => true;
    void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

    protected override string HtmlHelp_Source()
    {
      var nTopic = new Grasshopper.GUI.HTML.GH_HtmlFormatter(this)
      {
        Title = Name,
        Description =
        @"<p>This component is a special interface object that allows for quick accessing to Revit Element parameters.</p>" +
        @"<p>It's able to modify itself in order to show any parameter its input element parameter contains. " +
        @"It also allows to remove some output parameters if are not connected to anything else.</p>" +
        @"<p>Under the component contextual menu you would find these options:</p>" +
        @"<dl>" +
        @"<dt><b>Get common parameters</b></dt><dd>Populates the output parameters list with common parameters in all input elements</dd>" +
        @"<dt><b>Get all parameters</b></dt><dd>Populates the output parameters list with all parameters found in all input elements</dd>" +
        @"<dt><b>Remove unconnected parameters</b></dt><dd>Removes the output parameters that are not connected to anything else</dd>" +
        @"</dl>",
        ContactURI = @"https://www.rhino3d.com/inside/revit/beta/"
      };

      nTopic.AddRemark("SHIFT + Double click runs \"Get common parameters\"");
      nTopic.AddRemark("CTRL + Double click runs \"Remove unconnected parameters\".");

      return nTopic.HtmlFormat();
    }
  }
}
