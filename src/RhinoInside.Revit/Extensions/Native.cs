using System.Runtime.InteropServices;
using System.Security;

namespace RhinoInside.Revit.Native
{
  [SuppressUnmanagedCodeSecurity]
  static class NativeLoader
  {
    [
      DllImport("RhinoInside.Revit.Native.dll",
      EntryPoint = "LdrGetStackTraceFilePath",
      CharSet = CharSet.Unicode)
    ]
    static extern System.IntPtr LdrGetStackTraceFilePath();
    internal static string GetStackTraceFilePath() => Marshal.PtrToStringUni(LdrGetStackTraceFilePath());

    [
      DllImport("RhinoInside.Revit.Native.dll",
      EntryPoint = "LdrSetStackTraceFilePath",
      CharSet = CharSet.Unicode)
    ]
    internal static extern void SetStackTraceFilePath(string reportFilePath);

    [
      DllImport("RhinoInside.Revit.Native.dll",
      EntryPoint = "LdrGetReportOnLoad",
      CharSet = CharSet.Unicode)
    ]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetReportOnLoad(string moduleName);

    [
      DllImport("RhinoInside.Revit.Native.dll",
      EntryPoint = "LdrSetReportOnLoad",
      CharSet = CharSet.Unicode)
    ]
    internal static extern void SetReportOnLoad(string moduleName, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [
      DllImport("RhinoInside.Revit.Native.dll",
      EntryPoint = "LdrIsolateOpenNurbs",
      CharSet = CharSet.Unicode)
    ]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsolateOpenNurbs();
  }
}
