# Architecture

```text
Codex + packaged skills
       |
  Local MCP server [NOT IMPLEMENTED]
       |
 Authenticated per-user named pipe [NOT IMPLEMENTED]
       |
Windows companion (.NET 10 / WPF)
  |- local panel + click-through AI cursor [PROTOTYPE]
  |- monitor/DPI anchors [PARTIAL]
  |- UI Automation inspection [PLANNED]
  |- on-demand capture + masking [PLANNED]
  |- lesson/state engine [PLANNED]
  |- native approval + action broker + Stop [PLANNED]
```

## Current code
- `CursorMate.Core` provides pure geometry calculations, independent of WPF and the model.
- `CursorMate.Companion` shows a local WPF preview UI and a no-activate, click-through overlay with a mini pointer. In its demo, the pointer can mark a button inside CursorMate only.
- `CursorMate.SmokeTests` checks key edge/geometry cases without NuGet test packages.

The demo is **not** a computer-use agent, has no screenshot/voice/MCP infrastructure, and does not inject clicks.

## Planned interfaces (not yet implemented)

`DesktopContext` owns a user-selected HWND, live UI Automation element lookup, monitor transforms and capture. `GuideEngine` provides session, step and verification state. `ConsentBroker` owns native approvals and cancellation. `McpServer` exposes only narrowly scoped operations, never unrestricted shell or arbitrary coordinates. Use named pipes with current-user authentication for local IPC.

Suggested future MCP tools:
- `guide_start`: begin a visible session after local capture consent.
- `desktop_inspect`: summarize the selected window and live UIA elements.
- `guide_show_step`: highlight a live, verified element and explain a step.
- `guide_verify`: verify a postcondition and move session state.
- `guide_request_action`: ask trusted companion UI for one-step approval.
- `guide_stop`: locally stop and revoke session permissions.

**No MCP server config is declared in the current plugin manifest.** Add `mcp.json` only when the server is real and tested.

## Coordinate systems
UI Automation screen coordinates, Win32 screen coordinates, WPF DIPs, and capture pixel bounds are different units. Build an explicit `CursorAnchorResolver` with physical-pixel coordinates, per-monitor transforms, monitor containment, negative monitor origins, virtual-screen holes, and scaling. The current prototype does **not** solve mixed-DPI monitor support robustly.

## Performance
Measure p50/p95: time to open panel, identify a UIA target, render highlight, answer first step, and verify step. Prefer local UIA to repeated screenshots. Avoid model calls on every pointer frame. Response time depends on the chosen model/network and cannot be guaranteed.
