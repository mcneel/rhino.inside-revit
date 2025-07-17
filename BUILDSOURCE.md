# Build Rhino.Inside Revit from source
> Updated: 14 June 2025

These instructions will get you a copy of the project up and running on your
local machine for development and testing purposes.

## Prerequisites

* Git
  [📥](https://git-scm.com/downloads)
* Visual Studio 2022 (17.0 or above) - **Updated requirement**
  [📥](https://visualstudio.microsoft.com/downloads/)
* .NET Framework Developer Pack (4.8.1) and .NET 8 SDK
  [📥](https://www.microsoft.com/net/download/visual-studio-sdks)
* Rhino 7, 8, or 9 (multiple versions supported)
  [📥](https://www.rhino3d.com/download/rhino/)
* Autodesk Revit 2018-2026 (any supported version)
  [📥](https://www.autodesk.com/products/revit/free-trial)
* Add this link to your bookmarks
  ([API docs](https://apidocs.co/))

## Architecture Overview

RhinoInside.Revit uses a sophisticated **two-stage loader architecture** that supports multiple Rhino versions:

### Core Components
- **`RhinoInside.Revit.Loader`** - Bootstrap entry point (registered with Revit)
- **`RhinoInside.Revit.AddIn`** - Main application logic (version-specific)
- **`RhinoInside.Revit`** - Core functionality and Rhino integration
- **`RhinoInside.Revit.External`** - Shared external APIs
- **`RhinoInside.Revit.GH`** - Grasshopper integration (.gha plugin)
- **`RhinoInside.Revit.Native`** - Native C++ components

### Loading Process
1. **Stage 1**: Revit loads `RhinoInside.Revit.Loader.dll` via the `.addin` manifest
2. **Stage 2**: Loader detects available Rhino versions and dynamically loads the appropriate `RhinoInside.Revit.AddIn.dll` from version-specific subdirectories (`R7/`, `R8/`, `R9/`)

## Getting Source & Build

1. Clone the repository. At the command prompt, enter the following command:

    ```console
    git clone --recursive https://github.com/mcneel/rhino.inside-revit.git
    ```

2. In Visual Studio, open `rhino.inside-revit\src\RhinoInside.Revit.sln`.

3. **Configure Build Target**: Set the _Solution Configuration_ drop-down to match your installed versions:
   - **Configuration**: `Debug-R{rhino_version}` (e.g., `Debug-R8`)
   - **Platform**: `{revit_version}` (e.g., `2024`)

   **Example combinations**:
   - `Debug-R8|2024` - Rhino 8 with Revit 2024
   - `Debug-R7|2023` - Rhino 7 with Revit 2023
   - `Release-R8|2025` - Rhino 8 with Revit 2025

4. Navigate to _Build_ > _Build Solution_ to begin your build.

## Build Output & Deployment

### Debug Build Deployment
During debug builds, the system automatically deploys files to:

```
%APPDATA%\Autodesk\Revit\Addins\<revit_version>\
├── RhinoInside.Revit.addin                    # Revit add-in manifest
└── RhinoInside.Revit\                         # Main deployment folder
    ├── RhinoInside.Revit.Loader.dll           # Bootstrap loader
    ├── RhinoInside.Revit.Loader.pdb
    └── R{rhino_version}\                       # Version-specific folder
        ├── RhinoInside.Revit.AddIn.dll        # Main application
        ├── RhinoInside.Revit.dll              # Core functionality
        ├── RhinoInside.Revit.External.dll     # External APIs
        ├── RhinoInside.Revit.GH.gha           # Grasshopper plugin
        ├── RhinoInside.Revit.Native.dll       # Native components
        ├── Lokad.ILPack.dll                   # Assembly packing
        └── opennurbs_private.manifest         # OpenNURBS manifest
```

### Multi-Version Support
The build system supports multiple Rhino versions simultaneously:
- Each Rhino version gets its own subfolder (`R7/`, `R8/`, `R9/`)
- The loader automatically detects and selects the appropriate version at runtime
- Multiple configurations can coexist without conflicts

## Installing & Uninstalling

### Automatic Installation (Debug Builds)
Debug builds automatically deploy to the Revit add-ins folder. The add-in will be available the next time you start Revit.

### Manual Installation
For manual deployment or release builds:
1. Copy the `.addin` file to: `%APPDATA%\Autodesk\Revit\Addins\<revit_version>\`
2. Copy the `RhinoInside.Revit\` folder and contents to the same location

### Uninstalling
**Option 1 - Visual Studio Clean:**
Use Visual Studio _Build_ > _Clean Solution_ command

**Option 2 - Manual Removal:**
Navigate to `%APPDATA%\Autodesk\Revit\Addins\<revit_version>` and remove:
- `RhinoInside.Revit.addin` file
- `RhinoInside.Revit\` folder

**Option 3 - Disable Without Removing:**
Rename the folder from `RhinoInside.Revit` to `RhinoInside.Revit.disabled`

## Troubleshooting

### Common Issues
- **Add-in not loading**: Check that both the `.addin` file and `RhinoInside.Revit\` folder exist
- **Version conflicts**: Ensure you're building for the correct Rhino/Revit version combination
- **Missing dependencies**: Verify all required Rhino and Revit versions are installed

### Build Configurations
- **Framework Targets**:
  - Revit 2017-2024: .NET Framework 4.8.1
  - Revit 2025+: .NET 8.0-windows
- **Supported Combinations**: Any Rhino version (7-9) with any Revit version (2018-2026)
