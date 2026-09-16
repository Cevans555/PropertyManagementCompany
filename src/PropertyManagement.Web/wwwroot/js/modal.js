(() => {
    const modalElement = document.getElementById('app-modal');
    if (!modalElement || !window.bootstrap) return;

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const content = modalElement.querySelector('.modal-content');
    const ajaxHeaders = { 'X-Requested-With': 'XMLHttpRequest' };
    let refreshSelectors = [];
    let reloadOnSuccess = false;

    content.addEventListener('change', updateConditionalFields);

    content.addEventListener('input', event => {
        const field = event.target;
        if (!field.classList || !field.classList.contains('input-validation-error')) return;
        if (window.jQuery && jQuery.fn.valid && field.form) jQuery(field).valid();
    });

    document.addEventListener('click', async (event) => {
        const trigger = event.target.closest('[data-modal-url]');
        if (!trigger) return;

        event.preventDefault();
        refreshSelectors = (trigger.dataset.modalRefresh || '')
            .split(',')
            .map(selector => selector.trim())
            .filter(Boolean);
        reloadOnSuccess = trigger.dataset.modalReload === 'true';

        try {
            const response = await fetch(trigger.dataset.modalUrl, { headers: ajaxHeaders });
            if (redirectedToAccount(response)) return;
            if (!response.ok) {
                showError(response.status === 404 ? 'That item no longer exists.' : 'Something went wrong. Please try again.');
                return;
            }
            render(await response.text());
            modal.show();
        } catch {
            showError('Could not reach the server. Please try again.');
        }
    });

    modalElement.addEventListener('submit', async (event) => {
        const form = event.target.closest('form');
        if (!form) return;

        event.preventDefault();
        if (window.jQuery && jQuery.fn.valid && !jQuery(form).valid()) return;

        const submitButton = form.querySelector('[type="submit"]');
        if (submitButton) submitButton.disabled = true;

        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: ajaxHeaders });
            if (redirectedToAccount(response)) return;

            const contentType = response.headers.get('content-type') || '';
            if (response.ok && contentType.includes('application/json')) {
                modal.hide();
                if (reloadOnSuccess) {
                    window.location.reload();
                    return;
                }
                await refreshTargets(refreshSelectors);
            } else if (contentType.includes('text/html')) {
                render(await response.text());
            } else {
                showError('Something went wrong. Please try again.');
            }
        } catch {
            showError('Could not reach the server. Please try again.');
        } finally {
            if (submitButton && submitButton.isConnected) submitButton.disabled = false;
        }
    });

    function updateConditionalFields() {
        content.querySelectorAll('[data-show-when]').forEach(element => {
            const name = CSS.escape(element.dataset.showWhen);
            const field = content.querySelector(`input[name="${name}"]:checked, select[name="${name}"]`);
            element.hidden = !field || field.value !== element.dataset.showValue;
        });
    }

    function render(html) {
        content.innerHTML = html;
        updateConditionalFields();

        const form = content.querySelector('form');
        if (form && window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
            jQuery(form).removeData('validator').removeData('unobtrusiveValidation');
            jQuery.validator.unobtrusive.parse(form);
        }

        const firstField = content.querySelector('.is-invalid, .input-validation-error, input:not([type="hidden"]), select, textarea');
        if (firstField) firstField.focus();
    }

    async function refreshTargets(selectors) {
        for (const selector of selectors) {
            const target = document.querySelector(selector);
            if (!target || !target.dataset.refreshUrl) continue;

            const response = await fetch(target.dataset.refreshUrl, { headers: ajaxHeaders });
            if (response.ok) target.innerHTML = await response.text();
        }
    }

    function redirectedToAccount(response) {
        if (response.redirected && new URL(response.url).pathname.startsWith('/Account/')) {
            window.location.href = response.url;
            return true;
        }
        return false;
    }

    function showError(message) {
        content.innerHTML =
            '<div class="modal-header"><h2 class="modal-title fs-5">Error</h2>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>' +
            '<div class="modal-body"><p class="text-danger mb-0"></p></div>';
        content.querySelector('p').textContent = message;
        modal.show();
    }
})();
