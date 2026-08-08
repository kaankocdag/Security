# HackerOne Bug Bounty Workspace

SystemAdmin-only workspace for preparing HackerOne / Amazon VRP reports. Existing Finding validation (`AmazonVRP`, `BugBountyEligible`, `SubmissionRecommendation`) is reused — not reinvented.

## Safety

- Scanner output is **never** auto-submitted to HackerOne.
- `HackerOne:ApiEnabled` defaults to **false**.
- Copy Full Report + Open HackerOne work without API.
- No DoS / brute / credential stuffing / post-exploitation automation.
- Application Security Candidate engines: single XSS marker, CORS Origin check, info-disclosure redaction, access-control heuristics only.

## XSS / Reflected Input candidates

- `FindingClass = VulnerabilityCandidate` (never auto `Vulnerability` without proven impact).
- `BugBountySeverity = Unassigned` until verified; `TechnicalPotentialSeverity` is separate.
- Weakness label: `Potential Weakness: CWE-79`.
- Reflection metadata: Context, Count, HtmlEncoded, AttributeEncoded, ContentType, HttpStatus, Location, InputSource, Marker.
- Properly encoded → `DoNotSubmit` + reason `Properly encoded reflected input; no XSS impact.`
- Unknown context → `ManualReview`. Never auto `Submit` without demonstrated browser-side impact.
- HackerOne markdown is **English only** (`Language: en-US`) and independent from UI language.
- Includes `Confirmed Vulnerability: No` / `Demonstrated Impact: No` / `Submission Recommendation: Manual Review`.
- EligibilityReason / classifier reasons used in H1 export are English (no TR mixed labels).
- Steps use backtick-wrapped URLs so redacted query strings do not become broken markdown links.

## Routes (web)

| Path | Purpose |
|------|---------|
| `/hackerone` | Overview counts |
| `/hackerone/candidates` | Submit + ManualReview findings |
| `/hackerone/report-builder` | Draft fields, copy, open H1, gated API submit |
| `/hackerone/programs` | Seeded Amazon VRP + sync (flagged) |
| `/hackerone/submissions` | API submission history |
| `/hackerone/settings` | Template, readiness threshold, encrypted token |

Sidebar: **HackerOne** (`adminOnly`).

## API (`CanManageBugBounty` = SystemAdmin)

| Method | Path |
|--------|------|
| GET | `/api/hackerone/overview` |
| GET | `/api/hackerone/candidates` |
| GET/PUT | `/api/hackerone/programs`, `.../enabled`, `POST .../sync` |
| POST | `/api/hackerone/domains/sync-scopes` (Hangfire) / `.../now` |
| Domains | HackerOne source + bounty summary (exact $ rarely in API) |
| GET/PUT | `/api/hackerone/settings`, token put/delete |
| CRUD-ish | `/api/hackerone/drafts`, markdown, readiness, submit |
| GET | `/api/hackerone/submissions` |
| GET | `/api/hackerone/scan-profiles` |
| POST | `/api/hackerone/candidate-assessment` |

## Architecture

```
Finding (validation fields)
    → HackerOneReportDraft (markdown + readiness)
    → Copy / Open HackerOne
    → (optional) IHackerOneApiClient submit with gates + audit

BugBountyProgram + PolicyRules (seed AmazonVRP / amazonvrp)
ScanProfile AmazonVRP → UA / rate from HackerOne:AmazonVrp config
ApplicationSecurityCandidate AssessmentMode → safe engines → Findings + RootCauseGroup
```

## Submit gates (all required)

1. `HackerOne:ApiEnabled`
2. `ReportReadinessScore >= MinReadinessScoreForSubmit`
3. Finding `SubmissionRecommendation != DoNotSubmit`
4. `BugBountyEligible || ManualReview`
5. UI explicit confirmation modal → `ExplicitConfirm=true`
6. `BugBountyAuditLog` on every attempt

## Config

```json
"HackerOne": {
  "ApiEnabled": false,
  "BaseUrl": "https://api.hackerone.com/v1",
  "OpenReportUrlTemplate": "https://hackerone.com/{handle}",
  "MinReadinessScoreForSubmit": 70,
  "AmazonVrp": {
    "UserAgent": "...",
    "RateLimitPerMinute": 20
  }
}
```

### HackerOne API credentials (HTTP Basic)

Per [HackerOne API Tokens](https://docs.hackerone.com/en/articles/8544782-api-tokens):

| Field in Settings | HackerOne meaning | Basic Auth |
|-------------------|-------------------|------------|
| Token identifier | Name you chose when creating the token | **Username** |
| API token value | Secret shown once | **Password** |

Both are required. Token is encrypted at rest (`HackerOneApiCredentials.ProtectedApiToken` via Data Protection).

Also required for live API calls:

1. Token created with at least one **group / permission** on HackerOne (otherwise no access).
2. `HackerOne:ApiEnabled=true` in Api `appsettings` + restart.
3. Copy Full Report / Open HackerOne work **without** API; sync/submit need the flag + credentials.

## Tests

- `HackerOneMarkdownBuilderTests`
- `HackerOneApiClientAndPolicyTests`
- `SubmitGateLogicTests`
- `FindingValidationClassifierTests` (existing)
- `AssessmentModeGuardTests` includes `ApplicationSecurityCandidate`
