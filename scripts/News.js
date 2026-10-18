var NewsJs = (function () {

    var ttimer = null;
    var BASE = location.pathname.replace(/[^\/]*$/, '');

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function call(method, list) {
        $ApiRequest('News/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('nw-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('nw-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('nw-open');
        }, 3200);
    }

    function openNav() {
        el('nw-nav').classList.add('nw-open');
        el('nw-scrim').classList.add('nw-open');
    }

    function closeNav() {
        el('nw-nav').classList.remove('nw-open');
        el('nw-scrim').classList.remove('nw-open');
    }

    function ago(d) {
        var s = (Date.now() - d.getTime()) / 1000;
        if (s < 60) {
            return 'Just now';
        }
        if (s < 3600) {
            var m = Math.floor(s / 60);
            return m + (m === 1 ? ' min ago' : ' mins ago');
        }
        if (s < 86400) {
            var h = Math.floor(s / 3600);
            return h + (h === 1 ? ' hour ago' : ' hours ago');
        }
        return d.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
    }

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function times(root) {
        var list = (root || document).querySelectorAll('time[data-t]:not([data-x])');
        for (var i = 0; i < list.length; i++) {
            var t = list[i];
            t.setAttribute('data-x', '1');
            var d = new Date(t.getAttribute('datetime'));
            if (isNaN(d.getTime())) {
                continue;
            }
            var mode = t.getAttribute('data-t');
            if (mode === 'ago') {
                t.textContent = ago(d);
                t.title = d.toLocaleString();
            } else if (mode === 'hm') {
                t.textContent = d.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
            } else if (mode === 'full') {
                t.textContent = d.toLocaleString(undefined, { weekday: 'short', day: 'numeric', month: 'long', year: 'numeric', hour: 'numeric', minute: '2-digit' });
            } else if (mode === 'day') {
                t.textContent = d.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
            }
        }
    }

    var askBox = null;
    var askDone = null;
    var askCheck = null;
    var askBack = null;

    function askParts() {
        return { h: askBox.querySelector('h2'), p: askBox.querySelector('p'), i: askBox.querySelector('input'), e: askBox.querySelector('em'), b: askBox.querySelectorAll('button') };
    }

    function askClose(ok) {
        if (!askBox || !askBox.classList.contains('nw-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('nw-open');
        askDone = null;
        askCheck = null;
        if (askBack && askBack.focus && document.body.contains(askBack)) {
            askBack.focus();
        }
        askBack = null;
        if (ok && f) {
            f(v);
        }
    }

    function askOk() {
        var x = askParts();
        if (askCheck) {
            var m = askCheck(x.i.value.trim());
            if (m) {
                x.e.textContent = m;
                x.i.focus();
                return;
            }
        }
        askClose(true);
    }

    function askBuild() {
        askBox = document.createElement('div');
        askBox.className = 'nw-ask';
        askBox.innerHTML = '<div class="nw-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="nw-err" role="alert"></em><div class="nw-askacts"><button type="button" class="nw-btn nw-btn-ghost nw-btn-sm"></button><button type="button" class="nw-btn nw-btn-sm"></button></div></div>';
        (el('nw-site') || document.body).appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () {
            askClose(false);
        });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () {
            x.e.textContent = '';
        });
        askBox.addEventListener('mousedown', function (e) {
            if (e.target === askBox) {
                askClose(false);
            }
        });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('nw-open')) {
                return;
            }
            if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();
                askClose(false);
            } else if (e.key === 'Enter' && e.target !== x.b[0]) {
                e.preventDefault();
                e.stopPropagation();
                askOk();
            } else if (e.key === 'Tab') {
                var f = [x.i, x.b[0], x.b[1]].filter(function (n) {
                    return n.offsetParent !== null;
                });
                var at = f.indexOf(document.activeElement);
                e.preventDefault();
                f[(at + (e.shiftKey ? f.length - 1 : 1)) % f.length].focus();
            }
        }, true);
    }

    function ask(o, done) {
        if (!askBox) {
            askBuild();
        }
        var x = askParts();
        askBack = document.activeElement;
        askDone = done;
        askCheck = o.check || null;
        x.h.textContent = o.title || '';
        x.p.textContent = o.text || '';
        x.e.textContent = '';
        x.b[0].textContent = o.no || 'Cancel';
        x.b[1].textContent = o.ok || 'OK';
        x.b[1].className = 'nw-btn nw-btn-sm' + (o.danger ? ' nw-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('nw-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('nw-open');
        if (input) {
            x.i.focus();
            x.i.setSelectionRange(x.i.value.length, x.i.value.length);
        } else {
            x.b[o.danger ? 0 : 1].focus();
        }
    }

    function leave(go) {
        ask({ title: 'Leave without saving?', text: 'Your changes since the last save will be lost.', ok: 'Leave', no: 'Keep editing', danger: true }, go);
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function applyTheme(theme) {
        el('nw-site').setAttribute('data-theme', theme === 'dark' ? 'dark' : 'light');
    }

    function toggleTheme() {
        var next = el('nw-site').getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        applyTheme(next);
        try {
            localStorage.setItem('iv-theme', next);
        } catch (e) {
        }
    }

    function cmpList() {
        try {
            var v = JSON.parse(localStorage.getItem('iv-compare') || '[]');
            return Array.isArray(v) ? v.filter(function (x) {
                return typeof x === 'string' && /^[A-Za-z0-9-]{1,20}$/.test(x);
            }).slice(0, 3) : [];
        } catch (e) {
            return [];
        }
    }

    function cmpStore(list) {
        try {
            localStorage.setItem('iv-compare', JSON.stringify(list.slice(0, 3)));
            return true;
        } catch (e) {
            return false;
        }
    }

    function cmpCount() {
        var n = cmpList().length;
        var c = el('nw-ccount');
        if (c) {
            c.textContent = String(n);
            c.hidden = n === 0;
        }
    }

    function cmpButtons(root) {
        var s = cmpList();
        var list = (root || document).querySelectorAll('[data-cmp]');
        for (var i = 0; i < list.length; i++) {
            var on = s.indexOf(list[i].getAttribute('data-cmp')) >= 0;
            list[i].setAttribute('aria-pressed', on ? 'true' : 'false');
            list[i].title = on ? 'In your comparison' : 'Add to compare';
            var t = list[i].querySelector('span');
            if (t) {
                t.textContent = on ? 'Comparing' : 'Compare';
            }
        }
    }

    function cmp(b) {
        var id = b.getAttribute('data-cmp');
        var s = cmpList();
        var at = s.indexOf(id);
        var on = at < 0;
        if (on) {
            if (s.length >= 3) {
                toast('You can compare up to three products. Remove one first.');
                return;
            }
            s.push(id);
        } else {
            s.splice(at, 1);
        }
        if (!cmpStore(s)) {
            toast('Comparing needs browser storage, which is turned off here.');
            return;
        }
        cmpButtons();
        cmpCount();
        toast(on ? 'Added. Open the compare icon at the top to see them side by side.' : 'Removed from your comparison.');
        if (on) {
            call('Compared', [{ key: 'cid', vlu: id }]);
        }
        if (window.NewsJscompared) {
            window.NewsJscompared(id, on);
        }
    }

    function subscribe() {
        call('Subscribe', [{ key: 'nlmail', vlu: val('nw-nlmail') }]);
        return false;
    }

    function subscribed(msg) {
        el('nw-nlmail').value = '';
        toast(msg);
    }

    function fieldErr(id, msg) {
        var e = el('nw-e-' + id);
        if (e) {
            e.textContent = msg;
        }
        var f = el('nw-' + id);
        if (f && f.focus) {
            f.focus();
        }
    }

    function pane(root, k) {
        var box = typeof root === 'string' ? el(root) : root;
        var tabs = box.querySelectorAll('.nw-tab');
        for (var i = 0; i < tabs.length; i++) {
            tabs[i].classList.toggle('nw-act', tabs[i].getAttribute('data-p') === k);
            tabs[i].setAttribute('aria-selected', tabs[i].getAttribute('data-p') === k ? 'true' : 'false');
        }
        var ps = box.querySelectorAll('.nw-panes > div');
        for (var j = 0; j < ps.length; j++) {
            ps[j].classList.toggle('nw-act', ps[j].getAttribute('data-p') === k);
        }
    }

    function fmake(sel) {
        var f = sel.closest('form');
        var makes = [];
        try {
            makes = JSON.parse(f.getAttribute('data-makes') || '[]');
        } catch (e) {
        }
        var ms = f.querySelector('select[name="model"]');
        var cur = ms.getAttribute('data-cur') || '';
        var html = '<option value="">Any model</option>';
        makes.forEach(function (m) {
            if (m.key === sel.value) {
                m.models.forEach(function (x) {
                    html += '<option value="' + esc(x.key) + '"' + (x.key === cur ? ' selected' : '') + '>' + esc(x.name) + '</option>';
                });
            }
        });
        ms.innerHTML = html;
        ms.disabled = !sel.value;
        ms.setAttribute('data-cur', '');
    }

    function finders() {
        [].forEach.call(document.querySelectorAll('form.nw-find select[name="make"]'), function (s) {
            fmake(s);
            s.addEventListener('change', function () {
                fmake(s);
            });
        });
    }

    var curTag = '';
    var curPage = 1;

    function tag(t) {
        curTag = t;
        curPage = 1;
        try {
            history.replaceState(null, '', 'News' + (t ? '?tag=' + t : ''));
        } catch (e) {
        }
        call('Filter', [{ key: 'tag', vlu: curTag }, { key: 'pg', vlu: '1' }]);
    }

    function page(n) {
        curPage = n;
        call('Filter', [{ key: 'tag', vlu: curTag }, { key: 'pg', vlu: String(n) }]);
        var r = el('nw-list');
        window.scrollTo({ top: r.getBoundingClientRect().top + window.scrollY - 140, behavior: 'smooth' });
    }

    function share() {
        var url = location.href;
        if (navigator.share) {
            navigator.share({ title: document.title, url: url }).catch(function () {
            });
            return;
        }
        try {
            navigator.clipboard.writeText(url).then(function () {
                toast('Link copied.');
            }, function () {
                toast(url);
            });
        } catch (e) {
            toast(url);
        }
    }

    function start() {
        curTag = qs('tag');
    }

    function reveal() {
        var stored = '';
        try {
            stored = localStorage.getItem('iv-theme') || '';
        } catch (e) {
        }
        if (stored) {
            applyTheme(stored);
        }
        times();
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            [].forEach.call(document.querySelectorAll('video[data-bg]'), function (v) {
                v.removeAttribute('autoplay');
                v.pause();
            });
        }
        cmpButtons();
        cmpCount();
        finders();
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closeNav();
            }
        });
        document.addEventListener('input', function (e) {
            var id = e.target && e.target.id ? e.target.id.replace('nw-', 'nw-e-') : '';
            if (id && el(id)) {
                el(id).textContent = '';
            }
        });
        start();
    }

    return {
        reveal: reveal,
        toast: toast,
        openNav: openNav,
        closeNav: closeNav,
        toggleTheme: toggleTheme,
        cmp: cmp,
        subscribe: subscribe,
        subscribed: subscribed,
        fieldErr: fieldErr,
        pane: pane,
        tag: tag,
        page: page,
        share: share
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', NewsJs.reveal);
} else {
    NewsJs.reveal();
}
