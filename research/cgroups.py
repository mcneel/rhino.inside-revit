"""Group Revit built-in categories logically and output the data in json

The built-in categories are provided in text files under DATA_DIR

Usage:
    python3 ./cgroups.py              group and output categories
    python3 ./cgroups.py  <catname>   group and output <catname> category only
"""
# pylint: disable=bad-continuation
import sys
import os
import os.path as op

from typing import Set, List, TypeVar
import json
import re


DATA_DIR = "./bic_data"

CGROUP_T = TypeVar("CGROUP")  # pylint: disable=invalid-name


class CGROUP:
    """Represents a category grouping"""

    def __init__(
        self,
        name: str,
        exclusives: List[str],
        includes: List[str],
        excludes: List[str],
        cgroups: List[CGROUP_T],
        hidden: bool = False,
    ):
        self.name: str = name
        self.exclusives: List[str] = exclusives
        self.includes: List[str] = includes
        self.excludes: List[str] = excludes
        self.cgroups: List[CGROUP_T] = cgroups
        self.hidden: bool = hidden


class CategoryComp:
    """Represents data for a category selector component"""

    def __init__(self, name: str, categories: List[str]):
        self.name = name
        self.categories = categories


class CategoryCompCollection:
    """Represents data for a collection of category selector components"""

    def __init__(
        self,
        version: str,
        bics: List[str],
        components: List[CategoryComp],
        used_bics: Set[str],
    ):
        self.meta = {
            "version": version,
            "total": len(bics),
            "included": len(used_bics),
            "excluded": list(bics.difference(used_bics)),
        }
        self.components = components


# =============================================================================
# this is a hand-crafted tree of CGroups that represents the grouping logic
# -----------------------------------------------------------------------------
CGROUPS = [
    CGROUP(
        name="Skip",
        exclusives=[
            r".+Obsolete.*",
            r".+OBSOLETE.*",
            r".+Deprecated.*",
            r"OST_GbXML.*",
            r"OST_gbXML.*",
            r"OST_DSR_.*",
        ],
        includes=[],
        excludes=[],
        cgroups=[],
        hidden=True,
    ),
    CGROUP(
        name="Site",
        exclusives=[],
        includes=[
            r"OST_Site.*",
            r"OST_Sewer.*",
            r"OST_Road.*",
            r"OST_Building.*",
            r"OST_Contour.*",
            r"OST_Parking.*",
        ],
        excludes=[],
        cgroups=[
            CGROUP(
                name="Topography",
                exclusives=[],
                includes=[r"OST_.*Topo.*"],
                excludes=[],
                cgroups=[],
            ),
        ],
    ),
    CGROUP(
        name="References",
        exclusives=[],
        includes=[
            r"OST_Grid.*",
            r"OST_Level.*",
            r"OST_Level.*",
            r"OST_Constraint.*",
            r"OST_Reference.*",
        ],
        excludes=[
            r"OST_GridChains.*",
            r"OST_ReferencePoints.*",
            r"OST_ReferenceViewer.*",
        ],
        cgroups=[],
    ),
    CGROUP(
        name="Modeling",
        exclusives=[],
        includes=[r"OST_Generic.*",],
        excludes=["OST_GenericLines",],
        cgroups=[
            CGROUP(
                name="Mass",
                exclusives=[],
                includes=[r"OST_Mass.*"],
                excludes=[
                    r"OST_.+Cutter",
                    r"OST_.+Splitter",
                    r"OST_.+All",
                    r"OST_.+Outlines",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Ceilings",
                exclusives=[],
                includes=[r"OST_Ceiling.*"],
                excludes=[
                    r"OST_.+Cut.*",
                    r"OST_.+Projection.*",
                    r"OST_.+Default.*",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Columns",
                exclusives=[],
                includes=[r"OST_Column.*"],
                excludes=[r"OST_.+LocalCoordSys"],
                cgroups=[],
            ),
            CGROUP(
                name="Curtain Systems",
                exclusives=[],
                includes=[r"OST_Curta.*"],
                excludes=[
                    r"OST_.+FaceManager.*",
                    r"OST_CurtainGrids.+",
                    r"OST_Curtain.+Cut",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Floors",
                exclusives=[],
                includes=[r"OST_Floor.*"],
                excludes=[
                    r"OST_.+LocalCoordSys",
                    r"OST_.+Cut.*",
                    r"OST_.+Projection.*",
                    r"OST_.+Default.*",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Doors",
                exclusives=[],
                includes=[r"OST_Door.*"],
                excludes=[r"OST_.+Cut.*", r"OST_.+Projection.*",],
                cgroups=[],
            ),
            CGROUP(
                name="Casework",
                exclusives=[],
                includes=[r"OST_Casework.*"],
                excludes=[],
                cgroups=[],
            ),
            CGROUP(
                name="Windows",
                exclusives=[],
                includes=[r"OST_Window.*"],
                excludes=[r"OST_.+Cut.*", r"OST_.+Projection.*",],
                cgroups=[],
            ),
            CGROUP(
                name="Furniture",
                exclusives=[],
                includes=[r"OST_Furniture.*"],
                excludes=[],
                cgroups=[],
            ),
            CGROUP(
                name="Adaptive",
                exclusives=[],
                includes=[r"OST_Adaptive.*"],
                excludes=[],
                cgroups=[],
            ),
            CGROUP(
                name="Speciality",
                exclusives=[],
                includes=[r"OST_Speciality.*"],
                excludes=[],
                cgroups=[],
            ),
            CGROUP(
                name="Openings",
                exclusives=[r"OST_.+Opening", r"OST_Arc.*", r"OST_Shaft.*",],
                includes=[],
                excludes=[r"OST_.+Cut.*", r"OST_.+Projection.*",],
                cgroups=[],
            ),
            CGROUP(
                name="Railing",
                exclusives=[],
                includes=[r"OST_Railing.*"],
                excludes=[r"OST_.+Cut.*", r"OST_.+Projection.*",],
                cgroups=[],
            ),
            CGROUP(
                name="Stairs",
                exclusives=[],
                includes=[r"OST_Stair.*", r"OST_.+Stairs"],
                excludes=[r"OST_.+Cut.*", r"OST_.+Projection.*",],
                cgroups=[],
            ),
            CGROUP(
                name="Ramps",
                exclusives=[],
                includes=[r"OST_Ramp.*"],
                excludes=[r"OST_.+Cut.*", r"OST_.+Projection.*",],
                cgroups=[],
            ),
            CGROUP(
                name="Walls",
                exclusives=[],
                includes=[r"OST_Wall.*", r"OST_Reveals", r"OST_Stacked.*"],
                excludes=[
                    r"OST_.+LocalCoordSys",
                    r"OST_.+RefPlanes",
                    r"OST_.+Default",
                    r"OST_.+Cut.*",
                    r"OST_.+Projection.*",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Roofs",
                exclusives=[],
                includes=[
                    r"OST_Roof.*",
                    r"OST_Fascia.*",
                    r"OST_Purlin.*",
                    r"OST_Gutter.*",
                    r"OST_Cornices.*",
                    r"OST_Dormer.*",
                ],
                excludes=[
                    r"OST_.+Opening.*",
                    r"OST_.+Cut.*",
                    r"OST_.+Projection.*",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Spatial",
                exclusives=[],
                includes=[
                    r"OST_Area.*",
                    r"OST_Zone.*",
                    r"OST_MEPSpace.*",
                    r"OST_Zoning.*",
                    r"OST_Room.*",
                ],
                excludes=[
                    r"OST_.+Fill",
                    r"OST_.+Visibility",
                    r"OST_AreaRein.*",
                    r"OST_AreaReport.*",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Structural",
                exclusives=[],
                includes=[
                    r"OST_Struct.+",
                    r"OST_.+Bracing",
                    r"OST_Truss.*",
                    r"OST_Joist.*",
                    r"OST_FabricArea.*",
                    r"OST_Rebar.*",
                    r"OST_Girder.*",
                    r"OST_Edge.*",
                    r"OST_Load.*",
                    r"OST_Internal.*Load.*",
                    r"OST_Isolated.*",
                    r"OST_Framing.*",
                    r"OST_Footing.*",
                    r"OST_Foundation.*",
                    r"OST_Fnd.*",
                    r"OST_Span.*",
                    r"OST_Steel.*",
                    r"OST_SWall.*",
                    r"OST_Brace.*",
                    r"OST_Bridge.*",
                    r"OST_.*PointLoad.*",
                    r"OST_Beam.*",
                ],
                excludes=[
                    r"OST_.+LocalCoordSys",
                    r"OST_.+Other",
                    r"OST_.+LocationLine",
                    r"OST_.+PlanReps",
                    r"OST_.+NobleWarning",
                    r"OST_.+Failed",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Mechanical",
                exclusives=[],
                includes=[
                    r"OST_Mechanical.*",
                    r"OST_.+Ducts",
                    r"OST_Duct.*",
                    r"OST_MEPAnalytical.*",
                    r"OST_Flex.*",
                    r"OST_MEPSystem.*",
                    r"OST_HVAC.*",
                    r"OST_Fabrication.+",
                ],
                excludes=[
                    r"OST_.+Reference.*",
                    r"OST_.+TmpGraphic.*",
                    r"OST_.+Visibility",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Electrical",
                exclusives=[],
                includes=[
                    r"OST_.+Pipes",
                    r"OST_Conduit.*",
                    r"OST_Cable.*",
                    r"OST_Wire.*",
                    r"OST_Light.*",
                    r"OST_Device.*",
                    r"OST_Panel.*",
                    r"OST_Elec.*",
                    r"OST_Routing.*",
                    r"OST_Switch.*",
                    r"OST_Connector.*",
                    r"OST_Route.*",
                    r"OST_.+Devices|OST_.+Device(Tags)|OST_.+Templates?",
                ],
                excludes=[
                    r"OST_.+Axis",
                    r"OST_.+Template.*",
                    r"OST_.+Definition.*",
                    r"OST_.+Material",
                ],
                cgroups=[],
            ),
            CGROUP(
                name="Plumbing",
                exclusives=[],
                includes=[
                    r"OST_Pipe.*",
                    r"OST_Fluid.*",
                    r"OST_Fixture.*",
                    r"OST_PlumbingFixture.*",
                    r"OST_Piping.*",
                    r"OST_Sprinkler.*",
                ],
                excludes=[r"OST_.+Reference.*", r"OST_.+Material",],
                cgroups=[],
            ),
        ],
    ),
    CGROUP(
        name="Drafting",
        exclusives=[],
        includes=[],
        excludes=[],
        cgroups=[
            CGROUP(
                name="Views",
                exclusives=[],
                includes=[
                    r"OST_.*Annotation.*",
                    "OST_Views",
                    "OST_PlanRegion",
                    r"OST_Schedule.*",
                    r"OST_Camera.*",
                    r"OST_Crop.*",
                    r"OST_Compass.*",
                    r"OST_Section.*",
                    r"OST_Sun.*",
                    r"OST_RenderRegions",
                ],
                excludes=[r"OST_.+ViewParamGroup",],
                cgroups=[],
            ),
            CGROUP(
                name="Sheets",
                exclusives=[],
                includes=[
                    r"OST_Sheet.*",
                    r"OST_Viewport.*",
                    r"OST_Title.*",
                    r"OST_Guide.*",
                    r"OST_Revisions.*",
                ],
                excludes=[],
                cgroups=[],
            ),
            CGROUP(
                name="Tags",
                exclusives=[r"OST_Tag.*", r"OST_.+Tags", r"OST_.+Labels"],
                includes=[],
                excludes=[],
                cgroups=[],
            ),
            CGROUP(
                name="Annotation",
                exclusives=[
                    r"OST_.+DownArrow.*",
                    r"OST_.+DownText.*",
                    r"OST_.+UpArrow.*",
                    r"OST_.+UpText.*",
                    r"OST_.+Annotation.*",
                    r"OST_Callout.*",
                    r"OST_Spot.*",
                    r"OST_Cloud.*",
                    r"OST_Elev.*",
                    r"OST_Repeating.*",
                    "OST_BrokenSectionLine",
                    r"OST_Legend.*",
                    r"OST_Detail.*",
                    "OST_InvisibleLines",
                    "OST_DemolishedLines",
                    "OST_InsulationLines",
                    "OST_FillPatterns",
                    "OST_FilledRegion",
                    "OST_HiddenLines",
                    r"OST_Center.*",
                    r"OST_Keynote.*",
                    r"OST_Matchline.*",
                    r"OST_Model.*",
                    r"OST_.+Text.*",
                    r"OST_.+Overhead.*",
                    r"OST_Curve.*",
                    r"OST_Dim.*",
                    r"OST_Dimension.*",
                    r"OST_Masking.*",
                    r"OST_.+Tag.*",
                    r"OST_.+Label.*",
                    r"OST_.+Symbol.*",
                    r"OST_.+TickMark.*",
                    "OST_RevisionClouds",
                ],
                includes=[],
                excludes=[r"OST_DimLock.+", r"OST_IOS.+", r"OST_.+Symbology",],
                cgroups=[],
            ),
        ],
    ),
    CGROUP(
        name="Containers",
        exclusives=[],
        includes=[
            r"OST_Part.*",
            r"OST_Assemblies.*",
            r"OST_Group.*",
            r"OST_.+Groups",
        ],
        excludes=[],
        cgroups=[],
    ),
    CGROUP(
        name="Links",
        exclusives=[
            "OST_RvtLinks",
            "OST_TopographyLink",
            r"OST_Coordination.*",
            r"OST_PointCloud.*",
            r"OST_Raster.*",
        ],
        includes=[],
        excludes=[],
        cgroups=[],
    ),
    CGROUP(
        name="Analysis",
        exclusives=[r"OST_.*Analy.*"],
        includes=[],
        excludes=[r"OST_AnalysisResults"],
        cgroups=[
            CGROUP(
                name="Paths",
                exclusives=[r"OST_Path.*"],
                includes=[],
                excludes=[],
                cgroups=[],
            ),
        ],
    ),
    CGROUP(
        name="Rendering",
        exclusives=[],
        includes=[r"OST_Entourage.*",],
        excludes=[],
        cgroups=[
            CGROUP(
                name="Materials",
                exclusives=[
                    r"OST_Material.*",
                    r"OST_Appearance.*",
                    r"OST_Decal.*",
                    r"OST_Planting.*",
                ],
                includes=[],
                excludes=[],
                cgroups=[],
            )
        ],
    ),
]
# =============================================================================


def expand_exclusives(
    cgroup: CGROUP, used_bics: Set[str], remaining_bics: Set[str]
):
    """Apply the exclusive filters and expand to builtin category names"""
    exclusives = set()
    excludes = set()

    local_bics = remaining_bics.copy()
    for bic in local_bics:
        for excluspat in cgroup.exclusives:
            if re.match(excluspat, bic):
                if bic in used_bics:
                    raise Exception(
                        f'Exclusive conflict in "{cgroup.name}" @ "{excluspat}"'
                    )
                exclusives.add(bic)

    filtered_exclusives = exclusives.copy()
    for exclusitem in exclusives:
        for excpat in cgroup.excludes:
            if re.match(excpat, exclusitem):
                excludes.add(exclusitem)
    filtered_exclusives.difference_update(excludes)
    used_bics.update(filtered_exclusives)

    remaining_bics.difference_update(used_bics)

    sub_components = []
    for sub_cgroup in cgroup.cgroups:
        sub_components.append(
            expand_exclusives(sub_cgroup, used_bics, remaining_bics)
        )
    cgroup.exclusives = filtered_exclusives


def expand_includes(
    cgroup: CGROUP, used_bics: Set[str], remaining_bics: Set[str]
):
    """Apply the include filters and expand to builtin category names"""
    includes = set()
    excludes = set()

    local_bics = remaining_bics.copy()
    for bic in local_bics:
        for incpat in cgroup.includes:
            if re.match(incpat, bic):
                includes.add(bic)

    filtered_includes = includes.copy()
    for incitem in includes:
        for excpat in cgroup.excludes:
            if re.match(excpat, incitem):
                excludes.add(incitem)
    filtered_includes.difference_update(excludes)
    used_bics.update(filtered_includes)

    sub_components = []
    for sub_cgroup in cgroup.cgroups:
        sub_components.append(
            expand_includes(sub_cgroup, used_bics, remaining_bics)
        )
    cgroup.includes = filtered_includes


def filter_cgroup(cgroup: CGROUP, name: str):
    """Find a cgroup in tree by name"""
    if cgroup.name == name:
        return cgroup
    for scgroup in cgroup.cgroups:
        if mcg := filter_cgroup(scgroup, name):
            return mcg


def create_ccomp(cgroup: CGROUP) -> CategoryComp:
    """Create component data from expanded cgroup"""
    root_categories = cgroup.exclusives
    root_categories.update(cgroup.includes)

    sub_components = []
    for sub_cgroup in cgroup.cgroups:
        sub_components.append(create_ccomp(sub_cgroup))

    sub_categories = {}
    for sub_comp in sub_components:
        sub_categories[sub_comp.name] = sub_comp.categories
        all_sub_bips = []
        for sub_bips in sub_comp.categories.values():
            all_sub_bips.extend(sub_bips)
        root_categories = root_categories.difference(all_sub_bips)
    categories = {"_": sorted(list(root_categories))}
    categories.update(sub_categories)
    return CategoryComp(name=cgroup.name, categories=categories)


def create_ccomp_collection(
    version: str, builtin_category_names: List[str]
) -> CategoryCompCollection:
    """Create component collection from list of builtin category names"""
    remaining_bics = builtin_category_names.copy()
    used_bics: Set[str] = set()
    for cgroup in CGROUPS:
        expand_exclusives(cgroup, used_bics, remaining_bics)

    for cgroup in CGROUPS:
        expand_includes(cgroup, used_bics, remaining_bics)

    all_comps: List[CategoryComp] = []

    if len(sys.argv) > 1:
        matching_cgroup = None
        for cgroup in CGROUPS:
            matching_cgroup = filter_cgroup(cgroup, name=sys.argv[1])
            if matching_cgroup:
                all_comps.append(create_ccomp(matching_cgroup))
    else:
        for cgroup in CGROUPS:
            if not cgroup.hidden:
                all_comps.append(create_ccomp(cgroup))

    return CategoryCompCollection(
        version=version,
        bics=builtin_category_names,
        components=all_comps,
        used_bics=used_bics,
    )


def load_bics(data_file: str):
    """Load builtin category names from file"""
    bics_data: Set[str] = set()
    with open(data_file, "r") as bicfile:
        bics_data.update([x.strip() for x in bicfile.readlines()])
    return bics_data


def dump_bics(data_file: str, ccomps_col: CategoryCompCollection):
    """Dump component collection data into file"""
    with open(data_file, "w") as datafile:
        json.dump(
            ccomps_col, datafile, indent=2, default=lambda x: x.__dict__,
        )


for entry in os.listdir(DATA_DIR):
    if entry.endswith(".txt"):
        bic_file = op.join(DATA_DIR, entry)
        dafa_filename = op.splitext(op.basename(bic_file))[0]
        bic_file_version = dafa_filename.split("_")[1]
        bic_names = load_bics(bic_file)
        ccomp_collection = create_ccomp_collection(bic_file_version, bic_names)
        json_file = op.join(DATA_DIR, dafa_filename + ".json")
        dump_bics(json_file, ccomp_collection)
