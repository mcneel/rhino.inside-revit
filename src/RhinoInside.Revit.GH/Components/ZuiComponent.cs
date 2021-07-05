using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;

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

    protected enum ParamRelevance
    {
      Binding = default,
      Primary = 1,
      Secondary = 2,
      Tertiary = 3,
      Quarternary = 4,
      Quinary = 5,
      Senary = 6,
      Septenary = 7,
      Occasional = int.MaxValue,
    }

    protected struct ParamDefinition
    {
      public readonly IGH_Param Param;
      public readonly ParamRelevance Relevance;

      public ParamDefinition(IGH_Param param)
      {
        Param = param;
        Relevance = ParamRelevance.Binding;
      }

      public ParamDefinition(IGH_Param param, ParamRelevance relevance)
      {
        Param = param;
        Relevance = relevance;
      }

      public static ParamDefinition Create<T>(string name, string nickname, string description = "", GH_ParamAccess access = GH_ParamAccess.item, bool optional = false, ParamRelevance relevance = ParamRelevance.Binding)
        where T : class, IGH_Param, new()
      {
        var param = new T()
        {
          Name = name,
          NickName = nickname,
          Description = description,
          Access = access,
          Optional = optional
        };

        return new ParamDefinition(param, relevance);
      }

      public static ParamDefinition Create<T>(string name, string nickname, string description, object defaultValue, GH_ParamAccess access = GH_ParamAccess.item, bool optional = false, ParamRelevance relevance = ParamRelevance.Binding)
        where T : class, IGH_Param, new()
      {
        var param = new T()
        {
          Name = name,
          NickName = nickname,
          Description = description,
          Access = access,
          Optional = optional
        };

        if (typeof(T).IsGenericSubclassOf(typeof(GH_PersistentParam<>)))
        {
          dynamic persistentParam = param;
          persistentParam.SetPersistentData(defaultValue);
        }

        return new ParamDefinition(param, relevance);
      }
    }

    protected abstract ParamDefinition[] Inputs { get; }
    protected sealed override void RegisterInputParams(GH_InputParamManager manager)
    {
      foreach (var definition in Inputs.Where(x => x.Relevance <= ParamRelevance.Primary))
        manager.AddParameter(definition.Param.CreateTwin());
    }

    protected abstract ParamDefinition[] Outputs { get; }
    protected sealed override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      foreach (var definition in Outputs.Where(x => x.Relevance <= ParamRelevance.Primary))
        manager.AddParameter(definition.Param.CreateTwin());
    }

    #region UI
    public virtual bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      if (index >= templateParams.Length)
        return false;

      if (index == 0)
      {
        if (componentParams.Count == 0) return templateParams.Length > 0;

        return componentParams[0].Name != templateParams[0].Param.Name;
      }

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

      if (componentParams.Count == 0)
      {
        if (templateParams.Length > 0)
          return templateParams[templateParams.Length + offset].Param;
      }
      else
      {
        var currentName = componentParams[reference].Name;
        for (int i = 0; i < templateParams.Length; ++i)
        {
          if (templateParams[i].Param.Name == currentName)
            return templateParams[i + offset].Param;
        }
      }

      return default;
    }

    public virtual IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      if (GetTemplateParam(side, index) is IGH_Param param)
        return param.CreateTwin();

      return default;
    }

    public virtual bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      var templateParams  = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      string current = componentParams[index].Name;
      for (int i = 0; i < templateParams.Length; ++i)
      {
        if (templateParams[i].Param.Name == current)
          return templateParams[i].Relevance != ParamRelevance.Binding;
      }

      return true;
    }

    public virtual bool DestroyParameter(GH_ParameterSide side, int index) => CanRemoveParameter(side, index);

    public virtual void VariableParameterMaintenance()
    {
      foreach (var input in Inputs)
      {
        if (Params.Input<IGH_Param>(input.Param.Name) is IGH_Param param)
        {
          param.Access = input.Param.Access;
          param.Optional = input.Param.Optional;

          if (input.Param is Param_Number input_number && param is Param_Number param_number)
            param_number.AngleParameter = input_number.AngleParameter;
        }
      }

      foreach (var output in Outputs)
      {
        if (Params.Output<IGH_Param>(output.Param.Name) is IGH_Param param)
        {
          param.Access = output.Param.Access;
          param.Optional = output.Param.Optional;

          if (output.Param is Param_Number input_number && param is Param_Number param_number)
            param_number.AngleParameter = input_number.AngleParameter;
        }
      }
    }

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

    public override void CreateAttributes() => m_attributes = new Attributes(this);
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader)) return false;

      // Upgrade from non IGH_VariableParameterComponent data
      if (!reader.ChunkExists("ParameterData"))
      {
        // Inputs
        {
          // Tentatively register all parameters
          foreach (var definition in Inputs)
            Params.RegisterInputParam(definition.Param.CreateTwin());

          var found = new bool[Params.Input.Count];
          int index = 0;
          var chunk = default(GH_IReader);
          while ((chunk = reader.FindChunk("param_input", index++)) is object)
          {
            var name = string.Empty;
            if (chunk.TryGetString("Name", ref name))
            {
              var i = Params.IndexOfInputParam(name);
              if (i > 0) continue;
              var param = Params.Input[i];

              var access = param.Access;
              var optional = param.Optional;
              param.Read(chunk);
              param.Optional = optional;
              param.Access = access;

              found[i] = true;
            }
          }

          // Remove not-found parameters
          for (int i = Params.Input.Count - 1; i >= 0; --i)
          {
            if (!found[i] && CanRemoveParameter(GH_ParameterSide.Input, i))
            {
              var param = Params.Input[i];
              Params.UnregisterInputParameter(param);
            }
          }
        }

        // Outputs
        {
          // Tentatively register all parameters
          foreach (var definition in Outputs)
            Params.RegisterOutputParam(definition.Param.CreateTwin());

          var found = new bool[Params.Output.Count];
          int index = 0;
          var chunk = default(GH_IReader);
          while ((chunk = reader.FindChunk("param_output", index++)) is object)
          {
            var name = string.Empty;
            if (chunk.TryGetString("Name", ref name))
            {
              var o = Params.IndexOfOutputParam(name);
              if (o > 0) continue;
              var param = Params.Output[o];

              var access = param.Access;
              var optional = param.Optional;
              param.Read(chunk);
              param.Optional = optional;
              param.Access = access;

              found[o] = true;
            }
          }

          // Remove not-found parameters
          for (int o = Params.Output.Count - 1; o >= 0; --o)
          {
            if (!found[o] && CanRemoveParameter(GH_ParameterSide.Output, o))
            {
              var param = Params.Output[o];
              Params.UnregisterOutputParameter(param);
            }
          }
        }

        VariableParameterMaintenance();
      }

      return true;
    }
    #endregion
  }
}
