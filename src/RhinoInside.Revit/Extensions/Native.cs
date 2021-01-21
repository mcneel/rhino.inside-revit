using System.Runtime.InteropServices;
using System.Security;

namespace RhinoInside.Revit.Native
{
  [SuppressUnmanagedCodeSecurity]
  static class NativeLoader
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

    [
      DllImport("RhinoInside.Revit.Native.dll",
      EntryPoint = "LdrIsolateOpenNurbs",
      CharSet = CharSet.Unicode)
    ]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsolateOpenNurbs();
  }
}
