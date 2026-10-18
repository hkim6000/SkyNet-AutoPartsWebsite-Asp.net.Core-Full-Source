var AdminNewsJs = (function () {

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
        $ApiRequest('AdminNews/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('nz-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('nz-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('nz-open');
        }, 3200);
    }

    function openNav() {
        el('nz-nav').classList.add('nz-open');
        el('nz-scrim').classList.add('nz-open');
    }

    function closeNav() {
        el('nz-nav').classList.remove('nz-open');
        el('nz-scrim').classList.remove('nz-open');
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
        el('nz-modal').classList.add('nz-open');
        times(el('nz-mbody'));
        var f = el('nz-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('nz-modal');
        if (m) {
            m.classList.remove('nz-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('nz-per')) {
            el('nz-per').value = k;
        }
        if (k !== 'range') {
            if (el('nz-from')) {
                el('nz-from').value = '';
            }
            if (el('nz-to')) {
                el('nz-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('nz-per') }, { key: 'pfrom', vlu: val('nz-from') }, { key: 'pto', vlu: val('nz-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('nz-main'));
        if (window.MutationObserver && el('nz-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('nz-main'));
                }, 30);
            }).observe(el('nz-main'), { childList: true, subtree: true });
        }
        if (window.AdminNewsJsloaded) {
            window.AdminNewsJsloaded();
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
        if (!askBox || !askBox.classList.contains('nz-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('nz-open');
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
        askBox.className = 'nz-ask';
        askBox.innerHTML = '<div class="nz-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="nz-err" role="alert"></em><div class="nz-askacts"><button type="button" class="nz-btn nz-btn-ghost nz-btn-sm"></button><button type="button" class="nz-btn nz-btn-sm"></button></div></div>';
        (el('nz-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('nz-open')) {
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
        x.b[1].className = 'nz-btn nz-btn-sm' + (o.danger ? ' nz-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('nz-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('nz-open');
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

    var pk = { kind: '', allow: '', multi: false, tag: '', done: null, page: 1, chosen: [] };
    var pkTimer = null;

    function pkBuild() {
        if (el('nz-pk')) {
            return;
        }
        var d = document.createElement('div');
        d.className = 'nz-pk';
        d.id = 'nz-pk';
        d.setAttribute('role', 'dialog');
        d.setAttribute('aria-modal', 'true');
        d.innerHTML = '<div class="nz-pkbox"><header><h2 id="nz-pkt">Choose from Media</h2><button type="button" class="nz-ibtn" aria-label="Close" onclick="AdminNewsJs.pkClose()">&times;</button></header><div class="nz-pkbar"><input type="search" id="nz-pkq" placeholder="Search by name or tag" maxlength="60" oninput="AdminNewsJs.pkSearch()"><select id="nz-pkkind" aria-label="Type" onchange="AdminNewsJs.pkList(1)"><option value="">Photos and videos</option><option value="photo">Photos</option><option value="video">Videos</option><option value="doc">Documents</option></select><select id="nz-pktag" aria-label="Tag" onchange="AdminNewsJs.pkList(1)"><option value="">Any tag</option></select><label class="nz-btn nz-btn-sm nz-btn-ghost nz-pkup" id="nz-pkupl">Upload a photo<input type="file" id="nz-pkfile" accept="image/jpeg,image/png,image/webp" onchange="AdminNewsJs.pkUpload(this)"></label></div><em class="nz-err nz-pkerr" id="nz-pkerr" role="alert"></em><div class="nz-pkgrid" id="nz-pkgrid"></div><div class="nz-pkmore" id="nz-pkmore"></div><footer><span id="nz-pkn"></span><button type="button" class="nz-btn nz-btn-ghost" onclick="AdminNewsJs.pkClose()">Cancel</button><button type="button" class="nz-btn nz-btn-accent" id="nz-pkuse" onclick="AdminNewsJs.pkUse()">Add</button></footer></div>';
        (el('nz-site') || document.body).appendChild(d);
        d.addEventListener('mousedown', function (e) {
            if (e.target === d) {
                pkClose();
            }
        });
        d.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                e.stopPropagation();
                pkClose();
            }
        });
    }

    function pickMedia(opts) {
        pkBuild();
        pk.allow = opts.allow || '';
        pk.multi = !!opts.multi;
        pk.tag = opts.tag || '';
        pk.done = opts.done;
        pk.chosen = [];
        el('nz-pkt').textContent = opts.title || 'Choose from Media';
        el('nz-pkq').value = '';
        el('nz-pkerr').textContent = '';
        var k = el('nz-pkkind');
        k.value = pk.allow === 'photo' || pk.allow === 'video' || pk.allow === 'doc' ? pk.allow : '';
        k.disabled = !!pk.allow;
        el('nz-pkupl').hidden = pk.allow === 'video' || pk.allow === 'doc';
        el('nz-pkuse').hidden = !pk.multi;
        pkCount();
        el('nz-pk').classList.add('nz-open');
        pkList(1);
        setTimeout(function () {
            el('nz-pkq').focus();
        }, 30);
    }

    function pkList(pg) {
        pk.page = pg || 1;
        api('MediaPick', [{ key: 'pkind', vlu: val('nz-pkkind') }, { key: 'pallow', vlu: pk.allow }, { key: 'pterm', vlu: val('nz-pkq') }, { key: 'ptag', vlu: val('nz-pktag') }, { key: 'pctx', vlu: pk.tag }, { key: 'ppg', vlu: String(pk.page) }]);
    }

    function pkSearch() {
        clearTimeout(pkTimer);
        pkTimer = setTimeout(function () {
            pkList(1);
        }, 300);
    }

    function pkShown(html, more, tags, append) {
        var g = el('nz-pkgrid');
        if (append) {
            g.insertAdjacentHTML('beforeend', html);
        } else {
            g.innerHTML = html;
            g.scrollTop = 0;
        }
        el('nz-pkmore').innerHTML = more ? '<button type="button" class="nz-btn nz-btn-sm nz-btn-ghost" onclick="AdminNewsJs.pkList(' + (pk.page + 1) + ')">Show more</button>' : '';
        if (tags !== null) {
            var t = el('nz-pktag');
            var cur = t.value;
            t.innerHTML = '<option value="">Any tag</option>' + tags.map(function (x) {
                return '<option value="' + esc(x) + '">' + esc(x) + '</option>';
            }).join('');
            t.value = cur;
        }
        var b = g.querySelectorAll('.nz-pki');
        for (var i = 0; i < b.length; i++) {
            b[i].classList.toggle('nz-on', pk.chosen.some(function (c) {
                return c.src === b[i].getAttribute('data-src');
            }));
        }
    }

    function pkItem(b) {
        return { src: b.getAttribute('data-src'), thumb: b.getAttribute('data-thumb'), kind: b.getAttribute('data-kind'), title: b.getAttribute('data-title') };
    }

    function pkToggle(b) {
        var it = pkItem(b);
        if (!pk.multi) {
            var f = pk.done;
            pkClose();
            if (f) {
                f([it]);
            }
            return;
        }
        var at = -1;
        pk.chosen.forEach(function (c, i) {
            if (c.src === it.src) {
                at = i;
            }
        });
        if (at >= 0) {
            pk.chosen.splice(at, 1);
        } else {
            pk.chosen.push(it);
        }
        b.classList.toggle('nz-on', at < 0);
        pkCount();
    }

    function pkCount() {
        var n = pk.chosen.length;
        el('nz-pkn').textContent = pk.multi ? (n ? n + ' selected' : 'Tap to select one or more') : 'Tap one to use it';
        el('nz-pkuse').disabled = n === 0;
        el('nz-pkuse').textContent = n > 1 ? 'Add ' + n : 'Add';
    }

    function pkUse() {
        var f = pk.done;
        var list = pk.chosen.slice();
        pkClose();
        if (f && list.length) {
            f(list);
        }
    }

    function pkClose() {
        var d = el('nz-pk');
        if (d) {
            d.classList.remove('nz-open');
        }
        pk.done = null;
    }

    function pkShrink(img, max, q) {
        var r = Math.min(1, max / Math.max(img.naturalWidth, img.naturalHeight));
        var c = document.createElement('canvas');
        c.width = Math.round(img.naturalWidth * r);
        c.height = Math.round(img.naturalHeight * r);
        c.getContext('2d').drawImage(img, 0, 0, c.width, c.height);
        var d = c.toDataURL('image/webp', q);
        return d.indexOf('data:image/webp') === 0 ? d : c.toDataURL('image/jpeg', q);
    }

    function pkUpload(input) {
        var f = input.files && input.files[0];
        el('nz-pkerr').textContent = '';
        if (!f) {
            return;
        }
        if (!/^image\/(jpeg|png|webp)$/.test(f.type) || f.size > 30 * 1024 * 1024) {
            el('nz-pkerr').textContent = 'Choose a JPG, PNG or WebP photo under 30 MB.';
            input.value = '';
            return;
        }
        var url = URL.createObjectURL(f);
        var img = new Image();
        img.onload = function () {
            URL.revokeObjectURL(url);
            var full = pkShrink(img, 1600, 0.86);
            var thumb = pkShrink(img, 640, 0.8);
            el('nz-pkupl').classList.add('nz-busy');
            api('MediaUp', [{ key: 'full', vlu: full }, { key: 'thumb', vlu: thumb }, { key: 'pname', vlu: f.name.replace(/\.[^.]+$/, '').replace(/[-_]+/g, ' ').substring(0, 80) }, { key: 'pctx', vlu: pk.tag }, { key: 'pw', vlu: String(img.naturalWidth) }, { key: 'ph', vlu: String(img.naturalHeight) }]);
        };
        img.onerror = function () {
            URL.revokeObjectURL(url);
            el('nz-pkerr').textContent = 'That image could not be read.';
        };
        img.src = url;
        input.value = '';
    }

    function pkUploaded(ok, msg) {
        el('nz-pkupl').classList.remove('nz-busy');
        if (!ok) {
            el('nz-pkerr').textContent = msg;
            return;
        }
        el('nz-pkq').value = '';
        el('nz-pkkind').value = pk.allow === 'photo' ? 'photo' : '';
        el('nz-pktag').value = '';
        pkList(1);
        toast('Photo added to Media.');
    }

    var opened = '';
    var image = '';

    window.AdminNewsJsloaded = function () {
        list(1);
        var id = qs('id');
        if (id) {
            edit(id === 'new' ? '' : id);
        }
    };

    function loadArgs() {
        return [];
    }

    function list(pg) {
        api('List', [{ key: 'nq', vlu: val('nz-q') }, { key: 'ntag', vlu: val('nz-ftag') }, { key: 'npub', vlu: val('nz-fpub') }, { key: 'pg', vlu: String(pg || 1) }]);
    }

    function edit(id) {
        api('Form', [{ key: 'nid', vlu: id || '' }]);
    }

    function opened2() {
        var d = el('nz-ndata');
        opened = d ? d.getAttribute('data-id') : '';
        image = d ? d.getAttribute('data-image') : '';
        openModal();
    }

    function pickImage() {
        pickMedia({ allow: 'photo', tag: 'news', title: 'Choose a picture', done: function (list) {
            image = list[0].src;
            el('nz-nimg').innerHTML = '<img src="' + esc(list[0].thumb) + '" alt="">';
        } });
    }

    function noImage() {
        image = '';
        el('nz-nimg').innerHTML = '<span>Add a picture</span>';
    }

    function fmt(cmd) {
        var ed = el('nz-nbody');
        ed.focus();
        if (cmd === 'link') {
            var sel = window.getSelection();
            if (!sel.rangeCount || sel.getRangeAt(0).collapsed) {
                toast('Select some words first, then add the link.');
                return;
            }
            var range = sel.getRangeAt(0).cloneRange();
            ask({ title: 'Add a link', text: 'A page name like Products, or a full address starting with https://', value: 'https://', ok: 'Add link' }, function (v) {
                ed.focus();
                var s2 = window.getSelection();
                s2.removeAllRanges();
                s2.addRange(range);
                document.execCommand('createLink', false, v);
            });
            return;
        }
        if (cmd === 'clear') {
            document.execCommand('removeFormat');
            document.execCommand('unlink');
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
            } else if (t === 'a') {
                var h = c.getAttribute('href') || '';
                out += h ? '<a href="' + esc(h) + '">' + inner + '</a>' : inner;
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
        api('Save', [{ key: 'nid', vlu: opened }, { key: 'ntitle', vlu: val('nz-ntitle') }, { key: 'nslug', vlu: val('nz-nslug') }, { key: 'ntag2', vlu: val('nz-ntag') }, { key: 'ndate', vlu: val('nz-ndate') }, { key: 'nsum', vlu: val('nz-nsum') }, { key: 'nbody', vlu: cleanNode(el('nz-nbody')) }, { key: 'nimage', vlu: image }, { key: 'npub2', vlu: el('nz-npub').checked ? '1' : '0' }]);
    }

    function saved(isNew) {
        closeModal();
        list(1);
        toast(isNew ? 'Post added.' : 'Saved.');
        try {
            history.replaceState(null, '', 'AdminNews');
        } catch (e) {
        }
    }

    function fail(msg) {
        el('nz-e-save').textContent = msg;
    }

    function remove(id, title) {
        ask({ title: 'Delete “' + title + '”?', text: 'The post is removed from the website for good.', ok: 'Delete post', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'nid', vlu: id }]);
        });
    }

    function deleted() {
        closeModal();
        list(1);
        toast('Post deleted.');
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
            var id = e.target && e.target.id ? e.target.id.replace('nz-', 'nz-e-') : '';
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
        list: list,
        edit: edit,
        opened: opened2,
        pickImage: pickImage,
        noImage: noImage,
        fmt: fmt,
        save: save,
        saved: saved,
        fail: fail,
        remove: remove,
        deleted: deleted,
        pickMedia: pickMedia,
        pkList: pkList,
        pkSearch: pkSearch,
        pkShown: pkShown,
        pkToggle: pkToggle,
        pkUse: pkUse,
        pkClose: pkClose,
        pkUpload: pkUpload,
        pkUploaded: pkUploaded
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminNewsJs.reveal);
} else {
    AdminNewsJs.reveal();
}
