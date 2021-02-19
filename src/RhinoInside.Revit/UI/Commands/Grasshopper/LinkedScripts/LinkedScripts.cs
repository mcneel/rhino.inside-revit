using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Forms.Interop;
using System.Reflection;
using System.Reflection.Emit;
using Autodesk.Revit.Attributes;
using DB = Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.PlugIns;
using RhinoInside.Revit.Settings;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  abstract class LinkedScripts
  {
    public static void CreateUI(ScriptPkg pkg, Func<string, string, RibbonPanel> panelMaker)
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
      var panel = panelMaker(Addin.AddinName, pkg.Name);

      // Currently only supporting two levels in the UI:
      // 1) Pushbuttons on panel for every LinkedScript at the root level
      // 2) Pulldowns containing pushbuttons for all the LinkedScripts recursively found under their directory
      // Lets make the pulldowns first so they are first on the panel
      items.OfType<LinkedItemGroup>().ToList().ForEach((group) =>
      {
        var pullDownData = new PulldownButtonData(group.Name, group.Name)
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
    }

    public static void RemoveUI(ScriptPkg pkg)
    {
      // TODO: 
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
      var commandName = script.ScriptCommandType.Name + (script.Name ?? "");
      var commandButtonName = script.Name.Replace("-", "\n");
      var typeAssmLocation = assmLoc;
      var typeName = script.ScriptCommandType.FullName;
      return new PushButtonData(commandName, commandButtonName, typeAssmLocation, typeName)
      {
        Image = ImageBuilder.LoadRibbonButtonImage($"Ribbon.Grasshopper.{script.ScriptType}.png", true),
        LargeImage = ImageBuilder.LoadRibbonButtonImage($"Ribbon.Grasshopper.{script.ScriptType}.png"),
        ToolTip = "Launch script in Grasshopper player",
        LongDescription = $"Script Path: {script.ScriptPath}",
      };
    }
  }
}
