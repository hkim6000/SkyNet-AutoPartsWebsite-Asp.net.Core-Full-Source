var AdminQuotesJs = (function () {

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
        $ApiRequest('AdminQuotes/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('qt-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('qt-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('qt-open');
        }, 3200);
    }

    function openNav() {
        el('qt-nav').classList.add('qt-open');
        el('qt-scrim').classList.add('qt-open');
    }

    function closeNav() {
        el('qt-nav').classList.remove('qt-open');
        el('qt-scrim').classList.remove('qt-open');
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

    var stimer = null;
    var otimer = null;

    function api(method, list) {
        list = list || [];
        list.push({ key: 'tok', vlu: getCookie('iv_tok') });
        call(method, list);
    }

    function expired() {
        setCookie('iv_tok', '', 0);
        location.replace('AdminLogin');
    }

    function signOut() {
        api('SignOut', []);
    }

    function out() {
        setCookie('iv_tok', '', 0);
        location.href = 'AdminLogin';
    }

    function openModal() {
        el('qt-modal').classList.add('qt-open');
        times(el('qt-mbody'));
        var f = el('qt-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('qt-modal');
        if (m) {
            m.classList.remove('qt-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('qt-per')) {
            el('qt-per').value = k;
        }
        if (k !== 'range') {
            if (el('qt-from')) {
                el('qt-from').value = '';
            }
            if (el('qt-to')) {
                el('qt-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('qt-per') }, { key: 'pfrom', vlu: val('qt-from') }, { key: 'pto', vlu: val('qt-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('qt-main'));
        if (window.MutationObserver && el('qt-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('qt-main'));
                }, 30);
            }).observe(el('qt-main'), { childList: true, subtree: true });
        }
        if (window.AdminQuotesJsloaded) {
            window.AdminQuotesJsloaded();
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
        if (!askBox || !askBox.classList.contains('qt-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('qt-open');
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
        askBox.className = 'qt-ask';
        askBox.innerHTML = '<div class="qt-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="qt-err" role="alert"></em><div class="qt-askacts"><button type="button" class="qt-btn qt-btn-ghost qt-btn-sm"></button><button type="button" class="qt-btn qt-btn-sm"></button></div></div>';
        (el('qt-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('qt-open')) {
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
        x.b[1].className = 'qt-btn qt-btn-sm' + (o.danger ? ' qt-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('qt-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('qt-open');
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

    var kind = '';
    var current = '';

    window.AdminQuotesJsloaded = function () {
        kind = qs('kind');
        var st = qs('status');
        if (st && el('qt-fstat')) {
            el('qt-fstat').value = st;
        }
        list(1);
        if (qs('id')) {
            open(qs('id'));
        }
    };

    function loadArgs() {
        return [{ key: 'arg', vlu: qs('kind') }];
    }

    function setKind(k) {
        kind = k;
        list(1);
    }

    function list(pg) {
        api('List', [{ key: 'qkind', vlu: kind }, { key: 'qq', vlu: val('qt-q') }, { key: 'qstat', vlu: val('qt-fstat') }, { key: 'pg', vlu: String(pg || 1) }].concat(period()));
    }

    function open(id) {
        current = id;
        api('Open', [{ key: 'qid', vlu: id }]);
    }

    function status(st) {
        if (st === 'quoted') {
            ask({ title: 'Quoted amount', text: 'The total you sent, including freight. Leave it blank to keep the catalog total.', value: el('qt-qamt') ? el('qt-qamt').getAttribute('data-v') : '', place: '1,299', ok: 'Mark quoted' }, function (v) {
                api('Status', [{ key: 'qid', vlu: current }, { key: 'qst', vlu: st }, { key: 'qamt', vlu: v }]);
            });
            return;
        }
        api('Status', [{ key: 'qid', vlu: current }, { key: 'qst', vlu: st }, { key: 'qamt', vlu: '' }]);
    }

    function note() {
        var t = val('qt-ntext');
        if (!t) {
            el('qt-ntext').focus();
            return;
        }
        api('Note', [{ key: 'qid', vlu: current }, { key: 'ntext', vlu: t }]);
    }

    function remove() {
        ask({ title: 'Delete this inquiry?', text: 'It is removed for good, with its notes.', ok: 'Delete', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'qid', vlu: current }]);
        });
    }

    function reopen() {
        open(current);
        list();
    }

    function csv() {
        api('Csv', [{ key: 'qkind', vlu: kind }, { key: 'qq', vlu: val('qt-q') }, { key: 'qstat', vlu: val('qt-fstat') }].concat(period()));
    }

    function csvReady(b64, name) {
        var bytes = atob(b64);
        var arr = new Uint8Array(bytes.length);
        for (var i = 0; i < bytes.length; i++) {
            arr[i] = bytes.charCodeAt(i);
        }
        var url = URL.createObjectURL(new Blob([arr], { type: 'text/csv' }));
        var a = document.createElement('a');
        a.href = url;
        a.download = name;
        document.body.appendChild(a);
        a.click();
        setTimeout(function () {
            URL.revokeObjectURL(url);
            a.remove();
        }, 500);
    }

    function reveal() {
        if (!getCookie('iv_tok')) {
            location.replace('AdminLogin');
            return;
        }
        api('Load', loadArgs());
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closeNav();
                closeModal();
            }
        });
        document.addEventListener('input', function (e) {
            var id = e.target && e.target.id ? e.target.id.replace('qt-', 'qt-e-') : '';
            if (id && el(id)) {
                el(id).textContent = '';
            }
        });
    }

    return {
        reveal: reveal,
        toast: toast,
        openNav: openNav,
        closeNav: closeNav,
        signOut: signOut,
        out: out,
        expired: expired,
        openModal: openModal,
        closeModal: closeModal,
        per: per,
        search: search,
        loaded: loaded,
        setKind: setKind,
        list: list,
        open: open,
        status: status,
        note: note,
        remove: remove,
        reopen: reopen,
        csv: csv,
        csvReady: csvReady
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminQuotesJs.reveal);
} else {
    AdminQuotesJs.reveal();
}
