# HexCiv sales candidate audit

- Generated: 2026-08-15T07:10:08Z
- Candidate: `stage4m-20260814` at `b878666`
- Classification: **MEASURABLE_SALES_CANDIDATE**
- Artifact candidate: **GO**
- Measurement contract: **GO**
- Measurement activation: **BLOCKED**
- Public sales: **NO_GO**

Organic purchases remain **unmeasured**. The one self-purchase is excluded from every organic sales KPI.

## Packages

| Edition | Result |
|---|---|
| full | PASS |
| demo-30-turns | PASS |

## Validation evidence

| Evidence | Result |
|---|---|
| historical-content-schema | PASS |
| historical-campaign-foundation | PASS |
| uruk-regional-simulation | PASS |
| uruk-vertical-slice | PASS |
| normal-game-regression | PASS |
| demo-save-continuation | PASS |
| product-build | PASS |
| demo-build | PASS |
| product-launch | PASS |
| demo-launch | PASS |

## Activation gates

| Gate | Requirement | Status | Evidence |
|---|---|---|---|
| P1 | production funnel row verified and probe removed | BLOCKED | shop-funnel is deployed and returned recorded=true, but the production row has not been read with privileged SQL |
| P2 | three real gameplay screenshots published | BLOCKED | https://github.com/kanta13jp1/my_web_app/pull/4408 is open and not deployed |
| H5 | private itch.io listing reviewed | BLOCKED | Cloudflare verification and account login require the operator |

This audit never publishes a build, changes a price, performs a payment, or writes to an external service.
