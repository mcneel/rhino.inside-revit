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
        var location = typeof(object).Assembly.Location;
        if (File.Exists(location) && FileVersionInfo.GetVersionInfo(location) is FileVersionInfo info)
          return $"{Environment.Version} ({info.ProductVersion})";

        return $"{Environment.Version}";
      }
    }

    public static string CLRMaxVersion
    {
      get
      {
        string subkey = Environment.Version.Major < 4 ?
          $@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v{Environment.Version.Major}.{Environment.Version.Minor}\" :
          $@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v{Environment.Version.Major}\Full\";

        using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey(subkey))
        {
          if (ndpKey?.GetValue("Version") is string version)
            return version;
        }

        return string.Empty;
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

    static void CreateReportFile
    (
      string revitVersionName,
      string revitVersionBuild,
      string revitSubVersionNumber,
      string revitVersionNumber,
      Autodesk.Revit.ApplicationServices.ProductType revitProduct,
      Autodesk.Revit.ApplicationServices.LanguageType revitLanguage,
      ExternalApplicationArray loadedApplications,
      string reportFilePath,
      string logFile,
      IEnumerable<string> attachments
    )
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
              writer.WriteLine($"- Environment.MaxVersion: {CLRMaxVersion}");

              writer.WriteLine($"- {revitVersionName}");
              writer.WriteLine($"  - VersionBuild: {revitVersionBuild}");
#if REVIT_2019
              writer.WriteLine($"  - SubVersionNumber: {revitSubVersionNumber}");
#else
              writer.WriteLine($"  - VersionNumber: {revitVersionNumber}");
#endif
              writer.WriteLine($"  - ProductType: {revitProduct}");
              writer.WriteLine($"  - Language: {revitLanguage}");

              var rhino = Addin.RhinoVersionInfo;
              writer.WriteLine($"- Rhino: {rhino.ProductVersion} ({rhino.FileDescription})");
              writer.WriteLine($"- Rhino.Inside Revit: {Addin.DisplayVersion}");

              if (loadedApplications is object)
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
                writer.WriteLine("## Attachments");
                writer.WriteLine();
                foreach (var attachment in attachments)
                {
                  var attachmentName = Path.GetFileName(attachment);
                  writer.WriteLine($"[{attachmentName}](Attachments/{attachmentName})  ");
                }
              }

              if (File.Exists(logFile))
              {
                writer.WriteLine();
                writer.WriteLine("## Log");
                writer.WriteLine();

                using (var log = new StreamReader(logFile))
                {
                  string line;
                  while ((line = log.ReadLine()) != null)
                  {
                    writer.WriteLine(line);
                  }
                }
              }
            }
          }

          // Addins
          if (loadedApplications is object)
          {
            var LoadedApplicationsCSV = archive.CreateEntry($"{now}/Addins/{now}.csv");
            using (var writer = new StreamWriter(LoadedApplicationsCSV.Open()))
            {
              writer.WriteLine(@"""Company-Name"",""Product-Name"",""Product-Version"",""AddInType-FullName"",""Assembly-FullName"",""Assembly-Location""");

              foreach (var application in loadedApplications)
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

              foreach (var application in loadedApplications)
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

            Settings.AddIns.GetSystemAddins(revitVersionNumber, out var systemAddins);
            foreach (var addin in systemAddins)
              CreateReportEntry(archive, $"{now}/Addins/System/{Path.GetFileName(addin)}", addin);

            Settings.AddIns.GetInstalledAddins(revitVersionNumber, out var installedAddins);
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


    public static void SendEmail(UIControlledApplication app, string subject, bool includeAddinsList, IEnumerable<string> attachments)
    {
      var revit = app.ControlledApplication;
      var now = DateTime.Now.ToString("yyyyMMddTHHmmssZ");
      var reportFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"RhinoInside-Revit-Report-{now}.zip");

      CreateReportFile
      (
        revit.VersionName,
        revit.VersionBuild,
#if REVIT_2019
        revit.SubVersionNumber,
#else
        revit.VersionNumber,
#endif
        revit.VersionNumber,
        revit.Product,
        revit.Language,
        includeAddinsList ? app.LoadedApplications : default,
        reportFilePath,
        Path.ChangeExtension(revit.RecordingJournalFilename, "log.md"),
        attachments
      );

#if REVIT_2019
      var revitVersion = $"{revit.SubVersionNumber} ({revit.VersionBuild})";
#else
      var revitVersion = $"{revit.VersionNumber} ({revit.VersionBuild})";
#endif

      SendEmail(subject, reportFilePath, revitVersion, includeAddinsList, attachments);
    }

    public static void SendEmail(UIApplication app, string subject, bool includeAddinsList, IEnumerable<string> attachments)
    {
      var revit = app.Application;
      var now = DateTime.Now.ToString("yyyyMMddTHHmmssZ");
      var reportFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"RhinoInside-Revit-Report-{now}.zip");

      CreateReportFile
      (
        revit.VersionName,
        revit.VersionBuild,
#if REVIT_2019
        revit.SubVersionNumber,
#else
        revit.VersionNumber,
#endif
        revit.VersionNumber,
        revit.Product,
        revit.Language,
        includeAddinsList ? app.LoadedApplications : default,
        reportFilePath,
        Path.ChangeExtension(revit.RecordingJournalFilename, "log.md"),
        attachments
      );

#if REVIT_2019
      var revitVersion = $"{revit.SubVersionNumber} ({revit.VersionBuild})";
#else
      var revitVersion = $"{revit.VersionNumber} ({revit.VersionBuild})";
#endif

      SendEmail(subject, reportFilePath, revitVersion, includeAddinsList, attachments);
    }

    static void SendEmail(string subject, string ReportFilePath, string revitVersion, bool includeAddinsList, IEnumerable<string> attachments)
    {
      foreach (var file in attachments)
      {
        try { File.Delete(file); }
        catch (IOException) { }
      }

      subject = Uri.EscapeUriString(subject);
      var mailtoURI = $"mailto:tech@mcneel.com?subject={subject}&body=";

      var mailBody = @"Please give us any additional info you see fit here..." + Environment.NewLine + Environment.NewLine;
      if (File.Exists(ReportFilePath))
        mailBody += $"!!! Please drag and drop the '{ReportFilePath}' file here to attach the error files !!!" + Environment.NewLine + Environment.NewLine;

      mailBody += $"OS: {Environment.OSVersion}" + Environment.NewLine;
      mailBody += $"CLR: {ErrorReport.CLRVersion}" + Environment.NewLine;
      mailBody += $"Revit: {revitVersion}" + Environment.NewLine;

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
      in MINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
      IntPtr UserStreamParam,
      IntPtr CallbackParam
    );

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
      IntPtr ExceptionParam,
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

        if (ExceptionParam.ExceptionPointers == IntPtr.Zero)
          return MiniDumpWriteDump
          (
            GetCurrentProcess(),
            GetCurrentProcessId(),
            fs.SafeFileHandle,
            DumpType,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero
          );

        return MiniDumpWriteDump
        (
          GetCurrentProcess(),
          GetCurrentProcessId(),
          fs.SafeFileHandle,
          DumpType,
          in ExceptionParam,
          IntPtr.Zero,
          IntPtr.Zero
        );
      }
    }
  }
}
