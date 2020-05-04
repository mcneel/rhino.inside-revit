using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using static Microsoft.Win32.SafeHandles.InteropServices.Kernel32;

namespace RhinoInside.Revit
{
  static class ErrorReport
  {
    public static string CLRVersion
    {
      get
      {
        string subkey = Environment.Version.Major < 4 ?
          $@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v{Environment.Version.Major}.{Environment.Version.Minor}\" :
          $@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v{Environment.Version.Major}\Full\";

        using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey(subkey))
        {
          if (ndpKey?.GetValue("Version") is string version)
            return $"{Environment.Version} ({version})";
        }

        return $"{Environment.Version}";
      }
    }

    static void CreateReportEntry(ZipArchive archive, string entryName, string filePath)
    {
      try
      {
        using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          var entry = archive.CreateEntry(entryName);
          using (var stream = entry.Open())
            file.CopyTo(stream);
        }
      }
      catch (IOException) { }
    }

    static void CreateReportFile(UIApplication app, string reportFilePath, bool includeAddinsList, IEnumerable<string> attachments)
    {
      attachments = attachments.Where(x => File.Exists(x)).ToArray();

      using (var zip = new FileStream(reportFilePath, FileMode.Create))
      {
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create))
        {
          var now = DateTime.Now.ToString("yyyyMMddTHHmmssZ");

          // Report.md
          {
            var Report = archive.CreateEntry($"{now}/Report.md");
            using (var writer = new StreamWriter(Report.Open()))
            {
              writer.WriteLine($"# Rhino.Inside.Revit");

              writer.WriteLine();
              writer.WriteLine($"## Host");
              writer.WriteLine($"- Environment.OSVersion: {Environment.OSVersion}");
              writer.WriteLine($"  - SystemInformation.TerminalServerSession: {System.Windows.Forms.SystemInformation.TerminalServerSession}");
              writer.WriteLine($"- Environment.Version: {CLRVersion}");

              var revit = app.Application;
              writer.WriteLine($"- {revit.VersionName}");
              writer.WriteLine($"  - VersionBuild: {revit.VersionBuild}");
#if REVIT_2019
              writer.WriteLine($"  - SubVersionNumber: {revit.SubVersionNumber}");
#else
              writer.WriteLine($"  - VersionNumber: {revit.VersionNumber}");
#endif
              writer.WriteLine($"  - ProductType: {revit.Product}");
              writer.WriteLine($"  - Language: {revit.Language}");

              var rhino = Addin.RhinoVersionInfo;
              writer.WriteLine($"- Rhino: {rhino.ProductVersion} ({rhino.FileDescription})");
              writer.WriteLine($"- Rhino.Inside Revit: {Addin.DisplayVersion}");

              if (includeAddinsList)
              {
                writer.WriteLine();
                writer.WriteLine("## Addins");
                writer.WriteLine();
                writer.WriteLine("[Loaded Applications](Addins/LoadedApplications.md)  ");
              }

              writer.WriteLine();
              writer.WriteLine("## Console");
              writer.WriteLine();
              writer.WriteLine("[Startup](Console/Startup.txt)  ");

              if (attachments.Any())
              {
                writer.WriteLine();
                writer.WriteLine($"## Attachments");
                writer.WriteLine();
                foreach (var attachment in attachments)
                {
                  var attachmentName = Path.GetFileName(attachment);
                  writer.WriteLine($"[{attachmentName}](Attachments/{attachmentName})  ");
                }
              }
            }
          }

          // Addins
          if (includeAddinsList)
          {
            var LoadedApplicationsCSV = archive.CreateEntry($"{now}/Addins/{now}.csv");
            using (var writer = new StreamWriter(LoadedApplicationsCSV.Open()))
            {
              writer.WriteLine(@"""Company-Name"",""Product-Name"",""Product-Version"",""AddInType-FullName"",""Assembly-FullName"",""Assembly-Location""");

              foreach (var application in app.LoadedApplications)
              {
                var addinType = application.GetType();
                var versionInfo = File.Exists(addinType.Assembly.Location) ? FileVersionInfo.GetVersionInfo(addinType.Assembly.Location) : null;

                string CompanyName        = (versionInfo?.CompanyName ?? string.Empty).Replace(@"""", @"""""");
                string ProductName        = (versionInfo?.ProductName ?? string.Empty).Replace(@"""", @"""""");
                string ProductVersion     = (versionInfo?.ProductVersion ?? string.Empty).Replace(@"""", @"""""");
                string AddInTypeFullName  = (addinType?.FullName?? string.Empty).Replace(@"""", @"""""");
                string AssemblyFullName   = (addinType?.Assembly.FullName ?? string.Empty).Replace(@"""", @"""""");
                string AssemblyLocation   = (addinType?.Assembly.Location ?? string.Empty).Replace(@"""", @"""""");

                writer.WriteLine($@"""{CompanyName}"",""{ProductName}"",""{ProductVersion}"",""{AddInTypeFullName}"",""{AssemblyFullName}"",""{AssemblyLocation}""");
              }
            }

            var LoadedApplicationsMD = archive.CreateEntry($"{now}/Addins/LoadedApplications.md");
            using (var writer = new StreamWriter(LoadedApplicationsMD.Open()))
            {
              writer.WriteLine($"# UIApplication.LoadedApplications");
              writer.WriteLine();
              writer.WriteLine($"> NOTE:  ");
              writer.WriteLine($"> Applications listed in load order.  ");
              writer.WriteLine($"> Same information in CSV format [here]({now}.csv).  ");
              writer.WriteLine();

              foreach (var application in app.LoadedApplications)
              {
                var addinType = application.GetType();

                writer.WriteLine($"1. **{addinType.FullName}**");
                writer.WriteLine($"  - AssemblyFullName: {addinType.Assembly.FullName}");
                writer.WriteLine($"  - AssemblyLocation: {addinType.Assembly.Location}");

                var versionInfo = File.Exists(addinType.Assembly.Location) ? FileVersionInfo.GetVersionInfo(addinType.Assembly.Location) : null;
                writer.WriteLine($"    - CompanyName: {versionInfo?.CompanyName}");
                writer.WriteLine($"    - ProductName: {versionInfo?.ProductName}");
                writer.WriteLine($"    - ProductVersion: {versionInfo?.ProductVersion}");
                writer.WriteLine();
              }
            }

            Settings.AddIns.GetSystemAddins(app.Application.VersionNumber, out var systemAddins);
            foreach (var addin in systemAddins)
              CreateReportEntry(archive, $"{now}/Addins/System/{Path.GetFileName(addin)}", addin);

            Settings.AddIns.GetInstalledAddins(app.Application.VersionNumber, out var installedAddins);
            foreach (var addin in installedAddins)
              CreateReportEntry(archive, $"{now}/Addins/Installed/{Path.GetFileName(addin)}", addin);
          }

          // Console
          {
            if (Rhinoceros.StartupLog is string[] log && log.Length > 0)
            {
              var StartupTXT = archive.CreateEntry($"{now}/Console/Startup.txt");
              using (var writer = new StreamWriter(StartupTXT.Open()))
              {
                foreach (var line in log)
                  writer.WriteLine(line.Replace("\n", ""));
              }
            }
          }

          // Attachments
          {
            foreach (var attachement in attachments)
              CreateReportEntry(archive, $"{now}/Attachments/{Path.GetFileName(attachement)}", attachement);
          }
        }
      }
    }

    public static void SendEmail(UIApplication app, string subject, bool includeAddinsList, IEnumerable<string> attachments)
    {
      var now = DateTime.Now.ToString("yyyyMMddTHHmmssZ");
      var ReportFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"RhinoInside-Revit-Report-{now}.zip");

      CreateReportFile
      (
        app,
        ReportFilePath,
        includeAddinsList,
        attachments
      );

      foreach (var file in attachments)
      {
        try { File.Delete(file); }
        catch (IOException) { }
      }

      subject = Uri.EscapeUriString(subject);
      var mailtoURI = $"mailto:tech@mcneel.com?subject={subject}&body=";

      var mailBody = @"Please give us any additional info you see fit here..." + Environment.NewLine + Environment.NewLine;
      if (File.Exists(ReportFilePath))
        mailBody += $"<Please attach '{ReportFilePath}' file here>" + Environment.NewLine + Environment.NewLine;

      mailBody += $"OS: {Environment.OSVersion}" + Environment.NewLine;
      mailBody += $"CLR: {ErrorReport.CLRVersion}" + Environment.NewLine;

      var revit = app.Application;
#if REVIT_2019
      mailBody += $"Revit: {revit.SubVersionNumber} ({revit.VersionBuild})" + Environment.NewLine;
#else
      mailBody += $"Revit: {revit.VersionNumber} ({revit.VersionBuild})" + Environment.NewLine;
#endif

      var rhino = Addin.RhinoVersionInfo;
      mailBody += $"Rhino: {rhino.ProductVersion} ({rhino.FileDescription})" + Environment.NewLine;
      mailBody += $"Rhino.Inside Revit: {Addin.DisplayVersion}" + Environment.NewLine;

      mailBody = Uri.EscapeDataString(mailBody);

      using (Process.Start(mailtoURI + mailBody)) { }
    }
  }

  sealed class MiniDumper
  {
    [Flags]
    public enum MINIDUMP_TYPE : uint
    {
      // From dbghelp.h:
      MiniDumpNormal = 0x00000000,
      MiniDumpWithDataSegs = 0x00000001,
      MiniDumpWithFullMemory = 0x00000002,
      MiniDumpWithHandleData = 0x00000004,
      MiniDumpFilterMemory = 0x00000008,
      MiniDumpScanMemory = 0x00000010,
      MiniDumpWithUnloadedModules = 0x00000020,
      MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
      MiniDumpFilterModulePaths = 0x00000080,
      MiniDumpWithProcessThreadData = 0x00000100,
      MiniDumpWithPrivateReadWriteMemory = 0x00000200,
      MiniDumpWithoutOptionalData = 0x00000400,
      MiniDumpWithFullMemoryInfo = 0x00000800,
      MiniDumpWithThreadInfo = 0x00001000,
      MiniDumpWithCodeSegs = 0x00002000,
      MiniDumpWithoutAuxiliaryState = 0x00004000,
      MiniDumpWithFullAuxiliaryState = 0x00008000,
      MiniDumpWithPrivateWriteCopyMemory = 0x00010000,
      MiniDumpIgnoreInaccessibleMemory = 0x00020000,
      MiniDumpValidTypeFlags = 0x0003ffff,
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4)]  // Pack=4 is important! So it works also for x64!
    struct MINIDUMP_EXCEPTION_INFORMATION
    {
      public uint ThreadId;
      public IntPtr ExceptionPointers;
      [MarshalAs(UnmanagedType.Bool)]
      public bool ClientPointers;
    }

    [DllImport("DBGHELP",
      EntryPoint = "MiniDumpWriteDump",
      CallingConvention = CallingConvention.StdCall,
      CharSet = CharSet.Unicode,
      ExactSpelling = true,
      SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool MiniDumpWriteDump
    (
      SafeProcessHandle hProcess,
      uint ProcessId,
      SafeFileHandle hFile,
      MINIDUMP_TYPE DumpType,
      ref MINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
      IntPtr UserStreamParam,
      IntPtr CallbackParam
    );

    public static bool Write(string fileName) => Write(fileName, MINIDUMP_TYPE.MiniDumpNormal);

    public static bool Write(string fileName, MINIDUMP_TYPE DumpType)
    {
      using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
      {
        var ExceptionParam = new MINIDUMP_EXCEPTION_INFORMATION
        {
          ThreadId = GetCurrentThreadId(),
          ExceptionPointers = Marshal.GetExceptionPointers(),
          ClientPointers = false
        };

        return MiniDumpWriteDump
        (
          GetCurrentProcess(),
          GetCurrentProcessId(),
          fs.SafeFileHandle,
          DumpType,
          ref ExceptionParam,
          IntPtr.Zero,
          IntPtr.Zero
        );
      }
    }
  }
}
