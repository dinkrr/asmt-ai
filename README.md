# Pre-assessment AI

An Angular and ASP.NET Core scaffold for the request-side pre-assessment workflow. The API uses controllers and action methods. The existing browser prototype and OpenPencil mockups are separate design artifacts.

## Structure

- `frontend/`: Angular application; shared services live in `src/app/core`, and request screens in `src/app/features/requests`.
- `design/`: browser prototype, mockups, previews, and design tooling. Portal observations are kept local and excluded from Git.
- `backend/src/AsmtAI.Domain`: domain types, independent of application and infrastructure.
- `backend/src/AsmtAI.Application`: use cases and interfaces, depending on Domain.
- `backend/src/AsmtAI.Infrastructure`: EF Core persistence and Microsoft Agent Framework integration, depending on Application.
- `backend/src/AsmtAI.Api`: ASP.NET Core controllers, request services, and composition root.
- `backend/tests/AsmtAI.Application.Tests`: application use-case tests.
- `backend/tests/AsmtAI.Infrastructure.Tests`: persistence and provider-selection tests.
- `backend/tests/AsmtAI.Api.Tests`: local workflow and role-boundary tests.

## Local Role Demo

The local demo has two selectable perspectives: an applicant can submit a request with a PowerPoint presentation, and an ASMT head/expert can generate a presentation analysis, write questions, record a transcript, and save feedback. Analysis is an unsaved AI draft for human review; question and feedback generation are not connected. There is no real portal integration. The demo supports .NET A2-A4 requests and accepts `.ppt` or `.pptx` files up to 10 MB; AI analysis requires `.pptx` with readable slide text.

**Use synthetic data only.** The role selector and `X-Demo-Role` header are easily changed and are not authentication or authorization. All applicants share one demo identity, so there is no real user isolation. Request endpoints are available only in the Development environment and only to loopback clients; do not expose the development server or use it for real assessment material. Before any real use, add authenticated identities, server-enforced roles and per-user access, secure file handling, audit logging, data retention rules, and appropriate storage and privacy controls. The health endpoint confirms the API process is responding, not that dependencies are ready.

## Persistence

The request and review-draft repositories use EF Core's in-memory provider. The database is shared across scopes in one application instance but is lost on restart and is not shared between processes. EF Core's in-memory provider is not suitable for production; select a persistent provider and add migrations before deployment.

## Run locally

Requirements: .NET 10 SDK and Node.js compatible with Angular 21 (Node 24 works).

```powershell
dotnet run --project backend/src/AsmtAI.Api --launch-profile http
```

In a second terminal:

```powershell
npm ci --prefix frontend
npm start --prefix frontend
```

Open the URL printed by Angular. Its development proxy forwards `/api/**` to `http://localhost:5233`; update `frontend/proxy.conf.json` if the API runs on a different port. OpenAPI is available in development at `http://localhost:5233/openapi/v1.json`.

## Copilot Provider

The Development API defaults to the `copilot` provider and expects the `ai-agents` repository next to this repository. It reads the complete `unified-assessment-agent.md`, `review-self-presentation/SKILL.md`, and that skill's two reference files directly from the repository for each analysis. The source definitions are not copied into this project. Changes in that repository affect subsequent analyses. The questions and transcript-feedback agent/skills are not used yet.

The provider uses the GitHub Copilot SDK through its Microsoft Agent Framework adapter. Sign in to the Copilot CLI with `copilot login` if needed; a VS Code or GitHub CLI sign-in alone does not guarantee SDK authentication. A Copilot subscription or supported free allowance is required, and requests count against its usage allowance. The SDK may bundle its own CLI runtime. The optional settings below can be supplied as environment variables before starting the API:

```powershell
$env:Ai__AgentRepositoryPath = 'C:\path\to\ai-agents'
$env:Ai__Copilot__Model = 'your-available-copilot-model'
```

Omit the model setting to use the SDK default. Analysis sends the candidate name, target level, extracted slide text, and assessment instructions to the configured Copilot service. Slide images and speaker notes are not analyzed; legacy `.ppt` files can be downloaded but not analyzed. The Copilot session disables tools, automatic local instruction and skill discovery, and cross-session search. This is not a guarantee that the SDK or provider retains no session data: review their policies before use. The application does not automatically save the draft or make an assessment decision.

`IAssessmentAssistant` remains the application boundary. To add another Agent Framework provider, implement `IAssessmentAgentProvider`, register it with dependency injection, and set `Ai:Provider` (or `Ai__Provider`) to its unique name. Selection is case-insensitive; unknown or duplicate names fail when the assistant is resolved. Outside Development, the default provider name remains `agent-framework` and demo endpoints are unavailable.

Do not commit credentials or real assessment data. Add authorization, data handling safeguards, input boundaries, and human review before exposing AI operations.

## Verify

```powershell
dotnet build backend/AsmtAI.slnx
dotnet test backend/AsmtAI.slnx
npm test --prefix frontend -- --watch=false
npm run build --prefix frontend
```