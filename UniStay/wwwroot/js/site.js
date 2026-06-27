// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
    'use strict';

    // Auto-initialize location cascade if the required dropdowns exist on the page
    var govSel = document.getElementById('governorateSelect');
    var markazSel = document.getElementById('markazSelect');
    var citySel = document.getElementById('villageSelect');
    if (govSel && markazSel && citySel && typeof initLocationCascade === 'function') {
        initLocationCascade('governorateSelect', 'markazSelect', 'villageSelect');
    }
})();
