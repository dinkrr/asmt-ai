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

The local demo has two selectable perspectives: an applicant can submit a request with a PowerPoint presentation, and an ASMT head/expert can review it, write questions, record a transcript, and save feedback. Reviews are written manually; no AI generation or real portal integration is configured. The demo supports .NET A2-A4 requests and accepts `.ppt` or `.pptx` files up to 10 MB.

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

## Agent Framework

`IAssessmentAssistant` is the application boundary and `AgentFrameworkAssessmentAssistant` adapts an Agent Framework `AIAgent`. The API registers the selection mechanism but no provider or credentials. To add a provider, implement `IAssessmentAgentProvider` with a unique name and `CreateAgent()`, register the implementation with dependency injection, and set `Ai:Provider` (or the `Ai__Provider` environment variable) to that name. Selection is case-insensitive; unknown or duplicate names fail when the assistant is resolved. With no setting, the default name is `agent-framework`.

For an already-created agent, `builder.Services.AddAssessmentAgent(agent)` registers the `agent-framework` strategy; use it instead of `AddAssessmentAi()`. A strategy's `CreateAgent()` is called only when a future workflow resolves the assistant; the scaffold does not invoke an agent. Provider implementations are responsible for configuring the model, credentials, and assessment-specific instructions.

Do not commit credentials or real assessment data. Add authorization, data handling safeguards, input boundaries, and human review before exposing AI operations.

## Verify

```powershell
dotnet build backend/AsmtAI.slnx
dotnet test backend/AsmtAI.slnx
npm test --prefix frontend -- --watch=false
npm run build --prefix frontend
```