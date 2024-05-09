using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using static Microsoft.Win32.SafeHandles.InteropServices.Kernel32;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit
{
  static partial class Diagnostics
  {
    internal static class Browser
    {
      public static void Start(string url)
      {
        try
        {
          Process.Start(new ProcessStartInfo(url)
          {
            UseShellExecute = true,
            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
          });

          return;
        }
        catch{ }

        using
        (
          var taskDialog = new TaskDialog("Open Browser")
          {
            MainIcon = UIX.TaskDialogIcons.IconWarning,
            TitleAutoPrefix = true,
            AllowCancellation = true,
            MainInstruction = "Unable to found a default web browser",
            MainContent = string.Empty,
            ExpandedContent = string.Empty,
            CommonButtons = TaskDialogCommonButtons.None,
            DefaultButton = TaskDialogResult.None,
            FooterText = new Uri(url).Authority
          }
        )
        {
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Copy URL to clipboard", url);
          taskDialog.DefaultButton = TaskDialogResult.CommandLink1;
          if (taskDialog.Show() == TaskDialogResult.CommandLink1)
          {
            System.Windows.Forms.Clipboard.SetText(url);
          }
        }
      }
    }

    internal static class ErrorReport
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

      public static string OnLoadStackTraceFilePath
      {
        get => NativeLoader.GetStackTraceFilePath();
        set => NativeLoader.SetStackTraceFilePath(value);
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
        string revitVersionNumber,
        string revitSubVersionNumber,
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
                writer.WriteLine($"  - SubVersionNumber: {revitSubVersionNumber}");
                writer.WriteLine($"  - ProductType: {revitProduct}");
                writer.WriteLine($"  - Language: {revitLanguage}");

                var rhino = Core.Distribution.VersionInfo;
                writer.WriteLine($"- Rhino: {rhino.ProductVersion} ({rhino.FileDescription})");
                writer.WriteLine($"- Rhino.Inside Revit: {Core.DisplayVersion}");

                writer.WriteLine();
                writer.WriteLine("### Environment Variables");
                writer.WriteLine();
                writer.WriteLine("|PATH|");
                writer.WriteLine("|:---|");
                var PATH = Environment.GetEnvironmentVariable("PATH");
                foreach (var pathEntry in PATH.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                  writer.WriteLine($"| {pathEntry} |");

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

                  string CompanyName = (versionInfo?.CompanyName ?? string.Empty).Replace(@"""", @"""""");
                  string ProductName = (versionInfo?.ProductName ?? string.Empty).Replace(@"""", @"""""");
                  string ProductVersion = (versionInfo?.ProductVersion ?? string.Empty).Replace(@"""", @"""""");
                  string AddInTypeFullName = (addinType?.FullName ?? string.Empty).Replace(@"""", @"""""");
                  string AssemblyFullName = (addinType?.Assembly.FullName ?? string.Empty).Replace(@"""", @"""""");
                  string AssemblyLocation = (addinType?.Assembly.Location ?? string.Empty).Replace(@"""", @"""""");

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

      public static void SendEmail
      (
        UIX.UIHostApplication app,
        string subject,
        string body,
        bool includeAddinsList,
        IEnumerable<string> attachments
      )
      {
        var services = app.Services;
        var now = DateTime.Now.ToString("yyyyMMddTHHmmssZ");
        var reportFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"RhinoInside-Revit-Report-{now}.zip");

        CreateReportFile
        (
          services.VersionName,
          services.VersionBuild,
          services.VersionNumber,
          services.SubVersionNumber,
          services.Product,
          services.Language,
          includeAddinsList ? app.LoadedApplications : default,
          reportFilePath,
          OnLoadStackTraceFilePath,
          attachments
        );

        var revitVersion = $"{services.SubVersionNumber} ({services.VersionBuild})";

        SendEmail(subject, body, reportFilePath, revitVersion, attachments);
      }

      static void SendEmail
      (
        string subject,
        string body,
        string ReportFilePath,
        string revitVersion,
        IEnumerable<string> attachments
      )
      {
        foreach (var file in attachments)
        {
          try { File.Delete(file); }
          catch (IOException) { }
        }

        subject = Uri.EscapeDataString(subject);
        var mailtoURI = $"mailto:tech@mcneel.com?subject={subject}&body=";

        var mailBody = @"Give us any additional info you see fit here..." + Environment.NewLine + Environment.NewLine;
        if (File.Exists(ReportFilePath))
          mailBody += $"⚠ Please drag and drop the '{ReportFilePath}' file here to attach the report files ⚠" + Environment.NewLine + Environment.NewLine;

        if (!string.IsNullOrEmpty(body))
        {
          mailBody += new string('-', 78) + Environment.NewLine;
          mailBody += body + Environment.NewLine;
          mailBody += new string('-', 78) + Environment.NewLine + Environment.NewLine;
        }

        mailBody += $"OS: {Environment.OSVersion}" + Environment.NewLine;
        mailBody += $"CLR: {ErrorReport.CLRVersion}" + Environment.NewLine;
        mailBody += $"Revit: {revitVersion}" + Environment.NewLine;

        var rhino = Core.Distribution.VersionInfo;
        mailBody += $"Rhino: {rhino.ProductVersion} ({rhino.FileDescription})" + Environment.NewLine;
        mailBody += $"Rhino.Inside Revit: {Core.DisplayVersion}" + Environment.NewLine;

        mailBody = Uri.EscapeDataString(mailBody);

        using (Process.Start(mailtoURI + mailBody)) { }
      }

      public static Result ShowLoadError()
      {
        using
        (
          var taskDialog = new TaskDialog("Oops! Something went wrong :(")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}",
            MainIcon = UIX.TaskDialogIcons.IconError,
            TitleAutoPrefix = true,
            AllowCancellation = true,
            MainInstruction = "Rhino.Inside failed to load",
            MainContent = $"Please run some tests before reporting.{Environment.NewLine}Those tests would help us figure out what happened.",
            ExpandedContent = "This problem use to be due an incompatibility with other installed add-ins.\n\n" +
                              "While running on these modes you may see other add-ins errors and it may take longer to load, don't worry about that no persistent change will be made on your computer.",
            VerificationText = "Exclude installed add-ins list from the report.",
            FooterText = "Current version: " + Core.DisplayVersion
          }
        )
        {
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "1. Run Revit without other Addins…", "Good for testing if Rhino.Inside would load if no other add-in were installed.");
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "2. Run Rhino.Inside in verbose mode…", "Enables all logging mechanisms built in Rhino for support purposes.");
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "3. Send report…", "Reports this problem by email to tech@mcneel.com");
          taskDialog.DefaultButton = TaskDialogResult.CommandLink3;

          while (true)
            switch (taskDialog.Show())
            {
              case TaskDialogResult.CommandLink1: RunWithoutAddIns(); break;
              case TaskDialogResult.CommandLink2: RunVerboseMode(); break;
              case TaskDialogResult.CommandLink3:
                SendEmail
                (
                  Core.Host,
                  subject: "Rhino.Inside Revit failed to load",
                  body: null,
                  includeAddinsList: !taskDialog.WasVerificationChecked(),
                  attachments: new string[]
                  {
                    Core.Host.Services.RecordingJournalFilename,
                    RhinoDebugMessages_txt,
                    RhinoAssemblyResolveLog_txt
                  }
                );
                return Result.Succeeded;
              default: return Result.Cancelled;
            }
        }
      }

      static void RunWithoutAddIns()
      {
        var SafeModeFolder = Path.Combine(Core.Host.Services.CurrentUserAddinsLocation, "RhinoInside.Revit", "SafeMode");
        Directory.CreateDirectory(SafeModeFolder);

        Settings.AddIns.GetInstalledAddins(Core.Host.Services.VersionNumber, out var manifests);
        if (manifests.FirstOrDefault(x => Path.GetFileName(x) == "RhinoInside.Revit.addin") is string manifestFile)
        {
          if (Settings.AddIns.LoadFrom(manifestFile, out var safeManifest))
          {
            // Make all paths absolute
            foreach(var addin in safeManifest)
              addin.Assembly = safeManifest.ToFileInfo(addin.Assembly).FullName;

            var safeManifestFile = Path.Combine(SafeModeFolder, Path.GetFileName(manifestFile));
            Settings.AddIns.SaveAs(safeManifest, safeManifestFile);
          }

          var journalFile = Path.Combine(SafeModeFolder, "RhinoInside.Revit-SafeMode.txt");
          using (var journal = File.CreateText(journalFile))
          {
            var TabName = "Rhino.Inside";
            journal.WriteLine("' ");
            journal.WriteLine("Dim Jrn");
            journal.WriteLine("Set Jrn = CrsJournalScript");
            journal.WriteLine($" Jrn.RibbonEvent \"TabActivated:{TabName}\"");
            journal.WriteLine($" Jrn.RibbonEvent \"Execute external command:CustomCtrl_%CustomCtrl_%{TabName}%More%CommandStart:RhinoInside.Revit.AddIn.Commands.CommandStart\"");
            journal.WriteLine($" Jrn.RibbonEvent \"Execute external command:CustomCtrl_%CustomCtrl_%{TabName}%Rhinoceros%CommandRhino:RhinoInside.Revit.AddIn.Commands.CommandRhino\"");
          }
#if DEBUG
          var batchFile = Path.Combine(SafeModeFolder, "RhinoInside.Revit-SafeMode.bat");
          using (var batch = File.CreateText(batchFile))
          {
            batch.WriteLine($"\"{Process.GetCurrentProcess().MainModule.FileName}\" \"{Path.GetFileName(journalFile)}\"");
          }
#endif
          var si = new ProcessStartInfo()
          {
            FileName = Process.GetCurrentProcess().MainModule.FileName,
            Arguments = $"\"{journalFile}\""
          };

          using (var RevitApp = Process.Start(si)) RevitApp.WaitForExit();
        }
      }

      static readonly string RhinoDebugMessages_txt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RhinoDebugMessages.txt");
      static readonly string RhinoAssemblyResolveLog_txt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RhinoAssemblyResolveLog.txt");

      static void RunVerboseMode()
      {
        const string SDKRegistryKeyName = @"Software\McNeel\Rhinoceros\SDK";

        if (File.Exists(RhinoDebugMessages_txt))
          File.Delete(RhinoDebugMessages_txt);

        if (File.Exists(RhinoAssemblyResolveLog_txt))
          File.Delete(RhinoAssemblyResolveLog_txt);

        using (File.Create(RhinoAssemblyResolveLog_txt)) { }

        bool deleteKey = false;

        try
        {
          using (var existingSDK = Registry.CurrentUser.OpenSubKey(SDKRegistryKeyName))
            if (existingSDK is null)
            {
              using (var newSDK = Registry.CurrentUser.CreateSubKey(SDKRegistryKeyName))
                if (newSDK is null)
                  return;

              deleteKey = true;
            }

          var debugLogging = Rhinoceros.DebugLogging.Current;
          try
          {
            Rhinoceros.DebugLogging.Current = new Rhinoceros.DebugLogging
            {
              Enabled = true,
              SaveToFile = true
            };

            var si = new ProcessStartInfo()
            {
              FileName = Process.GetCurrentProcess().MainModule.FileName,
              Arguments = "/nosplash",
              UseShellExecute = false
            };
            si.EnvironmentVariables["RhinoInside_RunScript"] = "_Grasshopper";

            using (var RevitApp = Process.Start(si)) { RevitApp.WaitForExit(); }
          }
          finally
          {
            Rhinoceros.DebugLogging.Current = debugLogging;
          }
        }
        finally
        {
          if (deleteKey)
            try { Registry.CurrentUser.DeleteSubKey(SDKRegistryKeyName); }
            catch { }
        }
      }

      public static void DumpException(Exception e, UIX.UIHostApplication app)
      {
        var RhinoInside_dmp = Path.Combine
        (
          Path.GetDirectoryName(app.Services.RecordingJournalFilename),
          Path.GetFileNameWithoutExtension(app.Services.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
        );

        if (MiniDumper.Write(RhinoInside_dmp))
        {
          var attachments = e.Data["Attachments"] as IEnumerable<string> ?? Enumerable.Empty<string>();
          e.Data["Attachments"] = attachments.Append(RhinoInside_dmp).ToArray();
        }
      }

      public static void TraceException(Exception e, UIX.UIHostApplication app)
      {
        var comment = $@"Managed exception caught from external API application '{e.Source}' in method '{e.TargetSite}'{Environment.NewLine}{e}";
        comment = comment.Replace(Environment.NewLine, $"{Environment.NewLine}'");
        app.Services.WriteJournalComment(comment, true);
      }

      public static void ReportException(Exception e, UIX.UIHostApplication app)
      {
        var attachments = Enumerable.Empty<string>();
        while (e.InnerException is object)
        {
          if (e.Data["Attachments"] is IEnumerable<string> attach)
            attachments.Concat(attach);

          // Show the most inner exception
          e = e.InnerException;
        }

        if (MessageBox.Show
        (
          owner: app.GetMainWindow(),
          caption: $"{Core.Product}.{Core.Platform} {Core.Version} - Oops! Something went wrong :(",
          icon: MessageBoxImage.Error,
          messageBoxText: $"'{e.GetType().FullName}' at {e.Source}." + Environment.NewLine +
                          Environment.NewLine + e.Message + Environment.NewLine +
                          Environment.NewLine + "Do you want to report this problem by email to tech@mcneel.com?",
          button: MessageBoxButton.YesNo,
          defaultResult: MessageBoxResult.Yes
        ) == MessageBoxResult.Yes)
        {
          if (e.Data["Attachments"] is IEnumerable<string> attach)
            attachments.Concat(attach);

          SendEmail
          (
            app,
            subject: $"{Core.Product}.{Core.Platform} - {e.GetType().FullName}",
            body: e.ToString(),
            includeAddinsList: false,
            attachments: attachments.Prepend(app.Services.RecordingJournalFilename)
          );
        }
      }
    }

    internal static class MiniDumper
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

      public static bool Write(string fileName) => Write(fileName, MINIDUMP_TYPE.MiniDumpNormal);

      public static bool Write(string fileName, MINIDUMP_TYPE DumpType)
      {
        using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
        {
          var ExceptionParam = new SafeNativeMethods.MINIDUMP_EXCEPTION_INFORMATION
          {
            ThreadId = GetCurrentThreadId(),
            ExceptionPointers = Marshal.GetExceptionPointers(),
            ClientPointers = false
          };

          if (ExceptionParam.ExceptionPointers == IntPtr.Zero)
            return SafeNativeMethods.MiniDumpWriteDump
            (
              GetCurrentProcess(),
              GetCurrentProcessId(),
              fs.SafeFileHandle,
              DumpType,
              IntPtr.Zero,
              IntPtr.Zero,
              IntPtr.Zero
            );

          return SafeNativeMethods.MiniDumpWriteDump
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

      [SuppressUnmanagedCodeSecurity]
      static class SafeNativeMethods
      {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]  // Pack=4 is important! So it works also for x64!
        public struct MINIDUMP_EXCEPTION_INFORMATION
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
        public static extern bool MiniDumpWriteDump
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
        public static extern bool MiniDumpWriteDump
        (
          SafeProcessHandle hProcess,
          uint ProcessId,
          SafeFileHandle hFile,
          MINIDUMP_TYPE DumpType,
          IntPtr ExceptionParam,
          IntPtr UserStreamParam,
          IntPtr CallbackParam
        );
      }

    }

    internal static class Logger
    {
      public static string LogPath { get; private set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Rhino-Inside-Revit.log");

      public static bool Active = Directory.Exists(LogPath);
      static string AppDomainAssembliesPath => Path.Combine(LogPath, $"AppDomain.Assemblies.md");
      static string ThreadLogPath => Path.Combine(LogPath, $"Thread{Thread.CurrentThread.ManagedThreadId}-Log.md");

      [ThreadStatic]
      static FileStream threadLogStream;
      static Stream ThreadLogStream
      {
        get
        {
          try { return threadLogStream ?? (threadLogStream = new FileStream(ThreadLogPath, FileMode.Append, FileAccess.Write, FileShare.Read)); }
          catch { }
          return default;
        }
      }

      static Logger()
      {
        if (Active)
        {
#if !DEBUG
        // This should disable logging on each run, we don't want the user forget to disable it.
        try
        {
          var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Rhino-Inside-Revit-{Now}.log");
          Directory.Move(LogPath, logPath);
          LogPath = logPath;
        }
        catch { }
#endif
          // Capture assemblies here and log in an other thread.
          var assemblies = AppDomain.CurrentDomain.GetAssemblies();
          Task.Run(() =>
          {
            using (var writer = new StreamWriter(AppDomainAssembliesPath, true, System.Text.Encoding.UTF8))
            {
              writer.WriteLine("# Assemblies");
              writer.WriteLine();
              writer.WriteLine("| #|Name|Version|Culture|Token|Location|");
              writer.WriteLine("|-:|:---|:------|:-----:|:---:|:-------|");

              var index = 0;
              foreach (var assembly in assemblies)
              {
                var assemblyName = assembly.GetName();
                var publicKeyToken = assemblyName.GetPublicKeyToken() ?? Array.Empty<byte>();
                var token = string.Concat(Array.ConvertAll(publicKeyToken, x => x.ToString("X2")));
                var location = assembly.IsDynamic ? string.Empty : assembly.Location;

                writer.WriteLine($"|{index++}|{assemblyName.Name}|{assemblyName.Version}|{assemblyName.CultureName}|{token}|{location}|");
              }
            }
          });

          AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
          AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; ;
        }
      }

      private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
      {
        // TODO : Add more info to each exception entry like callstack info.

        Log(Severity.Exception, e.Exception.GetType().FullName, $"Source = {e.Exception.Source}", e.Exception.Message);
      }

      private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
        if (e.ExceptionObject is Exception exception)
          LogCritical(exception.GetType().FullName, $"Source = {exception.Source}", exception.Message);

        if (e.IsTerminating)
          LogCritical("CLR is terminating.");
      }

      static string Now => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff");

      [ThreadStatic]
      static int IndentLevel = 1;
      public static int EntryCount = 0;

      internal struct Entry : IDisposable
      {
        readonly int Indent;
        public readonly int Id;
        public readonly DateTime Timestamp;

        internal string IndentPrefix => Indent > 0 ? new string('>', Indent) : string.Empty;
        internal string Header => $"ID {Id} {Timestamp:yyyy-MM-dd HH:mm:ss.ff}";
        internal string HeaderId => $"#id-{Id}-{Timestamp:yyyy-MM-dd-HHmmssff}";

        internal Entry(int id, int indent)
        {
          Id = id;
          Timestamp = DateTime.Now;
          Indent = indent;
        }

        void IDisposable.Dispose()
        {
          Logger.Log(this, EntryRole.Subordinate, Severity.Return, string.Empty);

          if (Indent > 0)
            IndentLevel = Indent - 1;
        }

        internal static Entry Next() => Active ? new Entry(Interlocked.Increment(ref EntryCount), IndentLevel) : default;
        internal static Entry Scope() => Active ? new Entry(Interlocked.Increment(ref EntryCount), ++IndentLevel) : default;

        public void LogTrace(string name, params string[] details) => Logger.Log(this, EntryRole.Subordinate, Severity.Trace, name, details);
        public void LogWarning(string name, params string[] details) => Logger.Log(this, EntryRole.Subordinate, Severity.Warning, name, details);
        public void Log(Severity severity, string name, params string[] details) => Logger.Log(this, EntryRole.Subordinate, severity, name, details);

        internal void LogEntry(Stream stream, EntryRole role, Severity severity, string name, params string[] details)
        {
          string Add(string token) => string.IsNullOrEmpty(token) ? string.Empty : $"{token} ";

          string Italic(string token) => string.IsNullOrEmpty(token) ? string.Empty : $"*{token}*";
          string Bold(string token) => string.IsNullOrEmpty(token) ? string.Empty : $"**{token}**";
          string Link(string token, string url) => string.IsNullOrEmpty(token) ? string.Empty : $"[{token}]({url})";

          using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 0x1000, true))
          {
            writer.AutoFlush = false;

            // Header
            if (role == EntryRole.Main)
            {
              writer.WriteLine($"{Add(IndentPrefix)}###### {Header}");
              writer.WriteLine(IndentPrefix);
              writer.WriteLine($"{Add(IndentPrefix)}{Add(SeverityIcon(severity))}{Add(Bold(name))} ");
            }
            else
            {
              writer.WriteLine($"{Add(IndentPrefix)}{Add(Link(SeverityIcon(severity), HeaderId))}{Add(Italic(Now))}{Add(name)} ");
            }

            // Details
            foreach (var detail in details)
              writer.WriteLine($"{Add(IndentPrefix)}{Add(detail)} ");

            // Footer
            if (severity == Severity.Return && Indent > 0)
              writer.WriteLine(new string('>', Indent - 1));
            else
              writer.WriteLine(IndentPrefix);

            writer.Flush();
          }
        }
      }

      internal enum EntryRole
      {
        Main,
        Subordinate
      }

      public enum Severity
      {
        Exception = -4, // '⚡'
        Critical = -3, // '⛔'
        Error = -2, // '❌'
        Warning = -1, // '⚠'
        Trace = 0,
        Information = +1, // 'ℹ'
        Succeded = +2, // '✔️'
        Return = +3, // '⤶'
      }

      static string SeverityIcon(Severity severity)
      {
        switch (severity)
        {
          case Severity.Exception: return "⚡";
          case Severity.Critical: return "⛔";
          case Severity.Warning: return "⚠️";
          case Severity.Error: return "❌";
          case Severity.Succeded: return "✔️";
          case Severity.Information: return "ℹ️";
          case Severity.Return: return "⤶";
        }

        return string.Empty;
      }

      public static Entry LogScope([CallerMemberName] string name = "", params string[] details)
      {
        var entry = Entry.Scope();
        Log(entry, EntryRole.Main, Severity.Trace, name, details);
        return entry;
      }

      public static void LogTrace([CallerMemberName] string name = "", params string[] details) =>
        Log(Entry.Next(), EntryRole.Main, Severity.Trace, name, details);

      public static void LogInformation([CallerMemberName] string name = "", params string[] details) =>
        Log(Entry.Next(), EntryRole.Main, Severity.Information, name, details);

      public static void LogSucceded([CallerMemberName] string name = "", params string[] details) =>
        Log(Entry.Next(), EntryRole.Main, Severity.Succeded, name, details);

      public static void LogWarning([CallerMemberName] string name = "", params string[] details) =>
        Log(Entry.Next(), EntryRole.Main, Severity.Warning, name, details);

      public static void LogError([CallerMemberName] string name = "", params string[] details) =>
        Log(Entry.Next(), EntryRole.Main, Severity.Error, name, details);

      public static void LogCritical([CallerMemberName] string name = "", params string[] details) =>
        Log(Entry.Next(), EntryRole.Main, Severity.Critical, name, details);

      static void Log(Severity severity, string name, params string[] details) =>
        Log(Entry.Next(), EntryRole.Main, severity, name, details);

      static void Log(Entry entry, EntryRole role, Severity severity, string name, params string[] details)
      {
        if (Active && ThreadLogStream is Stream threadLogStream)
          DispatchAsync(() => entry.LogEntry(threadLogStream, role, severity, name, details));
      }

      #region Dispatcher
      static readonly WeakReference<Task> dispatcher = new WeakReference<Task>(default);
      static void DispatchAsync(Action action)
      {
        lock (dispatcher)
        {
          dispatcher.SetTarget
          (
            dispatcher.TryGetTarget(out var currentDispatcher) ?
            currentDispatcher.ContinueWith(_ => action()) :
            Task.Run(action)
          );
        }
      }
      #endregion
    }

    internal static class NativeLoader
    {
      internal static string GetStackTraceFilePath() => Marshal.PtrToStringUni(SafeNativeMethods.LdrGetStackTraceFilePath());
      internal static void SetStackTraceFilePath(string reportFilePath) => SafeNativeMethods.LdrSetStackTraceFilePath(reportFilePath);
      internal static bool GetReportOnLoad(string moduleName) => SafeNativeMethods.LdrGetReportOnLoad(moduleName);
      internal static void SetReportOnLoad(string moduleName, bool enable) => SafeNativeMethods.LdrSetReportOnLoad(moduleName, enable);
      internal static bool IsolateOpenNurbs(IntPtr hHostWnd) => SafeNativeMethods.LdrIsolateOpenNurbs(hHostWnd);

      [SuppressUnmanagedCodeSecurity]
      static class SafeNativeMethods
      {
        [DllImport("RhinoInside.Revit.Native.dll",
         EntryPoint = "LdrGetStackTraceFilePath",
         CharSet = CharSet.Unicode)]
        public static extern System.IntPtr LdrGetStackTraceFilePath();

        [DllImport("RhinoInside.Revit.Native.dll",
         EntryPoint = "LdrSetStackTraceFilePath",
         CharSet = CharSet.Unicode)]
        public static extern void LdrSetStackTraceFilePath(string reportFilePath);

        [DllImport("RhinoInside.Revit.Native.dll",
         EntryPoint = "LdrGetReportOnLoad",
         CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LdrGetReportOnLoad(string moduleName);

        [DllImport("RhinoInside.Revit.Native.dll",
         EntryPoint = "LdrSetReportOnLoad",
         CharSet = CharSet.Unicode)]
        public static extern void LdrSetReportOnLoad(string moduleName, [MarshalAs(UnmanagedType.Bool)] bool enable);

        [DllImport("RhinoInside.Revit.Native.dll",
         EntryPoint = "LdrIsolateOpenNurbs",
         CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LdrIsolateOpenNurbs(IntPtr hHostWnd);
      }
    }
  }
}
