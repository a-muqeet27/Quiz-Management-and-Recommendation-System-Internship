(function () {
    const form = document.getElementById('quizForm');
    if (!form) return;

    const panels = Array.from(document.querySelectorAll('.quiz-wizard-panel'));
    const prevBtn = document.getElementById('prevStepBtn');
    const nextBtn = document.getElementById('nextStepBtn');
    const submitBtn = document.getElementById('submitQuizBtn');

    let currentStep = 1;
    const totalSteps = panels.length;
    let availableCount = 0;

    const fields = {
        title: document.getElementById('Title'),
        subject: document.getElementById('SubjectId'),
        topic: document.getElementById('TopicId'),
        questions: document.getElementById('NoOfQuestions'),
        arrangements: document.getElementById('ArrangementCount'),
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

    function difficultyLabel(value) {
        return { 0: 'Easy', 1: 'Medium', 2: 'Hard' }[value] || 'Any';
    }

    function typeLabel(value) {
        return { 1: 'Single Correct', 0: 'Multiple Correct' }[value] || 'Any';
    }

    function updateReview() {
        const title = fields.title?.value?.trim() || 'Untitled Quiz';
        const requested = Number(fields.questions?.value || 0);

        document.getElementById('reviewTitle').textContent = title;
        document.getElementById('reviewSubject').textContent = selectedText(fields.subject);
        document.getElementById('reviewTopic').textContent = selectedText(fields.topic).replace('-- Any Topic --', 'Any');
        document.getElementById('reviewQuestions').textContent = fields.questions?.value || '-';
        const reviewArrangements = document.getElementById('reviewArrangements');
        if (reviewArrangements) {
            reviewArrangements.textContent = fields.arrangements?.value || '1';
        }
        document.getElementById('reviewAvailable').textContent = availableCount;
        document.getElementById('reviewMarks').textContent = fields.marks?.value || '-';
        document.getElementById('reviewTime').textContent = fields.time?.value ? `${fields.time.value} minutes` : '-';
        const difficultyRaw = fields.difficulty?.value;
        document.getElementById('reviewDifficulty').textContent =
            (difficultyRaw === undefined || difficultyRaw === null || difficultyRaw === '')
                ? 'Any'
                : difficultyLabel(Number(difficultyRaw));

        // If the select is empty, show "Any" instead of mapping ""/0.
        const questionTypeRaw = fields.questionType?.value;
        document.getElementById('reviewType').textContent =
            (questionTypeRaw === undefined || questionTypeRaw === null || questionTypeRaw === '')
                ? 'Any'
                : typeLabel(Number(questionTypeRaw));
        document.getElementById('reviewStatus').textContent = fields.isActive?.checked ? 'Active' : 'Inactive';

        const info = document.getElementById('availableQuestionText');
        if (!fields.subject?.value) {
            info.textContent = 'Select a subject to see how many questions are available for random generation.';
        } else if (availableCount < requested) {
            info.textContent = `Only ${availableCount} question(s) match your filters, but you asked for ${requested}. Add more questions or lower the count.`;
            info.parentElement.className = 'alert alert-warning mt-4 mb-0';
        } else {
            info.textContent = `${availableCount} question(s) match your filters. The quiz will randomly pick ${requested} of them.`;
            info.parentElement.className = 'alert alert-info mt-4 mb-0';
        }
    }

    function showStep(step) {
        currentStep = step;

        panels.forEach(panel => {
            panel.classList.toggle('active', Number(panel.dataset.panel) === step);
        });

        prevBtn.style.visibility = step === 1 ? 'hidden' : 'visible';
        nextBtn.classList.toggle('d-none', step === totalSteps);
        submitBtn.classList.toggle('d-none', step !== totalSteps);

        if (step === 2 || step === 3) {
            refreshQuestionCount().then(updateReview);
        }

        if (step === 3) {
            updateReview();
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
            if (fields.arrangements && !fields.arrangements.reportValidity()) return false;
            if (!fields.marks?.reportValidity()) return false;

            const requested = Number(fields.questions?.value || 0);
            if (availableCount < requested) {
                alert(`Not enough questions in the bank. Need ${requested}, but only ${availableCount} match your filters.`);
                return false;
            }
        }

        return true;
    }

    async function loadTopics(subjectId, selectedTopicId) {
        const topicSelect = fields.topic;
        topicSelect.innerHTML = '<option value="">-- Any Topic --</option>';

        if (!subjectId) {
            availableCount = 0;
            updateReview();
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

        await refreshQuestionCount();
        updateReview();
    }

    function buildQuestionParams() {
        const params = new URLSearchParams();
        if (fields.subject?.value) params.append('subjectId', fields.subject.value);
        if (fields.topic?.value) params.append('topicId', fields.topic.value);
        if (fields.difficulty?.value) params.append('difficulty', fields.difficulty.value);
        if (fields.questionType?.value) params.append('questionType', fields.questionType.value);
        return params;
    }

    async function refreshQuestionCount() {
        if (!fields.subject?.value) {
            availableCount = 0;
            return;
        }

        try {
            const response = await fetch(`/Quiz/GetQuestionCount?${buildQuestionParams().toString()}`);
            const data = await response.json();
            availableCount = data.count || 0;
        } catch {
            availableCount = 0;
        }
    }

    prevBtn?.addEventListener('click', () => {
        if (currentStep > 1) showStep(currentStep - 1);
    });

    nextBtn?.addEventListener('click', async () => {
        if (currentStep === 2) {
            await refreshQuestionCount();
        }

        if (!validateStep(currentStep)) return;
        updateReview();
        if (currentStep < totalSteps) showStep(currentStep + 1);
    });

    fields.subject?.addEventListener('change', () => {
        loadTopics(fields.subject.value);
    });

    [fields.topic, fields.difficulty, fields.questionType, fields.questions].forEach(el => {
        el?.addEventListener('change', async () => {
            await refreshQuestionCount();
            updateReview();
        });
    });

    ['input', 'change'].forEach(eventName => {
        form.addEventListener(eventName, updateReview);
    });

    showStep(1);
    updateReview();

    if (fields.subject?.value) {
        loadTopics(fields.subject.value, fields.topic?.value);
    }
})();
