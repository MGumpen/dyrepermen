// jQuery Validation godtar kun punktum som desimalskilletegn. Applikasjonen
// kjorer med fast nb-NO-kultur, sa serveren tolker "3,15" riktig - men
// klientvalideringen avviser det for skjemaet i det hele tatt sendes.
//
// Symptomet er en engelsk feilmelding under et norsk felt, pa en verdi som
// er helt gyldig. Feilen er usynlig for tester som poster direkte til
// serveren, siden de aldri kjorer JavaScript.
//
// Ma lastes ETTER jquery.validate.unobtrusive.
(function ($) {
    'use strict';

    if (!$ || !$.validator) {
        return;
    }

    function tilTall(verdi) {
        if (typeof verdi !== 'string') {
            return verdi;
        }
        // Fjern mellomrom som tusenskille, bytt komma mot punktum.
        return parseFloat(verdi.replace(/\s/g, '').replace(',', '.'));
    }

    // Heltall, eller desimaltall med komma. Tillater tusenskille med
    // mellomrom, som er norsk standard.
    $.validator.methods.number = function (verdi, element) {
        return this.optional(element)
            || /^-?(?:\d+|\d{1,3}(?:[ ]\d{3})+)(?:,\d+)?$/.test(verdi);
    };

    $.validator.methods.range = function (verdi, element, param) {
        var tall = tilTall(verdi);
        return this.optional(element) || (tall >= param[0] && tall <= param[1]);
    };

    $.validator.methods.min = function (verdi, element, param) {
        return this.optional(element) || tilTall(verdi) >= param;
    };

    $.validator.methods.max = function (verdi, element, param) {
        return this.optional(element) || tilTall(verdi) <= param;
    };

    var talmelding = 'Skriv inn et tall. Bruk komma, for eksempel 3,15.';

    $.validator.messages.number = talmelding;

    // ASP.NET Core skriver ut data-val-number med sin egen engelske tekst,
    // og den attributten vinner over meldingen over. Den ma derfor byttes
    // ut i markupen.
    //
    // Dette kjores synkront, ikke i en ready-lytter: unobtrusive registrerte
    // sin ready-lytter da skriptet ble lastet, altsa for dette. Kjorte vi i
    // ready ogsa, ville vi kommet etter at reglene alt var lest.
    // Skriptet ligger nederst i body, sa skjemafeltene finnes allerede.
    document.querySelectorAll('[data-val-number]').forEach(function (felt) {
        felt.setAttribute('data-val-number', talmelding);
    });
})(window.jQuery);
