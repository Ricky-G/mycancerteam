# MyCancerTeam

MyCancerTeam is a C#/.NET multi-agent foundation designed to support cancer patients and caregivers with practical, empathetic, privacy-first guidance.

## Purpose and mission
Cancer care is complex and stressful. This project helps patients/support people understand information, prepare clinician questions, organize records, compare options, track uncertainty, and coordinate logistics.

## Clinical guidance philosophy
- Supports informed clinical conversations, not clinician replacement.
- Separates facts, assumptions, uncertainty, and open questions.
- Distinguishes standard care, guideline-based care, emerging evidence, and speculative reasoning.
- Includes confidence, rationale, references, and validation reminders.

## Agent team (starter)
- Team Lead Agent (coordination + synthesis)
- Patient / Support Person Owner Agent
- Research Oncology Agent
- Radiation Oncologist Agent
- Medical Oncologist Agent
- Radiologist Agent
- Specialist Surgeon Agent
- Psychologist / Emotional Support Agent
- Financial Assistant Agent
- Social Worker / Care Navigation Agent
- Admin / Logistics Agent

## Safety and validation boundaries
- Do not treat outputs as medical diagnosis or final treatment advice.
- Urgent red flags require immediate medical care.
- Verify all major treatment decisions with qualified clinicians.
- Local-first file storage is used for notes/drafts; sensitive folders are git-ignored.
- Draft support is local markdown generation only; no automatic message sending.

## Repository layout
- `src/MyCancerTeam.App` - console host
- `src/MyCancerTeam.Core` - agent/workflow abstractions and models
- `src/MyCancerTeam.Infrastructure` - configuration, note store, drafts, Azure credential/client setup, research refresh scaffolding
- `tests/MyCancerTeam.Tests` - baseline tests
- `config/environments/{dev,test,prod}` - environment configuration
- `docs/future-architecture.md` - roadmap/TODO architecture extensions
- `samples/queries.txt` - sample prompts

## Prerequisites
- .NET SDK 8+
- Azure login available for `DefaultAzureCredential` (`az login`)

## Quick start
1. Clone the repo.
2. Copy `.env.example` into your environment setup (do not commit `.env`).
3. Update endpoint/deployment placeholders.
4. Ensure Azure auth:
   - `az login`
   - If needed: `az account set --subscription <subscription-id>`
5. Run:
   - `dotnet restore MyCancerTeam.slnx`
   - `dotnet run --project /home/runner/work/mycancerteam/mycancerteam/amul047/mycancerteam/src/MyCancerTeam.App/MyCancerTeam.App.csproj`

## Configuration
Configuration is loaded from:
1. `config/environments/<env>/appsettings.json`
2. environment variables (`MYCANCERTEAM_*`) overriding file values.

Environment selector:
- `MYCANCERTEAM_ENVIRONMENT=dev|test|prod`

Core vars:
- `MYCANCERTEAM_AZURE_OPENAI_ENDPOINT`
- `MYCANCERTEAM_AZURE_OPENAI_DEPLOYMENT`
- `MYCANCERTEAM_LOCAL_WORKING_FOLDER`
- `MYCANCERTEAM_ITERATIONS_FOLDER`
- `MYCANCERTEAM_CLINICAL_NOTES_FOLDER`
- `MYCANCERTEAM_REPORTS_FOLDER`
- `MYCANCERTEAM_IMAGING_FOLDER`
- `MYCANCERTEAM_RADIATION_PLAN_FOLDER`
- `MYCANCERTEAM_MEDICATION_PLAN_FOLDER`
- `MYCANCERTEAM_INSURANCE_DOCUMENTS_FOLDER`
- `MYCANCERTEAM_RESEARCH_CACHE_FOLDER`
- `MYCANCERTEAM_RESEARCH_SUMMARIES_FOLDER`
- `MYCANCERTEAM_GLOBAL_TREATMENT_SEARCH_FOLDER`
- `MYCANCERTEAM_INTL_SECOND_OPINIONS_FOLDER`
- `MYCANCERTEAM_DRAFTS_FOLDER`
- `MYCANCERTEAM_AGENT_MEMORY_FOLDER`
- `MYCANCERTEAM_LATEST_SHARED_NOTES_PATH`
- `MYCANCERTEAM_DAILY_RESEARCH_REFRESH_SCHEDULE` (optional)

## Local folder setup
The app auto-creates local folders under `.local/` by default:
- `.local/notes/notes.md`
- `.local/agent-memory/`
- `.local/clinical-notes/`
- `.local/imaging/`
- `.local/radiation-plans/`
- `.local/medication-plans/`
- `.local/reports/`
- `.local/insurance-documents/`
- `.local/iterations/`
- `.local/research-cache/`
- `.local/research-summaries/`
- `.local/global-treatment-search/`
- `.local/international-second-opinions/`
- `.local/drafts/`
- `.local/drafts/emails/`
- `.local/drafts/insurance/`
- `.local/drafts/second-opinions/`
- `.local/drafts/trials/`

## Run commands
- Build: `dotnet build MyCancerTeam.slnx`
- Run app: `dotnet run --project src/MyCancerTeam.App`
- Example run with arg: `dotnet run --project src/MyCancerTeam.App -- "Need questions for radiation side effects"`

## Test commands
- `dotnet test MyCancerTeam.slnx`

## Example workflows supported in foundation
- Travel/practical support routing
- Home support routing
- Imaging and report workflow routing
- Radiation and medication plan workflow routing
- Symptom support routing
- Insurance/financial routing
- Research monitoring routing
- Global treatment access routing
- Draft communication generation to local markdown

## Troubleshooting
- **Missing Azure config:** set endpoint/deployment env vars or update `config/environments/<env>/appsettings.json`.
- **Credential failures:** run `az login`, verify tenant/subscription.
- **No notes file:** app creates local notes automatically on first write.

## Local notes and privacy
`notes.md` and agent memory files are local only and intentionally excluded from git.
Do not store secrets in committed files.

## Ignored folders/files
See `.gitignore` for local-sensitive patterns including `.local/`, patient documents, research cache/summaries, drafts, iterations, and `.env` files.
