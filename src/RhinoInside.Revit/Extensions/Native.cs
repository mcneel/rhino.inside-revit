using System.Runtime.InteropServices;
using System.Security;

namespace RhinoInside.Revit.Native
{
  [SuppressUnmanagedCodeSecurity]
  internal static class NativeMethods
  {
    [
      DllImport("RhinoInside.Revit.Native.dll",
      EntryPoint = "LdrSetStackTraceFilePath",
      CharSet = CharSet.Unicode)
    ]
    internal static extern void SetStackTraceFilePath(string reportFilePath);

    [
      DllImport("RhinoInside.Revit.Native.dll",
      EntryPoint = "LdrReportOnLoad",
      CharSet = CharSet.Unicode)
    ]
    internal static extern void ReportOnLoad(string moduleName, [MarshalAs(UnmanagedType.Bool)] bool enable);
  }
}
