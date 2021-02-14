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
  // Easy static access to addin options
  [Serializable]
  [XmlRoot("Options")]
  public class AddinOptions
  {
    #region Runtime and UI
    [XmlElement]
    public bool LoadOnStartup
    {
      get => _loadOnStartup;
      set
      {
        _loadOnStartup = value;
        LoadOnStartupChanged?.Invoke(this, null);
      }
    }
    private bool _loadOnStartup = false;
    public static event EventHandler<EventArgs> LoadOnStartupChanged;

    [XmlElement]
    public bool CompactRibbon {
      get => _compactRibbon;
      set
      {
        _compactRibbon = value;
        CompactRibbonChanged?.Invoke(this, null);
      }
    }
    private bool _compactRibbon = false;
    public static event EventHandler<EventArgs> CompactRibbonChanged;

    #endregion

    #region Updates
    [XmlElement]
    public bool CheckForUpdatesOnStartup
    {
      get => _checkForUpdatesOnStartup;
      set
      {
        _checkForUpdatesOnStartup = value;
        CheckForUpdatesOnStartupChanged?.Invoke(this, null);
      }
    }
    private bool _checkForUpdatesOnStartup = true;
    public static event EventHandler<EventArgs> CheckForUpdatesOnStartupChanged;

    [XmlElement]
    public string UpdateChannel
    {
      get => _updateChannel;
      set
      {
        _updateChannel = value;
        UpdateChannelChanged?.Invoke(this, null);
      }
    }
    private string _updateChannel = AddinUpdater.DefaultChannel.Id.ToString();
    public static event EventHandler<EventArgs> UpdateChannelChanged;
    #endregion

    #region Scripts
    [XmlElement]
    public bool LoadScriptsOnStartup
    {
      get => _loadScriptsOnStartup;
      set
      {
        _loadScriptsOnStartup = value;
        LoadScriptsOnStartupChanged?.Invoke(this, null);
      }
    }
    private bool _loadScriptsOnStartup = true;
    public static event EventHandler<EventArgs> LoadScriptsOnStartupChanged;

    [XmlElement]
    public HashSet<string> ScriptLocations
    {
      get => _scriptLocations;
      set
      {
        _scriptLocations = value;
        ScriptLocationsChanged?.Invoke(this, null);
      }
    }
    private HashSet<string> _scriptLocations = new HashSet<string>();
    public static event EventHandler<EventArgs> ScriptLocationsChanged;
    #endregion

    // settings set by admin are readonly
    public static bool IsReadOnly =>
      // Push instance to initialize
      Current != null && usingAdminOptions;

    // Singleton
    private static bool usingAdminOptions = false;
    private static AddinOptions instance = null;
    private static readonly object padlock = new object();
    public static AddinOptions Current
    {
      get
      {
        lock (padlock)
        {
          if (instance is null)
          {
            var xml = new XmlSerializer(typeof(AddinOptions));

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
                  instance = (AddinOptions) xml.Deserialize(optsFile);
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
              instance = new AddinOptions();
          }
          return instance;
        }
      }
    }

    // Data store information
    private static string DataDirectoryPath => Path.Combine(Addin.AddinCompany, Addin.AddinName, "Revit", $"{Addin.Version.Major}.0");
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
      var xmlSerializer = new XmlSerializer(typeof(AddinOptions));

      // ensure directory exists
      // clear previous contents, or create empty file
      Directory.CreateDirectory(UserDataDirectory);
      File.WriteAllText(UserOptionsFilePath, "");

      // serialize options to the empty file
      using (var optsFile = File.OpenWrite(UserOptionsFilePath))
      {
        xmlSerializer.Serialize(optsFile, Current);
      }
    }
  }
}
