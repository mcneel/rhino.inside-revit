#!/usr/bin/env bash

# This script is here to help you deploy sdk documentation via github
# Please read it carefully. It should be self explanatory.
#
# Will Pearson <will@mcneel.com>
# Ehsan Iran-Nejad  <ehsan@mcneel.com>

set -e

# usage
if [ -z "$1" ]; then
  echo "Usage: $0 <dir> [<source_desc>]"
  exit 1
fi

# check existence of directory containing generated docs
if [ ! -d "$1" ]; then
  echo "$1 doesn't exist!"
  exit 127
fi

# change to docs output directory
cd "$1"

# git
echo
echo "==> Setting up git access..."

# set up git user
git config user.name Bozo
git config user.email bozo@mcneel.com

echo
echo "==> Committing..."

# stage all new files, modifications and deletions in output directory
git add -A . > /dev/null
git status -s

# commit new docs
git commit -m "Publish!" -m "Generated from $2" || exit 0
git status -s

echo
echo "==> Publishing..."
git push "https://bozo@mcneel.com:${{ secrets.API_TOKEN_GITHUB }}@github.com/mcneel/rhino.inside-revit.git"