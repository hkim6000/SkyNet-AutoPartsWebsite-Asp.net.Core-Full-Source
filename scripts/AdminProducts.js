var AdminProductsJs = (function () {

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
        $ApiRequest('AdminProducts/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('pz-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('pz-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('pz-open');
        }, 3200);
    }

    function openNav() {
        el('pz-nav').classList.add('pz-open');
        el('pz-scrim').classList.add('pz-open');
    }

    function closeNav() {
        el('pz-nav').classList.remove('pz-open');
        el('pz-scrim').classList.remove('pz-open');
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
        el('pz-modal').classList.add('pz-open');
        times(el('pz-mbody'));
        var f = el('pz-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('pz-modal');
        if (m) {
            m.classList.remove('pz-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('pz-per')) {
            el('pz-per').value = k;
        }
        if (k !== 'range') {
            if (el('pz-from')) {
                el('pz-from').value = '';
            }
            if (el('pz-to')) {
                el('pz-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('pz-per') }, { key: 'pfrom', vlu: val('pz-from') }, { key: 'pto', vlu: val('pz-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('pz-main'));
        if (window.MutationObserver && el('pz-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('pz-main'));
                }, 30);
            }).observe(el('pz-main'), { childList: true, subtree: true });
        }
        if (window.AdminProductsJsloaded) {
            window.AdminProductsJsloaded();
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
        if (!askBox || !askBox.classList.contains('pz-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('pz-open');
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
        askBox.className = 'pz-ask';
        askBox.innerHTML = '<div class="pz-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="pz-err" role="alert"></em><div class="pz-askacts"><button type="button" class="pz-btn pz-btn-ghost pz-btn-sm"></button><button type="button" class="pz-btn pz-btn-sm"></button></div></div>';
        (el('pz-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('pz-open')) {
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
        x.b[1].className = 'pz-btn pz-btn-sm' + (o.danger ? ' pz-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('pz-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('pz-open');
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
        if (el('pz-pk')) {
            return;
        }
        var d = document.createElement('div');
        d.className = 'pz-pk';
        d.id = 'pz-pk';
        d.setAttribute('role', 'dialog');
        d.setAttribute('aria-modal', 'true');
        d.innerHTML = '<div class="pz-pkbox"><header><h2 id="pz-pkt">Choose from Media</h2><button type="button" class="pz-ibtn" aria-label="Close" onclick="AdminProductsJs.pkClose()">&times;</button></header><div class="pz-pkbar"><input type="search" id="pz-pkq" placeholder="Search by name or tag" maxlength="60" oninput="AdminProductsJs.pkSearch()"><select id="pz-pkkind" aria-label="Type" onchange="AdminProductsJs.pkList(1)"><option value="">Photos and videos</option><option value="photo">Photos</option><option value="video">Videos</option><option value="doc">Documents</option></select><select id="pz-pktag" aria-label="Tag" onchange="AdminProductsJs.pkList(1)"><option value="">Any tag</option></select><label class="pz-btn pz-btn-sm pz-btn-ghost pz-pkup" id="pz-pkupl">Upload a photo<input type="file" id="pz-pkfile" accept="image/jpeg,image/png,image/webp" onchange="AdminProductsJs.pkUpload(this)"></label></div><em class="pz-err pz-pkerr" id="pz-pkerr" role="alert"></em><div class="pz-pkgrid" id="pz-pkgrid"></div><div class="pz-pkmore" id="pz-pkmore"></div><footer><span id="pz-pkn"></span><button type="button" class="pz-btn pz-btn-ghost" onclick="AdminProductsJs.pkClose()">Cancel</button><button type="button" class="pz-btn pz-btn-accent" id="pz-pkuse" onclick="AdminProductsJs.pkUse()">Add</button></footer></div>';
        (el('pz-site') || document.body).appendChild(d);
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
        el('pz-pkt').textContent = opts.title || 'Choose from Media';
        el('pz-pkq').value = '';
        el('pz-pkerr').textContent = '';
        var k = el('pz-pkkind');
        k.value = pk.allow === 'photo' || pk.allow === 'video' || pk.allow === 'doc' ? pk.allow : '';
        k.disabled = !!pk.allow;
        el('pz-pkupl').hidden = pk.allow === 'video' || pk.allow === 'doc';
        el('pz-pkuse').hidden = !pk.multi;
        pkCount();
        el('pz-pk').classList.add('pz-open');
        pkList(1);
        setTimeout(function () {
            el('pz-pkq').focus();
        }, 30);
    }

    function pkList(pg) {
        pk.page = pg || 1;
        api('MediaPick', [{ key: 'pkind', vlu: val('pz-pkkind') }, { key: 'pallow', vlu: pk.allow }, { key: 'pterm', vlu: val('pz-pkq') }, { key: 'ptag', vlu: val('pz-pktag') }, { key: 'pctx', vlu: pk.tag }, { key: 'ppg', vlu: String(pk.page) }]);
    }

    function pkSearch() {
        clearTimeout(pkTimer);
        pkTimer = setTimeout(function () {
            pkList(1);
        }, 300);
    }

    function pkShown(html, more, tags, append) {
        var g = el('pz-pkgrid');
        if (append) {
            g.insertAdjacentHTML('beforeend', html);
        } else {
            g.innerHTML = html;
            g.scrollTop = 0;
        }
        el('pz-pkmore').innerHTML = more ? '<button type="button" class="pz-btn pz-btn-sm pz-btn-ghost" onclick="AdminProductsJs.pkList(' + (pk.page + 1) + ')">Show more</button>' : '';
        if (tags !== null) {
            var t = el('pz-pktag');
            var cur = t.value;
            t.innerHTML = '<option value="">Any tag</option>' + tags.map(function (x) {
                return '<option value="' + esc(x) + '">' + esc(x) + '</option>';
            }).join('');
            t.value = cur;
        }
        var b = g.querySelectorAll('.pz-pki');
        for (var i = 0; i < b.length; i++) {
            b[i].classList.toggle('pz-on', pk.chosen.some(function (c) {
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
        b.classList.toggle('pz-on', at < 0);
        pkCount();
    }

    function pkCount() {
        var n = pk.chosen.length;
        el('pz-pkn').textContent = pk.multi ? (n ? n + ' selected' : 'Tap to select one or more') : 'Tap one to use it';
        el('pz-pkuse').disabled = n === 0;
        el('pz-pkuse').textContent = n > 1 ? 'Add ' + n : 'Add';
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
        var d = el('pz-pk');
        if (d) {
            d.classList.remove('pz-open');
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
        el('pz-pkerr').textContent = '';
        if (!f) {
            return;
        }
        if (!/^image\/(jpeg|png|webp)$/.test(f.type) || f.size > 30 * 1024 * 1024) {
            el('pz-pkerr').textContent = 'Choose a JPG, PNG or WebP photo under 30 MB.';
            input.value = '';
            return;
        }
        var url = URL.createObjectURL(f);
        var img = new Image();
        img.onload = function () {
            URL.revokeObjectURL(url);
            var full = pkShrink(img, 1600, 0.86);
            var thumb = pkShrink(img, 640, 0.8);
            el('pz-pkupl').classList.add('pz-busy');
            api('MediaUp', [{ key: 'full', vlu: full }, { key: 'thumb', vlu: thumb }, { key: 'pname', vlu: f.name.replace(/\.[^.]+$/, '').replace(/[-_]+/g, ' ').substring(0, 80) }, { key: 'pctx', vlu: pk.tag }, { key: 'pw', vlu: String(img.naturalWidth) }, { key: 'ph', vlu: String(img.naturalHeight) }]);
        };
        img.onerror = function () {
            URL.revokeObjectURL(url);
            el('pz-pkerr').textContent = 'That image could not be read.';
        };
        img.src = url;
        input.value = '';
    }

    function pkUploaded(ok, msg) {
        el('pz-pkupl').classList.remove('pz-busy');
        if (!ok) {
            el('pz-pkerr').textContent = msg;
            return;
        }
        el('pz-pkq').value = '';
        el('pz-pkkind').value = pk.allow === 'photo' ? 'photo' : '';
        el('pz-pktag').value = '';
        pkList(1);
        toast('Photo added to Media.');
    }

    var photos = [];
    var thumbs = {};
    var docs = [];
    var opened = '';
    var makes = [];

    window.AdminProductsJsloaded = function () {
        list(1);
        var id = qs('id');
        if (id) {
            edit(id === 'new' ? '' : id);
        }
    };

    function loadArgs() {
        return [{ key: 'arg', vlu: qs('status') + '|' + qs('sort') + '|' + qs('stock') }];
    }

    function list(pg) {
        api('List', [{ key: 'pq', vlu: val('pz-q') }, { key: 'pcat', vlu: val('pz-fcat') }, { key: 'pser', vlu: val('pz-fser') }, { key: 'pstat', vlu: val('pz-fstat') }, { key: 'pstock', vlu: val('pz-fstock') }, { key: 'psort', vlu: val('pz-fsort') }, { key: 'pg', vlu: String(pg || 1) }]);
    }

    function edit(id) {
        api('Form', [{ key: 'pid', vlu: id || '' }]);
    }

    function pane(k) {
        var tabs = document.querySelectorAll('#pz-mbody .pz-tab');
        for (var i = 0; i < tabs.length; i++) {
            tabs[i].classList.toggle('pz-act', tabs[i].getAttribute('data-p') === k);
        }
        var ps = document.querySelectorAll('#pz-mbody .pz-panes > div');
        for (var j = 0; j < ps.length; j++) {
            ps[j].classList.toggle('pz-act', ps[j].getAttribute('data-p') === k);
        }
    }

    function opened2() {
        var d = el('pz-pdata');
        opened = d ? d.getAttribute('data-id') : '';
        try {
            photos = JSON.parse(d.getAttribute('data-photos') || '[]');
            thumbs = JSON.parse(d.getAttribute('data-thumbs') || '{}');
            docs = JSON.parse(d.getAttribute('data-docs') || '[]');
            makes = JSON.parse(d.getAttribute('data-makes') || '[]');
        } catch (e) {
            photos = [];
            thumbs = {};
            docs = [];
            makes = [];
        }
        drawPhotos();
        drawDocs();
        var rows = document.querySelectorAll('#pz-fits .pz-frow');
        for (var i = 0; i < rows.length; i++) {
            models(rows[i].querySelector('[data-f="make"]'), rows[i].getAttribute('data-model') || '');
        }
        openModal();
    }

    function drawPhotos() {
        var box = el('pz-photos');
        if (!box) {
            return;
        }
        if (!photos.length) {
            box.innerHTML = '<p class="pz-none">No photos yet. Add some from Media.</p>';
        } else {
            var cover = -1;
            photos.forEach(function (p, i) {
                if (cover < 0 && !/\.(mp4|webm)$/i.test(p)) {
                    cover = i;
                }
            });
            box.innerHTML = photos.map(function (p, i) {
                var t = thumbs[p] || p.replace('/full/', '/thumbs/');
                var v = /\.(mp4|webm)$/i.test(p);
                return '<div class="pz-ph1">' + (i === cover ? '<span class="pz-pcov">Cover</span>' : '') + (v ? '<span class="pz-pvid">Video</span>' : '') + '<img src="' + esc(t) + '" alt="' + (v ? 'Video ' : 'Photo ') + (i + 1) + '"><div class="pz-pbtns"><button type="button" aria-label="Move earlier" onclick="AdminProductsJs.move(' + i + ',-1)"' + (i === 0 ? ' disabled' : '') + '><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 15l6-6 6 6"/></svg></button><button type="button" aria-label="Move later" onclick="AdminProductsJs.move(' + i + ',1)"' + (i === photos.length - 1 ? ' disabled' : '') + '><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 9l6 6 6-6"/></svg></button><button type="button" aria-label="Remove photo" onclick="AdminProductsJs.unpick(' + i + ')"><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"/></svg></button></div></div>';
            }).join('');
        }
        var n = el('pz-pcount');
        if (n) {
            n.textContent = String(photos.length + docs.length);
        }
    }

    function drawDocs() {
        var box = el('pz-docs');
        if (!box) {
            return;
        }
        box.innerHTML = docs.length ? docs.map(function (d, i) {
            return '<div class="pz-doc1"><img src="' + esc(d.thumb) + '" alt=""><b>' + esc(d.title) + '</b><button type="button" class="pz-ibtn" aria-label="Remove document" onclick="AdminProductsJs.undoc(' + i + ')"><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"/></svg></button></div>';
        }).join('') : '<p class="pz-none">No documents attached.</p>';
        drawPhotos();
    }

    function move(i, d) {
        var j = i + d;
        if (j < 0 || j >= photos.length) {
            return;
        }
        var t = photos[i];
        photos[i] = photos[j];
        photos[j] = t;
        drawPhotos();
    }

    function unpick(i) {
        photos.splice(i, 1);
        drawPhotos();
    }

    function undoc(i) {
        docs.splice(i, 1);
        drawDocs();
    }

    function addMedia() {
        var tag = (val('pz-pcat') || 'products');
        pickMedia({ multi: true, tag: 'products,' + tag, title: 'Add photos and videos', done: function (list) {
            var n = 0;
            list.forEach(function (it) {
                if (photos.indexOf(it.src) < 0 && photos.length < 12) {
                    photos.push(it.src);
                    thumbs[it.src] = it.thumb;
                    n++;
                }
            });
            drawPhotos();
            if (n < list.length) {
                toast(photos.length >= 12 ? 'A product can have up to 12 photos and videos.' : 'Some were already on this product.');
            }
        } });
    }

    function addDoc() {
        pickMedia({ multi: true, allow: 'doc', tag: 'docs', title: 'Attach documents', done: function (list) {
            list.forEach(function (it) {
                if (!docs.some(function (d) { return d.src === it.src; }) && docs.length < 8) {
                    docs.push({ src: it.src, thumb: it.thumb, title: it.title });
                }
            });
            drawDocs();
        } });
    }

    function addSpec() {
        el('pz-specs').insertAdjacentHTML('beforeend', el('pz-stpl').innerHTML);
        el('pz-specs').lastElementChild.querySelector('input').focus();
    }

    function specMove(b, d) {
        var r = b.closest('.pz-srow');
        var s = d < 0 ? r.previousElementSibling : r.nextElementSibling;
        if (!s) {
            return;
        }
        if (d < 0) {
            r.parentNode.insertBefore(r, s);
        } else {
            r.parentNode.insertBefore(s, r);
        }
    }

    function addFit() {
        el('pz-fits').insertAdjacentHTML('beforeend', el('pz-ftpl').innerHTML);
        var row = el('pz-fits').lastElementChild;
        models(row.querySelector('[data-f="make"]'), '');
    }

    function models(sel, cur) {
        var row = sel.closest('.pz-frow');
        var ms = row.querySelector('[data-f="model"]');
        var html = '<option value="">Model</option>';
        makes.forEach(function (m) {
            if (m.key === sel.value) {
                m.models.forEach(function (x) {
                    html += '<option value="' + esc(x.key) + '"' + (x.key === cur ? ' selected' : '') + '>' + esc(x.name) + '</option>';
                });
            }
        });
        ms.innerHTML = html;
    }

    function dropRow(b) {
        b.parentNode.remove();
    }

    function save() {
        var specs = [];
        var rows = document.querySelectorAll('#pz-specs .pz-srow');
        for (var i = 0; i < rows.length; i++) {
            var x = rows[i].querySelectorAll('input, select');
            if (x[0].value.trim()) {
                specs.push([x[0].value.trim(), x[1].value.trim(), x[2].value].join('|').replace(/~/g, ' '));
            }
        }
        var fits = [];
        var frows = document.querySelectorAll('#pz-fits .pz-frow');
        for (var k = 0; k < frows.length; k++) {
            var f = frows[k].querySelectorAll('select, input');
            if (f[0].value && f[1].value) {
                fits.push([f[0].value, f[1].value, f[2].value, f[3].value, f[4].value, f[5].value.trim()].join('|').replace(/~/g, ' '));
            }
        }
        var keys = ['pname', 'psku', 'pcat', 'pser', 'psum', 'pprice', 'pstock', 'plead', 'pwar', 'pinst', 'pstatus', 'ptext', 'pfeat2', 'pbox', 'ptorque'];
        var list2 = keys.map(function (k) {
            return { key: k, vlu: el('pz-' + k) ? el('pz-' + k).value : '' };
        });
        list2.push({ key: 'pfeatured', vlu: el('pz-pfeatured') && el('pz-pfeatured').checked ? '1' : '0' });
        list2.push({ key: 'pid', vlu: opened });
        list2.push({ key: 'pphotos', vlu: photos.join('|') });
        list2.push({ key: 'pdocs', vlu: docs.map(function (d) { return d.src; }).join('|') });
        list2.push({ key: 'pspecs', vlu: specs.join('~') });
        list2.push({ key: 'pfits', vlu: fits.join('~') });
        api('Save', list2);
    }

    function saved(id, isNew) {
        closeModal();
        list(1);
        toast(isNew ? 'Product ' + id + ' added.' : 'Saved.');
        try {
            history.replaceState(null, '', 'AdminProducts');
        } catch (e) {
        }
    }

    function fail(msg, p) {
        el('pz-e-save').textContent = msg;
        if (p) {
            pane(p);
        }
    }

    function remove(id, name) {
        ask({ title: 'Delete ' + name + '?', text: 'The product disappears from the website and the admin. Quote requests keep their own copy of the line. This cannot be undone.', ok: 'Delete product', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'pid', vlu: id }]);
        });
    }

    function deleted() {
        closeModal();
        list(1);
        toast('Product deleted.');
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
            var id = e.target && e.target.id ? e.target.id.replace('pz-', 'pz-e-') : '';
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
        pane: pane,
        opened: opened2,
        move: move,
        unpick: unpick,
        undoc: undoc,
        addMedia: addMedia,
        addDoc: addDoc,
        addSpec: addSpec,
        specMove: specMove,
        addFit: addFit,
        models: models,
        dropRow: dropRow,
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
    document.addEventListener('DOMContentLoaded', AdminProductsJs.reveal);
} else {
    AdminProductsJs.reveal();
}
