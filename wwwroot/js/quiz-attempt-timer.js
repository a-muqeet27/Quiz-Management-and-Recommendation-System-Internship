(function () {
    var timerElement = document.getElementById('quiz-timer');
    var form = document.getElementById('quiz-attempt-form');

    if (!timerElement || !form) {
        return;
    }

    var timerValue = document.getElementById('quiz-timer-value');
    var timerAlert = document.getElementById('quiz-timer-alert');
    var timedOutInput = document.getElementById('TimedOut');
    var saveButton = document.getElementById('save-quiz-btn');

    var totalSeconds = parseInt(timerElement.getAttribute('data-total') || timerElement.getAttribute('data-remaining') || '0', 10);
    var remainingSeconds = parseInt(timerElement.getAttribute('data-remaining') || '0', 10);
    var isSubmitting = false;

    function formatTime(totalSeconds) {
        var minutes = Math.floor(totalSeconds / 60);
        var seconds = totalSeconds % 60;
        return String(minutes).padStart(2, '0') + ':' + String(seconds).padStart(2, '0');
    }

    function updateTimerDisplay() {
        if (!timerValue) {
            return;
        }

        timerValue.textContent = formatTime(remainingSeconds);

        var fraction = totalSeconds > 0 ? remainingSeconds / totalSeconds : 0;

        timerElement.classList.remove('bg-success', 'bg-warning', 'bg-danger', 'text-dark');

        if (fraction > 0.5) {
            timerElement.classList.add('bg-success');
        } else if (fraction > 0.2) {
            timerElement.classList.add('bg-warning', 'text-dark');
        } else {
            timerElement.classList.add('bg-danger');
        }
    }

    function disableForm() {
        if (saveButton) {
            saveButton.disabled = true;
        }

        form.querySelectorAll('input, button, select, textarea').forEach(function (field) {
            if (field.type !== 'hidden') {
                field.disabled = true;
            }
        });
    }

    function submitQuiz(isTimedOut) {
        if (isSubmitting) {
            return;
        }

        isSubmitting = true;

        if (timedOutInput) {
            timedOutInput.value = isTimedOut ? 'true' : 'false';
        }

        if (isTimedOut && timerAlert) {
            timerAlert.classList.remove('d-none');
        }

        form.submit();
    }

    updateTimerDisplay();

    if (remainingSeconds <= 0) {
        submitQuiz(true);
        return;
    }

    var intervalId = window.setInterval(function () {
        remainingSeconds -= 1;
        updateTimerDisplay();

        if (remainingSeconds <= 0) {
            window.clearInterval(intervalId);
            submitQuiz(true);
        }
    }, 1000);

    form.addEventListener('submit', function () {
        if (!isSubmitting) {
            isSubmitting = true;
            window.clearInterval(intervalId);

            if (timedOutInput) {
                timedOutInput.value = 'false';
            }
        }
    });
})();