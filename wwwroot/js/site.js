document.querySelectorAll('.password-toggle-btn').forEach(function (button) {
    button.addEventListener('click', function () {
        var input = button.closest('.input-group')?.querySelector('.password-toggle-field');
        if (!input) {
            return;
        }

        var isHidden = input.type === 'password';
        input.type = isHidden ? 'text' : 'password';
        button.setAttribute('aria-label', isHidden ? 'Hide password' : 'Show password');
        button.setAttribute('title', isHidden ? 'Hide password' : 'Show password');
    });
});

document.querySelectorAll('.comment-share-form').forEach(function (form) {
    form.addEventListener('submit', function (event) {
        var shareUrl = form.getAttribute('data-share-url');
        if (!shareUrl || !navigator.clipboard) {
            return;
        }

        event.preventDefault();
        navigator.clipboard.writeText(shareUrl).then(function () {
            form.submit();
        }).catch(function () {
            form.submit();
        });
    });
});

(function () {
    var copiedUrl = document.body.getAttribute('data-copied-share-url');
    if (copiedUrl && navigator.clipboard) {
        navigator.clipboard.writeText(copiedUrl).catch(function () { });
    }

    if (window.location.hash && window.location.hash.startsWith('#comment-')) {
        var target = document.querySelector(window.location.hash);
        if (target) {
            target.classList.add('border-primary', 'shadow-sm');
            target.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }
})();

(function () {
    var modalElement = document.getElementById('deleteCommentModal');
    var confirmButton = document.getElementById('confirmDeleteCommentBtn');

    if (!modalElement || !confirmButton || !window.bootstrap) {
        return;
    }

    var pendingDeleteForm = null;
    var deleteModal = new bootstrap.Modal(modalElement);

    document.querySelectorAll('.comment-delete-form').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            event.preventDefault();
            pendingDeleteForm = form;
            deleteModal.show();
        });
    });

    confirmButton.addEventListener('click', function () {
        if (!pendingDeleteForm) {
            return;
        }

        var formToSubmit = pendingDeleteForm;
        pendingDeleteForm = null;
        deleteModal.hide();
        formToSubmit.submit();
    });

    modalElement.addEventListener('hidden.bs.modal', function () {
        pendingDeleteForm = null;
    });
})();
