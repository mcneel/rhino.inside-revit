---
title: Rhino.Inside.Revit Community
toc: false
---

<!-- {% capture community_note %}
Welcome to the {{ site.terms.rir }} community. On this page, you will find the resources created by, and for the community. Please see the [Discussion Forums]({{ site.forum_url }}){: target='_blank'} to discuss features and potential issues and ask questions
{% endcapture %}
{% include ltr/bubble_note.html note=community_note %} -->

<div id="discoverGallery">
    <div class="discover-filters-box">
        <ul class="discover-filters">
            <li class="discover-filter" v-for="kind in discoverKinds" v-on:click="filterCardsByKind" v-bind:kind="(( kind.cardType ))">(( kind.title ))</li>
            <input class="discover-search" type="text" placeholder="search..." v-model.trim="keyword">
        </ul>
    </div>
    <div class="discover-grid">
        <div class="discover-item" v-for="card in discoverCards" >
            <a v-bind:href="card.url" target="blank">
                <div class="gallery-thumbnail gallery-thumbnail-dim">
                    <img class="gallery-img no-popup" v-bind:src="card.thumbnail" />
                </div>
            </a>
            <div class="gallery-info">
                <a class="gallery-info-title" v-bind:href="card.url">(( card.title ))</a>
                <a class="gallery-info-author" v-bind:href="card.authorUrl">(( card.author ))</a>
                <div class="gallery-info-subtitle">
                    (( card.subtitle ))
                </div>
                <ul class="discover-info-tags">
                    <li v-for="tag in card.tags" v-on:click="filterCardsByTag" v-bind:tag="(( tag ))">(( tag ))</li>
                </ul>
                <div class="gallery-info-extra">
                    (( card.description ))
                </div>
            </div>
        </div>
    </div>
</div>


<script>

    const siteUrl = "{{ site.baseurl }}";

    function attachDiscoverItemHover() {
        $(".discover-item").hover(function(){
            $(this).addClass("discover-item-focused");
            $(this).find(".gallery-thumbnail").removeClass("gallery-thumbnail-dim");
            $(this).find(".gallery-info-extra").css("display", "block");
        }, function(){
            $(this).removeClass("discover-item-focused");
            $(this).find(".gallery-thumbnail").addClass("gallery-thumbnail-dim");
            $(this).find(".gallery-info-extra").css("display", "none");
        });
    };

    async function getDiscoverCards(filter) {
        const res = await fetch('http://127.0.0.1:4000/rhino.inside-revit/static/data/discover.json');
        if (res.ok) {
            return await res.json();
        }
    };

    Vue.options.delimiters = ['((', '))'];

    getDiscoverCards().then((cards) => {
        // cleanup the links
        cards.forEach((c) => {
            // if (!c.url.startsWith("http")) {
            //     c.url = siteUrl + c.url;
            // }
            if (!c.thumbnail.startsWith("http")) {
                c.thumbnail = siteUrl + c.thumbnail;
            }
        });

        var app = new Vue({
            el: '#discoverGallery',
            data: {
                baseUrl: siteUrl,
                keyword: null,
                cardKind: null,
                discoverKinds: [{
                    title: "All",
                    cardType: "all"
                },{
                    title: "Featured",
                    cardType: "featured"
                },{
                    title: "Examples",
                    cardType: "example"
                },{
                    title: "Blog Posts",
                    cardType: "blogpost"
                }, {
                    title: "Podcasts",
                    cardType: "podcast"
                }, {
                    title: "Videos",
                    cardType: "video"
                }, {
                    title: "Courses",
                    cardType: "course"
                }, {
                    title: "Workshops",
                    cardType: "workshop"
                }],
                allCards: cards,
                discoverCards: cards
            },
            methods: {
                filterCardsByKind: function (event) {
                    let kind = event.target.getAttribute('kind');
                    this.keyword = 'kind: ' + kind;
                    $('.discover-filter').removeClass('discover-filter-active');
                    $(event.target).addClass('discover-filter-active');
                },
                filterCardsByTag:  function(event) {
                    let tag = event.target.getAttribute('tag');
                    this.keyword = 'tag: ' + tag;
                    $('.discover-filter').removeClass('discover-filter-active');
                }
            },
            watch: {
                keyword: function(val) {
                    var allCs = this.allCards;
                    let kwd = val.toLowerCase()
                    if (kwd.startsWith('kind:')) {
                        kwd = kwd.replace('kind:', '').trim();
                        if (kwd == 'featured') {
                            this.discoverCards = allCs.filter(
                                (c) => c.featured
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
                    }
                    else if (kwd.startsWith('tag:')) {
                        kwd = kwd.replace('tag:', '').trim();
                        if (kwd != '') {
                            this.discoverCards = allCs.filter(
                                (c) => c.tags.includes(kwd)
                            );
                        } else {
                            this.discoverCards = allCs;
                        }
                    }
                    else {
                        this.discoverCards = allCs.filter(
                            (c) => c.title.toLowerCase().includes(kwd)
                                || c.author.toLowerCase().includes(kwd)
                                || c.description.toLowerCase().includes(kwd)
                            );
                    }
                }
            },
            mounted:function() {
                $('.discover-filter').each(function() {
                    var thisObj = $(this);
                    if (thisObj.attr('kind') == 'all') {
                        thisObj.addClass('discover-filter-active');
                    }
                });
        
                attachDiscoverItemHover();
            },
            updated: function() {
                this.$nextTick(function() {
                    attachDiscoverItemHover();
                })
            }

        });
    });
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
