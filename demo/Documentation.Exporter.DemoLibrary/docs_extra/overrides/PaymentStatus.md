---
injection_mode: extend
target_anchor: page_append
---

## State Transition Matrix (Sidecar `page_append`)

| From State | To State | Trigger |
| :--- | :--- | :--- |
| `Pending` | `Authorized` | Gateway reserve succeeded |
| `Authorized` | `Settled` | Capture completed |
| `Pending` | `Declined` | Issuer rejected authorization |
