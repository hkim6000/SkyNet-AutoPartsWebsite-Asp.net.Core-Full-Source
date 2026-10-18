var AdminDealersJs = (function () {

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
        $ApiRequest('AdminDealers/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('dl-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('dl-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('dl-open');
        }, 3200);
    }

    function openNav() {
        el('dl-nav').classList.add('dl-open');
        el('dl-scrim').classList.add('dl-open');
    }

    function closeNav() {
        el('dl-nav').classList.remove('dl-open');
        el('dl-scrim').classList.remove('dl-open');
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
        el('dl-modal').classList.add('dl-open');
        times(el('dl-mbody'));
        var f = el('dl-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('dl-modal');
        if (m) {
            m.classList.remove('dl-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('dl-per')) {
            el('dl-per').value = k;
        }
        if (k !== 'range') {
            if (el('dl-from')) {
                el('dl-from').value = '';
            }
            if (el('dl-to')) {
                el('dl-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('dl-per') }, { key: 'pfrom', vlu: val('dl-from') }, { key: 'pto', vlu: val('dl-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('dl-main'));
        if (window.MutationObserver && el('dl-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('dl-main'));
                }, 30);
            }).observe(el('dl-main'), { childList: true, subtree: true });
        }
        if (window.AdminDealersJsloaded) {
            window.AdminDealersJsloaded();
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
        if (!askBox || !askBox.classList.contains('dl-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('dl-open');
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
        askBox.className = 'dl-ask';
        askBox.innerHTML = '<div class="dl-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="dl-err" role="alert"></em><div class="dl-askacts"><button type="button" class="dl-btn dl-btn-ghost dl-btn-sm"></button><button type="button" class="dl-btn dl-btn-sm"></button></div></div>';
        (el('dl-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('dl-open')) {
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
        x.b[1].className = 'dl-btn dl-btn-sm' + (o.danger ? ' dl-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('dl-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('dl-open');
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

    var opened = '';
    var state = '';

    window.AdminDealersJsloaded = function () {
        list(1);
        if (qs('id')) {
            edit(qs('id'));
        } else if (qs('new')) {
            edit('');
        }
    };

    function loadArgs() {
        return [];
    }

    function setState(st) {
        state = st;
        list(1);
    }

    function list(pg) {
        api('List', [{ key: 'dq', vlu: val('dl-q') }, { key: 'dstate', vlu: state }, { key: 'dkind', vlu: val('dl-fkind') }, { key: 'dact', vlu: val('dl-fact') }, { key: 'pg', vlu: String(pg || 1) }]);
    }

    function edit(id) {
        api('Form', [{ key: 'did', vlu: id || '' }, { key: 'pname', vlu: qs('name') }, { key: 'pstate', vlu: qs('state') }, { key: 'pkind', vlu: qs('kind') }, { key: 'pemail', vlu: qs('email') }, { key: 'pphone', vlu: qs('phone') }]);
    }

    function opened2() {
        var d = el('dl-ddata');
        opened = d ? d.getAttribute('data-id') : '';
        openModal();
    }

    function save() {
        var svc = [];
        var cbs = document.querySelectorAll('#dl-svcs input:checked');
        for (var i = 0; i < cbs.length; i++) {
            svc.push(cbs[i].value);
        }
        var keys = ['dname', 'dkind', 'dtier', 'daddr', 'dcity', 'dst', 'dzip', 'dphone', 'demail', 'dweb', 'dhours', 'dsince', 'dnotes'];
        var list2 = keys.map(function (k) {
            return { key: k, vlu: el('dl-' + k) ? el('dl-' + k).value : '' };
        });
        list2.push({ key: 'dactive', vlu: el('dl-dactive').checked ? '1' : '0' });
        list2.push({ key: 'dsvc', vlu: svc.join(',') });
        list2.push({ key: 'did', vlu: opened });
        api('Save', list2);
    }

    function saved(isNew) {
        closeModal();
        list(1);
        toast(isNew ? 'Dealer added.' : 'Saved.');
        try {
            history.replaceState(null, '', 'AdminDealers');
        } catch (e) {
        }
    }

    function fail(msg) {
        el('dl-e-save').textContent = msg;
    }

    function remove(id, name) {
        ask({ title: 'Delete ' + name + '?', text: 'The dealer disappears from the website and this list. Switch it off instead if it may come back.', ok: 'Delete dealer', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'did', vlu: id }]);
        });
    }

    function deleted() {
        closeModal();
        list(1);
        toast('Dealer deleted.');
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
            var id = e.target && e.target.id ? e.target.id.replace('dl-', 'dl-e-') : '';
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
        setState: setState,
        list: list,
        edit: edit,
        opened: opened2,
        save: save,
        saved: saved,
        fail: fail,
        remove: remove,
        deleted: deleted
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminDealersJs.reveal);
} else {
    AdminDealersJs.reveal();
}
