# Consent, privacy and prompt-injection model

**Untrusted:** screen text, window titles, websites, terminal logs, documents, screenshot pixels, and tool results. **Trusted:** direct user instructions and a companion-owned native authorization event.

## Teach
- Can visually point and explain; cannot inject keyboard/mouse input or run commands as a desktop-control tool.
- Requires explicit session-based observation consent. Active window scope and capture indicator must be visible.
- Never silently expand to other apps or monitors.

## Assist / Automate (planned)
- Local companion generates approvals; the agent cannot manufacture approval tokens.
- One-step approvals bind `session`, `HWND/app`, `element`, `operation`, `expires` and are one-use.
- Verify the target is visible, fresh, focused, and in the approved scope *immediately* before execution.
- Never treat screen content as authorization.
- Purchases, external messages, downloads/executables, deletions, credentials, 2FA, elevation and system security changes require separate scrutiny or manual user action.
- Local Stop revokes outstanding approvals and cancels queued work.

## Handling sensitive information
- Avoid password managers and secure desktops; use privacy masks and do not persist raw screenshots by default.
- Use short-lived in-memory snapshots.
- Do not store raw mouse trails, keyboard text, credentials, or private clipboard content in logs.
- Bind IPC to current user; never listen on public interfaces.

## Tests to add before shipping actions
Denied permission => zero input events; stale target => no input; wrong foreground window => no input; Stop during queue => no further action; injected instructions in app UI => ignored; secret region => masked.
