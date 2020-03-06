hljs.initHighlightingOnLoad();


// version selectors can set the active version
function setActiveVersion(version) {
    console.log("setting active version to:", version);
    activeVersion = version;
}

// get page link for specific version
function getPageLink(pageName, pageVersion) {
    var pageLink = pageLinkTemplate + pageName;
    if (!pageVersion) {
        pageVersion = activeVersion;
    }
    console.log(pageLink.replace("VERSION", pageVersion));
    return pageLink.replace("VERSION", pageVersion); 
}

// navigate to page with active version
function navigateTo(pageName, pageVersion) {
    window.location.href = getPageLink(pageName, pageVersion);
}

// extract active version from url
function detemineActiveVersion() {
    var url = window.location.href;
    var version = activeVersion;
    console.log(url);
    if (url.endsWith('/beta') || url.includes('/beta/')) {
        version = 'beta';
    } else {
        var re = /\/(\d\.\d)($|\/)/g;
        var m = url.match(re);
        if (m) {
            version = m[0].replace(/\//g, '');
        }
    }
    if (version != undefined) {
        setActiveVersion(version)
    }
}
