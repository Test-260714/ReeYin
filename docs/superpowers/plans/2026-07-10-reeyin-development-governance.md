# ReeYin-V Development Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立同时约束人类开发者与 AI 的 ReeYin-V 分层开发治理规范，并以强制风险门禁、验证证据和工业安全规则作为完成标准。

**Architecture:** 根级 `AGENTS.md` 提供工具可发现的强制入口，`CONTRIBUTING.md` 提供统一贡献流程，`docs/development/` 按主题解释执行细节，高风险目录通过就近 `AGENTS.md` 增加领域约束。所有文件共享同一风险分级、例外审批和交付证据模型，子目录规则不得降低根级门禁。

**Tech Stack:** Markdown、ReeYin-V（.NET 8、WPF、Prism）、PowerShell、ripgrep。

---

## File Map

- Create `AGENTS.md`: 全仓库强制门禁和 AI 权限边界。
- Create `CONTRIBUTING.md`: 人类与 AI 的统一贡献流程。
- Create `docs/development/README.md`: 规范索引、快速检查表和风险入口。
- Create `docs/development/architecture.md`: 架构职责、依赖和兼容性边界。
- Create `docs/development/coding-standards.md`: C#、XAML、异常、异步、日志和资源规则。
- Create `docs/development/testing-and-verification.md`: 风险分级、验证矩阵和证据要求。
- Create `docs/development/module-development.md`: ReeYin-V MVVM 与节点生命周期。
- Create `docs/development/safety-and-security.md`: 工业硬件、运动、数据和凭据安全。
- Create `docs/development/review-and-delivery.md`: 评审、例外、回滚和交付模板。
- Create `docs/development/ai-development.md`: AI 调查、权限、事实表达和完成声明。
- Create `Core/AGENTS.md`: Core 增量规则。
- Create `Hardware/AGENTS.md`: Hardware 增量规则。
- Create `Algorithm/AGENTS.md`: Algorithm 增量规则。
- Create `Tools/AGENTS.md`: Tools 增量规则。
- Create `CustomUI/AGENTS.md`: CustomUI 增量规则。

### Task 1: Create Global Governance Entry Points

**Files:**
- Create: `AGENTS.md`
- Create: `CONTRIBUTING.md`

- [ ] **Step 1: Write root enforcement rules**

Create `AGENTS.md` with these mandatory sections and no duplicated deep guidance:

```markdown
# ReeYin-V Repository Rules

## Scope And Rule Priority
## Mandatory Language
## Before Editing
## Scope And Change Control
## Safety And Security
## Verification Gate
## Delivery Gate
## AI-Specific Restrictions
## Detailed Standards
```

The file must state that closer `AGENTS.md` files can tighten but cannot weaken root rules, preserve existing user changes, prohibit unsupported completion claims, and link every detailed standard.

- [ ] **Step 2: Write the unified contribution workflow**

Create `CONTRIBUTING.md` with the exact lifecycle:

```text
problem definition -> local investigation -> risk classification -> design when required
-> scoped implementation -> layered verification -> self-review -> domain review -> delivery
```

Include minimum bug report fields, design triggers, compatibility expectations, rollback requirements, and the rule that commits/pushes/releases require explicit authorization.

- [ ] **Step 3: Validate entry-point completeness**

Run:

```powershell
rg -n "必须|禁止|验证|未验证|残余风险|AI" AGENTS.md CONTRIBUTING.md
```

Expected: both files contain enforceable language; `AGENTS.md` contains verification and AI-specific gates; `CONTRIBUTING.md` contains the complete workflow.

- [ ] **Step 4: Check repository status or record Git limitation**

Run:

```powershell
git status --short
```

Expected: the new files appear as untracked. If Git still reports that the directory is not a repository, record that exact limitation and do not claim a commit.

### Task 2: Create The Governance Index And Core Engineering Standards

**Files:**
- Create: `docs/development/README.md`
- Create: `docs/development/architecture.md`
- Create: `docs/development/coding-standards.md`

- [ ] **Step 1: Write the governance index**

Create `docs/development/README.md` with:

```markdown
# ReeYin-V Development Standards

## Quick Mandatory Checklist
## Risk Classification
## Standards Map
## Which Document To Read
## Rule Exceptions
```

Link all eight topic documents and all five scoped `AGENTS.md` files using repository-relative links.

- [ ] **Step 2: Write architecture boundaries**

Create `architecture.md` defining `Shell`, `Core`, `Application`, `Hardware`, `Tools`, `Algorithm`, `CustomizedDemand`, `CustomUI`, `OutputExe`, and third-party dependency responsibilities. State permitted and prohibited dependency directions, resource ownership, cross-module contracts, serialization/cache compatibility, and design-review triggers.

- [ ] **Step 3: Write coding standards**

Create `coding-standards.md` with checklists for C#, XAML, nullable values, async/threading, exceptions, logging, events, `IDisposable`, reflection/serialization, comments, generated files, and warning policy. Require current local style unless it conflicts with a stronger safety or lifecycle rule.

- [ ] **Step 4: Validate index links and headings**

Run:

```powershell
rg -n "^#|\]\(" docs/development/README.md docs/development/architecture.md docs/development/coding-standards.md
```

Expected: each file has one H1, all planned subject links appear in the index, and no absolute workstation path is embedded.

### Task 3: Create Verification, Safety, Review, And AI Standards

**Files:**
- Create: `docs/development/testing-and-verification.md`
- Create: `docs/development/safety-and-security.md`
- Create: `docs/development/review-and-delivery.md`
- Create: `docs/development/ai-development.md`

- [ ] **Step 1: Write the verification standard**

Define R0-R4 exactly as approved in the design. Include L0-L6 verification levels, the complete change-type matrix, fresh-evidence rule, minimum-project-first build strategy, test reporting fields, existing-failure handling, UI/manual scenario evidence, and the distinction between simulation, real-device verification, and unverified status.

- [ ] **Step 2: Write safety and security gates**

Define authorization requirements for real devices, motion safety checks, alarm/interlock preservation, timeout/stop/recovery behavior, production-data protection, credential redaction, network/telemetry review, external process restrictions, and R3/R4 approval requirements.

- [ ] **Step 3: Write review and delivery rules**

Define review ownership, high-risk review triggers, diff self-review, compatibility notes, rollback evidence, exception record fields, expiration requirements, and the approved completion template.

- [ ] **Step 4: Write AI-specific rules**

Define required context gathering, fact/judgment/assumption/risk separation, prohibition on invented evidence, protection of user changes, no autonomous exception approval, no destructive Git, no unauthorized commit/push/release/deploy, and honest reporting of environment limits.

- [ ] **Step 5: Cross-check the shared policy vocabulary**

Run:

```powershell
rg -n "R0|R1|R2|R3|R4|例外|批准人|有效期限|未验证|真实设备" docs/development
```

Expected: risk names and exception fields are consistent; no document describes simulated hardware validation as real-device success.

### Task 4: Create The ReeYin-V Module Development Standard

**Files:**
- Create: `docs/development/module-development.md`
- Read: `docs/reeyin-v-mvvm-skill.md`

- [ ] **Step 1: Reconcile the existing lifecycle reference**

Read the current MVVM reference and map the formal standard to these contracts without inventing alternate behavior:

```text
ModelParamBase
DialogViewModelBase
IViewModuleParam
InitModelParam<TModel>()
LoadKeyParam()
OnceInit()
Dispose()
TriggerModuleRun
RecipeParam / OutputParam
DynamicView / NodeMap
NodeParamCaches
```

- [ ] **Step 2: Write mandatory module checklists**

Create sections for main Model, main ViewModel, execution, Recipe/Output, dynamic views, cache access, navigation, XAML styles, legacy migration, and module-specific verification. Link to `../reeyin-v-mvvm-skill.md` as the deeper reference.

- [ ] **Step 3: Validate framework identifiers**

Run:

```powershell
rg -n "ModelParamBase|DialogViewModelBase|InitModelParam|LoadKeyParam|OnceInit|base\.Dispose|TriggerModuleRun|RecipeParam|OutputParam|NodeParamCaches|DynamicView" docs/development/module-development.md
```

Expected: every lifecycle identifier is covered and base-call requirements are explicit.

### Task 5: Create Scoped High-Risk Rules

**Files:**
- Create: `Core/AGENTS.md`
- Create: `Hardware/AGENTS.md`
- Create: `Algorithm/AGENTS.md`
- Create: `Tools/AGENTS.md`
- Create: `CustomUI/AGENTS.md`

- [ ] **Step 1: Write Core incremental rules**

Cover public contracts, dependency direction, database/cache compatibility, global event ownership, concurrency, initialization order, consumer builds, and migration/rollback requirements.

- [ ] **Step 2: Write Hardware incremental rules**

Cover real-device authorization, simulation separation, vendor abstraction, timeouts, retries, cancellation, stop/emergency behavior, idempotent initialization, event/resource cleanup, disconnect/reconnect, and required manual verification evidence.

- [ ] **Step 3: Write Algorithm incremental rules**

Cover input/output contracts, units, coordinate systems, null/empty/large inputs, numerical tolerance, deterministic tests, performance baselines, cancellation, Native ownership, and disposal.

- [ ] **Step 4: Write Tools incremental rules**

Cover Model/ViewModel lifecycle, recipe/output contracts, execution states, dynamic-page uniqueness, cache lookup, output refresh, error propagation, repeated initialization, and disposal.

- [ ] **Step 5: Write CustomUI incremental rules**

Cover Prism navigation, binding diagnostics, UI-thread ownership, shared resources, themes, DPI/scaling, accessibility, repeated navigation, event cleanup, and visual/manual evidence.

- [ ] **Step 6: Confirm scoped rules only tighten root rules**

Run:

```powershell
rg -n "降低|跳过.*验证|无需.*构建|自动批准|视为通过" Core/AGENTS.md Hardware/AGENTS.md Algorithm/AGENTS.md Tools/AGENTS.md CustomUI/AGENTS.md
```

Expected: no scoped file weakens root verification or approval requirements.

### Task 6: Perform Documentation Quality Gates

**Files:**
- Verify: all files from Tasks 1-5
- Verify: `docs/superpowers/specs/2026-07-10-reeyin-development-governance-design.md`

- [ ] **Step 1: Scan for unresolved placeholders and merge artifacts**

Run:

```powershell
rg -n "TBD|TODO|待定|稍后补充|以后补充|<<<<<<<|=======|>>>>>>>" AGENTS.md CONTRIBUTING.md docs/development Core/AGENTS.md Hardware/AGENTS.md Algorithm/AGENTS.md Tools/AGENTS.md CustomUI/AGENTS.md
```

Expected: no matches.

- [ ] **Step 2: Validate every relative Markdown link**

Use PowerShell to extract local Markdown link targets from the created files, resolve each target relative to its source file, and fail when a target does not exist. External HTTP(S) references are excluded from local existence checks.

Expected: zero missing local targets.

- [ ] **Step 3: Compare implementation against the approved design**

Check every acceptance criterion in the design document and verify a corresponding formal rule exists. Confirm especially: unified audience, strong gates, R0-R4, exception approval, AI restrictions, MVVM lifecycle, simulation/device distinction, and delivery evidence.

- [ ] **Step 4: Inspect final file inventory**

Run:

```powershell
Get-Item AGENTS.md,CONTRIBUTING.md,docs/development/*.md,Core/AGENTS.md,Hardware/AGENTS.md,Algorithm/AGENTS.md,Tools/AGENTS.md,CustomUI/AGENTS.md | Select-Object FullName,Length
```

Expected: 15 formal governance files exist and none is empty.

- [ ] **Step 5: Check final Git diff or record repository limitation**

Run:

```powershell
git status --short
git diff --check
git diff --stat
```

Expected: only intended governance files and the approved design/plan are present. If Git remains unavailable, report the limitation verbatim and use the verified file inventory instead; do not claim a clean diff or commit.

- [ ] **Step 6: Deliver evidence**

Report created files, validation commands and results, any environment limitation, unverified items, and residual risks. Do not claim build or runtime validation because this task changes documentation only.

## Commit Policy For This Environment

The planning workflow normally uses frequent commits. This workspace currently returns `fatal: not a git repository` even though a `.git` entry is visible to directory listing. Do not create, repair, or mutate repository metadata without explicit user authorization. If Git becomes operational and the user explicitly requests commits, use small documentation-only commits; otherwise leave all files uncommitted and report that state.
