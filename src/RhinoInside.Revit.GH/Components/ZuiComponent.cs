using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components
{
  /// <summary>
  /// Base class for all variable parameter components
  /// </summary>
  /// <seealso cref="IGH_VariableParameterComponent"/>
  [ComponentVersion(introduced: "1.0", updated: "1.3")]
  public abstract class ZuiComponent : Component, IGH_VariableParameterComponent
  {
    protected ZuiComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    {
      variableParameterScheme = VariableParameterScheme;
    }

    protected enum ParamRelevance
    {
      Binding     = int.MaxValue,
      Primary     = Binding - 1,
      Secondary   = Binding - 2,
      Tertiary    = Binding - 3,
      Quarternary = Binding - 4,
      Quinary     = Binding - 5,
      Senary      = Binding - 6,
      Septenary   = Binding - 7,
      Occasional  = Binding - 8,
      None        = default,
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
      foreach (var definition in Inputs.Where(x => x.Relevance >= ParamRelevance.Primary))
        manager.AddParameter(definition.Param.CreateTwin());
    }

    protected abstract ParamDefinition[] Outputs { get; }
    protected sealed override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      foreach (var definition in Outputs.Where(x => x.Relevance >= ParamRelevance.Primary))
        manager.AddParameter(definition.Param.CreateTwin());
    }

    #region UI
    ParamDefinition GetMostRelevantParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      int begin = -1, end = templateParams.Length;
      if (componentParams.Count > 0)
      {
        if (index <= 0)
        {
          end = IndexOf(templateParams, componentParams[0]);
        }
        else if (index >= componentParams.Count)
        {
          begin = IndexOf(templateParams, componentParams[componentParams.Count - 1]);
        }
        else
        {
          begin = IndexOf(templateParams, componentParams[index - 1]);
          end = IndexOf(templateParams, componentParams[index]);
        }
      }

      ParamDefinition mostRelevat = default;

      begin = Math.Max(-1, begin);
      end = Math.Min(end, templateParams.Length);

      for (int i = begin + 1; i < end; ++i)
      {
        var definition = templateParams[i];
        if (definition.Relevance >= ParamRelevance.Occasional && definition.Relevance > mostRelevat.Relevance)
          mostRelevat = definition;
      }

      return mostRelevat;
    }

    public virtual IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var template = GetMostRelevantParameter(side, index);
      if (template.Relevance != ParamRelevance.None)
        return template.Param.CreateTwin();

      return default;
    }

    public virtual bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return CanRemoveParameter(side, index);
    }

    public virtual bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return GetMostRelevantParameter(side, index).Relevance != ParamRelevance.None;
    }

    public virtual bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      var t = IndexOf(templateParams, componentParams[index]);
      return t >= 0 && templateParams[t].Relevance != ParamRelevance.Binding;
    }

    /// <summary>
    /// This function will get called before an attempt is made to add binding parameters.
    /// </summary>
    /// <param name="side">Parameter side.</param>
    /// <param name="index">Insertion index of parameter.</param>
    /// <returns>Return True if your component needs a parameter at the given location.</returns>
    public virtual bool ShouldInsertParameter(GH_ParameterSide side, int index)
    {
      return GetMostRelevantParameter(side, index) is ParamDefinition template &&
             template.Relevance == ParamRelevance.Binding;
    }

    /// <summary>
    /// This function will get called before an attempt is made to remove obsolete parameters.
    /// </summary>
    /// <param name="side">Parameter side.</param>
    /// <param name="index">Removal index of parameter.</param>
    /// <returns>Return True if your component does not support the parameter at the given location.</returns>
    public virtual bool ShouldRemoveParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      return IndexOf(templateParams, componentParams[index]) < 0;
    }

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
    internal class ZuiAttributes : GH_ComponentAttributes
    {
      public ZuiAttributes(ZuiComponent owner) : base(owner) { }

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

    public override void CreateAttributes() => Attributes = new ZuiAttributes(this);

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Show all parameters", Menu_ShowAllParameters, !AreAllParametersVisible(), false);
      Menu_AppendItem(menu, "Hide unconnected parameters", Menu_HideUnconnectedParameters, !AreAllParametersConnected(), false);
    }

    bool AreAllParametersVisible()
    {
      return Params.Input.Count == Inputs.Length && Params.Output.Count == Outputs.Length;
    }

    bool AreAllParametersConnected()
    {
      foreach (var param in Params.Input)
        if (param.Sources.Count == 0) return false;

      foreach (var param in Params.Output)
        if (param.Recipients.Count == 0) return false;

      return true;
    }

    void Menu_ShowAllParameters(object sender, EventArgs e)
    {
      if (OnPingDocument() is GH_Document document)
      {
        RecordUndoEvent("Show All Parameters");

        bool inputAdded = false;
        {
          for (int index = 0; index <= Params.Input.Count; ++index)
          {
            while (CanInsertParameter(GH_ParameterSide.Input, index))
            {
              var param = CreateParameter(GH_ParameterSide.Input, index);
              if (Params.RegisterInputParam(param, index))
                inputAdded |= !param.Optional;
            }
          }
        }

        bool outputAdded = false;
        {
          for (int index = 0; index <= Params.Output.Count; ++index)
          {
            while (CanInsertParameter(GH_ParameterSide.Output, index))
            {
              var param = CreateParameter(GH_ParameterSide.Output, index);
              outputAdded |= Params.RegisterOutputParam(param, index);
            }
          }
        }

        Params.OnParametersChanged();

        if (inputAdded) ExpireSolution(true);
        else
        {
          OnDisplayExpired(false);

          if (outputAdded)
            Phase = GH_SolutionPhase.Blank;
        }
      }
    }

    void Menu_HideUnconnectedParameters(object sender, EventArgs e)
    {
      if (OnPingDocument() is GH_Document document)
      {
        RecordUndoEvent("Hide Unconnected Parameters");

        bool inputRemoved = false;
        {
          int index = 0;
          foreach (var input in Params.Input.ToArray())
          {
            if
            (
              input.DataType > GH_ParamData.@void ||
              !CanRemoveParameter(GH_ParameterSide.Input, index)
            )
            {
              ++index;
            }
            else if (Params.UnregisterInputParameter(input))
            {
              inputRemoved |= true;
            }
          }
        }

        {
          int index = 0;
          foreach (var output in Params.Output.ToArray())
          {
            if (output.Recipients.Count > 0 || !CanRemoveParameter(GH_ParameterSide.Output, index))
              ++index;
            else
              Params.UnregisterOutputParameter(output);
          }
        }

        Params.OnParametersChanged();

        if (inputRemoved) ExpireSolution(true);
        else
        {
          OnDisplayExpired(false);
        }
      }
    }
    #endregion

    #region IO
    protected static ParamDefinition FindDefinition(ParamDefinition[] list, string name)
    {
      for (int i = 0; i < list.Length; ++i)
      {
        if (list[i].Param.Name == name)
          return list[i];
      }

      return default;
    }

    static int IndexOf(ParamDefinition[] list, IGH_Param value)
    {
      for (int i = 0; i < list.Length; ++i)
      {
        if (value.Name == list[i].Param.Name)
          return i;
      }

      return -1;
    }

    struct ParamComparer : IComparer<IGH_Param>
    {
      readonly ParamDefinition[] ReferenceList;

      public ParamComparer(ParamDefinition[] referenceList) => ReferenceList = referenceList;
      public int Compare(IGH_Param x, IGH_Param y) => IndexOf(ReferenceList, x) - IndexOf(ReferenceList, y);
    }

    public override void AddedToDocument(GH_Document document)
    {
      // If we read from a different version some parameters may need to be adjusted.
      if (VariableParameterScheme is string currentParameterScheme && currentParameterScheme != variableParameterScheme)
      {
        document.DestroyObjectTable();

        // PerformLayout here to obtain parameters pivots.
        Attributes.PerformLayout();

        // Detach Obsolete parameters.
        {
          var unknownParameters = new List<IGH_Param>();

          for (var inputIndex = Params.Input.Count - 1; inputIndex >= 0; --inputIndex)
          {
            if (!ShouldRemoveParameter(GH_ParameterSide.Input, inputIndex))
              continue;

            var input = Params.Input[inputIndex];
            var y = input.Attributes.Pivot.Y;
            Params.UnregisterInputParameter(input, false);
            input.IconDisplayMode = GH_IconDisplayMode.name;
            input.Optional = false;
            input.Attributes = default;
            input.CreateAttributes();
            input.Attributes.Pivot = new System.Drawing.PointF(Attributes.Bounds.Left + input.Attributes.Bounds.Width / 2.0f, y);
            unknownParameters.Add(input);
          }

          for (var outputIndex = Params.Output.Count - 1; outputIndex >= 0; --outputIndex)
          {
            if (!ShouldRemoveParameter(GH_ParameterSide.Output, outputIndex))
              continue;

            var output = Params.Output[outputIndex];
            var y = output.Attributes.Pivot.Y;
            Params.UnregisterOutputParameter(output, false);
            output.IconDisplayMode = GH_IconDisplayMode.name;
            output.Optional = false;
            output.Attributes = default;
            output.CreateAttributes();
            output.Attributes.Pivot = new System.Drawing.PointF(Attributes.Bounds.Right - output.Attributes.Bounds.Width / 2.0f, y);
            unknownParameters.Add(output);
          }

          // Add unknown Parameters to the document to keep as much
          // previous information as possible available to the user.
          // Input parameters may contain PersistentData.
          if (unknownParameters.Count > 0)
          {
            var previousVersion = string.IsNullOrWhiteSpace(variableParameterScheme) ? new Version(0,0,0,0) : new System.Reflection.AssemblyName(variableParameterScheme).Version;
            var currentVersion = string.IsNullOrWhiteSpace(currentParameterScheme) ? new Version(0, 0, 0, 0) : new System.Reflection.AssemblyName(currentParameterScheme).Version;

            var action = "Mutated";
            if (previousVersion is object && currentVersion is object)
            {
              if (previousVersion < currentVersion) action = "Upgraded";
              else if (previousVersion > currentVersion) action = "Downgraded";
            }

            var index = document.Objects.IndexOf(this);
            var group = new Grasshopper.Kernel.Special.GH_Group
            {
              NickName = $"{action} : {Name}", // We tag it to allow user find those groups.
              Border = Grasshopper.Kernel.Special.GH_GroupBorder.Blob,
              Colour = System.Drawing.Color.FromArgb(211, GH_Skin.palette_warning_standard.Fill)
            };
            document.AddObject(group, false, index++);

            group.AddObject(InstanceGuid);
            foreach (var param in unknownParameters)
            {
              param.Locked = true;
              if (document.AddObject(param, false, index++))
                group.AddObject(param.InstanceGuid);
            }
          }
        }

        // Refresh paremeters with current types & values.
        {
          foreach (var input in Inputs)
          {
            var index = Params.Input.IndexOf(input.Param.Name, out var param);
            if (index >= 0)
            {
              var inputType = input.Param.GetType();
              if (inputType != param.GetType() && param.CreateSurrogate(inputType) is IGH_Param surrogate)
              {
                GH_UpgradeUtil.MigrateRecipients(param, surrogate);
                Params.UnregisterInputParameter(param);
                Params.RegisterInputParam(surrogate, index);
                param = surrogate;
              }

              param.Access = input.Param.Access;
              param.Optional = input.Param.Optional;

              if (input.Param is Param_Number input_number && param is Param_Number param_number)
              {
                param_number.AngleParameter = input_number.AngleParameter;
                param_number.UseDegrees = input_number.UseDegrees;
              }
            }
          }

          foreach (var output in Outputs)
          {
            var index = Params.Output.IndexOf(output.Param.Name, out var param);
            if (index >= 0)
            {
              var outputType = output.Param.GetType();
              if (outputType != param.GetType() && param.CreateSurrogate(outputType) is IGH_Param surrogate)
              {
                GH_UpgradeUtil.MigrateSources(param, surrogate);
                Params.UnregisterOutputParameter(param);
                Params.RegisterOutputParam(surrogate, index);
                param = surrogate;
              }

              param.Access = output.Param.Access;
              param.Optional = output.Param.Optional;

              if (output.Param is Param_Number output_number && param is Param_Number param_number)
              {
                param_number.AngleParameter = output_number.AngleParameter;
                param_number.UseDegrees = output_number.UseDegrees;
              }
            }
          }
        }

        // Sort Parameters in Inputs & Outputs order.
        {
          Params.Input.Sort(new ParamComparer(Inputs));
          Params.Output.Sort(new ParamComparer(Outputs));
        }

        // Add Binding Parameters.
        {
          for (int i = 0; i <= Params.Input.Count; ++i)
          {
            while (ShouldInsertParameter(GH_ParameterSide.Input, i))
              Params.RegisterInputParam(CreateParameter(GH_ParameterSide.Input, i), i);
          }

          for (int i = 0; i <= Params.Output.Count; ++i)
          {
            while (ShouldInsertParameter(GH_ParameterSide.Output, i))
              Params.RegisterOutputParam(CreateParameter(GH_ParameterSide.Output, i), i);
          }
        }

        // Update Common fields
        if (Activator.CreateInstance(GetType()) is IGH_Component prototype)
        {
          Name = prototype.Name;
          NickName = CentralSettings.CanvasFullNames ? prototype.Name : prototype.NickName;
          Description = prototype.Description;
          Category = prototype.Category;
          SubCategory = prototype.SubCategory;
        }

        // ExpireLayout here in case we have removed, added or sorted parameters.
        Attributes.ExpireLayout();

        // Mark component as converted
        variableParameterScheme = currentParameterScheme;
      }

      base.AddedToDocument(document);
    }

    /// <summary>
    /// Scheme name for the current input and output parameters configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default implentation returns a simplified version of component Type
    /// <see cref="System.Reflection.Assembly.FullName"/>
    /// to ensure parameters are synchronized with the current component implementation.
    /// </para>
    /// <para>
    /// For a more accurate and manual control of upgrade-downgrade mechanism
    /// subtypes may override this property and return a constant value related
    /// to the current component implementation version.
    /// </para>
    /// <para>
    /// Automatic upgrade-downgrade may be disabled returning null here.
    /// </para>
    /// </remarks>
    protected virtual string VariableParameterScheme
    {
      get
      {
        var assembly = GetType().Assembly;
        if (assembly.IsDynamic) return null;

        var assemblyName = assembly.GetName();
        var scheme = assemblyName.Name;

        if (assemblyName.Version is object)
          scheme += $", Version={assemblyName.Version}";

        if (!string.IsNullOrWhiteSpace(assemblyName.CultureName))
          scheme += $", Culture={assemblyName.CultureName}";

        return scheme;
      }
    }
    string variableParameterScheme;

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
              if (i < 0) continue;
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
              if (o < 0) continue;
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

      // Read parameters-scheme value
      if (!reader.TryGetString("VariableParameterScheme", ref variableParameterScheme))
        variableParameterScheme = default;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer)) return false;

      // Write parameters-scheme value
      if (variableParameterScheme is object)
        writer.SetString("VariableParameterScheme", variableParameterScheme);

      return true;
    }
    #endregion
  }
}
