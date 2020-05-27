using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class SignalComponent : ZuiComponent
  {
    protected SignalComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    #region Signal
    protected static readonly string SignalParamName = "Signal";
    protected static IGH_Param CreateSignalParam() => new Parameters.Param_Enum<Types.ComponentSignal>()
    {
      Name = SignalParamName,
      NickName = "SIG",
      Description = "Component signal",
      Access = GH_ParamAccess.tree,
      WireDisplay = GH_ParamWireDisplay.hidden
    };

    protected int SignalParamIndex => Params.IndexOfInputParam(SignalParamName);
    protected IGH_Param SignalParam => SignalParamIndex < 0 ? default : Params.Input[SignalParamIndex];


    protected Kernel.ComponentSignal Signal { get; set; } = Kernel.ComponentSignal.Active;
    static Kernel.ComponentSignal? MaxSignal(IEnumerable<IGH_Goo> signals)
    {
      if (signals is object)
      {
        Kernel.ComponentSignal? max = default;
        foreach (var goo in signals)
        {
          if (goo is Types.ComponentSignal signal)
          {
            var value = signal.Value;
            if (!max.HasValue)
              max = value;

            if (value == Kernel.ComponentSignal.Frozen)
              continue;

            if (Math.Abs((int) value) > (int) max.Value)
              max = value;
          }
        }

        return max;
      }

      return default;
    }

    public override void ExpireSolution(bool recompute)
    {
      if (SignalParam is IGH_Param signal)
      {
        Phase = GH_SolutionPhase.Blank;

        if (signal.DataType == GH_ParamData.@void)
          Signal = Kernel.ComponentSignal.Frozen;

        OnSolutionExpired(recompute);
      }
      else
      {
        Signal = Kernel.ComponentSignal.Active;
        base.ExpireSolution(recompute);
      }
    }

    public override void CollectData()
    {
      if (Phase == GH_SolutionPhase.Collected)
        return;

      base.CollectData();

      var _Signal_ = Params.IndexOfInputParam(SignalParamName);
      if (_Signal_ >= 0)
      {
        var signal = Params.Input[_Signal_];
        Signal = MaxSignal(signal.VolatileData.AllData(false)).GetValueOrDefault();

        if (signal.DataType == GH_ParamData.@void)
          signal.NickName = SignalParamName;
        else
          signal.NickName = Signal.ToString();

        if (Signal != Kernel.ComponentSignal.Frozen)
        {
          if (OnPingDocument() is GH_Document doc)
          {
            doc.ScheduleSolution
            (
              GH_Document.ScheduleRecursive,
              x =>
              {
                base.ClearData();
                base.ExpireDownStreamObjects();

                // Mark it as Collected to avoid collect it again
                Phase = GH_SolutionPhase.Collected;
              }
            );
          }
        }

        Phase = GH_SolutionPhase.Computed;
      }
    }

    internal new class Attributes : ZuiComponent.Attributes
    {
      public Attributes(SignalComponent owner) : base(owner) { }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        if (channel == GH_CanvasChannel.Objects && Owner is SignalComponent component && component.Signal != Kernel.ComponentSignal.Active)
        {
          var basePalette = Owner.Hidden || !Owner.IsPreviewCapable ? GH_Palette.Hidden : GH_Palette.Normal;
          var baseStyle = GH_CapsuleRenderEngine.GetImpliedStyle(basePalette, Selected, Owner.Locked, Owner.Hidden);

          var palette = GH_CapsuleRenderEngine.GetImpliedPalette(Owner);
          if (palette == GH_Palette.Normal && !Owner.IsPreviewCapable)
            palette = GH_Palette.Hidden;

          var style = GH_CapsuleRenderEngine.GetImpliedStyle(palette, Selected, Owner.Locked, Owner.Hidden);
          var fill = style.Fill;
          var edge = style.Edge;
          var text = style.Text;

          try
          {
            switch (component.Signal)
            {
              case Kernel.ComponentSignal.Frozen:

                style.Edge = Color.FromArgb(150, fill.R, fill.G, fill.B);
                if (Selected)
                  style.Fill = Color.FromArgb(GH_Skin.palette_trans_selected.Fill.A, baseStyle.Fill.R, baseStyle.Fill.G, baseStyle.Fill.B);
                else
                  style.Fill = Color.FromArgb(GH_Skin.palette_trans_standard.Fill.A, baseStyle.Fill.R, baseStyle.Fill.G, baseStyle.Fill.B);

                style.Text = baseStyle.Text;
                break;
            }

            base.Render(canvas, graphics, channel);
          }
          finally
          {
            style.Fill = fill;
            style.Edge = edge;
            style.Text = text;
          }
        }
        else base.Render(canvas, graphics, channel);
      }
    }

    public override void CreateAttributes() => m_attributes = new Attributes(this);
    #endregion
  }
}
