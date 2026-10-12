const Modal = (function () {
    "use strict";

    const SELECTORS = {
        OPEN_TRIGGER: "[data-modal-open]",
        CLOSE_TRIGGER: "[data-modal-close]",
        MODAL: "[data-app-modal]",
        OPEN_MODAL: "[data-app-modal].show",
        MODAL_OPEN_ATTRIBUTE: "data-modal-open",
        ARIA_HIDDEN: "aria-hidden",
        ARIA_MODAL: "aria-modal",
        ROLE: "role"
    };

    const CSS_CLASSES = {};

    const CONSTANTS = {
        EVENT_CLICK: "click",
        EVENT_KEYDOWN: "keydown",
        EVENT_HIDDEN: "hidden.bs.modal",
        ESCAPE_KEY: "Escape",
        TRUE: "true",
        FALSE: "false",
        DIALOG_ROLE: "dialog"
    };

    const focusByModal = new Map();

    function _getModal(modalId) {
        const modal = document.getElementById(modalId);
        return modal && modal.matches(SELECTORS.MODAL) ? modal : null;
    }

    function _setOpenState(modal) {
        modal.setAttribute(SELECTORS.ARIA_HIDDEN, CONSTANTS.FALSE);
        modal.setAttribute(SELECTORS.ARIA_MODAL, CONSTANTS.TRUE);
        modal.setAttribute(SELECTORS.ROLE, CONSTANTS.DIALOG_ROLE);
    }

    function _setClosedState(modal) {
        modal.setAttribute(SELECTORS.ARIA_HIDDEN, CONSTANTS.TRUE);
        modal.removeAttribute(SELECTORS.ARIA_MODAL);
    }

    function _restoreFocus(modal) {
        const trigger = focusByModal.get(modal.id);
        focusByModal.delete(modal.id);

        if (trigger && document.contains(trigger)) {
            trigger.focus();
        }
    }

    function _handleHidden(event) {
        const modal = event.target;

        if (!modal.matches(SELECTORS.MODAL)) {
            return;
        }

        _setClosedState(modal);
        _restoreFocus(modal);
    }

    function open(modalId) {
        const modal = _getModal(modalId);

        if (!modal || !window.bootstrap || !window.bootstrap.Modal) {
            return;
        }

        const activeElement = document.activeElement;

        if (activeElement instanceof HTMLElement) {
            focusByModal.set(modalId, activeElement);
        }

        _setOpenState(modal);
        window.bootstrap.Modal.getOrCreateInstance(modal).show();
    }

    function close(modalId) {
        const modal = _getModal(modalId);

        if (!modal || !window.bootstrap || !window.bootstrap.Modal) {
            return;
        }

        window.bootstrap.Modal.getOrCreateInstance(modal).hide();
    }

    function closeAll() {
        document.querySelectorAll(SELECTORS.OPEN_MODAL).forEach(function (modal) {
            close(modal.id);
        });
    }

    function _handleClick(event) {
        const openTrigger = event.target.closest(SELECTORS.OPEN_TRIGGER);

        if (openTrigger) {
            const modalId = openTrigger.getAttribute(SELECTORS.MODAL_OPEN_ATTRIBUTE);

            if (modalId) {
                event.preventDefault();
                open(modalId);
            }

            return;
        }

        const closeTrigger = event.target.closest(SELECTORS.CLOSE_TRIGGER);

        if (!closeTrigger) {
            return;
        }

        const modal = closeTrigger.closest(SELECTORS.MODAL);

        if (modal) {
            event.preventDefault();
            close(modal.id);
        }
    }

    function _handleKeydown(event) {
        if (event.key === CONSTANTS.ESCAPE_KEY) {
            closeAll();
        }
    }

    document.addEventListener(CONSTANTS.EVENT_CLICK, _handleClick);
    document.addEventListener(CONSTANTS.EVENT_KEYDOWN, _handleKeydown);
    document.addEventListener(CONSTANTS.EVENT_HIDDEN, _handleHidden);

    return { open, close, closeAll };
}());
