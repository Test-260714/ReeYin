# Development Governance Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make simple ReeYin-V changes faster through strict Lite paths while giving every change a clear route from modified files to contracts, consumers, required reading, and verification.

**Architecture:** Keep `CONTRIBUTING.md` as the single authoritative workflow and risk source, keep root `AGENTS.md` as the non-negotiable entry, and add two operational maps for impact and verification. Scoped `AGENTS.md` files contain directory-specific deltas only. A read-only PowerShell validator enforces objective document structure without deciding risk or approval.

**Tech Stack:** Markdown, PowerShell 7/Windows PowerShell-compatible syntax, Git read-only inspection, `.sln`/`.csproj` static analysis.

---

## File Structure

**Modify:**

- `AGENTS.md` - concise repository-wide safety, scope, evidence, and navigation gate.
- `CONTRIBUTING.md` - authoritative `R0-Lite`, `R1-Lite`, R1-R4 workflow and rules-governance policy.
- `docs/development/README.md` - risk-based reading and navigation index.
- `docs/development/testing-and-verification.md` - verification levels and evidence details referencing the authoritative risk source.
- `docs/development/review-and-delivery.md` - review, exception, and scaled delivery templates.
- `docs/development/architecture.md` - correct exception references and link to the impact map.
- `docs/development/coding-standards.md` - link generated/third-party rules to protected directories.
- `docs/development/safety-and-security.md` - retain high-risk details while removing duplicate risk definitions.
- `docs/development/ai-development.md` - teach AI to apply and exit Lite paths without weakening authorization boundaries.
- `Core/AGENTS.md`, `Hardware/AGENTS.md`, `Algorithm/AGENTS.md`, `Tools/AGENTS.md`, `CustomUI/AGENTS.md` - replace repeated common reading/gate text with directory-specific deltas and map links.

**Create:**

- `docs/development/change-impact-map.md` - directory-to-contract/consumer/side-effect investigation map.
- `docs/development/build-and-test-map.md` - verified/static/unverified build and test entry map.
- `Application/AGENTS.md` - application services, alarms, permissions, navigation, and startup consumers.
- `Shell/AGENTS.md` - composition root, module loading, startup/shutdown, and release boundaries.
- `CustomizedDemand/AGENTS.md` - customer module cross-domain and compatibility boundaries.
- `Semiconductor/AGENTS.md` - semiconductor domain model, process, and data boundaries.
- `thirdparty/AGENTS.md` - provenance, license, integrity, and binary protection.
- `packages/AGENTS.md` - restored/vendored dependency protection.
- `OutputExe/AGENTS.md` - generated output and release artifact protection.
- `Resource/AGENTS.md` - shared resource identity and consumer compatibility.
- `GemeralUI/AGENTS.md` only if repository investigation proves an independent authority boundary; otherwise document its ownership in the impact map.
- `scripts/Test-Governance.ps1` - deterministic read-only governance validator.
- `scripts/verification/governance-fixtures/` only if fixture files are necessary; prefer temporary files created under the script's test mode and removed in `finally`.

The existing user-owned changes in `Directory.Build.props`, `scripts/`, and Halcon design/plan files must not be modified, deleted, reformatted, or included in this task's diff.

### Task 1: Establish Baseline and Rule Migration Matrix

**Files:**
- Read: `AGENTS.md`
- Read: `CONTRIBUTING.md`
- Read: `docs/development/*.md`
- Read: `*/AGENTS.md`
- Create during working notes only: no repository file

- [ ] **Step 1: Capture the exact workspace baseline**

Run:

```powershell
git status --short
git diff --name-only
```

Expected: user-owned changes remain visible and are recorded as exclusions.

- [ ] **Step 2: Inventory duplicate policy definitions**

Run:

```powershell
rg -n "R0|R1|R2|R3|R4|风险等级|不可豁免|新鲜证据|真实设备|生产数据" AGENTS.md CONTRIBUTING.md docs/development Core/AGENTS.md Hardware/AGENTS.md Algorithm/AGENTS.md Tools/AGENTS.md CustomUI/AGENTS.md
```

Expected: every complete risk table and duplicated hard gate has an identified future authority or intentional local summary.

- [ ] **Step 3: Inventory first-level directories and project relationships**

Run:

```powershell
Get-ChildItem -Directory | Select-Object -ExpandProperty Name
dotnet sln ReeYin.sln list
rg -n "ProjectReference|PackageReference|Reference Include|RegisterForNavigation|IModule|DynamicView" -g "*.csproj" -g "*.cs" -g "*.xaml"
```

Expected: evidence for directory roles, consumers, special dependencies, and `GemeralUI` ownership; no product build is claimed.

### Task 2: Make CONTRIBUTING the Authoritative Workflow

**Files:**
- Modify: `CONTRIBUTING.md:27`
- Modify: `CONTRIBUTING.md:41`
- Modify: `CONTRIBUTING.md:62`
- Modify: `CONTRIBUTING.md:91`

- [ ] **Step 1: Add the authoritative risk decision flow**

Add `R0-Lite` and `R1-Lite` before R1-R4, including all-entry conditions, explicit exclusions, and the rule that uncertainty or newly discovered consumers forces escalation.

- [ ] **Step 2: Add a compact decision tree and calibrated examples**

Include repository examples for pure wording, private null handling, shared resource changes, Recipe/Output changes, alarm semantics, hardware commands, and approved R4 operations. State that effects override filenames and directories.

- [ ] **Step 3: Scale investigation, design, verification, and delivery by risk**

Define the exact minimum for `R0-Lite` and `R1-Lite`; preserve existing R2-R4 consumer, compatibility, failure/recovery, review, rollback, authorization, and real-environment gates.

- [ ] **Step 4: Add governance-of-governance rules**

State that changes to risk, safety, authorization, or minimum verification are at least R2; new first-level source/resource/output directories require impact-map coverage; weakening scoped rules requires explicit design, compatibility, validation, and rollback.

- [ ] **Step 5: Check the authoritative workflow for contradictions**

Run:

```powershell
rg -n "R0-Lite|R1-Lite|R1|R2|R3|R4|退出|升级|规则自身" CONTRIBUTING.md
```

Expected: one complete definition for every path and an explicit Lite exit rule.

### Task 3: Create the Impact and Build/Test Maps

**Files:**
- Create: `docs/development/change-impact-map.md`
- Create: `docs/development/build-and-test-map.md`
- Modify: `docs/development/README.md`

- [ ] **Step 1: Write the change-impact map from observed repository facts**

For every first-level source, UI, resource, dependency, and output directory, record responsibility, contracts, typical consumers, investigation searches, risk escalation triggers, and whether a scoped `AGENTS.md` applies. Mark inferred relationships as requiring code confirmation.

- [ ] **Step 2: Resolve `GemeralUI` ownership**

Inspect `GemeralUI/ReeYin.ChartShow/ReeYin.ChartShow.csproj`, its references, resource dictionaries, and consumers. Create a scoped rule only if it owns an independent contract; otherwise map it under the appropriate UI authority and record the compatibility boundary.

- [ ] **Step 3: Write the build/test map with evidence states**

For each mapped project family, record the minimal project build, direct tests, consumer builds, target configuration/architecture, special SDK/native/license requirements, and one of `已验证`, `静态核对`, or `未验证`. Do not copy historical commands as verified evidence.

- [ ] **Step 4: Convert the development README into a navigation index**

Replace the duplicate full risk table with an authoritative link and concise summary. Add a path from changed file -> nearest rule -> impact map -> risk -> build/test map -> escalation.

- [ ] **Step 5: Verify map coverage**

Run:

```powershell
Get-ChildItem -Directory | Select-Object -ExpandProperty Name
rg -n "Application/|Shell/|Core/|Hardware/|Algorithm/|Tools/|CustomizedDemand/|Semiconductor/|CustomUI/|GemeralUI/|Resource/|thirdparty/|packages/|OutputExe/" docs/development/change-impact-map.md
```

Expected: every relevant first-level directory is covered or has an explicit exclusion reason.

### Task 4: Add Missing Scoped Rules

**Files:**
- Create: `Application/AGENTS.md`
- Create: `Shell/AGENTS.md`
- Create: `CustomizedDemand/AGENTS.md`
- Create: `Semiconductor/AGENTS.md`
- Create: `thirdparty/AGENTS.md`
- Create: `packages/AGENTS.md`
- Create: `OutputExe/AGENTS.md`
- Create: `Resource/AGENTS.md`
- Conditionally create: `GemeralUI/AGENTS.md`

- [ ] **Step 1: Add four domain rules**

Each file must contain only: scope, owned contracts, mandatory investigation, escalation triggers, and minimum verification/delivery. Link root rules, `CONTRIBUTING.md`, the impact map, and only relevant topic standards.

- [ ] **Step 2: Add four protection rules**

Define permitted and prohibited changes for third-party binaries, restored/vendored packages, generated outputs/release artifacts, and shared resources. Require provenance, license/integrity, regeneration source, or consumer compatibility as applicable.

- [ ] **Step 3: Confirm scoped rules only tighten root policy**

Run:

```powershell
rg -n "无需|跳过|自动批准|视为通过|降低|免除" Application/AGENTS.md Shell/AGENTS.md CustomizedDemand/AGENTS.md Semiconductor/AGENTS.md thirdparty/AGENTS.md packages/AGENTS.md OutputExe/AGENTS.md Resource/AGENTS.md
```

Expected: no language weakens safety, verification, review, authorization, or evidence requirements; any legitimate negative-language match is manually reviewed.

### Task 5: Simplify Existing Entry and Topic Documents

**Files:**
- Modify: `AGENTS.md`
- Modify: `docs/development/testing-and-verification.md`
- Modify: `docs/development/review-and-delivery.md`
- Modify: `docs/development/architecture.md`
- Modify: `docs/development/coding-standards.md`
- Modify: `docs/development/safety-and-security.md`
- Modify: `docs/development/ai-development.md`

- [ ] **Step 1: Shorten root AGENTS without removing hard gates**

Retain scope priority, mandatory terms, user-change protection, safety/security, evidence, delivery, and AI restrictions. Replace the duplicate risk details with a concise link to `CONTRIBUTING.md`; add impact/build map links and Lite escalation language.

- [ ] **Step 2: Replace duplicate risk matrices with focused summaries**

Testing owns L0-L6 and evidence; review owns sign-off, approval, exceptions, and delivery; safety owns non-waivable device/data/security gates; AI owns tool and authorization boundaries. Each references the authoritative workflow instead of redefining it.

- [ ] **Step 3: Add scaled evidence templates**

Provide compact `R0-Lite` and `R1-Lite` delivery forms while preserving the complete R2-R4 template. Lite reports must still state risk rationale, changed/not-changed scope, evidence, compatibility, rollback, and unverified items.

- [ ] **Step 4: Correct cross-document exception and impact links**

Ensure architecture links to the actual exception section in review/delivery and to the impact map. Link generated and third-party coding guidance to protected directory rules.

- [ ] **Step 5: Verify hard-gate preservation**

Run:

```powershell
rg -n "AI 不得|真实设备|生产数据|不可豁免|新鲜证据|回滚|停止|用户.*改动" AGENTS.md CONTRIBUTING.md docs/development
```

Expected: every safety and authorization concept remains present in its authority and reachable from the root entry.

### Task 6: Refocus Existing Domain Rules

**Files:**
- Modify: `Core/AGENTS.md`
- Modify: `Hardware/AGENTS.md`
- Modify: `Algorithm/AGENTS.md`
- Modify: `Tools/AGENTS.md`
- Modify: `CustomUI/AGENTS.md`

- [ ] **Step 1: Remove duplicated common workflow text**

Keep local responsibilities, actual compatibility contracts, investigation points, escalation triggers, and domain-specific verification. Replace repeated risk/evidence definitions with links.

- [ ] **Step 2: Add impact/build map routes**

Every domain rule must tell a contributor where to find typical consumers and verification commands and remind them that map entries are investigation starts, not proof of completeness.

- [ ] **Step 3: Check local rules remain independently actionable**

Run:

```powershell
rg -n "适用范围|契约|调查|升级|验证|change-impact-map|build-and-test-map" Core/AGENTS.md Hardware/AGENTS.md Algorithm/AGENTS.md Tools/AGENTS.md CustomUI/AGENTS.md
```

Expected: all five files contain scope, local contract, investigation, escalation, and verification guidance.

### Task 7: Implement the Read-Only Governance Validator

**Files:**
- Create: `scripts/Test-Governance.ps1`
- Preserve: all pre-existing files under `scripts/`

- [ ] **Step 1: Define script inputs and deterministic output**

Support `-RepositoryRoot` defaulting to the parent of `scripts`, and an optional `-FixtureRoot` for isolated failure tests. Return exit `0` for success and nonzero for objective violations; output `[PASS]`/`[FAIL]` records with paths and reasons.

- [ ] **Step 2: Implement required-file and scoped-link checks**

Validate the root/process/topic/map files, required scoped rules, and that scoped rules link to root policy. Resolve Markdown relative file links while ignoring web URLs, anchors, and illustrative code blocks.

- [ ] **Step 3: Implement content checks**

Detect merge markers, unfinished placeholders outside explicitly escaped examples, repository-external absolute paths in governance rules, and missing delivery-template fields. Never print suspected secret values; report only file, line, and category.

- [ ] **Step 4: Implement directory coverage checks**

Compare first-level directories against impact-map entries or an explicit exclusions table. Do not infer that every directory requires a scoped rule.

- [ ] **Step 5: Prove success and controlled failures**

Run on the repository, then run against temporary fixture copies containing one broken relative link, one missing required file, and one merge marker. Each negative fixture must return nonzero; cleanup occurs in `finally` and must stay under the temporary directory.

Expected: repository run exits `0`; each negative fixture exits nonzero with the intended diagnostic category.

### Task 8: Final Consistency and Scenario Verification

**Files:**
- Verify all files listed above
- Do not modify product source or user-owned unrelated changes

- [ ] **Step 1: Run governance validation**

Run:

```powershell
pwsh -NoProfile -File scripts/Test-Governance.ps1 -RepositoryRoot .
```

If `pwsh` is unavailable, run the same script with Windows PowerShell and record the environment distinction.

- [ ] **Step 2: Run Markdown and diff checks**

Run:

```powershell
git diff --check
rg -n "<<<<<<<|=======|>>>>>>>" AGENTS.md CONTRIBUTING.md docs/development Application/AGENTS.md Shell/AGENTS.md CustomizedDemand/AGENTS.md Semiconductor/AGENTS.md Core/AGENTS.md Hardware/AGENTS.md Algorithm/AGENTS.md Tools/AGENTS.md CustomUI/AGENTS.md thirdparty/AGENTS.md packages/AGENTS.md OutputExe/AGENTS.md Resource/AGENTS.md
git diff --stat
git status --short
```

Expected: no whitespace/conflict errors; unrelated user changes remain untouched and clearly separated.

- [ ] **Step 3: Perform four paper scenarios**

Document the derived reading, investigation, risk, verification, and escalation result for:

1. comment-only wording correction -> `R0-Lite`;
2. private null guard in one project with direct tests -> candidate `R1-Lite`;
3. Recipe/Output or shared resource contract change -> R2 or higher;
4. alarm semantics or hardware command change -> R3, with R4 only for specifically approved irreversible operations.

Expected: every scenario produces a deterministic next-action checklist and Lite candidates are rejected when any exclusion applies.

- [ ] **Step 4: Self-review against the approved design**

Check every goal, non-goal, file, migration step, validation requirement, and risk control in `docs/superpowers/specs/2026-07-14-development-governance-optimization-design.md`. Record omissions as unfinished; do not claim completion until resolved or explicitly disclosed.

- [ ] **Step 5: Deliver without unauthorized Git or external actions**

Report modified files, fresh command evidence with time/environment/exit/failure counts, unverified items, compatibility, rollback, residual risk, and required human review. Do not commit, push, merge, publish, deploy, modify production data, or operate real equipment without separate explicit authorization.

