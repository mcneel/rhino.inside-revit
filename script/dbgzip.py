#pylint: disable=broad-except,invalid-name
"""Analyzes the debug ZIP packages submitteed by customers

Usage:
    {cliname} <sb_ticket> --token=<api_token>
    {cliname} <zip_file> --ticket=<ticket_url>

Options:
    -h, --help                          Show this help
    <sb_ticket>                         SupportBee ticket url
    --token=<api_token>                 API token to access SupportBee
    <zip_file>                          Debug package zip file path
    --ticket=<ticket_url>               SupportBee ticket url for reporting
"""
import io
import sys
import os
import os.path as op
import shutil
import zipfile
import csv
import json
import re
from collections import namedtuple

# pipenv dependencies
from docopt import docopt
import requests


# cli info
__binname__ = op.splitext(op.basename(__file__))[0] # grab script name
__version__ = '1.0'


# cli configs =================================================================
DEFAULT_CACHE_DIR = '.packages'
MAX_JRN_LINES = 100
# =============================================================================

# replacement strings
WIN_EOL = b'\r\n'
EOL = b'\n'

ADSK_ADDON = "Autodesk"
MCNEEL_ADDON = "Robert McNeel"
ADDONS_TABLE_HEADER = """
Company Name | Product Name | Product Version | Type Name | Assembly Name | Assembly Location
--- | --- | --- | --- | --- | ---
"""


ReportInfo = namedtuple('ReportInfo', ['host_info', 'journal_file'])

ConflictedAddon = namedtuple('ConflictedAddon', ['name', 'version'])


# known third-party conflicts =================================================
KNOWN_CONFLICTS = [
    ConflictedAddon(name="pyRevit", version="*"),
    ConflictedAddon(name="AVAIL", version="*"),
    ConflictedAddon(name="Conveyor", version="*"),
    ConflictedAddon(name="Speckle", version="*"),
]
# =============================================================================


class CLIArgs:
    """Data type to hold command line args"""
    def __init__(self, args):
        self.sb_ticket = args['<sb_ticket>'] or args['--ticket']
        self.sb_token = args['--token']
        self.zip_file = args['<zip_file>']


class DebugFileParts:
    """Debug package components"""
    NamingFormat = r'RhinoInside-Revit-Report-(.+).zip'
    Report = "Report.md"
    ReportHostSection = "## Host"
    ReportAddinSection = "## Addins"
    ReportConsoleSection = "## Console"
    ReportAttachmentSection = "## Attachments"
    RIRJournalRibbonEvent = "Jrn.RibbonEvent \"Execute external command:CustomCtrl_%CustomCtrl_%Add-Ins%Rhinoceros%CommandRhinoInside:RhinoInside.Revit.UI.CommandRhinoInside\"" #pylint: disable=line-too-long
    ConsoleLog = "Console/Startup.txt"
    AppsCSV = "Addins/{name}.csv"


class DebugFile:
    """Wrap debug file to access properties and contents"""
    def __init__(self, file_path):
        self.path = file_path
        self._dfile = None

    def __enter__(self):
        self._dfile = zipfile.ZipFile(self.path, 'r')
        return self

    def __exit__(self, exception, exception_value, traceback):
        self._dfile.close()

    @staticmethod
    def extract_timestamp(zip_file):
        """Extract timestamp from file name"""
        return re.search(DebugFileParts.NamingFormat, zip_file).groups()[0]

    @property
    def timestamp(self):
        """Debug file creation timestamp"""
        return DebugFile.extract_timestamp(self.path)

    @property
    def root(self):
        """Debug file root folder"""
        first_file = self._dfile.namelist()[0]
        return op.dirname(first_file)

    def read_txt(self, filename, encoding='utf-8'):
        """Read contents of given file"""
        if self._dfile:
            try:
                # read, cleanup EOL and correct encoding
                with self._dfile.open(op.join(self.root, filename), 'r') as tf:
                    contents = \
                        tf.read()\
                            .replace(WIN_EOL, EOL)\
                            .decode(encoding, errors='ignore')
                return contents
            except Exception as rtxt_ex:
                sys.stderr.write("[WARN] %s" % str(rtxt_ex))
        return ""

    def read_csv(self, filename, headers=True):
        """Read contents of given csv file"""
        if self._dfile:
            try:
                # read, remove header if expected
                with self._dfile.open(op.join(self.root, filename), 'r') as tf:
                    csv_reader = csv.reader(io.TextIOWrapper(tf))
                    if headers:
                        return list(csv_reader)[1:]
                    return list(csv_reader)
            except Exception as csv_ex:
                sys.stderr.write("[WARN] %s" % str(csv_ex))
        return []

    def extract(self, filename, to_file):
        """Extract given file to given destination"""
        if self._dfile:
            # copy file (taken from zipfile's extract)
            source = self._dfile.open(op.join(self.root, filename))
            target = open(to_file, "wb")
            with source, target:
                shutil.copyfileobj(source, target)
        else:
            raise Exception("ZIP file is not open")


def ensure_cache_dir():
    """Ensure debug cache directory exists"""
    pwd = op.dirname(__file__)
    cache_dir = op.join(pwd, DEFAULT_CACHE_DIR)
    if not op.isdir(cache_dir):
        os.mkdir(cache_dir)
    return cache_dir


def process_report(dfile):
    """Extract interesting parts from report file"""
    report = dfile.read_txt(DebugFileParts.Report)
    report_parts = []
    # grab information from known sections of the report
    for part_name in [DebugFileParts.ReportHostSection,
                      DebugFileParts.ReportAttachmentSection]:
        match = re.search(
            r"{}\n+((?:.+\n)+)".format(part_name),
            report,
            flags=re.MULTILINE
            )
        if match:
            report_parts.append(match.groups()[0])
    return ReportInfo(
        # return host info verbatim
        host_info=report_parts[0],
        # grab journal file from report section
        # first line is expected to be the journal file
        # .dmp file might be listed after
        journal_file=re.search(r'\((.+)\)', report_parts[1]).groups()[0]
    )


def process_journal(dfile, journal_file):
    """Extract interesting parts from journal file"""
    record_lines = -1
    extracted = []
    # find where rir is executed in journal and
    # grab MAX_JRN_LINES lines after that
    for jline in dfile.read_txt(journal_file).split('\n'):
        if DebugFileParts.RIRJournalRibbonEvent in jline:
            record_lines = 0
        if record_lines != -1:
            if record_lines >= MAX_JRN_LINES:
                break
            else:
                extracted.append(jline)
                record_lines += 1
    return '\n'.join(extracted)


def process_console(dfile):
    """Extract info from console log file"""
    # grab all of the console log data
    return dfile.read_txt(DebugFileParts.ConsoleLog)


def process_addons(dfile):
    """Extract interesting parts from loaded addons info file"""
    # read addon data from csv file
    addons = ""
    csv_data = dfile.read_csv(
        DebugFileParts.AppsCSV.format(name=dfile.timestamp),
        headers=True
        )
    # report anything that is third-party
    # mark the know conflicts with a exclamation mark
    if csv_data:
        addons += ADDONS_TABLE_HEADER
        for csvline in csv_data:
            if all(x not in csvline[0] for x in [ADSK_ADDON, MCNEEL_ADDON]):
                if any(x.name in csvline[0] for x in KNOWN_CONFLICTS):
                    csvline[0] = '⚠️' + csvline[0]
                addons += ' | '.join(csvline) + '\n'
    else:
        addons += "Addon data not collected"
    return addons


def sanitize_report(report):
    """Cleanup user info from report"""
    # remove home dir refs, replace with env vars
    report = re.sub(r"C:\\users\\(.+?)\\appdata\\roaming\\",
                    r"%APPDATA%\\",
                    report, flags=re.IGNORECASE)
    report = re.sub(r"C:\\users\\(.+?)\\appdata\\local\\",
                    r"%LOCALAPPDATA%\\",
                    report, flags=re.IGNORECASE)
    return report


def process_dbpkg(zip_file, ticket=None):
    """Process given debug zip file"""
    # open zip file
    new_report = '\n'
    with DebugFile(zip_file) as dfile:
        if ticket:
            new_report = '# Ticket Info\n'
            new_report += "[Support Ticket]({})\n\n".format(ticket)
        # read Report.md
        new_report += '# Host Info\n'
        rinfo = process_report(dfile)
        new_report += rinfo.host_info
        new_report += '\n\n'

        # read journal errors
        new_report += '# Journal Report\n'
        new_report += \
            'Section of journal after loading Rhino.Inside.Revit '\
            '({} lines)\n'.format(MAX_JRN_LINES)
        new_report += '```\n'
        new_report += process_journal(dfile, rinfo.journal_file)
        new_report += '\n```\n\n'

        # read console log
        new_report += '# Console Log\n'
        new_report += '```\n'
        new_report += process_console(dfile)
        new_report += '```\n\n'

        # extract interesting addons
        new_report += '# Third-party Addons\n'
        new_report += '⚠️ shows addons with known conflicts\n'
        new_report += process_addons(dfile)
        new_report += '\n\n'

    # write report
    print(sanitize_report(new_report))


def download_file(zip_url, api_token, filename, download_dir):
    """Download zip file from supportbee"""
    local_filename = op.join(download_dir, filename)
    with requests.get(
            zip_url,
            params={"auth_token": api_token},
            stream=True) as r:
        r.raise_for_status()
        with open(local_filename, 'wb') as f:
            for chunk in r.iter_content(chunk_size=8192):
                f.write(chunk)
    return local_filename


def process_sb_ticket(ticket_url, api_token):
    """Process given supportbee ticket and download the debug file"""
    download_dir = ensure_cache_dir()
    # get ticket info
    r = requests.get(
        ticket_url,
        params={"auth_token": api_token},
        headers={
            "Content-Type": "application/json",
            "Accept": "application/json"
            })
    if r.status_code == 200:
        # download debug file from ticket
        ticket_data = json.loads(r.text)
        for att in ticket_data["ticket"]["content"]["attachments"]:
            if att["filename"].endswith(".zip"):
                return download_file(
                    zip_url=att["url"]["original"],
                    api_token=api_token,
                    filename=att["filename"],
                    download_dir=download_dir
                    )


def run_command(cfg: CLIArgs):
    """Orchestrate execution based on input args"""
    # process data
    # if zip file is provided
    if cfg.zip_file:
        # process zip file, include ticket url for reporting
        process_dbpkg(cfg.zip_file, ticket=cfg.sb_ticket)
    # otherwise if supportbee url is available
    elif cfg.sb_ticket:
        # download the zip file from ticket
        zip_file = process_sb_ticket(cfg.sb_ticket, cfg.sb_token)
        if zip_file:
            # process zip file, include ticket url for reporting
            process_dbpkg(zip_file, ticket=cfg.sb_ticket)
        else:
            # or raise error
            raise Exception("No Zip file is attached to the ticket")


if __name__ == '__main__':
    try:
        # do the work
        run_command(
            # make settings from cli args
            cfg=CLIArgs(
                # process args
                docopt(
                    __doc__.format(cliname=__binname__),
                    version='{} {}'.format(__binname__, __version__)
                )
            )
        )
    # gracefully handle exceptions and print results
    except Exception as run_ex:
        sys.stderr.write("[ERROR] %s" % str(run_ex))
        sys.exit(1)
