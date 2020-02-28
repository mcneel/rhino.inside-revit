using System;
using System.Windows.Forms;
using System.Windows.Forms.InteropExtension;

namespace RhinoInside.Revit
{
  class ModalForm : Form
  {
    public static new ModalForm ActiveForm { get; private set; }

    public ModalForm()
    {
      ActiveForm = this;
      Dock = DockStyle.Fill;
      ShowIcon = false;
      ShowInTaskbar = false;
      BackColor = System.Drawing.Color.White;
      Opacity = 0.1;
      this.Show(Revit.MainWindowHandle);
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      ActiveForm = null;
    }

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
      get
      {
        var createParams = base.CreateParams;
        createParams.Style = 0x40000000;
        createParams.ExStyle = 0x00080000;

        using (var mainWindowExtents = Revit.ActiveUIApplication.MainWindowExtents)
        {
          createParams.X = 0;
          createParams.Y = 0;
          createParams.Width = mainWindowExtents.Right - mainWindowExtents.Left;
          createParams.Height = mainWindowExtents.Bottom - mainWindowExtents.Top;
          createParams.Parent = Revit.MainWindowHandle;
        }

        return createParams;
      }
    }

    internal static bool ParentEnabled
    {
      get => ActiveForm?.Enabled ?? false;
      set
      {
        if (value)
        {
          if (ActiveForm is object)
            ActiveForm.Enabled = true;

          Revit.MainWindow.Enabled = true;
        }
        else
        {
          Revit.MainWindow.Enabled = false;

          if (ActiveForm is object)
            ActiveForm.Enabled = false;
        }
      }
    }
  }
}
