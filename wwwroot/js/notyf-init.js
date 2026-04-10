/**
 * Notyf - Global Toast Notification Wrapper
 * Initializes Notyf and exposes a simple showToast(message, type) API.
 * Call this after notyf.min.js is loaded.
 */
(function () {
    'use strict';

    var notyf = null;

    function initNotyf() {
        if (typeof Notyf === 'undefined') {
            console.warn('[Notyf] Notyf library not loaded.');
            return;
        }
        if (notyf) return;

        notyf = new Notyf({
            duration: 4000,
            position: {
                x: 'right',
                y: 'top'
            },
            dismissible: true,
            ripple: true,
            types: [
                {
                    type: 'success',
                    backgroundColor: '#71dd88',
                    foregroundColor: '#ffffff',
                    rippleColor: 'rgba(113, 221, 136, 0.3)',
                    icon: {
                        className: 'icon-base bx bx-check-circle',
                        tagName: 'i',
                        color: '#ffffff'
                    }
                },
                {
                    type: 'error',
                    backgroundColor: '#ff3e1d',
                    foregroundColor: '#ffffff',
                    rippleColor: 'rgba(255, 62, 29, 0.3)',
                    icon: {
                        className: 'icon-base bx bx-x-circle',
                        tagName: 'i',
                        color: '#ffffff'
                    }
                },
                {
                    type: 'warning',
                    backgroundColor: '#ffab00',
                    foregroundColor: '#000000',
                    rippleColor: 'rgba(255, 171, 0, 0.3)',
                    icon: {
                        className: 'icon-base bx bx-warning',
                        tagName: 'i',
                        color: '#000000'
                    }
                },
                {
                    type: 'info',
                    backgroundColor: '#03c3ec',
                    foregroundColor: '#ffffff',
                    rippleColor: 'rgba(3, 195, 236, 0.3)',
                    icon: {
                        className: 'icon-base bx bx-info-circle',
                        tagName: 'i',
                        color: '#ffffff'
                    }
                }
            ]
        });
    }

    /**
     * Show a toast notification.
     * @param {string} message - The message to display.
     * @param {'success'|'error'|'warning'|'info'} type - Toast type.
     */
    function showToast(message, type) {
        if (!notyf) initNotyf();
        if (!notyf) {
            console.warn('[Notyf] showToast called but notyf not available:', message);
            return;
        }

        var t = (type || 'info').toLowerCase();
        var displayType;

        if (t === 'success') {
            displayType = 'success';
        } else if (t === 'error' || t === 'danger') {
            displayType = 'error';
        } else if (t === 'warning') {
            displayType = 'warning';
        } else {
            displayType = 'info';
        }

        notyf.open({ type: displayType, message: message });
    }

    // Expose globally
    window.showToast = showToast;
    window.notyf = {
        success: function (msg) { showToast(msg, 'success'); },
        error: function (msg) { showToast(msg, 'error'); },
        warning: function (msg) { showToast(msg, 'warning'); },
        info: function (msg) { showToast(msg, 'info'); }
    };

    // Auto-init when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initNotyf);
    } else {
        initNotyf();
    }

})();
