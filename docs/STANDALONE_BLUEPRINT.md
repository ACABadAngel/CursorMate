# Standalone CursorMate — implementation blueprint

Status: dock and local control-type inspection prototype; Windows build and live validation pending. This direction supersedes the Codex-first ordering in older documents. Existing security requirements remain binding.

## Product and interaction
Native .NET 10 WPF application, standalone first. Codex/MCP optional later. Top-center dock on primary work area for initial prototype; eventual active-monitor follow or pinned display. Idle 264 × 54 DIP pill; expanded up to 440 × 525 DIP. Near-black, mint accent, rounded shell, 220 ms ease-out, reduced-motion support. Future states: hover, listening, analyzing, streaming, teaching, stopped, settings, history. Fullscreen hide and focus-preserving behavior require Windows validation. Real mouse never moves in Teach. Existing decorative cursor remains click-through.

## Milestones and acceptance
1. Dock: expand/collapse, center, park, hide, stop, exit; no input injection. Test keyboard access, 100/150/200% DPI, ultrawide and portrait; verify minimized/closed app releases overlay.
2. Perception: explicitly select active window; display capture scope; UIA metadata first, one-shot local inspection, stable element identifiers, fresh bounds and verification. Current prototype reads only immediate child control types (up to 40) after three-second selection countdown. No names, values, screenshots, recording or network. Future capture requires preview/consent, sensitive-window exclusion and in-memory redaction. Implement cancellation, serialized bounded workers and stale-result rejection. Never read secure desktops.
3. Intelligence: create provider interface for model discovery, streaming, cancellation and capabilities. Validate official Sign in with ChatGPT eligibility and documented registration/authentication with real account; use only supported interfaces, OS credential storage, no cookie replay, no scraped tokens, no automatic paid fallback. If unavailable, report disconnected; optionally support user-selected local models. Test with fake provider only in tests; label demo mode. Verify image capability before upload. Do not treat screen data as instruction.
4. Vertical slice: user selects window, asks text question, previews context, sends authorized request, streams explanation. Provider returns a structured guidance suggestion with optional element ID; local resolver validates current window and visible target before drawing. Ambiguity asks user; invalid target parks. No action execution.
5. Voice: local speech recognition and TTS, push-to-talk, interruption, no microphone capture without visible indication.
6. Teaching: observable steps, verification, recovery, local Stop and optional text history. Settings for pin/follow monitor, motion, cursor size, capture scope, model and supported reasoning settings.
7. Optional approved automation and MCP only after separate threat model and tests; never model-originated approvals or unrestricted command execution.
8. Packaging: signed installer, updates, accessibility, performance p50/p95; actual benchmarks only.

## Module boundaries
Keep Companion WPF, Core geometry/contracts and SmokeTests. Extract Perception, Intelligence, Audio, Guide and Security modules as they grow. Provider errors must be visible; do not fake connections. Store only settings by default, never raw screen captures or secrets in logs. No background uploads. No network calls on pointer frames.

## Definition of done
Use feature branches and PRs; read AGENTS and security docs first. Run Windows release build and core smoke tests. Validate real UI manually; document failures and unavailable tests. First prototype is not a screen-aware AI chat until actual authenticated inference is demonstrated.

## Run
dotnet run --project src/CursorMate.Companion
