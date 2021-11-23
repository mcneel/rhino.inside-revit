using Microsoft.Win32;

namespace RhinoInside.Revit
{
  static partial class Rhinoceros
  {
    internal struct DebugLogging
    {
      public static DebugLogging Current
      {
        get
        {
          using (var data = Registry.CurrentUser.OpenSubKey(@"Software\McNeel\Rhinoceros\7.0\Global Options\Debug Logging", true))
          {
            return new DebugLogging()
            {
              Enabled = (data.GetValue("Enabled", 0) as int?).GetValueOrDefault() != 0,
              SaveToFile = (data.GetValue("SaveToFile", 0) as int?).GetValueOrDefault() != 0,
            };
          }
        }
        set
        {
          using (var data = Registry.CurrentUser.OpenSubKey(@"Software\McNeel\Rhinoceros\7.0\Global Options\Debug Logging", true))
          {
            data.SetValue("Enabled", value.Enabled ? 1 : 0);
            data.SetValue("SaveToFile", value.SaveToFile ? 1 : 0);
            data.Flush();
          }
        }
      }

      public bool Enabled;
      public bool SaveToFile;
    }
  }
}
