using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RhinoInside.Revit.Settings
{
  // Easy static access to addin options
  [Serializable]
  [XmlRoot("Options")]
  public class AddInOptions
  {
    #region Rhino
    [XmlElement]
    public bool LoadOnStartup
    {
      get => _loadOnStartup;
      set
      {
        if (_loadOnStartup == value) return;
        _loadOnStartup = value;
        LoadOnStartupChanged?.Invoke(this, null);
      }
    }
    private bool _loadOnStartup = false;
    public static event EventHandler<EventArgs> LoadOnStartupChanged;

    [XmlElement]
    public bool IsolateSettings
    {
      get => _isolateSettings;
      set
      {
        if (_isolateSettings == value) return;
        _isolateSettings = value;
        IsolateSettingsChanged?.Invoke(this, null);
      }
    }
    private bool _isolateSettings = false;
    public static event EventHandler<EventArgs> IsolateSettingsChanged;

    [XmlElement]
    public bool UseHostLanguage
    {
      get => _useHostLanguage;
      set
      {
        if (_useHostLanguage == value) return;
        _useHostLanguage = value;
        UseHostLanguageChanged?.Invoke(this, null);
      }
    }
    private bool _useHostLanguage = true;
    public static event EventHandler<EventArgs> UseHostLanguageChanged;

    [XmlElement]
    public bool KeepUIOnTop
    {
      get => _keepUIOnTop;
      set
      {
        if (_keepUIOnTop == value) return;
        _keepUIOnTop = value;
        KeepUIOnTopChanged?.Invoke(this, null);
      }
    }
    private bool _keepUIOnTop = true;
    public static event EventHandler<EventArgs> KeepUIOnTopChanged;
    #endregion

    #region UI
    [XmlElement]
    public bool CompactTab
    {
      get => _compactTab;
      set
      {
        if (_compactTab == value) return;
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
        if (_compactRibbon == value) return;
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
        if (_checkForUpdatesOnStartup == value) return;
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
        if (_updateChannel == value) return;
        _updateChannel = value;
        UpdateChannelChanged?.Invoke(this, null);
      }
    }
    private string _updateChannel = AddInUpdater.DefaultChannel.Id.ToString();
    public static event EventHandler<EventArgs> UpdateChannelChanged;
    #endregion

    #region Scripts
    [XmlElement]
    public bool LoadUserScriptPackages
    {
      get => _loadUserScripts;
      set
      {
        if (_loadUserScripts == value) return;
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
        if (_loadInstalledScripts == value) return;
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
        if (_scriptLocations == value) return;
        _scriptLocations = value;
        ScriptLocationsChanged?.Invoke(this, EventArgs.Empty);
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
    private static XmlSerializer _xml = new XmlSerializer(typeof(AddInOptions), new Type[] { typeof(AddinCustomOptions) });

    // Singleton
    private static bool usingAdminOptions = false;
    private static AddInOptions _sessionInstance = null;  // reflects changes at load time
    private static AddInOptions _currentInstance = null;  // reflects latest changes
    private static readonly object padlock = new object();

    public AddInOptions Clone()
    {
      var clone = (AddInOptions) MemberwiseClone();
      clone.CustomOptions = CustomOptions.Clone();
      return clone;
    }

    public static AddInOptions Session
    {
      get
      {
        if (_sessionInstance is null)
          _sessionInstance = Current.Clone();
        return _sessionInstance;
      }
    }

    public static AddInOptions Current
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
                  _currentInstance = (AddInOptions) _xml.Deserialize(optsFile);
              }
              catch (Exception)
              {
                // TODO: log errors
              }
            }

            // otherwise use default
            if (_currentInstance is null)
              _currentInstance = new AddInOptions();
          }

          return _currentInstance;
        }
      }
    }

    // Data store information
    private static string DataDirectoryPath => Path.Combine(AddIn.AddInCompany, AddIn.AddInName, "Revit", $"{AddIn.Version.Major}.0");
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

    public AddinCustomOptions() { }

    // copy constructor
    private AddinCustomOptions(AddinCustomOptions source)
      => optStore = new Dictionary<string, Dictionary<string, string>>(source.optStore);

    public AddinCustomOptions Clone() => new AddinCustomOptions(this);

    public string Get(string root, string key)
    {
      if (optStore.TryGetValue(root, out var customOpts))
        if (customOpts.TryGetValue(key, out var value))
          return value;

      return null;
    }

    public void Set(string root, string key, string value)
    {
      if (value is null) Remove(root, key);
      else
      {
        if (optStore.TryGetValue(root, out var customOpts))
          customOpts[key] = value;
        else
          optStore[root] = new Dictionary<string, string> { { key, value } };
      }
    }

    public bool Remove(string root, string key)
    {
      if (optStore.TryGetValue(root, out var customOpts))
        return customOpts.Remove(key);

      return false;
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
      if (reader.IsEmptyElement)
      {
        reader.Read();
        return;
      }

      while (reader.Read())
      {
        if (reader.NodeType == XmlNodeType.EndElement)
        {
          reader.Read();
          break;
        }

        var opts = new Dictionary<string, string>();
        optStore[reader.Name.Trim()] = opts;

        if (reader.IsStartElement())
        {
          while (reader.Read())
          {
            if (reader.NodeType == XmlNodeType.EndElement)
              break;
            opts[reader.Name.Trim()] = reader.ReadString().Trim();
          }
        }
      }
    }

    public XmlSchema GetSchema() => null;
  }
}
