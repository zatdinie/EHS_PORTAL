(function ($) {
    'use strict';

    // The 5 SPA-navigable routes. Each entry maps a URL path to a skeleton type.
    var SPA_ROUTES = [
        { path: '/dashboard',    skeleton: 'dashboard' },
        { path: '/vendor',       skeleton: 'list' },
        { path: '/briefing',     skeleton: 'list' },
        { path: '/registration', skeleton: 'list' },
        { path: '/eula',         skeleton: 'list' }
    ];

    // The content wrapper div in _Layout.cshtml (line 140).
    var CONTENT_SEL = '.body-content';

    // Minimum ms to show skeleton — prevents an ugly flash on fast connections.
    var MIN_MS = 250;

    // ── Skeleton builders ──────────────────────────────────────────────────

    function skelLine(w, h, extra) {
        return '<div class="skel-line" style="width:' + w + ';height:' + h + ';' + (extra || '') + '"></div>';
    }

    function skelCard(rows, cols) {
        var th = '', tb = '';
        for (var c = 0; c < cols; c++)
            th += '<th>' + skelLine((55 + c * 15) + 'px', '11px') + '</th>';
        for (var r = 0; r < rows; r++) {
            var tds = '';
            for (var cc = 0; cc < cols; cc++)
                tds += '<td>' + skelLine((50 + (r * cols + cc) * 17 % 80) + 'px', '13px') + '</td>';
            tb += '<tr>' + tds + '</tr>';
        }
        return '<div class="ehs-card">' +
            '<div class="ehs-card-head">' +
                skelLine('140px', '16px') +
                skelLine('160px', '28px', 'border-radius:6px;') +
            '</div>' +
            '<div class="table-responsive"><table class="ehs-table">' +
                '<thead><tr>' + th + '</tr></thead>' +
                '<tbody>' + tb + '</tbody>' +
            '</table></div></div>';
    }

    function skelDashboard() {
        var stats = [1, 2, 3, 4].map(function () {
            return '<div class="col-md-3"><div class="ehs-stat">' +
                skelLine('80px', '12px', 'margin-bottom:12px;') +
                skelLine('64px', '36px', 'margin-bottom:8px;') +
                skelLine('110px', '12px') +
                '</div></div>';
        }).join('');

        var feed = '<div class="ehs-card"><div class="ehs-card-head">' +
            skelLine('120px', '16px') + '</div>' +
            [1, 2, 3, 4, 5, 6].map(function (i) {
                return '<div style="display:flex;gap:10px;padding:10px 16px;border-bottom:0.5px solid var(--ehs-border-row);">' +
                    '<div class="skel-dot"></div><div style="flex:1;">' +
                    skelLine((110 + i * 20) + 'px', '12px', 'margin-bottom:6px;') +
                    skelLine('80px', '11px') + '</div></div>';
            }).join('') + '</div>';

        return '<div class="container mt-4 page-shell">' +
            '<div class="ehs-page-header mb-3">' +
                skelLine('180px', '28px', 'margin-bottom:8px;') +
                skelLine('260px', '14px') +
                skelLine('100px', '36px', 'border-radius:8px;') +
            '</div>' +
            '<div class="row g-3 mb-4">' + stats + '</div>' +
            '<div class="row g-3">' +
                '<div class="col-md-8">' + skelCard(6, 4) + '</div>' +
                '<div class="col-md-4">' + feed + '</div>' +
            '</div></div>';
    }

    function skelList() {
        return '<div class="container mt-4 page-shell">' +
            '<div class="ehs-page-header mb-3">' +
                skelLine('180px', '28px', 'margin-bottom:8px;') +
                skelLine('260px', '14px') +
                skelLine('100px', '36px', 'border-radius:8px;') +
            '</div>' +
            skelCard(8, 5) + '</div>';
    }

    // ── Route matching ─────────────────────────────────────────────────────
    //
    // TODO(human): Implement matchRoute(pathname).
    //
    // This function receives a URL pathname like "/Vendor/Index" or "/dashboard"
    // and must decide: is this one of our 5 SPA routes?
    //
    // Return the matching route object from SPA_ROUTES if it's a match,
    // or null if it should do a regular full-page navigation.
    //
    // Rules to handle:
    //   "/vendor"        → match  (direct controller path)
    //   "/vendor/"       → match  (trailing slash variant)
    //   "/vendor/index"  → match  (explicit action name)
    //   "/vendor/edit/5" → NO match (sub-page, must full-page load)
    //   "/dashboard"     → match
    //
    // Hints:
    //   - Normalise with .toLowerCase() and .replace(/\/$/, '') (removes trailing slash)
    //   - Loop over SPA_ROUTES and compare r.path to the normalised pathname
    //   - A match is: exact path OR path + "/index"
    //
    function matchRoute(pathname) {
        var p = pathname.toLowerCase().replace(/\/$/, '');
        for (var i = 0; i < SPA_ROUTES.length; i++) {
            var r = SPA_ROUTES[i];
            if (p === r.path || p === r.path + '/index')
                return r;
        }
        return null;
    }

    // ── Active nav highlight ───────────────────────────────────────────────

    function updateActiveNav(pathname) {
        var p = pathname.toLowerCase();
        $('.inari-navbar .nav-link').each(function () {
            var href = ($(this).attr('href') || '').toLowerCase().replace(/\/$/, '');
            var active = false;
            for (var i = 0; i < SPA_ROUTES.length; i++) {
                if (href.indexOf(SPA_ROUTES[i].path) === 0 && p.indexOf(SPA_ROUTES[i].path) === 0) {
                    active = true;
                    break;
                }
            }
            $(this).toggleClass('active', active);
        });
    }

    // ── Script re-initialisation ───────────────────────────────────────────

    function reinitScripts(container) {
        // jQuery's .html() does NOT run <script> tags for security.
        // We manually find and re-execute them after each content swap.
        $(container).find('script').each(function () {
            if (this.src) {
                var s = document.createElement('script');
                s.src = this.src;
                document.head.appendChild(s);
            } else {
                try { window.eval(this.textContent); }
                catch (e) { console.warn('[spa-nav] script error:', e); }
            }
        });
    }

    // ── Core navigation ────────────────────────────────────────────────────

    var _busy = false;

    function navigate(url, push) {
        if (_busy) return;

        var pathname = url.split('?')[0].split('#')[0];
        var route = matchRoute(pathname);

        // Not a SPA route — fall back to full page load.
        if (!route) { window.location.href = url; return; }

        _busy = true;
        var $c = $(CONTENT_SEL);
        var t0 = Date.now();

        // Show skeleton immediately while fetch is in-flight.
        $c.html(route.skeleton === 'dashboard' ? skelDashboard() : skelList());
        updateActiveNav(pathname);

        $.ajax({
            url: url,
            type: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (html) {
                // Session expired: Forms Auth silently redirects to login page with 200.
                // jqXHR doesn't expose responseURL, so detect by login page content instead.
                if (html.indexOf('Admin Login') !== -1) {
                    window.location.reload();
                    _busy = false;
                    return;
                }
                // Enforce a minimum skeleton display time so it doesn't flash.
                var wait = Math.max(0, MIN_MS - (Date.now() - t0));
                setTimeout(function () {
                    $c.html(html);
                    reinitScripts($c[0]);
                    if (push !== false) history.pushState({ url: url }, '', url);
                    var m = html.match(/data-page-title="([^"]+)"/);
                    if (m) document.title = m[1] + ' \u2014 EHS Agreement System';
                    window.scrollTo(0, 0);
                    _busy = false;
                }, wait);
            },
            error: function () {
                // Session expired / server error — full page load handles redirect.
                window.location.href = url;
                _busy = false;
            }
        });
    }

    // ── Event wiring ───────────────────────────────────────────────────────

    // Intercept clicks on the 5 main nav links only.
    $(document).on('click', '.inari-navbar .nav-link', function (e) {
        var href = $(this).attr('href');
        if (!href || href === '#') return;
        // Let Ctrl/Cmd/Shift+click open new tabs normally.
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.which === 2) return;
        var pathname = href.split('?')[0].split('#')[0];
        if (!matchRoute(pathname)) return;
        e.preventDefault();
        navigate(href, true);
    });

    // Handle browser Back / Forward buttons.
    window.addEventListener('popstate', function (e) {
        var url = (e.state && e.state.url) ? e.state.url : window.location.href;
        var pathname = url.split('?')[0].split('#')[0];
        if (matchRoute(pathname)) navigate(url, false);
        else window.location.reload();
    });

    // On first load, register this page in history so popstate works
    // correctly when the user navigates back from a sub-page.
    (function () {
        var p = window.location.pathname;
        if (matchRoute(p))
            history.replaceState({ url: window.location.href }, '', window.location.href);
        updateActiveNav(p);
    }());

}(jQuery));
