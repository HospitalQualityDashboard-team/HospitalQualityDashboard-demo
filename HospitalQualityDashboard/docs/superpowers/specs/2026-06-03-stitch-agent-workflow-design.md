# Stitch Agent Workflow Design

## Context

`HospitalQualityDashboard` is an ASP.NET MVC 4 application on .NET Framework 4.7.2. The app uses Razor views, Bootstrap, jQuery, and service classes backed by ADO.NET. The requested integration is not to embed Stitch AI into the running MVC application, but to configure Codex/agent workflow so Stitch can be used as an external UI design tool.

The current Codex session does not expose a Stitch MCP connector directly. Stitch is available through the public `@google/stitch-sdk` package, which can generate UI screens from prompts and return HTML and screenshot URLs. Authentication should use environment configuration such as `STITCH_API_KEY`; secrets must not be committed.

## Goal

Create a safe agent workflow for using Stitch AI to design or iterate UI screens for this MVC project without changing application code until a generated design is explicitly reviewed and approved.

## Non-Goals

- Do not add Stitch SDK calls inside ASP.NET MVC controllers, services, or views.
- Do not add Node/npm runtime dependencies to the MVC application itself.
- Do not commit API keys, OAuth tokens, generated private HTML downloads, or hospital-sensitive data.
- Do not automatically replace existing Razor views with generated Stitch output.

## Recommended Approach

Use Stitch as an external design companion for Codex:

1. Codex reads existing MVC context, including the target controller, Razor view, shared layout, and `Content/Site.css`.
2. Codex writes a focused Stitch prompt that describes the target screen, current constraints, audience, and visual requirements.
3. A local or agent-side Stitch workflow generates one or more UI candidates.
4. Codex records the prompt, generated screen metadata, screenshots, HTML links, and review notes under `docs/stitch/`.
5. The user reviews the result.
6. Only after approval, Codex converts the selected HTML/CSS into MVC Razor and local CSS changes.

This keeps Stitch outputs separate from production MVC code and preserves a human review gate between AI-generated design and application implementation.

## Repository Additions

Add documentation only at first:

- `docs/stitch/README.md`: describes the Stitch workflow for this project.
- `docs/stitch/prompts/`: stores approved prompts used to generate screens.
- `docs/stitch/reviews/`: stores review notes and selected design decisions.

Generated code should not be committed by default. If a Stitch result needs to be preserved, store a small markdown note with the prompt, date, screen purpose, and links instead of committing large generated artifacts.

## Agent Rules

When the user asks Codex to use Stitch AI for a screen:

1. Confirm the target screen and purpose.
2. Inspect the existing MVC files for that screen.
3. Produce a Stitch prompt that includes:
   - Project type: ASP.NET MVC hospital quality dashboard.
   - Target user: Admin or department User.
   - Screen purpose and key workflows.
   - Existing UI constraints: Bootstrap, Razor layout, Vietnamese labels, dense operational dashboard style.
   - Output expectation: web app screen, not a marketing landing page.
4. Run Stitch only through an external tool or script configured outside the MVC runtime.
5. Save review notes under `docs/stitch/`.
6. Ask for approval before editing `.cshtml`, `.css`, `.js`, controllers, models, or services.

## Security And Privacy

Stitch prompts must not include:

- Real patient data.
- Real employee personal data.
- Real hospital credentials.
- Connection strings or database contents.
- Private deployment URLs.

Use representative mock data only, such as generic department names, report status labels, and sample indicator values.

Secrets must be supplied by environment variables on the user's machine, not checked into the repository. The expected variable is:

```powershell
$env:STITCH_API_KEY = Read-Host "Enter Stitch API key"
```

## Future Optional Tooling

If the team wants repeatable command-line use later, add a separate tooling directory outside the MVC app runtime, such as:

```text
tools/stitch-agent/
  package.json
  generate-screen.mjs
  edit-screen.mjs
  README.md
```

That tooling may depend on `@google/stitch-sdk`, but it should remain clearly separated from `HospitalQualityDashboard.csproj`. The MVC app should continue to build and run without Node installed.

## Validation

The workflow is valid when:

- The repository documents how Codex should use Stitch.
- No MVC runtime code depends on Stitch.
- No secrets are committed.
- Stitch outputs are reviewed before implementation.
- Any later Razor/CSS conversion follows existing MVC structure and passes the project's normal build or verification scripts.
