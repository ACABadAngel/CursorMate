# CursorMate — Product specification

**Status:** engineering plan. Only the local AI cursor preview exists currently.

## Core experience
The user asks how to set something up in software. CursorMate identifies the *currently selected* application, locates a control, draws a small unobtrusive highlight, moves a separate miniature companion pointer to the target, and explains one instruction. The user completes the step; CursorMate verifies the new state, then explains the next step. Users can ask "why?", "repeat", or "show me" without losing place.

### Modes
| Mode | Default? | May manipulate desktop? |
| --- | --- | --- |
| Teach | Yes | **No** — read-only observation plus click-through drawings |
| Assist | No | One freshly approved, scoped action at a time |
| Automate | No | Only an approved sequence; pause for sensitive/high-risk tasks |

Every non-Teach mode returns to Teach when finished. Local Stop cancels queued work.

## AI companion cursor
- A separate decorative mouse-pointer avatar, 14–18 DIPs tall, small mint outline.
- When idle: parks 18–28 DIPs beside the *real* cursor, with edge-aware placement.
- When teaching: eases toward a verified live target in ~250–500 ms, then stays outside its clickable bounds.
- When UI changes: re-resolves target anchor; stale/hidden targets return it to idle.
- Always click-through, no movement of hardware/OS cursor in Teach.
- Options: size, opacity, accent, disable, reduced motion, park by pointer or panel.
- Works across ultrawide, portrait, and mixed-DPI multi-monitor layouts.

## Proposed teaching flow
1. User starts a guide from tray/hotkey or Codex; selected app/window scope is clearly shown.
2. Inspect the focused window via Windows UI Automation. Ask to capture a screenshot only when necessary and allowed.
3. Plan a short, observable lesson. Show one instruction with highlight and AI cursor.
4. Wait for user input, inspect actual UI change, verify the expected state.
5. If target is ambiguous, ask user to identify it. If verification fails, explain and recover.
6. Offer **Do this step for me** but never execute before an app-native approval.
7. Summary and optional text-only saved tutorial.

## Target use cases
- VS Code: install and configure a Python extension.
- Roblox Studio: explain Explorer hierarchy, UI elements and properties.
- Windows: guide through a setting.
- Development: explain an error and highlight where to act.

## Milestones
**M0 (in repo now):** C# WPF demo panel and separate click-through mini cursor; testable geometry library. Not a full guide.
**M1:** reliable per-monitor WPF overlays, DPI, smooth easing, hotkey, tray, idle/target state.
**M2:** UI Automation active-window context, verified element IDs, active-window selection, privacy.
**M3:** read-only teaching state machine, target highlights, explicit next/verify, recovery.
**M4:** approved one-step action broker with local consent dialog, risk classes, instant Stop.
**M5:** real MCP server, Codex skill-to-tool integration, diagnostics.
**M6:** optional voice/TTS, cropped vision, installation and scoped multi-step automation.

## Definition of done
Every milestone must be reproducible on Windows, tested for denied permissions and Stop where relevant, documented honestly, and validated on multi-monitor arrangements. Model speed or automatic recognition accuracy is never guaranteed.

## Out of scope for v1
Silent background recording, no-approval remote control, arbitrary desktop shell execution, covert operation, unattended risky actions, automatic collection of passwords/2FA codes.
