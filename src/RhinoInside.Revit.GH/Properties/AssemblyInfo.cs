using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("RhinoInside.Revit.GH")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Robert McNeel & Associates")]
[assembly: AssemblyProduct("Rhino.Inside")]
[assembly: AssemblyCopyright("© 2019-2021 Robert McNeel & Associates.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.System32)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("C3F12BB4-8B1D-402A-A749-DFA120C0D7B0")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("0.1.*")]
//[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("WIP")]

namespace RhinoInside.Revit.GH
{
  /// <summary>
  /// Additional information about this library in Grasshopper
  /// </summary>
  public class AssemblyInfo : GH_AssemblyInfo
  {
    public AssemblyInfo() { }

    public override System.Drawing.Bitmap Icon => (System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject("logo_24x24");
    public override string Name => "Rhino.Inside for Autodesk Revit";
    public override string Version => $"{Assembly.GetName().Version} (WIP)";
    public override string Description => "Provides components to interact with Autodesk© Revit©";

    public override string AuthorName => "Robert McNeel & Associates";
    public override string AuthorContact => WebPageURI;

    public override GH_LibraryLicense License => Addin.DaysUntilExpiration < 0 ? GH_LibraryLicense.expired : GH_LibraryLicense.opensource;

    public static readonly string ContactURI = @"https://discourse.mcneel.com/c/rhino-inside/Revit/";
    public static readonly string WebPageURI = @"https://www.rhino3d.com/inside/revit/";
  }
}
