var AdminCareersJs = (function () {

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
        $ApiRequest('AdminCareers/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('cz-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('cz-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('cz-open');
        }, 3200);
    }

    function openNav() {
        el('cz-nav').classList.add('cz-open');
        el('cz-scrim').classList.add('cz-open');
    }

    function closeNav() {
        el('cz-nav').classList.remove('cz-open');
        el('cz-scrim').classList.remove('cz-open');
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
        el('cz-modal').classList.add('cz-open');
        times(el('cz-mbody'));
        var f = el('cz-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('cz-modal');
        if (m) {
            m.classList.remove('cz-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('cz-per')) {
            el('cz-per').value = k;
        }
        if (k !== 'range') {
            if (el('cz-from')) {
                el('cz-from').value = '';
            }
            if (el('cz-to')) {
                el('cz-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('cz-per') }, { key: 'pfrom', vlu: val('cz-from') }, { key: 'pto', vlu: val('cz-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('cz-main'));
        if (window.MutationObserver && el('cz-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('cz-main'));
                }, 30);
            }).observe(el('cz-main'), { childList: true, subtree: true });
        }
        if (window.AdminCareersJsloaded) {
            window.AdminCareersJsloaded();
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
        if (!askBox || !askBox.classList.contains('cz-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('cz-open');
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
        askBox.className = 'cz-ask';
        askBox.innerHTML = '<div class="cz-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="cz-err" role="alert"></em><div class="cz-askacts"><button type="button" class="cz-btn cz-btn-ghost cz-btn-sm"></button><button type="button" class="cz-btn cz-btn-sm"></button></div></div>';
        (el('cz-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('cz-open')) {
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
        x.b[1].className = 'cz-btn cz-btn-sm' + (o.danger ? ' cz-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('cz-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('cz-open');
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

    var tab = 'jobs';
    var opened = '';
    var current = '';

    window.AdminCareersJsloaded = function () {
        show(qs('tab') === 'apps' ? 'apps' : 'jobs');
        if (qs('id')) {
            edit(qs('id') === 'new' ? '' : qs('id'));
        }
        if (qs('app')) {
            open(qs('app'));
        }
    };

    function loadArgs() {
        return [];
    }

    function show(t) {
        tab = t;
        var tabs = document.querySelectorAll('.cz-tabs .cz-tab');
        for (var i = 0; i < tabs.length; i++) {
            tabs[i].classList.toggle('cz-act', tabs[i].getAttribute('data-p') === t);
        }
        el('cz-jobsp').hidden = t !== 'jobs';
        el('cz-appsp').hidden = t !== 'apps';
        try {
            history.replaceState(null, '', 'AdminCareers' + (t === 'apps' ? '?tab=apps' : ''));
        } catch (e) {
        }
        list(1);
    }

    function list(pg) {
        if (tab === 'apps') {
            api('Apps', [{ key: 'aq', vlu: val('cz-aq') }, { key: 'ajob', vlu: val('cz-fjob') }, { key: 'astat', vlu: val('cz-fastat') }, { key: 'pg', vlu: String(pg || 1) }]);
            return;
        }
        api('List', [{ key: 'jq', vlu: val('cz-q') }, { key: 'jdept', vlu: val('cz-fdept') }, { key: 'jopen', vlu: val('cz-fopen') }, { key: 'pg', vlu: String(pg || 1) }]);
    }

    function edit(id) {
        api('Form', [{ key: 'jid', vlu: id || '' }]);
    }

    function opened2() {
        var d = el('cz-jdata');
        opened = d ? d.getAttribute('data-id') : '';
        openModal();
    }

    function fmt(cmd) {
        var ed = el('cz-jbody');
        ed.focus();
        if (cmd === 'clear') {
            document.execCommand('removeFormat');
            return;
        }
        document.execCommand(cmd);
    }

    function cleanNode(n) {
        var out = '';
        [].forEach.call(n.childNodes, function (c) {
            if (c.nodeType === 3) {
                out += esc(c.nodeValue);
                return;
            }
            if (c.nodeType !== 1) {
                return;
            }
            var t = c.tagName.toLowerCase();
            if (/^(script|style|template|iframe|object|img|video|svg)$/.test(t)) {
                return;
            }
            var inner = cleanNode(c);
            if (t === 'br') {
                out += '<br>';
            } else if (t === 'b' || t === 'strong') {
                out += inner ? '<b>' + inner + '</b>' : '';
            } else if (t === 'i' || t === 'em') {
                out += inner ? '<i>' + inner + '</i>' : '';
            } else if (t === 'ul' || t === 'ol' || t === 'li') {
                out += '<' + t + '>' + inner + '</' + t + '>';
            } else if (t === 'p' || t === 'div' || /^h[1-6]$/.test(t)) {
                out += '<p>' + inner + '</p>';
            } else {
                out += inner;
            }
        });
        return out;
    }

    function save() {
        api('Save', [{ key: 'jid', vlu: opened }, { key: 'jtitle', vlu: val('cz-jtitle') }, { key: 'jslug', vlu: val('cz-jslug') }, { key: 'jdept2', vlu: val('cz-jdept') }, { key: 'jtype', vlu: val('cz-jtype') }, { key: 'jshift', vlu: val('cz-jshift') }, { key: 'jpay', vlu: val('cz-jpay') }, { key: 'jsum', vlu: val('cz-jsum') }, { key: 'jbody', vlu: cleanNode(el('cz-jbody')) }, { key: 'jposted', vlu: val('cz-jposted') }, { key: 'jopen2', vlu: el('cz-jopen').checked ? '1' : '0' }]);
    }

    function saved(isNew) {
        closeModal();
        list(1);
        toast(isNew ? 'Position added.' : 'Saved.');
        try {
            history.replaceState(null, '', 'AdminCareers');
        } catch (e) {
        }
    }

    function fail(msg) {
        el('cz-e-save').textContent = msg;
    }

    function remove(id, title) {
        ask({ title: 'Delete “' + title + '”?', text: 'The position and its applications are removed for good. Close it instead to keep the applications.', ok: 'Delete position', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'jid', vlu: id }]);
        });
    }

    function deleted() {
        closeModal();
        list(1);
        toast('Position deleted.');
    }

    function open(id) {
        current = id;
        api('Open', [{ key: 'aid', vlu: id }]);
    }

    function status(st) {
        api('Status', [{ key: 'aid', vlu: current }, { key: 'ast', vlu: st }]);
    }

    function note() {
        var t = val('cz-ntext');
        if (!t) {
            el('cz-ntext').focus();
            return;
        }
        api('Note', [{ key: 'aid', vlu: current }, { key: 'ntext', vlu: t }]);
    }

    function resume() {
        api('Resume', [{ key: 'aid', vlu: current }]);
    }

    function fileReady(b64, name, mime) {
        var bytes = atob(b64);
        var arr = new Uint8Array(bytes.length);
        for (var i = 0; i < bytes.length; i++) {
            arr[i] = bytes.charCodeAt(i);
        }
        var url = URL.createObjectURL(new Blob([arr], { type: mime }));
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

    function removeApp() {
        ask({ title: 'Delete this application?', text: 'The application and its résumé are removed for good.', ok: 'Delete', no: 'Keep it', danger: true }, function () {
            api('DeleteApp', [{ key: 'aid', vlu: current }]);
        });
    }

    function reopen() {
        open(current);
        list();
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
            var id = e.target && e.target.id ? e.target.id.replace('cz-', 'cz-e-') : '';
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
        show: show,
        list: list,
        edit: edit,
        opened: opened2,
        fmt: fmt,
        save: save,
        saved: saved,
        fail: fail,
        remove: remove,
        deleted: deleted,
        open: open,
        status: status,
        note: note,
        resume: resume,
        fileReady: fileReady,
        removeApp: removeApp,
        reopen: reopen
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminCareersJs.reveal);
} else {
    AdminCareersJs.reveal();
}
