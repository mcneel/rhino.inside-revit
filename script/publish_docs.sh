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

# set optional commit description
if [ $2 ]; then
  GIT_COMMIT_DESC="Generated from $2"
fi

# check branch is set
# this is set on github repo secrets
if [ -z $DOCS_GIT_BRANCH ]; then
  >&2 echo "DOCS_GIT_BRANCH is required"
  exit 1
fi

echo "Publishing docs from $(pwd) to github"

# print config
echo
echo "==> Config"
echo "GIT_BRANCH=$DOCS_GIT_BRANCH"
echo "GIT_COMMIT_DESC=$GIT_COMMIT_DESC"

# git
echo
echo "==> Settin up git access"

# set up git user
# git config user.name ?
# git config user.email ?

echo
echo "==> Committing..."

# stage all new files, modifications and deletions in output directory
git add -A . > /dev/null
git status -s

# commit new docs
git commit -m "Publish!" -m "$GIT_COMMIT_DESC" || exit 0
git status -s

echo
echo "==> Publish!"

# push new docs (skip if nothing committed above)
# git push origin HEAD:$GIT_BRANCH