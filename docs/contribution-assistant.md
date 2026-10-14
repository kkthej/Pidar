# Contribution assistant and submission history

Two related features on the "Make your data FAIR" form (`/Submissions/{id}/Edit`).

## 1. Submission history (who did what)

Every change to a submission is recorded in `submission_change`:

| Kind | When | Extra |
|---|---|---|
| `Created` | a data provider starts a draft | "copy of submission N" when copied |
| `Edited` | an answer changes (one row per field) | old value, new value, **source** |
| `Submitted` | the provider sends it for review | |
| `ChangesRequested` / `Rejected` | an Admin/Curator closes the review | the reviewer's message |
| `Approved` | an Admin/Curator publishes it | "published as dataset N" |
| `Deleted` | a never-submitted draft is deleted | its field history and chat are removed with it |

**Source** of an edited answer: `user` (typed or clicked), `assistant` (a suggestion the person accepted),
`import` (the Excel template), `system` (defaults of a new draft).

The browser keeps track of which answers were filled by the assistant or the Excel import and are still
unchanged, and posts that list with every autosave in the hidden `_sources` field. As soon as the person
edits such an answer it counts as their own. The server only accepts `assistant` and `import` from the
browser, only for real form fields, and the list only labels the history: it never changes what is stored.

Autosave runs 2 seconds after the last keystroke, so edits of the same field by the same person from the
same source within 10 minutes are merged into one row (first old value → latest new value).

Where it shows:
- **Form:** answers from the assistant carry a yellow "Suggested by the assistant" mark, answers from the
  Excel file a green one, until the person changes them.
- **Details page** (provider and reviewers): the same marks next to the answers, and a **History** card
  listing every change with who, when, old → new value and source.

## 2. Contribution assistant

A side panel ("Help me fill this in") where the data provider can describe the study in their own words or
paste a methods section. The assistant asks short questions and **suggests** answers. Each suggestion has
**Accept** / **Dismiss**; nothing reaches the form until Accept, and the normal autosave then stores it
with source `assistant`.

What the assistant can and cannot do:
- Its only tools are `propose_answers` (suggestions back to the browser), `get_section` (read another part of
  the form) and `show_section` (open a section on screen). It cannot save, submit, approve, or read other
  submissions or the public datasets.
- Every suggested value is checked on the server against the field definition in `ContributionForm`
  (known field, allowed options, numbers, years, links, length). Invalid ones go back to the model as errors.
- Emails, ORCID iDs and the contact person field are never sent to the model (shown as `[provided]`) and
  never filled by it.
- Only the owner can use it, and only while the submission is editable (Draft / Changes requested).

The conversation is stored per submission in `assistant_message` (text, suggestions, model, token counts),
so it continues after reopening the page and reviewers can see how many questions were asked.

### Running it locally

**Test mode (no API key needed).** In Development, with no key configured, the assistant runs in test mode:
a rule-based stand-in (`MockAssistantModel`) that recognises a few facts — species, strain, sex, age,
weight, number of animals and groups, organ, tumour model, imaging modality — and suggests them through the
same tool loop a real model uses. Try:

> We used 24 female C57BL/6 mice, 8 weeks old, in 3 groups, imaged with MRI of the brain. Tumour model.

A yellow "Test mode" badge shows in the panel header.

**With Claude.** Set the API key, then restart:

```bash
# dotnet run
dotnet user-secrets set "Assistant:ApiKey" "sk-ant-..."

# docker compose: add to .env
ASSISTANT_API_KEY=sk-ant-...
```

### Configuration (`Assistant` section; environment variables use `Assistant__Name`)

| Setting | Default | |
|---|---|---|
| `Enabled` | `true` | master switch |
| `Provider` | `auto` | `auto`: Anthropic if `ApiKey` is set, else test mode in Development, else off. Or `anthropic`, `mock`, `off` |
| `ApiKey` | — | never commit it |
| `Model` | `claude-haiku-5-5` | any Claude model id |
| `MaxTokens` | `1500` | per model round |
| `RequestsPerMinute` | `10` | per user (rate limiter) |
| `MessagesPerDay` | `150` | per user, counted in `assistant_message` |
| `MaxMessageLength` | `20000` | characters (room for a pasted methods section) |
| `HistoryMessages` | `16` | earlier messages sent back for context |

Without a key the assistant is **off in Production**, so deploying this change alone does not expose it.

### Before turning it on in production

- Approval from the PIDAR team for using an external AI service on researcher-provided text.
- A line in the privacy notice: what is sent (messages and current answers, without emails/ORCID iDs),
  what is kept (`assistant_message`, `submission_change`) and for how long.
- A retention rule for `assistant_message` (e.g. delete N months after approval) if wanted.

### Files

| | |
|---|---|
| `Models/Submissions/SubmissionChange.cs`, `AssistantMessage.cs` | entities |
| `Migrations/20261014100000_AddSubmissionHistoryAndAssistant.cs` | the two tables |
| `Services/Submissions/SubmissionHistory.cs` | writing/reading history |
| `Services/Assistant/ContributionAssistant.cs` | prompt, tool loop, validation of suggestions |
| `Services/Assistant/AssistantModels.cs` | Anthropic API client and the test-mode model |
| `Services/Assistant/AssistantSettings.cs` | configuration, registration, rate limit |
| `Controllers/AssistantController.cs` | `POST /Submissions/{id}/Assistant`, `GET …/Assistant/History` |
| `wwwroot/js/contribution-assistant.js` | panel, Accept/Dismiss, source marks, `_sources` |
