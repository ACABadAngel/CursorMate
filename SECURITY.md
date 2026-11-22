# Security policy

CursorMate is an early prototype; do not rely on it for unattended automation or sensitive workflows.

For security vulnerabilities, use GitHub's **Report a vulnerability** / private security advisory feature on this repository if available. Otherwise contact the repository maintainer privately through their GitHub profile before opening a public issue. Do not include credentials or private screenshots in issue reports.

Design invariants:
- Teaching never generates input or moves the OS cursor.
- Desktop capture is user-initiated, visible, scoped, and off by default.
- Desktop actions need a native, app-side approval; model-generated tokens are not authority.
- Approvals expire, are single-use or explicitly scoped, and revoke on Stop or loss of context.
- UI content, screenshots, web pages, and model outputs are untrusted.
- Local MCP transport must be limited to the current user and authenticated; avoid public network binds.
- Never persist screenshots, tokens, keystrokes, or sensitive private fields by default.
- No hidden remote access or elevation features.
