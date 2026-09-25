// ---------------------------------------------------------------------------------------------------------------
// darkerfx template extension: render fenced ```mermaid code blocks as SVG diagrams.
//
// DocFX's classic "default" template loads this styles/main.js on every page (via partials/scripts.tmpl.partial).
// Markdig emits a ```mermaid fence as <pre><code class="lang-mermaid">…</code></pre>; we lift each block's text into a
// <div class="mermaid"> and let Mermaid render it to inline SVG. Mermaid is imported lazily from a pinned CDN only when
// a page actually contains a diagram, so pages without one pay nothing and the DocFX build needs no network access.
// ---------------------------------------------------------------------------------------------------------------

(function () {
    "use strict";

    var MERMAID_SRC = "https://cdn.jsdelivr.net/npm/mermaid@10.9.1/dist/mermaid.esm.min.mjs";

    // The darkerfx theme marks dark mode with a `theme-dark` class on <body> (see toggle-theme.js).
    function isDarkTheme() {
        return document.body && document.body.classList.contains("theme-dark");
    }

    // Replace each <pre><code class="lang-mermaid"> with a <div class="mermaid"> carrying the raw diagram source.
    // Returns the number of diagrams prepared.
    function prepareDiagrams() {
        var codes = document.querySelectorAll("code.lang-mermaid");
        for (var i = 0; i < codes.length; i++) {
            var code = codes[i];
            var host = (code.parentElement && code.parentElement.tagName === "PRE") ? code.parentElement : code;
            var div = document.createElement("div");
            div.className = "mermaid";
            // textContent is already entity-decoded, so arrows like --> survive intact.
            div.textContent = code.textContent;
            host.parentNode.replaceChild(div, host);
        }
        return codes.length;
    }

    function renderDiagrams() {
        if (!prepareDiagrams()) {
            return;
        }

        import(MERMAID_SRC)
            .then(function (module) {
                var mermaid = module.default;
                mermaid.initialize({
                    startOnLoad: false,
                    // The diagram sources are authored in this repo's own markdown (never user input), so "loose" is
                    // safe and guarantees <br/> line breaks in labels render rather than being escaped.
                    securityLevel: "loose",
                    theme: isDarkTheme() ? "dark" : "neutral"
                });
                return mermaid.run({ querySelector: "div.mermaid" });
            })
            .catch(function (error) {
                // A CDN or render failure must not break the page; leave the source visible and log for diagnosis.
                console.error("Mermaid diagram rendering failed:", error);
            });
    }

    // Only touch pages that actually carry a diagram.
    function boot() {
        if (document.querySelector("code.lang-mermaid")) {
            renderDiagrams();
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();
