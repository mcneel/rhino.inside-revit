using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
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
    public bool CompactTab
    {
      get => _compactTab;
      set
      {
        _compactTab = value;
        CompactTabChanged?.Invoke(this, null);
      }
    }
    private bool _compactTab = false;
    public static event EventHandler<EventArgs> CompactTabChanged;

    [XmlElement]
    public bool CompactRibbon
    {
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
    public bool LoadUserScriptPackages
    {
      get => _loadUserScripts;
      set
      {
        _loadUserScripts = value;
        LoadScriptsOnStartupChanged?.Invoke(this, null);
      }
    }
    private bool _loadUserScripts = true;
    public static event EventHandler<EventArgs> LoadScriptsOnStartupChanged;

    [XmlElement]
    public bool LoadInstalledScriptPackages
    {
      get => _loadInstalledScripts;
      set
      {
        _loadInstalledScripts = value;
        LoadScriptPackagesOnStartupChanged?.Invoke(this, null);
      }
    }
    private bool _loadInstalledScripts = true;
    public static event EventHandler<EventArgs> LoadScriptPackagesOnStartupChanged;

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

    [XmlElement]
    public AddinCustomOptions CustomOptions = new AddinCustomOptions();

    // settings set by admin are readonly
    public static bool IsReadOnly =>
      // Push instance to initialize
      Current != null && usingAdminOptions;

    // de/serializer
    private static XmlSerializer _xml = new XmlSerializer(typeof(AddinOptions), new Type[] { typeof(AddinCustomOptions) });

    // Singleton
    private static bool usingAdminOptions = false;
    private static AddinOptions _sessionInstance = null;  // reflects changes at load time
    private static AddinOptions _currentInstance = null;  // reflects latest changes
    private static readonly object padlock = new object();

    public static AddinOptions Session
    {
      get
      {
        if(_sessionInstance is null)
          _sessionInstance = (AddinOptions)Current.MemberwiseClone();
        return _sessionInstance;
      }
    }

    public static AddinOptions Current
    {
      get
      {
        lock (padlock)
        {
          if (_currentInstance is null)
          {
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
                  _currentInstance = (AddinOptions) _xml.Deserialize(optsFile);
              }
              catch (Exception ex) {
                // TODO: log errors
              }
            }

            // otherwise use default
            if (_currentInstance is null)
              _currentInstance = new AddinOptions();
          }

          return _currentInstance;
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
      // ensure directory exists
      // clear previous contents, or create empty file
      Directory.CreateDirectory(UserDataDirectory);
      File.WriteAllText(UserOptionsFilePath, "");

      // serialize options to the empty file
      using (var optsFile = File.OpenWrite(UserOptionsFilePath))
      {
        _xml.Serialize(optsFile, Current);
      }
    }
  }

  [Serializable]
  public class AddinCustomOptions : IXmlSerializable
  {
    private Dictionary<string, Dictionary<string, string>> optStore =
      new Dictionary<string, Dictionary<string, string>>();

    public void AddOption(string root, string key, string value)
    {
      if (optStore.TryGetValue(root, out var customOpts))
        customOpts[key] = value;
      else
        optStore[root] = new Dictionary<string, string> {
            { key, value }
        };
    }

    public string GetOption(string root, string key)
    {
      if (optStore.TryGetValue(root, out var customOpts))
        if (customOpts.TryGetValue(key, out var value))
          return value;
      return null;
    }

    public void WriteXml(XmlWriter writer)
    {
      foreach (var root in optStore)
      {
        writer.WriteStartElement(root.Key);
        foreach (var option in optStore[root.Key])
          writer.WriteElementString(option.Key, option.Value);
        writer.WriteEndElement();
      }
    }

    public void ReadXml(XmlReader reader)
    {
      while (reader.Read())
      {
        if (reader.NodeType == XmlNodeType.EndElement)
        {
          reader.Read();
          break;
        }

        var opts = new Dictionary<string, string>();
        optStore[reader.Name] = opts;

        if (reader.IsStartElement())
        {
          while (reader.Read())
          {
            if (reader.NodeType == XmlNodeType.EndElement)
              break;
            opts[reader.Name] = reader.ReadString();
          }
        }
      }
    }

    public XmlSchema GetSchema() => null;
  }
}
