---
name: automate-workflow
description: Plan narrowly scoped desktop automation only with explicit user authorization and companion-side permission.
---

# Automate an approved workflow (planned)

This skill is a **policy/roadmap**, not an operational computer-control agent yet.

- Start in Teach mode. Present a sequence of concrete steps and likely risks.
- Require user opt-in **and** a native approval broker before doing anything.
- Block or separately confirm sensitive and irreversible steps. Never collect credentials, 2FA codes or passwords through model prompts.
- Bind scope to exact applications, operations and expiry. Revalidate before every action.
- Pause for ambiguity; never retry uncertain clicks blindly.
- Verify outcomes and revert to Teach on completion, denial, interruption or Stop.
- If the companion or MCP tools are absent, provide manual instructions instead.
