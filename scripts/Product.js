var ProductJs = (function () {

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
        $ApiRequest('Product/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('pd-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('pd-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('pd-open');
        }, 3200);
    }

    function openNav() {
        el('pd-nav').classList.add('pd-open');
        el('pd-scrim').classList.add('pd-open');
    }

    function closeNav() {
        el('pd-nav').classList.remove('pd-open');
        el('pd-scrim').classList.remove('pd-open');
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
        if (!askBox || !askBox.classList.contains('pd-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('pd-open');
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
        askBox.className = 'pd-ask';
        askBox.innerHTML = '<div class="pd-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="pd-err" role="alert"></em><div class="pd-askacts"><button type="button" class="pd-btn pd-btn-ghost pd-btn-sm"></button><button type="button" class="pd-btn pd-btn-sm"></button></div></div>';
        (el('pd-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('pd-open')) {
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
        x.b[1].className = 'pd-btn pd-btn-sm' + (o.danger ? ' pd-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('pd-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('pd-open');
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
        el('pd-site').setAttribute('data-theme', theme === 'dark' ? 'dark' : 'light');
    }

    function toggleTheme() {
        var next = el('pd-site').getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
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
        var c = el('pd-ccount');
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
        if (window.ProductJscompared) {
            window.ProductJscompared(id, on);
        }
    }

    function subscribe() {
        call('Subscribe', [{ key: 'nlmail', vlu: val('pd-nlmail') }]);
        return false;
    }

    function subscribed(msg) {
        el('pd-nlmail').value = '';
        toast(msg);
    }

    function fieldErr(id, msg) {
        var e = el('pd-e-' + id);
        if (e) {
            e.textContent = msg;
        }
        var f = el('pd-' + id);
        if (f && f.focus) {
            f.focus();
        }
    }

    function pane(root, k) {
        var box = typeof root === 'string' ? el(root) : root;
        var tabs = box.querySelectorAll('.pd-tab');
        for (var i = 0; i < tabs.length; i++) {
            tabs[i].classList.toggle('pd-act', tabs[i].getAttribute('data-p') === k);
            tabs[i].setAttribute('aria-selected', tabs[i].getAttribute('data-p') === k ? 'true' : 'false');
        }
        var ps = box.querySelectorAll('.pd-panes > div');
        for (var j = 0; j < ps.length; j++) {
            ps[j].classList.toggle('pd-act', ps[j].getAttribute('data-p') === k);
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
        [].forEach.call(document.querySelectorAll('form.pd-find select[name="make"]'), function (s) {
            fmake(s);
            s.addEventListener('change', function () {
                fmake(s);
            });
        });
    }

    var photos = [];
    var at = 0;
    var lbBack = null;
    var touchX = null;
    var qBack = null;

    function paint() {
        var p = photos[at];
        if (!p) {
            return;
        }
        var img = el('pd-lbimg');
        var vid = el('pd-lbvid');
        vid.pause();
        if (p.v) {
            img.hidden = true;
            vid.hidden = false;
            vid.poster = p.t;
            vid.src = p.f;
            vid.play().catch(function () {
            });
        } else {
            vid.hidden = true;
            vid.removeAttribute('src');
            img.hidden = false;
            img.src = p.f;
            img.alt = p.c;
        }
        el('pd-lbn').textContent = (at + 1) + ' / ' + photos.length;
        el('pd-lbc').textContent = p.c;
        var list = el('pd-lbthumbs').children;
        for (var i = 0; i < list.length; i++) {
            list[i].classList.toggle('pd-act', i === at);
            list[i].setAttribute('aria-current', i === at ? 'true' : 'false');
        }
    }

    function show(i) {
        if (!photos.length) {
            return;
        }
        lbBack = document.activeElement;
        at = Math.max(0, Math.min(i || 0, photos.length - 1));
        el('pd-lb').hidden = false;
        document.documentElement.style.overflow = 'hidden';
        paint();
        el('pd-lb').querySelector('.pd-lbx').focus();
    }

    function hide() {
        el('pd-lbvid').pause();
        el('pd-lb').hidden = true;
        document.documentElement.style.overflow = '';
        if (lbBack && lbBack.focus) {
            lbBack.focus();
        }
    }

    function step(d) {
        at = (at + d + photos.length) % photos.length;
        paint();
    }

    function pick(i) {
        at = i;
        paint();
    }

    function share() {
        var url = location.href;
        var t = document.title;
        if (navigator.share) {
            navigator.share({ title: t, url: url }).catch(function () {
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

    function send() {
        el('pd-asend').disabled = true;
        var list = [];
        ['aname', 'amail', 'aphone', 'amsg', 'aweb', 'apid'].forEach(function (k) {
            list.push({ key: k, vlu: val('pd-' + k) });
        });
        call('Send', list);
        return false;
    }

    function sent(ok, text) {
        el('pd-asend').disabled = false;
        if (!ok) {
            return;
        }
        el('pd-af').hidden = true;
        el('pd-adone').innerHTML = '<div class="pd-ok-box" role="status"><b>Question sent</b><p></p></div>';
        el('pd-adone').querySelector('p').textContent = text;
    }

    function quote() {
        qBack = document.activeElement;
        el('pd-qm').hidden = false;
        document.documentElement.style.overflow = 'hidden';
        setTimeout(function () {
            var f = el('pd-qname');
            if (f && window.innerWidth > 640) {
                f.focus();
            }
        }, 30);
    }

    function quoteClose() {
        el('pd-qm').hidden = true;
        document.documentElement.style.overflow = '';
        if (qBack && qBack.focus) {
            qBack.focus();
        }
    }

    function quoteSend() {
        el('pd-qsend').disabled = true;
        var list = [];
        ['qname', 'qcomp', 'qmail', 'qphone', 'qveh', 'qstate', 'qmsg', 'qweb', 'qpid', 'qqty'].forEach(function (k) {
            list.push({ key: k, vlu: val('pd-' + k) });
        });
        call('QuoteSend', list);
        return false;
    }

    function quoted(ok, text) {
        el('pd-qsend').disabled = false;
        if (!ok) {
            return;
        }
        el('pd-qf').hidden = true;
        el('pd-qdone').innerHTML = '<div class="pd-ok-box" role="status"><b>Quote request sent</b><p></p></div><div class="pd-hero-b"><button type="button" class="pd-btn pd-btn-ghost" onclick="ProductJs.quoteClose()">Close</button></div>';
        el('pd-qdone').querySelector('p').textContent = text;
    }

    function start() {
        var g = el('pd-gal');
        if (!g) {
            return;
        }
        try {
            photos = JSON.parse(g.getAttribute('data-photos') || '[]');
        } catch (e) {
            photos = [];
        }
        var th = '';
        photos.forEach(function (p, i) {
            th += '<button type="button" aria-label="' + (p.v ? 'Video' : 'Photo') + ' ' + (i + 1) + '" onclick="ProductJs.pick(' + i + ')"><img src="' + esc(p.t) + '" alt="" loading="lazy">' + (p.v ? '<span class="pd-gplay"></span>' : '') + '</button>';
        });
        el('pd-lbthumbs').innerHTML = th;
        var lb = el('pd-lb');
        lb.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                e.stopPropagation();
                hide();
            } else if (e.key === 'ArrowLeft') {
                step(-1);
            } else if (e.key === 'ArrowRight') {
                step(1);
            }
        });
        var stage = lb.querySelector('.pd-lbstage');
        stage.addEventListener('touchstart', function (e) {
            touchX = e.touches[0].clientX;
        }, { passive: true });
        stage.addEventListener('touchend', function (e) {
            if (touchX == null) {
                return;
            }
            var dx = e.changedTouches[0].clientX - touchX;
            touchX = null;
            if (Math.abs(dx) > 40) {
                step(dx < 0 ? 1 : -1);
            }
        });
        stage.addEventListener('click', function (e) {
            if (e.target === stage) {
                hide();
            }
        });
        var qm = el('pd-qm');
        qm.addEventListener('mousedown', function (e) {
            if (e.target === qm) {
                quoteClose();
            }
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !qm.hidden) {
                quoteClose();
            }
        });
        if (location.hash === '#quote') {
            quote();
        }
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
            var id = e.target && e.target.id ? e.target.id.replace('pd-', 'pd-e-') : '';
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
        show: show,
        hide: hide,
        step: step,
        pick: pick,
        share: share,
        send: send,
        sent: sent,
        quote: quote,
        quoteClose: quoteClose,
        quoteSend: quoteSend,
        quoted: quoted
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', ProductJs.reveal);
} else {
    ProductJs.reveal();
}
