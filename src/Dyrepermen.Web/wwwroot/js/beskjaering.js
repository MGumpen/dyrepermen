// Beskjaering av profilbildet for opplasting: dra bildet pa plass og zoom til
// dyret fyller sirkelen. Det som lastes opp, er et kvadratisk utsnitt av det
// brukeren ser - ikke originalen - sa bildet pa dashbordet blir akkurat det
// som ble valgt, og filen er et par hundre KB.
//
// Gjelder <input type="file" data-beskjaer="800">, der verdien er sidelengden
// pa utsnittet i piksler. Uten skript, eller uten createImageBitmap, lastes
// originalen opp som for: serveren sjekker type og storrelse uansett.
(function () {
    'use strict';

    var KVALITET = 0.85;
    var MAKS_ZOOM = 4;
    var PILSTEG = 12;

    if (typeof createImageBitmap !== 'function' || typeof DataTransfer === 'undefined') {
        return;
    }

    document.querySelectorAll('input[type=file][data-beskjaer]').forEach(function (felt) {
        var redigerer = document.getElementById(felt.dataset.beskjaerRedigerer);
        if (!redigerer || !felt.form) {
            return;
        }

        var lerret = redigerer.querySelector('canvas');
        var zoomFelt = redigerer.querySelector('input[type=range]');
        var side = parseInt(felt.dataset.beskjaer, 10) || 800;

        // Tilstanden er i visningens koordinater: bildets skala og hvor
        // ovre venstre hjorne ligger i forhold til firkanten.
        var tilstand = null;

        function visningsside() {
            return lerret.clientWidth;
        }

        // Skalaen som akkurat dekker firkanten. Zoom 1 er "fyll", aldri
        // mindre - da ville utsnittet fatt tomme kanter.
        function grunnskala(bilde) {
            return Math.max(visningsside() / bilde.width, visningsside() / bilde.height);
        }

        // Holder bildet innenfor firkanten, sa kantene aldri blir tomme.
        function avgrens() {
            var v = visningsside();
            var bredde = tilstand.bilde.width * tilstand.skala;
            var hoyde = tilstand.bilde.height * tilstand.skala;
            tilstand.x = Math.min(0, Math.max(v - bredde, tilstand.x));
            tilstand.y = Math.min(0, Math.max(v - hoyde, tilstand.y));
        }

        function tegn() {
            var v = visningsside();
            var tetthet = window.devicePixelRatio || 1;
            lerret.width = Math.round(v * tetthet);
            lerret.height = Math.round(v * tetthet);

            var ctx = lerret.getContext('2d');
            ctx.setTransform(tetthet, 0, 0, tetthet, 0, 0);
            ctx.clearRect(0, 0, v, v);
            ctx.drawImage(
                tilstand.bilde,
                tilstand.x, tilstand.y,
                tilstand.bilde.width * tilstand.skala,
                tilstand.bilde.height * tilstand.skala);
        }

        // Zoomer rundt et punkt i firkanten, sa det man ser pa, blir liggende
        // der det er. Uten dette glir motivet ut til siden ved hver zoom.
        function zoom(nyZoom, px, py) {
            var zoomet = Math.min(MAKS_ZOOM, Math.max(1, nyZoom));
            var nySkala = tilstand.grunn * zoomet;
            var forhold = nySkala / tilstand.skala;

            tilstand.x = px - (px - tilstand.x) * forhold;
            tilstand.y = py - (py - tilstand.y) * forhold;
            tilstand.skala = nySkala;
            tilstand.zoom = zoomet;
            zoomFelt.value = String(zoomet);

            avgrens();
            tegn();
        }

        function flytt(dx, dy) {
            tilstand.x += dx;
            tilstand.y += dy;
            avgrens();
            tegn();
        }

        felt.addEventListener('change', function () {
            var fil = felt.files && felt.files[0];

            if (!fil || fil.type.indexOf('image/') !== 0) {
                redigerer.hidden = true;
                tilstand = null;
                return;
            }

            // imageOrientation: 'from-image' snur bildet slik kameraet holdt
            // det, bade i forhandsvisningen og i utsnittet.
            createImageBitmap(fil, { imageOrientation: 'from-image' }).then(function (bilde) {
                redigerer.hidden = false;

                var grunn = grunnskala(bilde);
                var v = visningsside();
                tilstand = {
                    bilde: bilde,
                    grunn: grunn,
                    skala: grunn,
                    zoom: 1,
                    // Sentrert til a begynne med.
                    x: (v - bilde.width * grunn) / 2,
                    y: (v - bilde.height * grunn) / 2
                };

                zoomFelt.value = '1';
                tegn();
            }).catch(function () {
                // Kan ikke leses som bilde her. Serveren avgjor.
                redigerer.hidden = true;
                tilstand = null;
            });
        });

        // --- Dra -------------------------------------------------------
        // Pointer-hendelser dekker mus, finger og penn i ett.
        var forrige = null;

        lerret.addEventListener('pointerdown', function (e) {
            if (!tilstand) return;
            forrige = { x: e.clientX, y: e.clientY };
            // Fanger pekeren, sa dra fortsetter selv om fingeren glir utenfor
            // firkanten. Kan kaste for en peker nettleseren ikke kjenner -
            // da virker dra likevel, bare uten fangst.
            try { lerret.setPointerCapture(e.pointerId); } catch (feil) { /* se over */ }
        });

        lerret.addEventListener('pointermove', function (e) {
            if (!tilstand || !forrige) return;
            flytt(e.clientX - forrige.x, e.clientY - forrige.y);
            forrige = { x: e.clientX, y: e.clientY };
        });

        ['pointerup', 'pointercancel'].forEach(function (type) {
            lerret.addEventListener(type, function () { forrige = null; });
        });

        // --- Zoom ------------------------------------------------------
        zoomFelt.addEventListener('input', function () {
            if (!tilstand) return;
            var midt = visningsside() / 2;
            zoom(parseFloat(zoomFelt.value), midt, midt);
        });

        lerret.addEventListener('wheel', function (e) {
            if (!tilstand) return;
            e.preventDefault();
            var boks = lerret.getBoundingClientRect();
            zoom(tilstand.zoom * (e.deltaY < 0 ? 1.1 : 1 / 1.1),
                e.clientX - boks.left, e.clientY - boks.top);
        }, { passive: false });

        // --- Tastatur --------------------------------------------------
        // Piltastene flytter, + og - zoomer. Firkanten har tabindex, sa den
        // kan nas uten mus.
        lerret.addEventListener('keydown', function (e) {
            if (!tilstand) return;
            var midt = visningsside() / 2;
            var tiltak = {
                ArrowLeft: function () { flytt(PILSTEG, 0); },
                ArrowRight: function () { flytt(-PILSTEG, 0); },
                ArrowUp: function () { flytt(0, PILSTEG); },
                ArrowDown: function () { flytt(0, -PILSTEG); },
                '+': function () { zoom(tilstand.zoom * 1.1, midt, midt); },
                '-': function () { zoom(tilstand.zoom / 1.1, midt, midt); }
            }[e.key];

            if (tiltak) {
                e.preventDefault();
                tiltak();
            }
        });

        // --- Utsnittet -------------------------------------------------
        // Samme transformasjon som forhandsvisningen, skalert fra visningens
        // storrelse til utsnittets. Det brukeren sa i firkanten, er det som
        // lastes opp.
        felt.form.addEventListener('submit', function (e) {
            if (!tilstand || felt.dataset.beskaaret === 'ja') {
                return;
            }

            e.preventDefault();
            var knapper = felt.form.querySelectorAll('button[type=submit]');
            Array.prototype.forEach.call(knapper, function (k) { k.disabled = true; });

            var faktor = side / visningsside();
            var utsnitt = document.createElement('canvas');
            utsnitt.width = side;
            utsnitt.height = side;
            utsnitt.getContext('2d').drawImage(
                tilstand.bilde,
                tilstand.x * faktor, tilstand.y * faktor,
                tilstand.bilde.width * tilstand.skala * faktor,
                tilstand.bilde.height * tilstand.skala * faktor);

            utsnitt.toBlob(function (blob) {
                if (blob) {
                    var overforing = new DataTransfer();
                    overforing.items.add(new File([blob], 'profilbilde.jpg', { type: 'image/jpeg' }));
                    felt.files = overforing.files;
                }

                // Merket hindrer at submit-hendelsen beskjaerer en gang til
                // hvis skjemaet sendes pa nytt fra samme side.
                felt.dataset.beskaaret = 'ja';
                felt.form.submit();
            }, 'image/jpeg', KVALITET);
        });
    });
})();
