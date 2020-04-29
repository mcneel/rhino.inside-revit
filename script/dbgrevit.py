# pylint: disable-all
"""Open Rhino.Inside.Revit inside Revit for debugging

Usage:
    {} <revit_year> [<model_path>] [--rps] [--dryrun]

Options:
    -h, --help                          Show this help
    --rps                               Add RevitPythonShell addon
    --dryrun                            Create runtime env but do not start Revit
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
__binname__ = op.splitext(op.basename(__file__))[0] # grab script name
__version__ = '1.0'

# cli configs =================================================================
DEFAULT_JRN_NAME = 'debug.txt'
DEFAULT_JRN_ARTIFACT_PATTERN = r'journal\.\d{4}'
DEFAULT_CACHE_DIR = '.debug'
DEFAULT_REVIT_BIN_PATH = r'%PROGRAMFILES%\Autodesk\Revit {year}\Revit.exe'
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
    'addon': 'Rhino.Inside',
    'dll': r'%APPDATA%\Autodesk\Revit\Addins\{year}\RhinoInside.Revit\RhinoInside.Revit.dll',
    'entry': 'RhinoInside.Revit.Addin',
    'uuid': '02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74',
    'vendor': 'com.mcneel'
}

RPS_ADDON_INFO = {
    'addon': 'RevitPythonShell',
    'dll': r'%PROGRAMFILES(X86)%\RevitPythonShell{year}\RevitPythonShell.dll',
    'entry': 'RevitPythonShell.RevitPythonShellApplication',
    'uuid': '3a7a1d24-51ed-462b-949f-1ddcca12008d',
    'vendor': 'RIPS'
}
# =============================================================================

def ensure_cache_dir():
    """Ensure debug cache directory exists"""
    pwd = op.dirname(__file__)
    cache_dir = op.join(pwd, DEFAULT_CACHE_DIR)
    if not op.isdir(cache_dir):
        os.mkdir(cache_dir)
    return cache_dir


def create_rir_journal(journal_dir, model_path='', journal_name=DEFAULT_JRN_NAME):
    """Create a new Revit journal to lauch Revit and open Rhino.Inside.Revit

    Args:
        journal_dir (str): directory path to create the journal file inside
        model_path (str, optional): request to open this model in journal
        journal_name (str, optional): name of the journal file
    """
    # start a clean journal
    jm = rjm.JournalMaker(permissive=True)
    # open model
    if model_path:
        jm.open_model(model_path)
    # ask to open Rhinoceros tab
    jm.execute_command(
        tab_name='Add-Ins',
        panel_name='Rhinoceros',
        command_module='RhinoInside.Revit.UI',
        command_class='CommandRhinoInside'
        )
    # ask to open Grasshopper
    jm.execute_command(
        tab_name='Rhinoceros',
        panel_name='Grasshopper',
        command_module='RhinoInside.Revit.UI',
        command_class='CommandGrasshopper'
        )
    # write journal to file
    journal_filepath = op.join(journal_dir, journal_name)
    jm.write_journal(journal_filepath)
    return journal_filepath


def write_manifest(revit_year, addon_info, addons_dir):
    dll_path = op.expandvars(addon_info['dll'].format(year=revit_year))
    if op.isfile(dll_path):
        manifest_file_contents = \
            DEFAULT_ADDON_MANIFEST.format(
                addon=addon_info['addon'],
                dll=dll_path,
                entry=addon_info['entry'],
                uuid=addon_info['uuid'],
                vendor=addon_info['vendor'],
                )
        manifest_file_path = op.join(addons_dir, addon_info['addon'] + '.addin')
        with open(manifest_file_path, 'w') as mf:
            mf.write(manifest_file_contents)
    else:
        raise Exception(
                "Can not find {} dll at {}".format(
                    addon_info['addon'], dll_path
                    )
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
    raise Exception(
            "Can not find Revit {} binary at {}".format(revit_year, bin_path)
        )


def run_revit(revit_year, journal_file):
    """Launch given revit version with given journal file"""
    # find revit binary
    revit_path = find_revit_binary(revit_year)
    journal_file = op.abspath(journal_file)
    # launch Revit and wait
    print('running: %s %s' % (revit_path, journal_file))
    subprocess.run([revit_path, journal_file])


def clean_cache(cache_dir):
    """Clear cache files"""
    # find journal files and delete
    for entry in os.listdir(cache_dir):
        entry_path = op.join(cache_dir, entry)
        if op.isfile(entry_path) \
                and re.match(DEFAULT_JRN_ARTIFACT_PATTERN, entry):
            try:
                os.remove(entry_path)
            except Exception as del_ex:
                print('Error removing {} | {}'.format(entry_path, str(del_ex)))


if __name__ == '__main__':
    # process command line args
    args = docopt(
        __doc__.format(__binname__),
        version='{} {}'.format(__binname__, __version__)
        )

    revit_year = args['<revit_year>']
    model_path = args['<model_path>']
    add_rps = args['--rps']
    start_revit = not args['--dryrun']

    # prepare cache -------------------
    # make sure cache dir exists
    cache_dir = ensure_cache_dir()
    # cleanup
    clean_cache(cache_dir)

    # prepare env ---------------------
    # make journal
    journal_file = create_rir_journal(cache_dir, model_path=model_path)
    # create addon manifests
    add_addons(revit_year, cache_dir, add_rps=add_rps)

    # run revit -----------------------
    if start_revit:
        run_revit(revit_year, journal_file)
