# Utility Scripts

Support scripts for this project, loosely following the [Github's Script to Rule Them All](https://github.blog/2015-06-30-scripts-to-rule-them-all/) convention.

- Python scripts in this folder might have dependencies. Install `pipenv` and run `pipenv install` to get all the dependencies for these scripts. 

## `cibuild.msbuild`

CI/CD build script for Rhino.Inside.Revit

## `dbgrevit.py`

Utility to run Revit in debug mode using a custom journal file.
Provide the Revit year and any file that needs to be opened during debug.
This script opens Grasshopper automatically.
See `dbgrevit.py --help` for usage. Stores temporary artifacts under `script/.debug/`

**Example:**
```bash
# "pipenv run python" ensures the script is executed with the pipenv python
pipenv run python dbgrevit.py 2019 "C:\Views.rvt"

# can also open a grasshopper definition
pipenv run python dbgrevit.py 2019 "C:\Views.rvt" "C:\Definition.gh"
```

## `dbgpkg.py`

Utility to process debug packages (ZIP) submitted as load errors to the support ticketing system. Stores temporary artifacts under `script/.packages/`

**Example:**
```bash
# script prints the report to stdout. using pbcopy on macOS to copy results to pasteboard and then paste into github issue

# if ticket url is available and ticket has ZIP attached
pipenv run python dbgzip.py https://mcneel.supportbee.com/tickets/88888888 --token=APITOKEN | pbcopy

# OR, if zip file is downloaded separately
pipenv run python dbgzip.py ./RhinoInside-Revit-Report-20200326T104108Z.zip --ticket=https://mcneel.supportbee.com/tickets/88888888 | pbcopy

```