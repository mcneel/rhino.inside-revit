using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows;

namespace RhinoInside.Revit.Settings
{
  [Serializable]
  public class Options
  {
    [XmlElement]
    public bool CheckForUpdatesOnStartup { get; set; } = true;

    [XmlElement]
    public string UpdateChannel { get; set; } = AddinUpdater.DefaultChannel.Id.ToString();
  }

  // Easy static access to addin options
  public static class AddinOptions
  {
    // settings set by admin are readonly
    public static bool IsReadOnly =>
      // Push instance to initialize
      Instance != null && usingAdminOptions;
  
    public static bool CheckForUpdatesOnStartup
    {
      get => Instance.CheckForUpdatesOnStartup;
      set => Instance.CheckForUpdatesOnStartup = value;
    }

    public static string UpdateChannel
    {
      get => Instance.UpdateChannel;
      set => Instance.UpdateChannel = value;
    }

    // Singleton
    private static bool usingAdminOptions = false;
    private static Options instance = null;
    private static readonly object padlock = new object();
    public static Options Instance
    {
      get
      {
        lock (padlock)
        {
          if (instance is null)
          {
            var xml = new XmlSerializer(typeof(Options));

            // look for admin options file, then user, otherwise null
            // settings set by admin are readonly
            usingAdminOptions = File.Exists(AdminOptionsFilePath);
            string targetOptionFile =
              usingAdminOptions ? AdminDataDirectory :
                File.Exists(UserOptionsFilePath) ? UserOptionsFilePath : null;

            if (targetOptionFile != null)
            {
              try
              {
                // read settings
                using (var optsFile = File.OpenRead(UserOptionsFilePath))
                  instance = (Options) xml.Deserialize(optsFile);
              }
              catch
              {
                MessageBox.Show
                (
                  caption: $"Rhino.Inside - Oops! Something went wrong :(",
                  icon: MessageBoxImage.Error,
                  messageBoxText: "Error reading options. Using defaults instead.",
                  button: MessageBoxButton.OK
                );
              }
            }

            // otherwise use default
            if (instance is null)
              instance = new Options();
          }
          return instance;
        }
      }
    }

    // Data store information
    private static string DataDirectoryPath => Path.Combine("McNeel", "Rhino.Inside", "Revit", $"{Addin.Version.Major}.0");
    private static string AdminDataDirectory => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
      DataDirectoryPath
      );
    private static string UserDataDirectory => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      DataDirectoryPath
      );
    private static string OptionsFileName = "Options.xml";
    private static string AdminOptionsFilePath => Path.Combine(AdminDataDirectory, OptionsFileName);
    private static string UserOptionsFilePath => Path.Combine(UserDataDirectory, OptionsFileName);

    public static void Save()
    {
      var xmlSerializer = new XmlSerializer(typeof(Options));

      // ensure directory exists
      // clear previous contents, or create empty file
      Directory.CreateDirectory(UserDataDirectory);
      File.WriteAllText(UserOptionsFilePath, "");

      // serialize options to the empty file
      using (var optsFile = File.OpenWrite(UserOptionsFilePath))
      {
        xmlSerializer.Serialize(optsFile, Instance);
      }
    }
  }
}
