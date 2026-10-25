(function () {
    "use strict";

    const SELECTORS = {
        FORM: "form[data-dashboard-filters]",
        YEAR_SELECT: "select[name='year']",
        MONTH_SELECT: "select[name='month']"
    };

    const CONSTANTS = {
        EVENT_CHANGE: "change"
    };

    function _handleFilterChange(event) {
        const form = event.target.closest(SELECTORS.FORM);
        if (form) {
            form.submit();
        }
    }

    function _initialize() {
        const yearSelect = document.querySelector(SELECTORS.YEAR_SELECT);
        const monthSelect = document.querySelector(SELECTORS.MONTH_SELECT);

        if (yearSelect) {
            yearSelect.addEventListener(CONSTANTS.EVENT_CHANGE, _handleFilterChange);
        }
        if (monthSelect) {
            monthSelect.addEventListener(CONSTANTS.EVENT_CHANGE, _handleFilterChange);
        }
    }

    _initialize();
}());