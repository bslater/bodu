const sw = document.getElementById("switch-style"), b = document.body;

if (sw && b) {
    if (window.localStorage && localStorage.getItem("theme") === "theme-dark") {
        // Browser supports local storage and dark mode is configured.
        sw.checked = true;
    }
    else {
        // Browser does not support local storage, or no stored theme setting is
        // present. Determine dark mode based on browser settings. Browsers
        // which don't support media matching will default to light theme, in
        // line with what most people would expect from a web page.
        sw.checked = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    }

    b.classList.toggle("theme-dark", sw.checked)
    b.classList.toggle("theme-light", !sw.checked)

    sw.addEventListener("change", function () {
        b.classList.toggle("theme-dark", this.checked)
        b.classList.toggle("theme-light", !this.checked)

        if (window.localStorage) {
            localStorage.setItem("theme", this.checked ? "theme-dark" : "theme-light");
        }
    })
}

/* ----------------------------------------------------------------
 * Image lightbox — click to enlarge any in-article image.
 * ---------------------------------------------------------------- */
(function () {
    var MIN_DIMENSION = 64; // ignore inline icons / tiny images

    function init() {
        var overlay = document.createElement("div");
        overlay.className = "bodu-lightbox-overlay";
        overlay.setAttribute("role", "dialog");
        overlay.setAttribute("aria-modal", "true");
        overlay.setAttribute("aria-label", "Enlarged image");

        var img = document.createElement("img");
        img.alt = "";
        overlay.appendChild(img);

        var closeBtn = document.createElement("button");
        closeBtn.type = "button";
        closeBtn.className = "bodu-lightbox-close";
        closeBtn.setAttribute("aria-label", "Close enlarged image");
        closeBtn.innerHTML = "&times;";
        overlay.appendChild(closeBtn);

        document.body.appendChild(overlay);

        function open(src, alt, title) {
            img.src = src;
            img.alt = alt || "";
            if (title) { img.title = title; } else { img.removeAttribute("title"); }
            overlay.classList.add("is-open");
            document.body.classList.add("bodu-lightbox-locked");
        }

        function close() {
            overlay.classList.remove("is-open");
            document.body.classList.remove("bodu-lightbox-locked");
            img.removeAttribute("src");
        }

        function isZoomable(el) {
            if (!el || el.tagName !== "IMG") { return false; }
            if (el.classList.contains("no-zoom")) { return false; }
            if (!el.closest("article")) { return false; }
            if (el.naturalWidth && el.naturalWidth < MIN_DIMENSION) { return false; }
            if (el.naturalHeight && el.naturalHeight < MIN_DIMENSION) { return false; }
            return true;
        }

        document.addEventListener("click", function (e) {
            var target = e.target;
            if (!isZoomable(target)) { return; }
            e.preventDefault();
            open(target.currentSrc || target.src, target.alt, target.title);
        });

        overlay.addEventListener("click", function (e) {
            if (e.target === overlay || e.target === closeBtn) { close(); }
        });

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && overlay.classList.contains("is-open")) { close(); }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
