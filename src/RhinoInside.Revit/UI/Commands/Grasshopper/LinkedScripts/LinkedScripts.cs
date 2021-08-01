using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

using Autodesk.Revit.UI;
using ADW = Autodesk.Windows;
using ADIW = Autodesk.Internal.Windows;
using RhinoInside.Revit.Settings;

namespace RhinoInside.Revit.UI
{
  static class LinkedScripts
  {
    public static void CreateUI(RibbonHandler ribbon)
    {
      // Setup listeners, and in either case, update the packages ui
      // listed for changes in installed packages
      Rhino.Commands.Command.EndCommand += PackageManagerCommand_EndCommand;
      // listen for changes to user-script paths in options
      AddinOptions.ScriptLocationsChanged += AddinOptions_ScriptLocationsChanged;

      UpdateScriptPkgUI(ribbon);
    }

    private static async void AddinOptions_ScriptLocationsChanged(object sender, EventArgs e)
    {
      // wait for Revit to be ready and get uiApp
      var uiApp = await External.ActivationGate.Yield();
      UpdateScriptPkgUI(new RibbonHandler(uiApp));
    }

    private static async void PackageManagerCommand_EndCommand(object sender, Rhino.Commands.CommandEventArgs e)
    {
      if (e.CommandEnglishName == "PackageManager")
      {
        // wait for Revit to be ready and get uiApp
        var uiApp = await External.ActivationGate.Yield();
        UpdateScriptPkgUI(new RibbonHandler(uiApp));
      }
    }

    public static bool CreateUI(ScriptPkg pkg, RibbonHandler ribbon)
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
      RibbonPanel panel;
      try { panel = ribbon.CreateAddinPanel(pkg.Name); }
      catch { return false; }

      // Currently only supporting two levels in the UI:
      // 1) Pushbuttons on panel for every LinkedScript at the root level
      // 2) Pulldowns containing pushbuttons for all the LinkedScripts recursively found under their directory
      // Lets make the pulldowns first so they are first on the panel
      items.OfType<LinkedItemGroup>().ToList().ForEach((group) =>
      {
        var pullDownData = new PulldownButtonData(group.Name, group.Text)
        {
          Image = ImageBuilder.LoadRibbonButtonImage("Ribbon.Grasshopper.GhFolder.png", true),
          LargeImage = ImageBuilder.LoadRibbonButtonImage("Ribbon.Grasshopper.GhFolder.png"),
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
      items.OfType<LinkedScript>().ToList().ForEach((script) =>
      {
        AddPanelButton(panel, script, lsa);
      });

      return true;
    }

    public static bool HasUI(ScriptPkg pkg, RibbonHandler ribbon)
    {
      return ribbon.HasAddinPanel(pkg.Name);
    }

    public static bool RemoveUI(ScriptPkg pkg, RibbonHandler ribbon)
    {
      return ribbon.RemoveAddinPanel(pkg.Name);
    }

    private static HashSet<ScriptPkg> _lastState = new HashSet<ScriptPkg>();

    internal static void UpdateScriptPkgUI(RibbonHandler ribbon)
    {
      // determine which packages need to be loaded
      var curState = new HashSet<ScriptPkg>();
      if (AddinOptions.Current.LoadInstalledScriptPackages)
        curState.UnionWith(CommandGrasshopperPackageManager.GetInstalledScriptPackages());

      if (AddinOptions.Current.LoadUserScriptPackages)
        curState.UnionWith(ScriptPkg.GetUserScriptPackages());

      // create a combined set of both last and current states to iterate over
      var pkgs = new HashSet<ScriptPkg>(curState);
      pkgs.UnionWith(_lastState);

      // update the package ui
      foreach (var pkg in pkgs)
      {
        // skip existing packages
        if (curState.Contains(pkg) && _lastState.Contains(pkg))
          continue;

        // create new packages
        else if (curState.Contains(pkg) && !_lastState.Contains(pkg))
        {
          if (LinkedScripts.HasUI(pkg, ribbon))
            TaskDialog.Show(
              AddIn.AddInName,
              $"Package \"{pkg.Name}\" has been previously loaded in to the Revit UI." +
              "Restart Revit for changes to take effect."
              );
          else
            LinkedScripts.CreateUI(pkg, ribbon);
        }

        // or remove, removed packages
        else if (!curState.Contains(pkg) && _lastState.Contains(pkg))
        {
          if (LinkedScripts.HasUI(pkg, ribbon))
            LinkedScripts.RemoveUI(pkg, ribbon);
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
      BitmapSource commandIcon = GetScriptIcon(script, true);
      BitmapSource commandIconLarge = GetScriptIcon(script);
      
      return new PushButtonData(commandName, commandButtonName, typeAssmLocation, typeName)
      {
        Image = commandIcon,
        LargeImage = commandIconLarge,
        ToolTip = script.Description != null && !string.IsNullOrEmpty(script.Description) ? script.Description : "Launch script in Grasshopper player",
        LongDescription = $"Script Path: {script.ScriptPath}",
      };
    }

    internal static BitmapSource GetScriptIcon(LinkedScript script, bool small = false)
    {
      int desiredSize = small ? 16 : 32;
      if (script.GetScriptIcon(desiredSize, desiredSize) is BitmapSource icon)
        return icon;
      else
        return ImageBuilder.LoadRibbonButtonImage($"Ribbon.Grasshopper.{script.ScriptType}.png", small);
    }
  }
}
