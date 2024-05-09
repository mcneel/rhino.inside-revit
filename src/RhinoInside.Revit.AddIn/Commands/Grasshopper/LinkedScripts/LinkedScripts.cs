using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  static class LinkedScripts
  {
    public static void CreateUI(External.UI.UIHostApplication app)
    {
      // Setup listeners, and in either case, update the packages ui
      // listed for changes in installed packages
      Rhino.Commands.Command.EndCommand += PackageManagerCommand_EndCommand;
      // listen for changes to user-script paths in options
      Properties.AddInOptions.ScriptLocationsChanged += AddInOptions_ScriptLocationsChanged;

      UpdateScriptPkgUI(app);
    }

    private static async void AddInOptions_ScriptLocationsChanged(object sender, EventArgs e)
    {
      // wait for Revit to be ready and get uiApp
      var uiApp = await External.ActivationGate.Yield();
      UpdateScriptPkgUI(uiApp);
    }

    private static async void PackageManagerCommand_EndCommand(object sender, Rhino.Commands.CommandEventArgs e)
    {
      if (e.CommandEnglishName == "PackageManager")
      {
        // wait for Revit to be ready and get uiApp
        var uiApp = await External.ActivationGate.Yield();
        UpdateScriptPkgUI(uiApp);
      }
    }

    public static bool CreateUI(ScriptPkg pkg, External.UI.UIHostApplication app)
    {
      // --------------------------------------------------------------------
      // FIND SCRIPTS
      // --------------------------------------------------------------------
      var items = pkg.FindLinkedItems();

      // --------------------------------------------------------------------
      // CREATE ASSEMBLY
      // --------------------------------------------------------------------
      // generate assembly containing script command types
      var lsa = new LinkedScriptAssembly();

      // create types for all the scripts in the structure
      ProcessLinkedScripts(items, (script) =>
      {
        script.ScriptCommandType = lsa.MakeScriptCommandType(script);
      });

      // save and load the created assembly
      lsa.SaveAndLoad();

      // --------------------------------------------------------------------
      // CREATE UI
      // --------------------------------------------------------------------
      try
      {
        var panel = app.CreateRibbonPanel(Command.TabName, pkg.Name);

        // Currently only supporting two levels in the UI:
        // 1) Pushbuttons on panel for every LinkedScript at the root level
        // 2) Pulldowns containing pushbuttons for all the LinkedScripts recursively found under their directory
        // Lets make the pulldowns first so they are first on the panel
        items.OfType<LinkedItemGroup>().ToList().ForEach((group) =>
        {
          var pullDownData = new PulldownButtonData(group.Name, group.Text)
          {
            Image = Command.LoadRibbonButtonImage("Ribbon.Grasshopper.GhFolder.png", true),
            LargeImage = Command.LoadRibbonButtonImage("Ribbon.Grasshopper.GhFolder.png"),
            ToolTip = group.Tooltip,
          };
          if (panel.AddItem(pullDownData) is PulldownButton pulldown)
          {
            ProcessLinkedScripts(group.Items, (script) =>
            {
              AddPullDownButton(pulldown, script, lsa);
            });
          }
        });

        // now make pushbuttons
        foreach(var script in items.OfType<LinkedScript>())
          AddPanelButton(panel, script, lsa);

      }
      catch { return false; }

      return true;
    }

    private static HashSet<ScriptPkg> _lastState = new HashSet<ScriptPkg>();

    public static List<ScriptPkg> GetInstalledScriptPackages()
    {
      var pkgs = new List<ScriptPkg>();
      var pkgLocations = new List<DirectoryInfo>();
      {
        pkgLocations.AddRange(Rhino.Runtime.HostUtils.GetActivePlugInVersionFolders(false));
        pkgLocations.AddRange(Rhino.Runtime.HostUtils.GetActivePlugInVersionFolders(true));
      }

      foreach (var dirInfo in pkgLocations)
      {
        if (GetInstalledScriptPackage(dirInfo.FullName) is ScriptPkg pkg)
          pkgs.Add(pkg);
      }

      return pkgs;
    }

    /// <summary>
    /// Get script package on given location if exists
    /// </summary>
    public static ScriptPkg GetInstalledScriptPackage(string location)
    {
      // grab the name from the package directory
      var pkgName = Path.GetFileName(Path.GetDirectoryName(location));

      // Looks for Rhino.Inside/Revit/ or Rhino.Inside/Revit/x.x insdie the package
      var pkgAddinContents = Path.Combine(location, Core.Product, Core.Platform);
      var pkgAddinSpecificContents = Path.Combine(pkgAddinContents, $"{Core.Version.Major}.0");

      // load specific scripts if available, otherwise load for any Rhino.Inside.Revit
      if
      (
        new string[] { pkgAddinSpecificContents, pkgAddinContents }.
        FirstOrDefault(d => Directory.Exists(d)) is string pkgContentsDir
      )
      {
        return new ScriptPkg { Name = pkgName, Location = pkgContentsDir };
      }

      return null;
    }

    internal static void UpdateScriptPkgUI(External.UI.UIHostApplication app)
    {
      // determine which packages need to be loaded
      var curState = new HashSet<ScriptPkg>();
      if (Properties.AddInOptions.Current.LoadInstalledScriptPackages)
        curState.UnionWith(GetInstalledScriptPackages());

      if (Properties.AddInOptions.Current.LoadUserScriptPackages)
        curState.UnionWith(ScriptPkg.GetUserScriptPackages());

      // create a combined set of both last and current states to iterate over
      var pkgs = new HashSet<ScriptPkg>(curState);
      pkgs.UnionWith(_lastState);

      // update the package ui
      var panels = app.GetRibbonPanels(Command.TabName);
      foreach (var pkg in pkgs)
      {
        // skip existing packages
        if (curState.Contains(pkg) && _lastState.Contains(pkg))
          continue;

        // create new packages
        else if (curState.Contains(pkg) && !_lastState.Contains(pkg))
        {
          if (panels.ContainsKey(pkg.Name))
          {
            TaskDialog.Show
            (
              title: $"{Core.Product}.{Core.Platform}",
              $"Package \"{pkg.Name}\" has been previously loaded in to the Revit UI." +
              "Restart Revit for changes to take effect."
            );
          }
          else CreateUI(pkg, app);
        }

        // or remove, removed packages
        else if (!curState.Contains(pkg) && _lastState.Contains(pkg))
        {
          if (panels.TryGetValue(pkg.Name, out var panel))
            panel.Enabled = false;
        }
      }

      _lastState = curState;
    }

    internal static void ProcessLinkedScripts(List<LinkedItem> items, Action<LinkedScript> action)
    {
      items.ForEach((item) =>
      {
        switch (item)
        {
          case LinkedItemGroup group: ProcessLinkedScripts(group.Items, action); break;
          case LinkedScript script: action(script); break;
        }
      });
    }

    internal static void AddPullDownButton(PulldownButton pulldown, LinkedScript script, LinkedScriptAssembly lsa)
    {
      if (pulldown.AddPushButton(NewScriptButton(script, lsa.FilePath)) is PushButton pushButton)
      {
        // do stuff with button?
      }
    }

    internal static void AddPanelButton(RibbonPanel panel, LinkedScript script, LinkedScriptAssembly lsa)
    {
      if (panel.AddItem(NewScriptButton(script, lsa.FilePath)) is PushButton pushButton)
      {
        // do stuff with button?
      }
    }

    internal static PushButtonData NewScriptButton(LinkedScript script, string assmLoc)
    {
      // execution
      var typeAssmLocation = assmLoc;
      var typeName = script.ScriptCommandType.FullName;

      // ui
      var commandName = $"{script.ScriptCommandType.Name}-{script.Name}";
      var commandButtonName = script.Text;
      
      return new PushButtonData(commandName, commandButtonName, typeAssmLocation, typeName)
      {
        Image = GetScriptIcon(script, small: true),
        LargeImage = GetScriptIcon(script, small: false),
        ToolTip = script.Description ?? "Launch script in Grasshopper player",
        LongDescription = $"Script Path: {script.ScriptPath}",
      };
    }

    internal static ImageSource GetScriptIcon(LinkedScript script, bool small = false)
    {
      return script.GetScriptIcon(small) ??
        Command.LoadRibbonButtonImage($"Ribbon.Grasshopper.{script.ScriptType}.png", small);
    }
  }
}
