using System.Reflection;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Rhino.Inside for Autodesk Revit")]
[assembly: AssemblyDescription("Provides components to interact with Autodesk© Revit©")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.System32)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("C3F12BB4-8B1D-402A-A749-DFA120C0D7B0")]

namespace RhinoInside.Revit.GH
{
  /// <summary>
  /// Additional information about this library in Grasshopper
  /// </summary>
  public class AssemblyInfo : GH_AssemblyInfo
  {
    public AssemblyInfo() { }

    public override System.Drawing.Bitmap Icon => (System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject("logo_24x24");
    public override string Name => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title;
    public override string Version => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    public override string Description => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description;

    public override string AuthorName => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>().Company;
    public override string AuthorContact => WebPageURI;

    public override GH_LibraryLicense License => AddIn.DaysUntilExpiration < 0 ? GH_LibraryLicense.expired : GH_LibraryLicense.opensource;

    public static readonly string ContactURI = @"https://discourse.mcneel.com/c/rhino-inside/Revit/";
    public static readonly string WebPageURI = @"https://www.rhino3d.com/inside/revit/";
  }
}
