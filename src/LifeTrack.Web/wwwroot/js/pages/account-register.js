(function () {
    "use strict";

    const SELECTORS = {
        FORM: "form[data-register-form]",
        PASSWORD: "input[data-register-password]",
        CONFIRM: "input[data-register-confirm]",
        CHECKLIST: "[data-register-checklist]",
        RULE: "[data-register-rule]",
        RULE_KEY: "registerRule",
        CONFIRM_HINT: "[data-register-confirm-hint]"
    };

    const CSS_CLASSES = {
        RULE_MET: "app-password-checklist__rule--met",
        CONFIRM_MATCH: "app-password-confirm--match",
        CONFIRM_MISMATCH: "app-password-confirm--mismatch"
    };

    const CONSTANTS = {
        EVENT_INPUT: "input",
        EVENT_SUBMIT: "submit",
        CAPTURE: true,
        RULE_LENGTH: "length",
        RULE_LOWER: "lower",
        RULE_UPPER: "upper",
        RULE_DIGIT: "digit",
        RULE_SYMBOL: "symbol",
        MIN_LENGTH: 12,
        LOWER_PATTERN: /[a-záéíóúñü]/,
        UPPER_PATTERN: /[A-ZÁÉÍÓÚÑÜ]/,
        DIGIT_PATTERN: /[0-9]/,
        SYMBOL_PATTERN: /[^a-zA-Z0-9áéíóúÁÉÍÓÚñÑüÜ]/,
        MATCH_TEXT: "Las contraseñas coinciden.",
        MISMATCH_TEXT: "Las contraseñas aún no coinciden.",
        PASSWORD_MESSAGE: "La contraseña debe cumplir todos los requisitos.",
        CONFIRM_MESSAGE: "Las contraseñas no coinciden.",
        EMPTY_HINT_TEXT: "Repite la contraseña. Debe coincidir.",
        EMPTY: ""
    };

    function _checkRule(rule, password) {
        if (rule === CONSTANTS.RULE_LENGTH) {
            return password.length >= CONSTANTS.MIN_LENGTH;
        }

        if (rule === CONSTANTS.RULE_LOWER) {
            return CONSTANTS.LOWER_PATTERN.test(password);
        }

        if (rule === CONSTANTS.RULE_UPPER) {
            return CONSTANTS.UPPER_PATTERN.test(password);
        }

        if (rule === CONSTANTS.RULE_DIGIT) {
            return CONSTANTS.DIGIT_PATTERN.test(password);
        }

        if (rule === CONSTANTS.RULE_SYMBOL) {
            return CONSTANTS.SYMBOL_PATTERN.test(password);
        }

        return false;
    }

    function _renderChecklist(form) {
        const password = form.querySelector(SELECTORS.PASSWORD);
        const checklist = form.querySelector(SELECTORS.CHECKLIST);

        if (!password || !checklist) {
            return;
        }

        const rules = checklist.querySelectorAll(SELECTORS.RULE);
        let met = 0;

        for (let index = 0; index < rules.length; index++) {
            const rule = rules[index].dataset[SELECTORS.RULE_KEY];

            if (_checkRule(rule, password.value)) {
                rules[index].classList.add(CSS_CLASSES.RULE_MET);
                met++;
            } else {
                rules[index].classList.remove(CSS_CLASSES.RULE_MET);
            }
        }

        if (password.value === CONSTANTS.EMPTY || met === rules.length) {
            password.setCustomValidity(CONSTANTS.EMPTY);
            return;
        }

        password.setCustomValidity(CONSTANTS.PASSWORD_MESSAGE);
    }

    function _renderConfirm(form) {
        const password = form.querySelector(SELECTORS.PASSWORD);
        const confirm = form.querySelector(SELECTORS.CONFIRM);
        const hint = form.querySelector(SELECTORS.CONFIRM_HINT);

        if (!password || !confirm || !hint) {
            return;
        }

        confirm.classList.remove(CSS_CLASSES.CONFIRM_MATCH);
        confirm.classList.remove(CSS_CLASSES.CONFIRM_MISMATCH);

        if (confirm.value === CONSTANTS.EMPTY) {
            confirm.setCustomValidity(CONSTANTS.EMPTY);
            hint.textContent = CONSTANTS.EMPTY_HINT_TEXT;
            return;
        }

        if (confirm.value === password.value) {
            confirm.classList.add(CSS_CLASSES.CONFIRM_MATCH);
            confirm.setCustomValidity(CONSTANTS.EMPTY);
            hint.textContent = CONSTANTS.MATCH_TEXT;
            return;
        }

        confirm.classList.add(CSS_CLASSES.CONFIRM_MISMATCH);
        confirm.setCustomValidity(CONSTANTS.CONFIRM_MESSAGE);
        hint.textContent = CONSTANTS.MISMATCH_TEXT;
    }

    function _handleInput(event) {
        const form = event.target.form;

        if (!form || !form.matches(SELECTORS.FORM)) {
            return;
        }

        _renderChecklist(form);
        _renderConfirm(form);
    }

    function _handleSubmit(event) {
        const form = event.target;

        if (!form || !form.matches || !form.matches(SELECTORS.FORM)) {
            return;
        }

        _renderChecklist(form);
        _renderConfirm(form);
    }

    function _initialize() {
        const forms = document.querySelectorAll(SELECTORS.FORM);

        for (let index = 0; index < forms.length; index++) {
            _renderChecklist(forms[index]);
            _renderConfirm(forms[index]);
        }

        document.addEventListener(CONSTANTS.EVENT_INPUT, _handleInput);
        document.addEventListener(CONSTANTS.EVENT_SUBMIT, _handleSubmit, CONSTANTS.CAPTURE);
    }

    _initialize();
}());
