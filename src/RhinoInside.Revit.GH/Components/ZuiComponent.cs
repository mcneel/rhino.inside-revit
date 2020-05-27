using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;

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
      public readonly IGH_Param Param;
      public readonly ParamVisibility Relevance;

      internal ParamDefinition(IGH_Param param)
      {
        Param = param;
        Relevance = ParamVisibility.Binding;
      }

      internal ParamDefinition(IGH_Param param, ParamVisibility relevance)
      {
        Param = param;
        Relevance = relevance;
      }

      public static ParamDefinition FromParam(IGH_Param param) =>
        new ParamDefinition(param, ParamVisibility.Binding);

      public static ParamDefinition FromParam(IGH_Param param, ParamVisibility relevance) =>
        new ParamDefinition(param, relevance);

      public static ParamDefinition FromParam<T>(GH_PersistentParam<T> param, ParamVisibility relevance, object defaultValue)
        where T : class, IGH_Goo, new()
      {
        var def = new ParamDefinition(param, relevance);
        if (def.Param is GH_PersistentParam<T> persistentParam)
        {
          var data = new T();
          if (!data.CastFrom(defaultValue))
            throw new InvalidCastException();

          persistentParam.PersistentData.Append(data);
        }

        return def;
      }

      public static ParamDefinition Create<T>(string name, string nickname, string description, GH_ParamAccess access, bool optional = false, ParamVisibility relevance = ParamVisibility.Binding)
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

      public static ParamDefinition Create<T>(string name, string nickname, string description, object defaultValue, GH_ParamAccess access, bool optional = false, ParamVisibility relevance = ParamVisibility.Binding)
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

        bool IsGenericSubclassOf(Type type, Type baseGenericType)
        {
          for(; type != typeof(object); type = type.BaseType)
          {
            var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (baseGenericType == cur)
              return true;
          }

          return false;
        }

        if (IsGenericSubclassOf(typeof(T), typeof(GH_PersistentParam<>)))
        {
          dynamic persistentParam = param;
          persistentParam.SetPersistentData(defaultValue);
        }

        return new ParamDefinition(param, relevance);
      }
    }

    protected abstract ParamDefinition[] Inputs { get; }
    protected override sealed void RegisterInputParams(GH_InputParamManager manager)
    {
      foreach (var definition in Inputs.Where(x => x.Relevance.HasFlag(ParamVisibility.Default)))
        manager.AddParameter(CreateDuplicateParam(definition.Param));
    }

    protected abstract ParamDefinition[] Outputs { get; }
    protected override sealed void RegisterOutputParams(GH_OutputParamManager manager)
    {
      foreach (var definition in Outputs.Where(x => x.Relevance.HasFlag(ParamVisibility.Default)))
        manager.AddParameter(CreateDuplicateParam(definition.Param));
    }

    class NullAttributes : IGH_Attributes
    {
      public static NullAttributes Instance = new NullAttributes();

      NullAttributes() { }
      public PointF Pivot { get => PointF.Empty; set => throw new NotImplementedException(); }
      public RectangleF Bounds { get => RectangleF.Empty; set => throw new NotImplementedException(); }

      public bool AllowMessageBalloon => false;
      public bool HasInputGrip => false;
      public bool HasOutputGrip => false;
      public PointF InputGrip => PointF.Empty;
      public PointF OutputGrip => PointF.Empty;
      public IGH_DocumentObject DocObject => null;
      public IGH_Attributes Parent { get => null; set => throw new NotImplementedException(); }

      public bool IsTopLevel => false;
      public IGH_Attributes GetTopLevel => null;

      public string PathName => string.Empty;

      public Guid InstanceGuid => Guid.Empty;

      public bool Selected { get => false; set => throw new NotImplementedException(); }

      public bool TooltipEnabled => false;

      public void AppendToAttributeTree(List<IGH_Attributes> attributes) { }
      public void ExpireLayout() { }
      public bool InvalidateCanvas(GH_Canvas canvas, GH_CanvasMouseEvent e) => false;
      public bool IsMenuRegion(PointF point) => false;

      public bool IsPickRegion(PointF point) => false;
      public bool IsPickRegion(RectangleF box, GH_PickBox method) => false;

      public bool IsTooltipRegion(PointF canvasPoint) => false;

      public void NewInstanceGuid() => throw new NotImplementedException();
      public void NewInstanceGuid(Guid newID) => throw new NotImplementedException();

      public void PerformLayout() => throw new NotImplementedException();

      public void RenderToCanvas(GH_Canvas canvas, GH_CanvasChannel channel) { }
      public GH_ObjectResponse RespondToKeyDown(GH_Canvas sender, KeyEventArgs e) => GH_ObjectResponse.Ignore;
      public GH_ObjectResponse RespondToKeyUp(GH_Canvas sender, KeyEventArgs e) => GH_ObjectResponse.Ignore;
      public GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e) => GH_ObjectResponse.Ignore;
      public GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e) => GH_ObjectResponse.Ignore;
      public GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e) => GH_ObjectResponse.Ignore;
      public GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e) => GH_ObjectResponse.Ignore;
      public void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e) { }

      public bool Read(GH_IReader reader) => true;

      public bool Write(GH_IWriter writer) => true;
    }

    public static IGH_Param CreateDuplicateParam(IGH_Param original)
    {
      var attributes = original.Attributes;
      try
      {
        original.Attributes = NullAttributes.Instance;
        var newParam = GH_ComponentParamServer.CreateDuplicate(original);

        if (newParam.MutableNickName && CentralSettings.CanvasFullNames)
          newParam.NickName = newParam.Name;

        return newParam;
      }
      finally { original.Attributes = attributes; }
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
        if(templateParams.Length > 0)
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
        return CreateDuplicateParam(param);

      return default;
    }

    public virtual bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      string current = componentParams[index].Name;
      for (int i = 0; i < templateParams.Length; ++i)
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

    public override void CreateAttributes() => m_attributes = new Attributes(this);
    #endregion
  }
}
