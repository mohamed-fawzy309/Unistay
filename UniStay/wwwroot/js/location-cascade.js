(function () {
    'use strict';

    var _data = null;
    var _promise = null;

    function loadLocations() {
        if (_data) return Promise.resolve(_data);
        if (_promise) return _promise;
        _promise = fetch('/json/egypt_locations.json')
            .then(function (r) {
                if (!r.ok) throw new Error('HTTP ' + r.status);
                return r.json();
            })
            .then(function (d) {
                _data = d;
                return d;
            });
        return _promise;
    }

    function addPlaceholder(sel, text) {
        sel.innerHTML = '<option value="">' + text + '</option>';
    }

    function populate(sel, items, textKey, placeholder) {
        addPlaceholder(sel, placeholder);
        items.forEach(function (item) {
            var opt = document.createElement('option');
            opt.value = item[textKey];
            opt.textContent = item[textKey];
            sel.appendChild(opt);
        });
    }

    function findGov(data, val) {
        for (var i = 0; i < data.length; i++) {
            if (data[i].name_ar === val || data[i].name_en === val) return data[i];
        }
        return null;
    }

    function findCenter(gov, val) {
        if (!gov || !gov.centers) return null;
        for (var i = 0; i < gov.centers.length; i++) {
            if (gov.centers[i].name_ar === val || gov.centers[i].name_en === val) return gov.centers[i];
        }
        return null;
    }

    window.initLocationCascade = function (govId, markazId, cityId, preselected) {
        preselected = preselected || {};

        var govSel = document.getElementById(govId);
        var markazSel = document.getElementById(markazId);
        var citySel = document.getElementById(cityId);
        if (!govSel || !markazSel || !citySel) return;

        loadLocations()
            .then(function (data) {
                populate(govSel, data, 'name_ar', '-- اختر المحافظة --');

                if (preselected.governorate) govSel.value = preselected.governorate;

                govSel.addEventListener('change', function () {
                    var gov = findGov(data, govSel.value);
                    var centers = gov && gov.centers ? gov.centers : [];
                    populate(markazSel, centers, 'name_ar', '-- اختر المركز --');
                    addPlaceholder(citySel, '-- اختر القرية --');

                    if (preselected.markaz && govSel.value === preselected.governorate) {
                        markazSel.value = preselected.markaz;
                        var evt = new Event('change');
                        markazSel.dispatchEvent(evt);
                    }
                });

                markazSel.addEventListener('change', function () {
                    var gov = findGov(data, govSel.value);
                    var center = findCenter(gov, markazSel.value);
                    var cities = center && center.cities ? center.cities : [];
                    populate(citySel, cities, 'name_ar', '-- اختر القرية --');

                    if (preselected.city && markazSel.value === preselected.markaz && govSel.value === preselected.governorate) {
                        citySel.value = preselected.city;
                    }
                });

                if (preselected.governorate) {
                    var evt = new Event('change');
                    govSel.dispatchEvent(evt);
                }
            })
            .catch(function (err) {
                console.error('initLocationCascade:', err);
            });
    };
})();
