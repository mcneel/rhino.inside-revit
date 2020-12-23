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
            <li v-for="kind in discoverKinds" v-on:click="filterCardsByKind" v-bind:kind="(( kind.id ))">(( kind.title ))</li>
            <input class="discover-search" type="text" placeholder="search..." name="search">
        </ul>
    </div>
    <div class="gallery-large-grid">
        <div v-for="card in discoverCards" class="gallery-item">
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
                <div class="gallery-info-extra">
                    (( card.description ))
                </div>
            </div>
        </div>
    </div>
</div>


<script>
    async function getDiscoverCards(filter) {
        const res = await fetch(`http://127.0.0.1:4000/rhino.inside-revit/static/data/discover.json`);
        if (res.ok) {
            return await res.json();
        }
    };

    Vue.options.delimiters = ['((', '))'];

    getDiscoverCards().then((cards) => {
        var app = new Vue({
            el: '#discoverGallery',
            data: {
                filter: null,
                discoverKinds: [{
                    title: "All",
                    id: null
                },{
                    title: "Featured",
                    id: "featured"
                },{
                    title: "Blog Posts",
                    id: "blogpost"
                }, {
                    title: "Podcasts",
                    id: "podcast"
                }, {
                    title: "Videos",
                    id: "video"
                }, {
                    title: "Courses",
                    id: "course"
                }],
                allCards: cards,
                discoverCards: cards
            },
            methods: {
                filterCardsByKind: function (event) {
                    let cardKind = event.target.getAttribute(`kind`);
                    var allCs = this.allCards;
                    console.log(`filtering to ${cardKind}`);
                    if (cardKind != null) {
                        this.discoverCards = allCs.filter((c) => c.type == cardKind);
                    } else {
                        this.discoverCards = allCs;
                    }
                }
            }
        });
    })
</script>

## Contributing to Community

The {{ site.terms.rir }} community appreciates and benefits from your contributions. In case you have created videos, articles, blog posts, etc or have developed custom scripted components for {{ site.terms.rir }} you can share your creations with the community in a number of ways. Prepare a package containing links and other resources, and

- Share on the [Discussion Forums]({{ site.forum_url }}){: target='_blank'}
<!-- Email link here is obfuscated. See Wiki docs for guidelines -->
- Email to <a href="#" data-dump="bWFpbHRvOnJoaW5vLmluc2lkZS5yZXZpdEBtY25lZWwuY29tP3N1YmplY3Q9Q29tbXVuaXR5IFN1Ym1pc3Npb24=" onfocus="this.href = atob(this.dataset.dump)">{{ site.terms.rir }} Development/Wiki Team</a>
- Make changes to the Wiki and submit a pull request. [See Instructions Here]({{ site.metawiki_url | prepend: site.repo_url }})

