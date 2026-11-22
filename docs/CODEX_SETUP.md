# Work on CursorMate with Codex

## GitHub / cloud workflow
1. Open Codex in ChatGPT or the Codex desktop/IDE environment you use.
2. Ensure the **GitHub** connection is enabled and grants access to `ACABadAngel/CursorMate`.
3. Select/open this repository as the workspace, then pick an existing GitHub Issue from the roadmap.
4. Ask Codex to **read `AGENTS.md`, `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/SECURITY.md`** before editing.
5. Work on a feature branch. Ask for a PR describing implementation, test evidence, security impact and limitations.
6. Review CI and the PR before merging.

Exact Codex menu names vary between product surfaces. GitHub connectivity gives Codex repo context; it does **not** by itself install a Windows companion on your PC.

### Good first Codex prompt

> Work in ACABadAngel/CursorMate. Read AGENTS.md and docs/*.md first. Implement the first open milestone about monitor-aware AI cursor positioning as a small PR. Preserve Teach mode's prohibition on moving the real OS mouse. Build on Windows, run the core smoke tests, document what is verified, and do not claim unimplemented MCP features work.

## Local Windows workflow
Install Git, **.NET 10 SDK**, and Visual Studio 2022/2026 with .NET desktop development support, or use your preferred editor. Clone:

```powershell
git clone https://github.com/ACABadAngel/CursorMate.git
cd CursorMate
dotnet build src/CursorMate.Companion -c Release
dotnet run --project tests/CursorMate.SmokeTests -c Release
dotnet run --project src/CursorMate.Companion
```

Open this folder in Codex's local workspace. Start with a narrowly scoped issue. The WPF window needs an interactive Windows desktop; a headless/cloud Linux runner cannot demonstrate its overlay.

## Plugin status
The root `plugin.json` is a **portable, skills-only** package; `skills/` contains safe development/teaching workflow instructions. No `mcp.json` exists because there is no functional local MCP server. Do **not** register a fictional endpoint or expect live desktop inspection from the skills alone.

To build the later desktop-only plugin, implement and test the MCP server, add a valid local-server configuration matching current [OpenAI plugin packaging guidance](https://developers.openai.com/plugins/build/plugins), and follow current Codex MCP registration instructions.

## Avoid parallel conflicts
Give different Codex agents separate issues, files, or branches; use PR reviews to reconcile shared interfaces. Never let two agents write the same file on `main` simultaneously.
