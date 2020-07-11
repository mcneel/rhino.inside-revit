#! /bin/bash
# generic filters
cat filters.txt | xargs -I '{}' cp Filters.png '{}.png'
cat filters.txt | xargs -I '{}' cp Filters.svg '{}.svg'
