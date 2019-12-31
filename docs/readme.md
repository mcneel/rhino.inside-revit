# Rhino.Inside.Revit Project

This repo contains the contents of https://mcneel.github.io/rhino.inside-revit/

The site is hosted on [GitHub Pages](https://pages.github.com/) which uses a static site generator called [Jekyll](http://jekyllrb.com/).  The pages are made generally from a markdown syntax called [kramdown](http://kramdown.gettalong.org/syntax.html)

## Getting Started with GitHub

There are 3 ways to edit these GitHub Pages:

 1. Edit the markdown (md) files directly on GitHub.
 1. Clone the Github repository on your local computer. This brings all the files onto your computer.  Then edit those files and push the changes back up to GitHub.
 1. Setup a local GitHub Pages server on your machine. Clone the GitHub repository on your machine.  Then, edit the files. The changes can be previewed on the local GitHub server before pushing the changes up to GitHub.

The recommended product we use to pull the files to your local machine is [SourceTree](https://www.sourcetreeapp.com/).  Once downloaded and installed, log-in to GitHub through [SourceTree](https://www.sourcetreeapp.com/).  Clone the "mcneel/help-docs" repository to a local folder on your machine.  All other interactions with GitHub can be accomplished with [SourceTree](https://www.sourcetreeapp.com/)

If SourceTree does not work well in your case, GitHub offers a desktop application which simplifies syncing with the public repository.  Try [GitHub for Mac](https://mac.github.com/) and [GitHub for Windows](https://windows.github.com/). There's also the [git cheat sheet](https://training.github.com/kit/downloads/github-git-cheat-sheet.pdf).

Finally, if you are comfortable using GIT on the commandline, navigate somewhere safe and clone the repository.

```
git clone https://github.com/mcneel/help-docs.git
```

Notice that **'gh-pages'** is the only branch. Everything committed to this branch is automatically published when pushed to GitHub.

## Getting Started with local server preview.

Most edits can be made on the site, or by pulling the site locally then pushing the site back up to GitHub to see the reults.  If needed, it is possible to run a GitHub server locally to see any changes before pushing to GitHub. These instructions cover running this project locally on both Mac OS X and Windows so that you can preview changes before pushing them up to GitHub Pages.



### ![Mac Instructions](https://github.com/mcneel/help-docs/blob/gh-pages/static/images/mac_logo_small.png) Mac OS X Local Server Installation

Mac OS X Yosemite ships with Ruby and RubyGems, however it's [not wise](https://github.com/mcneel/help-docs/pull/2#issuecomment-112601698) to mess around with this installation. Instead, install your own Ruby using [Homebrew](http://brew.sh).

#### Install Homebrew and Ruby

As per the Homebrew website, install via the following one-liner (which will prompt you to install the Xcode Command Line Tools, if you don't already have them).

```
ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"
```

Then we can brew Ruby.

```
brew install ruby
```

Now close and reopen the Terminal window to make sure the system knows about the new version of Ruby.

#### Install Jekyll

The [GitHub Pages Ruby Gem](https://github.com/github/pages-gem) provides the same version of Jekyll as exists on the GitHub Pages servers. This ensures we aren't accidentally using features that have either been deprecated or simply don't exist yet!

```
gem install github-pages
```

You can now serve your local copy of this site by running the following commands, remembering to replace `CLONE_DIRECTORY` with the location to which you checked out this repository.

```
cd CLONE_DIRECTORY
jekyll serve
```

Navigate to http://localhost:4000 in your browser to view the site.

### ![Windows Instructions](https://github.com/mcneel/help-docs/blob/gh-pages/static/images/win_logo_small.png) Windows Local Server Installation

Please install  Ruby, Ruby Devkit, Jekyll, Github pages, wdm.  The instructiosn are below.

#### Install Ruby and Ruby DevKit

Go to http://rubyinstaller.org/downloads/ and download the installer for Ruby 2.2.X that matches your system architecture (x86/x64).

At the "Installation Destination and Optional Tasks" dialog, make sure to check the **"Add Ruby executable to your PATH"** box.

Then, from the same page download the Development Kit that corresponds to your Ruby installation. Jekyll won't be fully functional without this.

Run the self extracting archive, entering the path `C:\RubyDevKit` when prompted.

To initialize and install the DevKit, open up a command prompt and roll up your sleeves...

```
cd C:\RubyDevKit
ruby dk.rb init
ruby dk.rb install
```

#### Install Jekyll

As with OS X, install the GitHub Pages Ruby Gem, navigate to the clone directory and run jekyll.

```
gem install jekyll
gem install github-pages
gem install wdm
```

#### Running the Local Jekyll server

Using the command line navigate to the local GitHUB clone directory.  Then run the "jekyll serve" command.

```
cd "to your local CLONE_DIRECTORY"
jekyll serve
```
The server can be accessed through the Localhost address listed on the commandline.
