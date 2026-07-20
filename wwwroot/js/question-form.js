(function () {
    const subjectSelect = document.getElementById('SubjectId');
    const topicSelect = document.getElementById('TopicId');
    const questionTypeSelect = document.getElementById('QuestionType');
    const choicesContainer = document.getElementById('choicesContainer');
    const addChoiceBtn = document.getElementById('addChoiceBtn');
    const choiceHelpText = document.getElementById('choiceHelpText');

    if (!choicesContainer) return;

    async function loadTopics(subjectId, selectedTopicId) {
        if (!topicSelect) return;

        topicSelect.innerHTML = '<option value="">-- Any Topic --</option>';

        if (!subjectId) return;

        try {
            const response = await fetch(`/Question/GetTopics?subjectId=${subjectId}`);
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
    }

    function reindexChoices() {
        const rows = choicesContainer.querySelectorAll('.choice-row');
        rows.forEach((row, index) => {
            row.dataset.index = index;

            const textInput = row.querySelector('.choice-text');
            const idInput = row.querySelector('input[type="hidden"]');
            const correctInput = row.querySelector('.choice-correct');
            const correctLabel = row.querySelector('.form-check-label');

            if (textInput) {
                textInput.name = `Choices[${index}].ChoiceText`;
                textInput.placeholder = `Choice ${index + 1}`;
            }

            if (idInput) {
                idInput.name = `Choices[${index}].ChoiceId`;
            }

            if (correctInput) {
                correctInput.name = `Choices[${index}].IsCorrect`;
                correctInput.id = `choiceCorrect_${index}`;
            }

            if (correctLabel) {
                correctLabel.setAttribute('for', `choiceCorrect_${index}`);
            }
        });
    }

    function addChoiceRow(text = '', isCorrect = false) {
        const index = choicesContainer.querySelectorAll('.choice-row').length;
        const row = document.createElement('div');
        row.className = 'choice-row row g-2 align-items-center mb-2';
        row.dataset.index = index;
        row.innerHTML = `
            <div class="col-md-8">
                <input name="Choices[${index}].ChoiceText" value="${text}" class="form-control choice-text" placeholder="Choice ${index + 1}" />
                <input type="hidden" name="Choices[${index}].ChoiceId" value="0" />
            </div>
            <div class="col-md-3">
                <div class="form-check">
                    <input class="form-check-input choice-correct" type="checkbox" name="Choices[${index}].IsCorrect" value="true" id="choiceCorrect_${index}" ${isCorrect ? 'checked' : ''} />
                    <label class="form-check-label" for="choiceCorrect_${index}">Correct</label>
                </div>
            </div>
            <div class="col-md-1">
                <button type="button" class="btn btn-outline-danger btn-sm remove-choice-btn" title="Remove">&times;</button>
            </div>
        `;
        choicesContainer.appendChild(row);
    }

    function applyQuestionTypeDefaults() {
        const type = Number(questionTypeSelect?.value || 1);

        if (type === 1) {
            choiceHelpText.textContent = 'Single Correct: mark exactly one correct answer.';
        } else {
            // Database supports only 0/1; 0 means multiple-correct.
            choiceHelpText.textContent = 'Multiple Correct: mark one or more correct answers.';
        }
    }

    subjectSelect?.addEventListener('change', () => {
        loadTopics(subjectSelect.value);
    });

    questionTypeSelect?.addEventListener('change', applyQuestionTypeDefaults);

    addChoiceBtn?.addEventListener('click', () => {
        addChoiceRow();
        reindexChoices();
    });

    choicesContainer.addEventListener('click', (event) => {
        if (event.target.classList.contains('remove-choice-btn')) {
            const rows = choicesContainer.querySelectorAll('.choice-row');
            if (rows.length <= 2) {
                alert('Keep at least 2 choices.');
                return;
            }

            event.target.closest('.choice-row')?.remove();
            reindexChoices();
        }
    });

    applyQuestionTypeDefaults();

    if (subjectSelect?.value) {
        loadTopics(subjectSelect.value, topicSelect?.value);
    }
})();
