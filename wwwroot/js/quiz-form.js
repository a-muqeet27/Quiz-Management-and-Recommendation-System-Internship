(function () {
    const form = document.getElementById('quizForm');
    if (!form) return;

    const panels = Array.from(document.querySelectorAll('.quiz-wizard-panel'));
    const steps = Array.from(document.querySelectorAll('.quiz-step'));
    const prevBtn = document.getElementById('prevStepBtn');
    const nextBtn = document.getElementById('nextStepBtn');
    const submitBtn = document.getElementById('submitQuizBtn');

    let currentStep = 1;
    const totalSteps = panels.length;

    const fields = {
        title: document.getElementById('Title'),
        subject: document.getElementById('SubjectId'),
        topic: document.getElementById('TopicId'),
        questions: document.getElementById('NoOfQuestions'),
        marks: document.getElementById('TotalMarks'),
        time: document.getElementById('TimeLimitMinutes'),
        difficulty: document.getElementById('DifficultyFilter'),
        questionType: document.getElementById('QuestionTypeFilter'),
        isActive: document.getElementById('IsActive')
    };

    function selectedText(select) {
        if (!select || select.selectedIndex < 0) return '-';
        return select.options[select.selectedIndex].text;
    }

    function difficultyLabel() {
        return selectedText(fields.difficulty).replace('Any difficulty', 'Any');
    }

    function typeLabel() {
        return selectedText(fields.questionType).replace('Any type', 'Any');
    }

    function updatePreview() {
        const title = fields.title?.value?.trim() || 'Untitled Quiz';
        document.getElementById('previewTitle').textContent = title;
        document.getElementById('previewSubject').textContent = selectedText(fields.subject);
        document.getElementById('previewTopic').textContent = selectedText(fields.topic).replace('-- Any Topic --', 'Any');
        document.getElementById('previewQuestions').textContent = fields.questions?.value || '-';
        document.getElementById('previewMarks').textContent = fields.marks?.value || '-';
        document.getElementById('previewTime').textContent = fields.time?.value ? `${fields.time.value} min` : '-';

        document.getElementById('reviewTitle').textContent = title;
        document.getElementById('reviewSubject').textContent = selectedText(fields.subject);
        document.getElementById('reviewTopic').textContent = selectedText(fields.topic).replace('-- Any Topic --', 'Any');
        document.getElementById('reviewQuestions').textContent = fields.questions?.value || '-';
        document.getElementById('reviewMarks').textContent = fields.marks?.value || '-';
        document.getElementById('reviewTime').textContent = fields.time?.value ? `${fields.time.value} minutes` : '-';
        document.getElementById('reviewDifficulty').textContent = difficultyLabel();
        document.getElementById('reviewType').textContent = typeLabel();
        document.getElementById('reviewStatus').textContent = fields.isActive?.checked ? 'Active' : 'Inactive';
    }

    function showStep(step) {
        currentStep = step;

        panels.forEach(panel => {
            panel.classList.toggle('active', Number(panel.dataset.panel) === step);
        });

        steps.forEach(stepEl => {
            const stepNumber = Number(stepEl.dataset.step);
            stepEl.classList.toggle('active', stepNumber === step);
            stepEl.classList.toggle('completed', stepNumber < step);
        });

        prevBtn.style.visibility = step === 1 ? 'hidden' : 'visible';
        nextBtn.classList.toggle('d-none', step === totalSteps);
        submitBtn.classList.toggle('d-none', step !== totalSteps);

        if (step === 3) {
            refreshQuestionCount();
        }

        if (step === 4) {
            updatePreview();
        }
    }

    function validateStep(step) {
        if (step === 1) {
            if (!fields.title?.value.trim()) {
                fields.title?.focus();
                fields.title?.reportValidity?.();
                return false;
            }

            if (!fields.subject?.value) {
                fields.subject?.focus();
                alert('Please select a subject.');
                return false;
            }
        }

        if (step === 2) {
            if (!fields.questions?.reportValidity()) return false;
            if (!fields.marks?.reportValidity()) return false;
        }

        return true;
    }

    async function loadTopics(subjectId, selectedTopicId) {
        const topicSelect = fields.topic;
        topicSelect.innerHTML = '<option value="">-- Any Topic --</option>';

        if (!subjectId) {
            updatePreview();
            return;
        }

        try {
            const response = await fetch(`/Quiz/GetTopics?subjectId=${subjectId}`);
            const topics = await response.json();

            topics.forEach(topic => {
                const option = document.createElement('option');
                option.value = topic.topicId;
                option.textContent = topic.topicName;
                if (selectedTopicId && Number(selectedTopicId) === topic.topicId) {
                    option.selected = true;
                }
                topicSelect.appendChild(option);
            });
        } catch {
            console.error('Failed to load topics.');
        }

        updatePreview();
    }

    async function refreshQuestionCount() {
        const subjectId = fields.subject?.value;
        const info = document.getElementById('availableQuestionText');

        if (!subjectId) {
            info.textContent = 'Select a subject to see available questions.';
            return;
        }

        const params = new URLSearchParams({ subjectId });
        if (fields.topic?.value) params.append('topicId', fields.topic.value);
        if (fields.difficulty?.value) params.append('difficulty', fields.difficulty.value);
        if (fields.questionType?.value) params.append('questionType', fields.questionType.value);

        try {
            const response = await fetch(`/Quiz/GetQuestionCount?${params.toString()}`);
            const data = await response.json();
            info.textContent = `${data.count} question(s) match your current filters in the question bank.`;
        } catch {
            info.textContent = 'Could not load question count.';
        }
    }

    prevBtn?.addEventListener('click', () => {
        if (currentStep > 1) showStep(currentStep - 1);
    });

    nextBtn?.addEventListener('click', () => {
        if (!validateStep(currentStep)) return;
        updatePreview();
        if (currentStep < totalSteps) showStep(currentStep + 1);
    });

    fields.subject?.addEventListener('change', () => {
        loadTopics(fields.subject.value);
        refreshQuestionCount();
    });

    [fields.topic, fields.difficulty, fields.questionType].forEach(el => {
        el?.addEventListener('change', () => {
            updatePreview();
            refreshQuestionCount();
        });
    });

    ['input', 'change'].forEach(eventName => {
        form.addEventListener(eventName, updatePreview);
    });

    showStep(1);
    updatePreview();

    if (fields.subject?.value) {
        loadTopics(fields.subject.value, fields.topic?.value);
    }
})();
