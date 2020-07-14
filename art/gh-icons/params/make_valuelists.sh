#! /bin/bash
# generics
cat valuelists.txt | xargs -I '{}' cp ValueList.png '{}.png'
cat valuelists.txt | xargs -I '{}' cp ValueList.svg '{}.svg'
# walls
cat valuelists_walls.txt | xargs -I '{}' cp ValueList_Walls.png '{}.png'
cat valuelists_walls.txt | xargs -I '{}' cp ValueList_Walls.svg '{}.svg'
# curtain grids
cat valuelists_cwgrids.txt | xargs -I '{}' cp ValueList_CurtainGrids.png '{}.png'
cat valuelists_cwgrids.txt | xargs -I '{}' cp ValueList_CurtainGrids.svg '{}.svg'
# categories
cat valuelists_categories.txt | xargs -I '{}' cp ValueList_Categories.png '{}.png'
cat valuelists_categories.txt | xargs -I '{}' cp ValueList_Categories.svg '{}.svg'
# compound layers
cat valuelists_layers.txt | xargs -I '{}' cp ValueList_Layers.png '{}.png'
cat valuelists_layers.txt | xargs -I '{}' cp ValueList_Layers.svg '{}.svg'
