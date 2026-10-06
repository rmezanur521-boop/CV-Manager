(function () {
    const modalEl = document.getElementById('supportTicketModal');
    if (!modalEl) return;

    const form = document.getElementById('supportTicketForm');
    const summaryInput = document.getElementById('supportSummary');
    const charCounter = document.getElementById('supportCharCounter');
    const alertBox = document.getElementById('supportTicketAlert');
    const submitBtn = document.getElementById('supportSubmitBtn');
    const submitSpinner = document.getElementById('supportSubmitSpinner');
    const submitText = document.getElementById('supportSubmitText');
    const pageUrlInput = document.getElementById('supportPageUrl');
    const positionIdInput = document.getElementById('supportPositionId');
    const contextPageText = document.getElementById('supportContextPageText');
    const contextPositionText = document.getElementById('supportContextPositionText');

    const originalSubmitText = submitText ? submitText.textContent : 'Submit Ticket';

    function detectPositionContext() {
        let posId = '';
        let posTitle = '';

        const idInput = document.querySelector('input[name="Id"], input[name="PositionId"]');
        if (idInput && idInput.value && !isNaN(parseInt(idInput.value, 10))) {
            posId = idInput.value;
        }

        if (!posId) {
            const pathMatch = window.location.pathname.match(/\/Positions\/(?:Edit|Details|Duplicate)\/(\d+)/i);
            if (pathMatch && pathMatch[1]) {
                posId = pathMatch[1];
            }
        }

        if (!posId) {
            const discMatch = window.location.pathname.match(/\/Discussions(?:\/Index)?\/(\d+)/i);
            if (discMatch && discMatch[1]) {
                posId = discMatch[1];
            }
        }

        if (!posId) {
            const urlParams = new URLSearchParams(window.location.search);
            const qPos = urlParams.get('positionId');
            if (qPos && !isNaN(parseInt(qPos, 10))) {
                posId = qPos;
            }
        }

        const titleInput = document.querySelector('input[name="Title"]');
        if (titleInput && titleInput.value && titleInput.value.trim().length > 0) {
            posTitle = titleInput.value.trim();
        }

        if (!posTitle && posId) {
            const heading = document.querySelector('.page-header h1, .section-title h1, h1');
            if (heading && heading.textContent && heading.textContent.trim().length > 0) {
                const text = heading.textContent.trim();
                if (!text.toLowerCase().includes('position') && !text.toLowerCase().includes('पজিশন')) {
                    posTitle = text;
                }
            }
        }

        return { id: posId, title: posTitle };
    }

    modalEl.addEventListener('show.bs.modal', function () {
        if (alertBox) {
            alertBox.className = 'alert d-none mb-3';
            alertBox.textContent = '';
        }

        const currentUrl = window.location.href;
        if (pageUrlInput) {
            pageUrlInput.value = currentUrl;
        }
        if (contextPageText) {
            contextPageText.textContent = window.location.pathname + window.location.search;
            contextPageText.title = currentUrl;
        }

        const context = detectPositionContext();
        if (positionIdInput) {
            positionIdInput.value = context.id;
        }
        if (contextPositionText) {
            const noneText = modalEl.getAttribute('data-context-none') || 'None';
            if (context.title && context.id) {
                contextPositionText.textContent = `${context.title} (#${context.id})`;
            } else if (context.title) {
                contextPositionText.textContent = context.title;
            } else if (context.id) {
                contextPositionText.textContent = `#${context.id}`;
            } else {
                contextPositionText.textContent = noneText;
            }
        }

        if (summaryInput && charCounter) {
            charCounter.textContent = summaryInput.value.length + ' / 500';
        }

        setLoading(false);
    });

    if (summaryInput && charCounter) {
        summaryInput.addEventListener('input', function () {
            charCounter.textContent = summaryInput.value.length + ' / 500';
        });
    }

    function setLoading(isLoading) {
        if (!submitBtn) return;
        submitBtn.disabled = isLoading;
        if (submitSpinner) {
            submitSpinner.classList.toggle('d-none', !isLoading);
        }
        if (submitText) {
            const submittingText = modalEl.getAttribute('data-submitting-text') || '...';
            submitText.textContent = isLoading ? submittingText : originalSubmitText;
        }
    }

    if (form) {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            const summary = summaryInput ? summaryInput.value.trim() : '';
            if (summary.length < 5) {
                if (alertBox) {
                    alertBox.className = 'alert alert-danger mb-3';
                    alertBox.textContent = 'Summary must be at least 5 characters.';
                }
                return;
            }

            setLoading(true);
            if (alertBox) {
                alertBox.className = 'alert d-none mb-3';
            }

            try {
                const formData = new FormData(form);
                const response = await fetch('/Support/Create', {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'Accept': 'application/json'
                    }
                });

                if (response.status === 429) {
                    const rateLimitMsg = modalEl.getAttribute('data-rate-limit-msg') || 'You have submitted too many tickets recently. Please wait a few minutes.';
                    if (alertBox) {
                        alertBox.className = 'alert alert-warning mb-3';
                        alertBox.textContent = rateLimitMsg;
                    }
                    return;
                }

                const data = await response.json();

                if (response.ok && data.success) {
                    const templateMsg = modalEl.getAttribute('data-success-msg') || 'Support ticket #{0} created successfully!';
                    const displayMsg = data.ticketId ? templateMsg.replace('{0}', data.ticketId) : data.message;

                    if (alertBox) {
                        alertBox.className = 'alert alert-success mb-3';
                        alertBox.textContent = displayMsg;
                    }
                    form.reset();
                    if (charCounter) charCounter.textContent = '0 / 500';
                    setTimeout(() => {
                        const modalInstance = bootstrap.Modal.getInstance(modalEl);
                        if (modalInstance) {
                            modalInstance.hide();
                        }
                    }, 2000);
                } else {
                    const defaultErrorMsg = modalEl.getAttribute('data-error-msg') || 'Unable to submit support ticket. Please try again later.';
                    if (alertBox) {
                        alertBox.className = 'alert alert-danger mb-3';
                        alertBox.textContent = data.message || defaultErrorMsg;
                    }
                }
            } catch (err) {
                const defaultErrorMsg = modalEl.getAttribute('data-error-msg') || 'Unable to submit support ticket. Please try again later.';
                if (alertBox) {
                    alertBox.className = 'alert alert-danger mb-3';
                    alertBox.textContent = defaultErrorMsg;
                }
            } finally {
                setLoading(false);
            }
        });
    }
})();
