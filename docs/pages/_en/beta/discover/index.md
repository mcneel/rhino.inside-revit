---
title: Rhino.Inside.Revit Community
toc: false
---

<div id="discoverGallery">
    <div class="discover-filters-box">
        <ul class="discover-filters">
            <li class="discover-filter" v-for="kind in discoverKinds" v-on:click="filterCardsByKind" v-bind:kind="(( kind.keyword ))">(( kind.title ))</li>
            <input class="discover-search" type="text" placeholder="search..." v-model.trim="keyword">
        </ul>
    </div>
    <div class="discover-grid">
        <div class="discover-item" v-for="card in discoverCards" v-bind:class="{ discoverItemHighlight: card.highlight }">
            <a v-bind:href="card.url" target="blank">
                <div class="discover-thumbnail discover-thumbnail-dim">
                    <img class="discover-img no-popup" v-bind:src="card.thumbnail" />
                </div>
            </a>
            <div class="discover-info">
                <a class="discover-info-title" v-bind:href="card.url" target="blank">(( card.title ))</a>
                <a class="discover-info-author" v-bind:href="card.authorUrl" target="blank">(( card.author ))</a>
                <div class="discover-info-subtitle" >
                    (( card.subtitle ))
                </div>
                <ul class="discover-info-tags">
                    <li v-for="tag in card.tags" v-on:click="filterCardsByTag" v-bind:tag="(( tag ))">(( tag ))</li>
                </ul>
                <div class="discover-info-extra" v-html="card.description"></div>
            </div>
            <svg v-if="card.highlight" class="discover-highlight" height="16" width="16"><polygon points="16,0 16,16 0,16" style="fill:black" /></svg>
        </div>
    </div>
</div>


<script>
    const urlParams = new URLSearchParams(window.location.search);
    const proxyBaseUrl = "/";
    const siteUrl = "{{ site.baseurl }}";

    function attachDiscoverItemHover() {
        $(".discover-item").hover(function(){
            $(this).addClass("discover-item-focused");
            $(this).find(".discover-thumbnail").removeClass("discover-thumbnail-dim");
            $(this).find(".discover-info-extra").css("display", "block");
        }, function(){
            $(this).removeClass("discover-item-focused");
            $(this).find(".discover-thumbnail").addClass("discover-thumbnail-dim");
            $(this).find(".discover-info-extra").css("display", "none");
        });
    };

    function fixRelativeUrls(href) {
        if (window.location.href.includes('127.0.0.1')) {
            return siteUrl  + href;
        }
        return proxyBaseUrl + href;
    }

    async function getDiscoverCards(filter) {
        var dataUrl = '/' + 'static' + '/' + 'data' + '/' + 'discover.json';
        dataUrl = fixRelativeUrls(dataUrl);

        console.log(`fetching from ${dataUrl}`)
        const res = await fetch(dataUrl);
        if (res.ok) {
            return await res.json();
        }
    };

    Vue.options.delimiters = ['((', '))'];

    var app;

    getDiscoverCards().then((cards) => {
        // cleanup the links
        cards.forEach((c) => {
            if (!c.thumbnail.startsWith("http")) {
                c.thumbnail = fixRelativeUrls(c.thumbnail);
            }

            c.description = markdown.toHTML(c.description);
        });

        app = new Vue({
            el: '#discoverGallery',
            data: {
                keyword: '',
                pushNewState: true,
                discoverKinds: [{
                    title: "All",
                    keyword: "all"
                }, {
                    title: "Featured",
                    keyword: "featured"
                }, {
                    title: "Courses",
                    keyword: "course"
                }, {
                    title: "Tutorials",
                    keyword: "tutorial"
                }, {
                    title: "Examples",
                    keyword: "example"
                }, {
                    title: "Podcasts",
                    keyword: "podcast"
                }, {
                    title: "Articles",
                    keyword: "article"
                }, {
                    title: "Videos",
                    keyword: "video"
                }],
                allCards: cards,
                discoverCards: cards
            },
            methods: {
                filterCardsByKind: function (event) {
                    let kind = event.target.getAttribute('kind');
                    this.keyword = 'kind: ' + kind;
                },
                filterCardsByTag:  function(event) {
                    let tag = event.target.getAttribute('tag');
                    this.keyword = 'tag: ' + tag;
                }
            },
            watch: {
                keyword: function(val) {
                    var allCs = this.allCards;
                    let origKwd = val.toLowerCase();
                    const pushState = this.pushNewState;

                    if (origKwd.startsWith('kind:')) {
                        kwd = origKwd.replace('kind:', '').trim();
                        if (kwd == 'featured') {
                            this.discoverCards = allCs.filter(
                                (c) => c.highlight
                            );
                        }
                        else if (kwd != 'all') {
                            this.discoverCards = allCs.filter(
                                (c) => c.kinds.includes(kwd)
                            );
                        }
                        else {
                            this.discoverCards = allCs;
                        }

                        $('.discover-filter').removeClass('discover-filter-active');
                        $('.discover-filter').each(function() {
                            var df = $(this);
                            if (df.attr('kind') == kwd) {
                                df.addClass('discover-filter-active');
                                if (pushState) {
                                history.pushState({filterType: 'kind', keyword: origKwd}, `Kind: ${df.text()}`, `?keyword=${origKwd}`);
                                }
                            }
                        });
                    }
                    else if (origKwd.startsWith('tag:')) {
                        kwd = origKwd.replace('tag:', '').trim();
                        if (kwd != '') {
                            this.discoverCards = allCs.filter(
                                (c) => c.tags.includes(kwd)
                            );
                        } else {
                            this.discoverCards = allCs;
                        }

                        $('.discover-filter').removeClass('discover-filter-active');
                        if (pushState) {
                            history.pushState({filterType: 'tag', keyword: origKwd}, `Tag: ${kwd}`, `?keyword=${origKwd}`);
                        }
                    }
                    else {
                        this.discoverCards = allCs.filter(
                            (c) => c.title.toLowerCase().includes(origKwd)
                                || c.author.toLowerCase().includes(origKwd)
                                || c.description.toLowerCase().includes(origKwd)
                            );

                        $('.discover-filter').removeClass('discover-filter-active');
                        if (pushState) {
                            if (history.state && history.state.filterType != 'text') {
                                history.pushState({filterType: 'text', keyword: origKwd}, `Search: ${origKwd}`, `?keyword=${origKwd}`);
                            }
                            else {
                                history.replaceState({filterType: 'text', keyword: origKwd}, `Search: ${origKwd}`, `?keyword=${origKwd}`);
                            }
                        }
                    }

                    this.pushNewState = true;
                }
            },
            mounted:function() {
                const urlKeyword = urlParams.get('keyword');
                this.keyword =  urlKeyword ? urlKeyword: "kind: all";
                attachDiscoverItemHover();
            },
            updated: function() {
                this.$nextTick(function() {
                    attachDiscoverItemHover();
                })
            }

        });
    });

    window.onpopstate = function(event) {
        app.pushNewState = false;
        app.keyword = event.state.keyword;
    };

</script>

## Contributing to Community

The {{ site.terms.rir }} community appreciates and benefits from your contributions. In case you have created videos, articles, blog posts, etc or have developed custom scripted components for {{ site.terms.rir }} you can share your creations with the community in a number of ways. Prepare a package containing links and other resources, and

- Share on the [Discussion Forums]({{ site.forum_url }}){: target='_blank'}
<!-- Email link here is obfuscated. See Wiki docs for guidelines -->
- Email to <a href="#" data-dump="bWFpbHRvOnJoaW5vLmluc2lkZS5yZXZpdEBtY25lZWwuY29tP3N1YmplY3Q9Q29tbXVuaXR5IFN1Ym1pc3Npb24=" onfocus="this.href = atob(this.dataset.dump)">{{ site.terms.rir }} Development/Wiki Team</a>
- Make changes to the Wiki and submit a pull request. [See Instructions Here]({{ site.metawiki_url | prepend: site.repo_url }})

## Featuring Your Discoveries

Each feature page listed here, is a self-contained article on a specific topic. They also might have a ZIP package attached that includes one or more files (e.g. Sample Screenshot `*.png`, Grasshopper Definition `*.gh`, Rhino Model `*.3dm`, Revit Model `*.rvt`, Revit Family `*.rfa`)

Visitors can download the archive for each article by clicking on the download button included on the page. You can create your own articles, following a similar format, and send us the markdown file of the article, plus all the images and attachments in a package and we can add them to this page. You can also follow the [wiki guidelines]({{ site.metawiki_url | prepend: site.repo_url }}) and submit a PR with your content.
