using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace RhinoInside.Revit.GH.Components
{
  /// <summary>
  /// Base class for all variable parameter components
  /// </summary>
  /// <seealso cref="IGH_VariableParameterComponent"/>
  public abstract class ZuiComponent : Component, IGH_VariableParameterComponent
  {
    protected ZuiComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    [Flags]
    protected enum ParamVisibility
    {
      Voluntary = 0,
      Mandatory = 1,
      Default = 2,
      Binding = 3
    }

    protected struct ParamDefinition
    {
      public ParamDefinition(IGH_Param param)
      {
        Param = param;
        Relevance = ParamVisibility.Binding;
      }

      public ParamDefinition(IGH_Param param, ParamVisibility relevance)
      {
        Param = param;
        Relevance = relevance;
      }

      public readonly IGH_Param Param;
      public readonly ParamVisibility Relevance;
    }

    protected abstract ParamDefinition[] Inputs { get; }
    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      foreach (var definition in Inputs.Where(x => x.Relevance.HasFlag(ParamVisibility.Default)))
        manager.AddParameter(GH_ComponentParamServer.CreateDuplicate(definition.Param));
    }

    protected abstract ParamDefinition[] Outputs { get; }
    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      foreach (var definition in Outputs.Where(x => x.Relevance.HasFlag(ParamVisibility.Default)))
        manager.AddParameter(GH_ComponentParamServer.CreateDuplicate(definition.Param));
    }

    #region UI
    public virtual bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      if (index == 0)
        return componentParams[0].Name != templateParams[0].Param.Name;

      if (index >= templateParams.Length)
        return false;

      if (index >= componentParams.Count)
        return componentParams[componentParams.Count - 1].Name != templateParams[templateParams.Length - 1].Param.Name;

      string previous = componentParams[index - 1].Name;

      for (int i = 0; i < templateParams.Length; ++i)
      {
        if (templateParams[i].Param.Name == previous)
          return templateParams[i + 1].Param.Name != componentParams[index].Name;
      }

      return false;
    }

    IGH_Param GetTemplateParam(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      int offset = index == 0 ? -1 : +1;
      int reference = index == 0 ? index : index - 1;

      var currentName = componentParams[reference].Name;
      for (int i = 0; i < templateParams.Length; ++i)
      {
        if (templateParams[i].Param.Name == currentName)
          return templateParams[i + offset].Param;
      }

      return default;
    }

    public virtual IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      if (GetTemplateParam(side, index) is IGH_Param param)
        return GH_ComponentParamServer.CreateDuplicate(param);

      return default;
    }

    public virtual bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      string current = componentParams[index].Name;
      for (int i = 0; i < Inputs.Length; ++i)
      {
        if (templateParams[i].Param.Name == current)
          return !templateParams[i].Relevance.HasFlag(ParamVisibility.Mandatory);
      }

      return default;
    }

    public virtual bool DestroyParameter(GH_ParameterSide side, int index) => CanRemoveParameter(side, index);

    public virtual void VariableParameterMaintenance() { }

    void CanvasFullNamesChanged()
    {
      void UpdateName(IEnumerable<IGH_Param> values, ParamDefinition[] template)
      {
        int i = 0;
        foreach (var value in values)
        {
          while (i < template.Length && value.Name != template[i].Param.Name) ++i;

          if (i >= template.Length)
            break;

          if (value.MutableNickName)
          {
            if (CentralSettings.CanvasFullNames)
            {
              if (value.NickName == template[i].Param.NickName)
                value.NickName = template[i].Param.Name;
            }
            else
            {
              if (value.NickName == template[i].Param.Name)
                value.NickName = template[i].Param.NickName;
            }
          }
        }
      }

      UpdateName(Params.Input, Inputs);
      UpdateName(Params.Output, Outputs);
    }

    #endregion

    #region Display
    internal new class Attributes : GH_ComponentAttributes
    {
      public Attributes(ZuiComponent owner) : base(owner) { }

      bool CanvasFullNames = CentralSettings.CanvasFullNames;
      public override void ExpireLayout()
      {
        if (CanvasFullNames != CentralSettings.CanvasFullNames)
        {
          if (Owner is ZuiComponent zuiComponent)
            zuiComponent.CanvasFullNamesChanged();

          CanvasFullNames = CentralSettings.CanvasFullNames;
        }

        base.ExpireLayout();
      }
    }
    #endregion
  }
}
