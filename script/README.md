# Utility Scripts

Python scripts in this folder might have dependencies. Install `pipenv` and run `pipenv install` to get all the dependencies for these scripts.

## `script/cibuild.msbuild`

CI/CD build script for Rhino.Inside.Revit

## `script/dbgrevit.py`

Utility to run Revit in debug mode using a custom journal file.
Provide the Revit year and any file that needs to be opened during debug.
This script opens Grasshopper automatically.
See `dbgrevit.py --help` for usage. Stores temporary artifacts under `script/.debug/`

**Example:**
```powershell
# "pipenv run python" ensures the script is executed with the pipenv python
pipenv run python dbgrevit.py 2019 "C:\Views.rvt"
```