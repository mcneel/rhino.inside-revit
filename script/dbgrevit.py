# pylint: disable=broad-except,invalid-name
"""Open Rhino.Inside.Revit inside Revit for debugging

Usage:
    {} <revit_year> [<model_path>] [<ghdoc_path>] [--lang=<lang_code>] [--rps] [--dryrun]

Options:
    -h, --help          Show this help
    --rps               Add RevitPythonShell addon
    --dryrun            Create runtime env but do not start Revit
    <model_path>        Revit model to be opened
    <ghdoc_path>        Grasshopper document to be opened
    <lang_code>         Language code to Open Revit with
"""
import sys
import os
import os.path as op
import re
import subprocess

# pipenv dependencies
from docopt import docopt
import rjm

# cli info
__binname__ = op.splitext(op.basename(__file__))[0]  # grab script name
__version__ = "1.0"

# cli configs =================================================================
DEFAULT_JRN_NAME = "debug.txt"
DEFAULT_JRN_ARTIFACT_PATTERN = r"^journal\.\d{4}"
DEFAULT_ADDIN_MANIFEST_PATTERN = r".+\.addin$"
DEFAULT_CACHE_DIR = ".debug"
DEFAULT_REVIT_BIN_PATH = r"%PROGRAMFILES%\Autodesk\Revit {year}\Revit.exe"
DEFAULT_ADDON_MANIFEST = r"""<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>{addon}</Name>
    <Assembly>{dll}</Assembly>
    <FullClassName>{entry}</FullClassName>
    <AddInId>{uuid}</AddInId>
    <VendorId>{vendor}</VendorId>
  </AddIn>
</RevitAddIns>
"""

RIR_ADDON_INFO = {
    "addon": "Rhino.Inside",
    "dll": r"%APPDATA%\Autodesk\Revit\Addins\{year}\RhinoInside.Revit\RhinoInside.Revit.dll",  # pylint: disable=line-too-long
    "entry": "RhinoInside.Revit.Addin",
    "uuid": "02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74",
    "vendor": "com.mcneel",
}

RPS_ADDON_INFO = {
    "addon": "RevitPythonShell",
    "dll": r"%PROGRAMFILES(X86)%\RevitPythonShell{year}\RevitPythonShell.dll",
    "entry": "RevitPythonShell.RevitPythonShellApplication",
    "uuid": "3a7a1d24-51ed-462b-949f-1ddcca12008d",
    "vendor": "RIPS",
}
# =============================================================================


class CLIArgs:
    """Data type to hold command line args"""

    def __init__(self, args):
        self.revit_year = args["<revit_year>"]
        self.model_path = args["<model_path>"]
        self.ghdoc_path = args["<ghdoc_path>"]
        self.add_rps = args["--rps"]
        self.start_revit = not args["--dryrun"]
        self.lang_code = args["--lang"]


def ensure_cache_dir():
    """Ensure debug cache directory exists"""
    pwd = op.dirname(__file__)
    cache_dir = op.join(pwd, DEFAULT_CACHE_DIR)
    if not op.isdir(cache_dir):
        os.mkdir(cache_dir)
    return cache_dir


def clean_cache(cache_dir):
    """Clear cache files"""
    # find journal files and delete
    for entry in os.listdir(cache_dir):
        entry_path = op.join(cache_dir, entry)
        if op.isfile(entry_path) and (
            re.match(DEFAULT_JRN_ARTIFACT_PATTERN, entry)
            or re.match(DEFAULT_ADDIN_MANIFEST_PATTERN, entry)
        ):
            try:
                os.remove(entry_path)
            except Exception as del_ex:
                print("Error removing {} | {}".format(entry_path, str(del_ex)))


def prepare_cache():
    """Make sure cache dir exists and is clean"""
    # make sure cache dir exists
    cache_dir = ensure_cache_dir()
    # cleanup
    clean_cache(cache_dir)
    return cache_dir


def create_rir_journal(
    journal_dir, model_path="", ghdoc_path="", journal_name=DEFAULT_JRN_NAME
):
    """Create a new Revit journal to lauch Revit and open Rhino.Inside.Revit

    Args:
        journal_dir (str): directory path to create the journal file inside
        model_path (str, optional): request to open this model in journal
        ghdoc_path (str, optional): request to open this gh document
        journal_name (str, optional): name of the journal file
    """
    # start a clean journal
    # note on `take_default_action=False`
    # activating PerformAutomaticActionInErrorDialog causes an issue when
    # closing a Revit in journal-mode with a modified open document,
    # will cause Revit to automatically hit Yes to the "Do you want to save?"
    # pop up and shows the Save file pop up. Thus, user can not close Revit
    # unless they save the file which could take time depending on the size
    jm = rjm.JournalMaker(permissive=True, take_default_action=False)

    # open model
    if model_path:
        if op.isfile(model_path):
            jm.open_model(model_path)
        else:
            raise Exception("Revit model does not exist: {}".format(model_path))

    # ask to open Rhinoceros tab
    jm.execute_command(
        tab_name="Add-Ins",
        panel_name="Rhinoceros",
        command_module="RhinoInside.Revit.UI",
        command_class="CommandRhinoInside",
    )

    # make sure ghd ocument exists
    cmd_data = {}
    if ghdoc_path:
        if op.isfile(ghdoc_path):
            cmd_data["Open"] = ghdoc_path
        else:
            raise Exception("GH document does not exist: {}".format(ghdoc_path))

    # ask to open Grasshopper
    jm.execute_command(
        tab_name="Rhinoceros",
        panel_name="Grasshopper",
        command_module="RhinoInside.Revit.UI",
        command_class="CommandGrasshopper",
        command_data=cmd_data,
    )

    # write journal to file
    journal_filepath = op.join(journal_dir, journal_name)
    jm.write_journal(journal_filepath)
    return journal_filepath


def write_manifest(revit_year, addon_info, addons_dir):
    """Write the addin manifest file for given revit version and addon

    Args:
        revit_year (str): revit version number
        addon_info (dict): addin info dictionary
        addons_dir (str): target manifest file directory
    """
    dll_path = op.expandvars(addon_info["dll"].format(year=revit_year))
    if op.isfile(dll_path):
        manifest_file_contents = DEFAULT_ADDON_MANIFEST.format(
            addon=addon_info["addon"],
            dll=dll_path,
            entry=addon_info["entry"],
            uuid=addon_info["uuid"],
            vendor=addon_info["vendor"],
        )
        manifest_file_path = op.join(addons_dir, addon_info["addon"] + ".addin")
        with open(manifest_file_path, "w") as mf:
            mf.write(manifest_file_contents)
    else:
        raise Exception(
            "Can not find {} dll at {}".format(addon_info["addon"], dll_path)
        )


def add_addons(revit_year, addons_dir, add_rps=True):
    """Add Revit addon manifest files to be loaded at runtime"""
    write_manifest(revit_year, RIR_ADDON_INFO, addons_dir)
    # add optional revitpythonshell
    if add_rps:
        write_manifest(revit_year, RPS_ADDON_INFO, addons_dir)


def find_revit_binary(revit_year):
    """Find executable binary for given revit version"""
    bin_path = op.expandvars(DEFAULT_REVIT_BIN_PATH.format(year=revit_year))
    if op.isfile(bin_path):
        return bin_path
    raise Exception("Can not find Revit {} binary at {}".format(revit_year, bin_path))


def run_revit(cfg: CLIArgs, journal_file):
    """Launch given revit version with given journal file"""
    # find revit binary
    revit_path = find_revit_binary(cfg.revit_year)
    journal_file = op.abspath(journal_file)
    # launch Revit and wait
    if cfg.lang_code:
        opts = (revit_path, journal_file, "/language", cfg.lang_code)
    else:
        opts = (revit_path, journal_file)
    print("running: %s" % " ".join(list(opts)))
    subprocess.run(list(opts))


def run_command(cfg: CLIArgs):
    """Orchestrate execution using command line options"""
    # prepare cache -------------------
    cache_dir = prepare_cache()

    # prepare env ---------------------
    # make journal
    journal_file = create_rir_journal(
        cache_dir, model_path=cfg.model_path, ghdoc_path=cfg.ghdoc_path
    )
    # create addon manifests
    add_addons(cfg.revit_year, cache_dir, add_rps=cfg.add_rps)

    # run revit -----------------------
    if cfg.start_revit:
        run_revit(cfg, journal_file)


if __name__ == "__main__":
    try:
        # do the work
        run_command(
            # make settings from cli args
            cfg=CLIArgs(
                # process args
                docopt(
                    __doc__.format(__binname__),
                    version="{} {}".format(__binname__, __version__),
                )
            )
        )
    # gracefully handle exceptions and print results
    except Exception as run_ex:
        print(str(run_ex))
        sys.exit(1)
