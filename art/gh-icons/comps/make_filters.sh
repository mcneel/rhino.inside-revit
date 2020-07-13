#! /bin/bash
# generic filters
cat filters.txt | xargs -I '{}' cp Filters.png '{}.png'
cat filters.txt | xargs -I '{}' cp Filters.svg '{}.svg'
cat filters_inverted.txt | xargs -I '{}' cp FiltersInverted.png '{}.png'
cat filters_inverted.txt | xargs -I '{}' cp FiltersInverted.svg '{}.svg'
