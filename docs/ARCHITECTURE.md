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
  |- per-monitor DPI-aware overlays and anchors [IMPLEMENTED FOR DEMO]
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
The companion runs per-monitor DPI aware. Win32 provides the real pointer position and monitor bounds in physical desktop pixels. `CursorMath` resolves parking, target anchors, and elapsed-time animation in that space, including negative origins and gaps between displays. Each monitor has a separate layered, click-through WPF surface. It converts pixels relative to its monitor origin into local DIPs using that surface's DPI. The controller rebuilds surfaces when monitor geometry or DPI changes.

The demo reads fresh bounds from its own WPF button while the panel is active. It clips the bounds to the visible viewport and draws one outline across any intersecting displays. A missing or off-screen target returns the avatar to idle. The Core smoke tests cover simulated monitor layouts. The README describes interactive checks for Windows display drivers, native hit testing, and live layout changes.

Future UI Automation and capture adapters will need their own coordinate conversions and target verification.

## Performance
Measure p50/p95: time to open panel, identify a UIA target, render highlight, answer first step, and verify step. Prefer local UIA to repeated screenshots. Avoid model calls on every pointer frame. Response time depends on the chosen model/network and cannot be guaranteed.
