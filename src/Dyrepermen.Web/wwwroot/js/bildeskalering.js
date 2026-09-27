// Skalerer bilder ned for de lastes opp. Et mobilbilde er 3-8 MB rett fra
// kameraet; en kvittering er like lesbar pa 1600 px som jpeg, og da er den et
// par hundre KB. Vedleggene ligger i databasen, som har en fast grense - se
// ADR 0018.
//
// Gjelder <input type="file" data-skaler-bilder>. Verdien er lengste side i
// piksler (data-skaler-bilder="1200"); uten verdi brukes 1600, som holder for
// en kvittering. PDF-er og alt som ikke er bilder, sendes som de er. Feiler
// skaleringen, sendes originalen: serveren sjekker type og storrelse uansett,
// sa skriptet er en forbedring og ingen sperre.
(function () {
    'use strict';

    var STANDARD_SIDE = 1600;
    var KVALITET = 0.8;

    // Mindre enn dette er allerede smatt nok. Ingen grunn til a rekomprimere.
    var TERSKEL_BYTE = 500 * 1024;

    function skalerbar(fil) {
        return fil.type.indexOf('image/') === 0 && fil.size > TERSKEL_BYTE;
    }

    // imageOrientation: 'from-image' snur bildet slik kameraet holdt det.
    // Uten den havner en kvittering tatt pa hoykant pa siden.
    function dekod(fil) {
        return createImageBitmap(fil, { imageOrientation: 'from-image' });
    }

    function skaler(fil, maksSide) {
        return dekod(fil).then(function (bilde) {
            var faktor = Math.min(1, maksSide / Math.max(bilde.width, bilde.height));
            var lerret = document.createElement('canvas');
            lerret.width = Math.round(bilde.width * faktor);
            lerret.height = Math.round(bilde.height * faktor);
            lerret.getContext('2d').drawImage(bilde, 0, 0, lerret.width, lerret.height);
            bilde.close();

            return new Promise(function (ferdig) {
                lerret.toBlob(function (blob) {
                    // Blir den ikke mindre, beholdes originalen.
                    if (!blob || blob.size >= fil.size) {
                        ferdig(fil);
                        return;
                    }

                    var navn = fil.name.replace(/\.[^.]*$/, '') + '.jpg';
                    ferdig(new File([blob], navn, { type: 'image/jpeg' }));
                }, 'image/jpeg', KVALITET);
            });
        }).catch(function () {
            return fil;
        });
    }

    function knapper(felt) {
        return felt.form ? felt.form.querySelectorAll('button[type=submit]') : [];
    }

    function behandle(felt) {
        var filer = Array.prototype.slice.call(felt.files);
        var maksSide = parseInt(felt.dataset.skalerBilder, 10) || STANDARD_SIDE;

        if (!filer.some(skalerbar) || typeof DataTransfer === 'undefined') {
            return;
        }

        // Lagre-knappen sperres mens bildene skaleres, ellers kan skjemaet
        // sendes med originalene mens arbeidet pagar.
        var sperret = knapper(felt);
        Array.prototype.forEach.call(sperret, function (k) { k.disabled = true; });

        Promise.all(filer.map(function (fil) {
            return skalerbar(fil) ? skaler(fil, maksSide) : Promise.resolve(fil);
        })).then(function (ferdige) {
            var overforing = new DataTransfer();
            ferdige.forEach(function (fil) { overforing.items.add(fil); });
            felt.files = overforing.files;
        }).finally(function () {
            Array.prototype.forEach.call(sperret, function (k) { k.disabled = false; });
        });
    }

    if (typeof createImageBitmap !== 'function') {
        return;
    }

    document.querySelectorAll('input[type=file][data-skaler-bilder]').forEach(function (felt) {
        felt.addEventListener('change', function () { behandle(felt); });
    });
})();
