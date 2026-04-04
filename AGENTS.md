# AGENTS.md

This file applies inside `QuantConnect.InteractiveBrokersBrokerage/`. Read `../Docs/fork-notes.md` for the detailed mechanism reference.

## Purpose

- This IBKR submodule contains intentional initialization and session-model changes used by the parent trading system.
- Do not assume upstream IBKR brokerage behavior is identical to this checkout.

## Key Runtime Differences

- `ib-client-id` is configurable and is threaded through brokerage creation, connection, execution filtering, and order placement.
- QuantConnect account validation is disabled by default unless explicitly enabled by config.
- IB Gateway automation is optional; the brokerage can run without owning a local `IBAutomater` lifecycle.
- Remote-gateway mode is supported, so reconnect and restart logic must not assume `IBAutomater` is present.

## Working Guidance

- Do not assume client ID `0`, local gateway ownership, or automatic weekly restart behavior unless config and deployment context confirm it.
- When debugging live-trading connectivity, distinguish between:
  - transport/session issues
  - client-ID conflicts
  - remote-gateway vs local-automater deployment mode
- Keep long-form mechanism explanations in `../Docs/fork-notes.md`; keep this file focused on operational assumptions.
