using System;
using System.Drawing;
using System.Linq;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract partial class TransactionalComponent
  {
    internal class ComponentAttributes : ExpireButtonAttributes
    {
      public ComponentAttributes(TransactionalComponent owner) : base(owner) { }
      private new TransactionalComponent Owner => (TransactionalComponent) base.Owner;

      internal static string IssuePrefix => "⏯";
      internal static string ContinuePrefix => "✅";

      protected override bool Top => true;
      protected override bool Visible => Owner.RuntimeMessages(GH_RuntimeMessageLevel.Error).Any(x => x.StartsWith(IssuePrefix));
      protected override string DisplayText
      {
        get
        {
          var continues = Owner.RuntimeMessages(GH_RuntimeMessageLevel.Remark).Where(x => x.StartsWith(ContinuePrefix)).ToArray();
          return continues.Length == 1 ? continues[0] : "✅ Continue";
        }
      }

      public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
      {
        if (Visible && ButtonBounds.Contains(new Point((int) Math.Round(canvasPoint.X), (int) Math.Round(canvasPoint.Y))))
        {
          e.Title = "Continue";
          e.Icon = Owner.Icon_24x24;
          e.Description = "If you don't want to be asked again set component 'Error Mode' to 'Continue'.";
          e.Text = $"If suitable, a default resolution will be applied.";
          return;
        }

        base.SetupTooltip(canvasPoint, e);
      }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        if (channel == GH_CanvasChannel.Objects && !Owner.Locked && Owner is TransactionalComponent component)
        {
          if (component.TransactionStatus != ARDB.TransactionStatus.RolledBack && component.TransactionStatus != ARDB.TransactionStatus.Uninitialized)
          {
            var palette = GH_CapsuleRenderEngine.GetImpliedPalette(Owner);
            if (palette == GH_Palette.Normal && !Owner.IsPreviewCapable)
              palette = GH_Palette.Hidden;

            // Errors and warnings should be refelected in Canvas
            if (palette == GH_Palette.Normal || palette == GH_Palette.Hidden)
            {
              var style = GH_CapsuleRenderEngine.GetImpliedStyle(palette, Selected, Owner.Locked, Owner.Hidden);
              var fill = style.Fill;
              var edge = style.Edge;
              var text = style.Text;

              switch (component.TransactionStatus)
              {
                case ARDB.TransactionStatus.Uninitialized: palette = GH_Palette.Grey; break;
                case ARDB.TransactionStatus.Started: palette = GH_Palette.White; break;
                case ARDB.TransactionStatus.RolledBack:     /*palette = GH_Palette.Normal;*/ break;
                case ARDB.TransactionStatus.Committed: palette = GH_Palette.Black; break;
                case ARDB.TransactionStatus.Pending: palette = GH_Palette.Blue; break;
                case ARDB.TransactionStatus.Error: palette = GH_Palette.Pink; break;
                case ARDB.TransactionStatus.Proceed: palette = GH_Palette.Brown; break;
              }
              var replacement = GH_CapsuleRenderEngine.GetImpliedStyle(palette, Selected, Owner.Locked, Owner.Hidden);

              try
              {
                style.Edge = replacement.Edge;
                style.Fill = replacement.Fill;
                style.Text = replacement.Text;

                base.Render(canvas, graphics, channel);
              }
              finally
              {
                style.Fill = fill;
                style.Edge = edge;
                style.Text = text;
              }

              return;
            }
          }
        }

        base.Render(canvas, graphics, channel);
      }
    }

    public override void CreateAttributes() => m_attributes = new ComponentAttributes(this);

    protected internal void AddContinuableFailure(string message)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{ComponentAttributes.IssuePrefix} {message}");
    }

    protected internal void AddContinueFailure(string message)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{ComponentAttributes.ContinuePrefix} {message}");
    }
  }
}
