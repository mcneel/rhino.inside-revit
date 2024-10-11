#if RHINO_8
using System;
using System.Linq;
using System.Collections.Generic;

using Rhino;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Editing;
using Rhino.Runtime.Code.Environments;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Platform;
using Rhino.Runtime.Code.Storage;

namespace RhinoInside.Revit.GH.Scripting
{
  public sealed class RhinoInsideRevitPlatform : Platform
  {
    public static IPlatform Instance { get; } = new RhinoInsideRevitPlatform();

    #region Converters
    public static IParamValueConverter ElementIdConverter { get; } = new Converters.ElementIdConverter();
    #endregion

    #region Platform
    public override PlatformIdentity Id { get; } = new PlatformIdentity(
      name: "Rhino.Inside.Revit",
      shortName: "RIR",
      description: "Rhino.Inside.Revit platform",
      domain: "rhino3dinrevit",
      taxonomy: "mcneel.rhino3dinrevit.rhino",
      RhinoApp.Version
    );

    public override IPlatformDocument ActiveDocument { get; } = default;

    public override IEnumerable<CompileReference> References
    {
      get
      {
        yield return CompileReference.FromAssembly(typeof(Autodesk.Revit.DB.IExternalDBApplication).Assembly);
        yield return CompileReference.FromAssembly(typeof(Autodesk.Revit.UI.IExternalApplication).Assembly);
        yield return CompileReference.FromAssembly(typeof(Autodesk.Windows.IRibbonPopup).Assembly);
      }
    }

    public override IEnumerable<IStorageExtensionFilter> ReferenceFilters
    {
      get
      {
        // TODO:
        // return extension filters for platfrom plugin file exts
        yield break;
      }
    }

    public override IEnumerable<EditorLibrary> EditorLibraries
    {
      get
      {
        var revitVersion = new Version(Revit.ActiveDBApplication.SubVersionNumber);
        var nugetSpec = new PackageSpec($"Autodesk.Revit.SDK.refs.{revitVersion.Major}");
        var nugetPackage = NuGetEnvirons.User.AddPackage(nugetSpec);
        var refsDir = revitVersion.Major >= 2025 ? "ref/net8.0" : "ref/net48";
        foreach (string revitSDK in nugetPackage.GetFiles(refsDir)
                                                .Where(l => l.EndsWith("RevitAPI.dll") || l.EndsWith("RevitAPIUI.dll")))
        {
          AssemblyEditorLibrary revitSDKLib = default;
          try
          {
            // TODO:
            // implement url builder for https://www.revitapidocs.com/ maybe?
            revitSDKLib = new AssemblyEditorLibrary(revitSDK);
          }
          catch (Exception ex)
          {
            RhinoCode.Logger.Warn($"Error generating docs for Autodesk.Revit {revitVersion.Major} libraries | {ex.Message}");
          }

          if (revitSDKLib is AssemblyEditorLibrary)
            yield return revitSDKLib;
        }
      }
    }

    public override IEnumerable<IParamValueConverter> Converters
    {
      get
      {
        // TODO:
        // add more converters
        yield return ElementIdConverter;
      }
    }

    public override IDebugControls CreateDebugControls()
    {
      // This is not used for now.
      // ScriptEditor uses debug controls provided by Rhino3d platform
      throw new NotImplementedException();
    }

    public override void Pause(PauseContext context)
    {
      // TODO:
      // implement a way to pause/disable Revit UI
      // this is called when editor debugger is pausing on a breakpoint
      // and wants to deactivate the platform functions to disallow
      // changing document state
    }

    public override void Resume()
    {
      // TODO:
      // unpause from paused/disabled state
    }

    public override bool TryGetAssemblyPath(string name, out string path)
    {
      // TODO:
      // not necessary at this point

      path = default;
      return false;
    }

    public override void Write(string text)
    {
      // TODO:
      // not necessary at this point
    }

    public override void WriteError(string text)
    {
      // TODO:
      // not necessary at this point
    }
    #endregion
  }
}
#endif
