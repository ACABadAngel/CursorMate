# CursorMate

**An open-source Windows AI desktop companion that teaches by default and automates only when asked.**

CursorMate is a HeyClicky-style visual teaching assistant being developed for **OpenAI Codex**. It will identify UI controls, point to them with its own miniature animated cursor, explain one step at a time, and optionally perform an explicitly approved action.

> **Project status: early scaffold / pre-alpha.** The current Windows prototype only demonstrates a separate, click-through AI cursor and a local demo target. It does **not** yet read other applications, take screenshots, connect to Codex via MCP, speak, or perform desktop actions.

## Product principles

- **Teach first:** A question never authorizes a desktop action.
- **Separate AI cursor:** A decorative mini pointer rests near the real mouse when idle, then glides to the highlighted target. It never moves the OS cursor in Teach mode.
- **Automation with consent:** A trusted Windows companion must approve specific actions, never model text alone.
- **Read-only by default:** A user-started, visible session; selected window only; no background screen recording.
- **UI Automation first:** Prefer accessibility metadata, then on-demand cropped vision.
- **Verify real outcomes:** No success claims based solely on issuing clicks.
- **Multi-monitor:** The prototype uses a DPI-aware overlay per display. See the verification steps below for mixed-DPI and portrait layouts.
- **Local Stop:** Cancel queued actions immediately without needing the model.

## Repository layout

```text
src/
  CursorMate.Core/          Shared cursor geometry and future contracts
  CursorMate.Companion/     Windows WPF desktop prototype
tests/
  CursorMate.SmokeTests/    Dependency-free checks of core behavior
skills/
  teach-software/           Codex teaching workflow instructions
  assist-step/              One-step consent workflow instructions
  automate-workflow/       Planned scoped automation workflow
docs/
  PRODUCT.md                Feature specification and milestones
  ARCHITECTURE.md           Technical design
  SECURITY.md               Trust boundaries and safeguards
  CODEX_SETUP.md            Working on this repository in Codex
.github/workflows/          Build validation
plugin.json                 Skills-only portable plugin manifest (for now)
AGENTS.md                   Instructions Codex should follow in this repo
```

## Run the prototype on Windows

1. Install **.NET 10 SDK** on Windows 11 (or Windows 10 with suitable .NET desktop support).
2. Clone this repository:
   ```powershell
   git clone https://github.com/ACABadAngel/CursorMate.git
   cd CursorMate
   ```
3. Launch the desktop demo:
   ```powershell
   dotnet run --project src/CursorMate.Companion
   ```
4. The companion starts a **click-through decorative cursor** beside your physical pointer. Select **Point at this demo button** to highlight the local target, **Park beside real mouse** to go idle, or **Hide AI cursor** to turn off the overlay. **Reduce cursor motion** snaps the avatar to its anchor for this session. The companion also respects the Windows animation setting.

The prototype has **no screenshot recording, external app inspection, network requests, or input injection**.

## Build and check

```powershell
dotnet build src/CursorMate.Companion -c Release
dotnet run --project tests/CursorMate.SmokeTests -c Release
```

The WPF app must be built on Windows. The core smoke tests are cross-platform. Automated CI builds and tests are configured; check GitHub Actions for results.

### Verify the overlay on Windows

1. In **Settings → System → Display**, test with one display, then two and three extended displays. Use different scales such as 100%, 150%, and 200%. Where available, include an ultrawide and a portrait display. Place a secondary display left of and another above the primary. Offset them vertically to leave a gap in the virtual desktop.
2. Start the demo and move the real mouse across each display, slowly through seams and quickly between distant corners. At rest, check that the entire mint avatar stays inside the display and parks 18–28 DIPs from the mouse hotspot. Check that the avatar has a consistent logical size across scales. On one display at a time, use a stationary mouse to confirm that pointing, parking, and toggling reduced motion with the keyboard leave the real mouse in place.
3. Point at the demo button, then click on its painted highlight stroke. Confirm the click reaches the underlying companion window without the overlay taking focus. Also click the demo button itself normally.
4. Point at the demo button again. Drag the companion across displays and leave it across a seam. Resize it, then shrink it enough to scroll the panel. The highlight should follow the visible button. Scroll the button fully out of view or minimize the panel and check that the highlight clears and the avatar parks. Select the button again after restoring it.
5. Change a display's scale, position, or orientation. Disconnect and reconnect a secondary display while the demo runs. Confirm parking recovers on each remaining display. Refocus the companion and select the demo button again if the panel lost focus during the change.
6. Turn on **Reduce cursor motion** and repeat a park and target transition. Check that the avatar snaps into place. Turn it off and disable **Animation effects** in Windows accessibility settings to check the system setting. Use **Hide AI cursor** to remove both the avatar and highlight, then **Show AI cursor** to resume parking.

## Work on it with Codex

See [docs/CODEX_SETUP.md](docs/CODEX_SETUP.md). In Codex, open/connect this GitHub repository or clone it locally, read [AGENTS.md](AGENTS.md), and start with a single GitHub issue. Have Codex implement, test, and submit one focused PR at a time. The `skills/` instructions can also be used as plugin skills, but **the MCP server is not implemented yet**.

## Roadmap

1. Reliable click-through overlay, smooth mini cursor, and per-monitor DPI support.
2. Active-window inspection with Windows UI Automation and live target tracking.
3. Real read-only Teach sessions with step verification.
4. Explicit local approval for a single UI action, immediate Stop.
5. Local MCP server, Codex integration and packaged plugin wiring.
6. On-demand vision, voice, installer, and hardened workflow automation.

See [docs/PRODUCT.md](docs/PRODUCT.md) and GitHub Issues for the detailed plan.

## Open source

Licensed under the [MIT License](LICENSE). Contributions welcome; see [CONTRIBUTING.md](CONTRIBUTING.md). Security-sensitive reports: see [SECURITY.md](SECURITY.md).
