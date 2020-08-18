The `cgroups.py` parses the built-in category text files under the `bic_data/` and attempts to organize based on a predefined grouping logic. The output files are stored as `.json` under the same directory and have the schema as shown below:

```json
{
    "meta": {
        "version": "2018",                          // revit version
        "total": 1004,                              // total number of built-in categories
        "included": 724,                            // number of built-in categories included in components
        "excluded": [                               // list of built-in categories excluded from components
            "OST_BranchPanelScheduleTemplates",
            "OST_StairsRailingAboveCut",
            ...
        ]
    },
    "components": [                                 // list of catagory selector components

        {
            "name": "Site",                         // component name
            "categories": {                         // category groups
                "_": [                              // root categories
                    "OST_BuildingPad",
                    "OST_Parking",
                    ...
                ],
                "Topography": {                     // subgroup
                    "_": [                              // root categories of subgroup
                        "OST_SecondaryTopographyContours",
                        "OST_Topography",
                        ...
                    ]
                }
            }
        },

        ...

    ]
}
```