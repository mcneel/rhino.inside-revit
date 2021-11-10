using System;
using Eto.Drawing;
using Eto.Forms;

namespace RhinoInside.Revit.AddIn.Forms
{
  /// <summary>
  /// Rhino UI framework base dialog (modal) type for this addon
  /// Current implementation is centered on Revit window and uses the generic Rhino icon
  /// </summary>
  abstract class ModalDialog : Dialog
  {
    private Autodesk.Revit.UI.UIApplication _uiApp = null;

    public ModalDialog(Autodesk.Revit.UI.UIApplication uiApp, Size initialSize)
    {
      BaseWindowUtils.SetupWindow(this, uiApp, initialSize);

      _uiApp = uiApp;
      SizeChanged += ModalDialog_SizeChanged;

      DefaultButton = new Button { Text = "OK" };
      PositiveButtons.Add(DefaultButton);

      AbortButton = new Button { Text = "Cancel" };
      NegativeButtons.Add(AbortButton);
    }

    private void ModalDialog_SizeChanged(object sender, EventArgs e)
    {
      SizeChanged -= ModalDialog_SizeChanged;

      // Centers ModalDialog on Owner center.
      var native = this.ToNative();
      native.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
    }

    DialogResult dialogResult = DialogResult.None;

    public new bool Close() => Close(DialogResult.Cancel);
    public bool Close(DialogResult result)
    {
      var canceled = true;
      var closed = default(EventHandler<EventArgs>);
      Closed += closed = (sender, arg) =>
      {
        dialogResult = result;
        canceled = false;
      };

      base.Close();
      Closed -= closed;

      return !canceled;
    }

    public new DialogResult ShowModal()
    {
      if (AbortButton is object)
        AbortButton.Click += (o, s) => Close(DialogResult.Cancel);

      base.ShowModal();
      return dialogResult;
    }
  }
}
