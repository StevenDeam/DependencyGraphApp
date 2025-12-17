Dependency Progress Dashboard (WPF) — Specification v1.0
1. Purpose

Build a Windows desktop application that reads a CSV describing tasks, milestones, and dependencies for complex HW/FW integration programs and renders:

A tournament/bracket-style dependency graph (“assemblies merge into higher assemblies”),

A milestone dashboard with computed progress and health,

A drill-down inspector for blockers and downstream dependents,

Export of the graph view to PNG/PDF.

The tool is date-light: only milestones have target dates; tasks do not (v1). Progress is tracked via percent complete on leaf tasks (manual in spreadsheet). Parent milestone progress is computed.

Non-goals (v1): multi-user, network sync, editing CSV in-app, authentication, real Gantt date scheduling for tasks.

2. Core Concepts & Rules
2.1 Work Item Types

Task: leaf work done by an engineer; has manual percent complete (0–100).

Milestone: a gate/merge node; percent complete is computed from its descendants; may have a target date.

2.2 Hierarchy vs Dependency

Hierarchy (ParentId) is for grouping/rollups (e.g., tasks under “CCA1 Integration”).

Dependency (PrereqId) is for blocking (Finish-to-Start).

Constraint (v1): Each WorkItem may have at most one prerequisite (0 or 1).

A WorkItem may have many dependents.

2.3 Progress Computation

Task % is read directly from CSV.

Milestone % is computed as a weighted average of all descendant tasks (not milestones):

MilestonePercent = sum(task.Percent * task.Weight) / sum(task.Weight)

If sum(weight)=0 for all descendant tasks, fall back to equal weights (weight=1).

Weight corresponds to “Story Points” or “Effort Weight” (integer >= 0).

2.4 Blocked/Status Logic

Statuses are derived unless explicitly provided. App must support these statuses:

NotStarted

InProgress

Blocked

Done

NotApplicable (NA)

Default derivation (v1):

Done if PercentComplete >= 100 (task) OR computed >= 100 (milestone).

NotStarted if PercentComplete == 0 and not blocked.

InProgress if 0 < PercentComplete < 100 and not blocked.

Blocked if PrereqId exists and prerequisite item is not Done.

NA if explicitly marked NA in CSV (applies to tasks or milestones); NA tasks are excluded from rollups.

Notes:

A blocked item can still show partial % (engineers may have started but cannot finish). Status should still render as Blocked if prereq not done.

2.5 Health (Milestones Only)

Milestone “Health” is a simple indicator to highlight risk around target dates.

Inputs:

Milestone TargetDate (required for health; if missing, health = NoDate)

Definitions (v1):

DaysToTarget = (TargetDate - Today).Days

Health:

Green if Done OR (not blocked AND DaysToTarget > 14)

Yellow if not Done AND (blocked OR DaysToTarget between 7 and 14 inclusive)

Red if not Done AND (DaysToTarget <= 6)

NoDate if no target date

(Keep it intentionally simple; coding agent should implement exactly as above.)

3. CSV Input Specification
3.1 File Handling

App loads a single CSV via File->Open or startup “Open CSV”.

Must support reloading same file (F5 / button).

If parse errors exist, show a clear error panel listing row + column + message.

3.2 Required Columns

WorkItems.csv (single file):

Column	Type	Required	Notes
Id	string	yes	Unique stable identifier. No duplicates.
Title	string	yes	Human-readable name.
Type	enum	yes	Task or Milestone
Discipline	string	yes	e.g., HW, FW, Test, ME (free text)
PercentComplete	int	tasks yes	0–100. For milestones, ignored/optional.
Weight	int	yes	Story points / effort weight. >=0.
ParentId	string	no	Hierarchy grouping. Can be blank.
PrereqId	string	no	Single prerequisite dependency. Can be blank.
TargetDate	date	milestones yes	YYYY-MM-DD. Tasks blank.
IsNA	bool	no	true/false. Default false. NA items excluded from rollups.
Level	int	no	Optional: hints bracket layout (0..N).
3.3 Validation Rules

Id must be unique.

Type must be Task/Milestone.

Task PercentComplete must be 0..100.

PrereqId if provided must reference an existing Id.

ParentId if provided must reference an existing Id (or else error).

Reject cyclic dependencies in the Prereq graph (must detect and show error).

Enforce “single prerequisite” by schema (one column).

4. UI Requirements (WPF)
4.1 Main Window Layout

Three-column layout:

Top Command Bar

Open CSV

Reload

Export PNG

Export PDF

Search box (filters nodes by Id/Title substring)

Toggle: Milestones only (hides tasks in graph)

Toggle: Show blocked paths only

Left Panel: Filters

Discipline multi-select (checkbox list populated from CSV)

Status multi-select

Health filter (milestones only)

Clear Filters button

Legend (status colors + edge types)

Center: Graph View (primary)

A bracket/tournament visual representing milestone merges and dependencies.

Nodes render as “cards”:

Title (bold)

Id (smaller)

Percent (large)

Status pill

For milestones: TargetDate + Health indicator

Optional small “blocked by: <Id>” line if blocked

Edges:

Dependency edges (prereq → item): solid line with arrow direction.

Optional (toggle) hierarchy edges: light dashed (parent → child) if shown.

Interaction:

Click node: selects and populates Inspector panel (right).

Mouse wheel zoom + click-drag pan (basic graph navigation).

Hover tooltip: shows full details (Id, title, discipline, status, %).

Right Panel: Inspector
When a node is selected:

Summary section:

Id, Title, Type, Discipline

Status, Percent (computed if milestone)

Target date (milestones only)

Dependency section:

Prerequisite (if any): show card summary + “jump to” button

Dependents list: all items that depend on selected (click navigates)

Children section (hierarchy):

Direct children list (sorted by: Blocked first, then lowest %)

For milestones: show rollup breakdown by discipline (simple list: discipline -> avg %)

4.2 Secondary View: Milestone Dashboard Tab

A second tab or view mode with a table:

Columns:

Milestone (Title + Id)

Target Date

Percent

Status

Health

Blocked Count (number of descendant tasks currently blocked)

Top Blocker (the prerequisite chain item closest to blocking completion; see section 5.3)

Support sorting and filtering using the same filter panel.

5. Graph Layout & Algorithms
5.1 Graph Construction

Build two graphs:

Dependency graph (edges from prereq → item)

Hierarchy tree (edges from parent → child)

Primary graph display uses dependency edges between milestones and/or tasks depending on toggles.

5.2 Layout Strategy (v1)

Implement a deterministic, readable bracket without advanced research-grade layout:

Use Level if present:

X position derived from Level (higher level = further right, or vice-versa).

If Level missing:

Compute Level as longest dependency distance from leaves (topological order):

Nodes with no dependents can be considered leaves; but to keep it simple:

Level(node) = max(Level(prereq)) + 1 with prereq edges

Y positioning:

Group by Level, then order by Title or Id for stability.

Add vertical spacing constant.

Render edges as straight or slightly curved lines (simple Bezier acceptable).

Must support zoom/pan and re-render cleanly.

5.3 Blocker Discovery (for dashboard)

Define helper:

FindBlockerChain(item):

Follow PrereqId repeatedly until null or until a Done node.

TopBlocker:

If selected milestone is blocked (directly or via descendants), show the first not-done item in its prerequisite chain that blocks the highest number of descendant tasks.

v1 simplification: for a milestone, compute all blocked descendant tasks; take the prerequisite item that appears most frequently in their chains; if tie, choose the one with earliest target date (milestone) else lexical Id.

6. Export Requirements

Export current graph view to:

PNG (at current zoom OR “fit to view” option)

PDF (fit to page, landscape default)

Include:

Title + loaded CSV filename + export timestamp

Legend in a corner (optional but preferred)

7. Architecture & Implementation Constraints
7.1 Technology

.NET (modern) + WPF

MVVM pattern

No external services required

Minimal third-party dependencies preferred

7.2 Project Structure

Recommended:

DependencyDashboard.App (WPF UI)

DependencyDashboard.Core (models, parsing, computation, graph algorithms)

7.3 Key Classes (minimum)

WorkItem

Id, Title, Type, Discipline, PercentCompleteRaw, Weight, ParentId, PrereqId, TargetDate, IsNA

Computed fields: ComputedPercent, ComputedStatus, Health

DependencyEdge (PrereqId -> Id)

CsvLoader (parse + validate + diagnostics)

ProgressCalculator (rollups + status + health)

GraphLayoutEngine (assign node positions)

GraphViewModel, DashboardViewModel, InspectorViewModel

7.4 Performance Expectations

Should handle ~2,000 work items reasonably.

Avoid O(N^2) repeated chain traversals; cache chains or use memoization.

8. Acceptance Criteria (Definition of Done)

Load CSV, validate, show clear errors for:

missing required columns

duplicate Id

missing referenced prereq/parent

cycles in prereq dependencies

Render graph with:

nodes for items passing filters

edges for prerequisites

zoom/pan

Computed milestone progress matches spec (weighted rollup of descendant tasks excluding NA)

Blocked status and “blocked by” logic works (single prerequisite)

Milestone dashboard shows correct health categories and sortable table

Inspector panel shows prereq + dependents + children

Export PNG and PDF succeeds and matches visible graph state (or fit-to-view option)

Search filters nodes by Id/Title substring

9. Example CSV (minimal)

(Include in repository as sample_workitems.csv)

Id,Title,Type,Discipline,PercentComplete,Weight,ParentId,PrereqId,TargetDate,IsNA,Level
CCA1_HW_VIS,CCA1 Visual Inspection,Task,HW,100,2,CCA1_HW,,,
CCA1_HW_CONT,CCA1 Continuity Check,Task,HW,50,3,CCA1_HW,CCA1_HW_VIS,,,
CCA1_FW_FPGA,CCA1 Load Firmware to FPGA,Task,FW,0,5,CCA1_FW,CCA1_HW_CONT,,,
CCA1_FW_LOADED,CCA1 Firmware Loaded,Milestone,FW,,0,CCA1,CCA1_FW_FPGA,2026-01-15,false,2
CCA2_HW_VIS,CCA2 Visual Inspection,Task,HW,100,2,CCA2_HW,,,
CCA2_FW_FPGA,CCA2 Load Firmware to FPGA,Task,FW,25,5,CCA2_FW,CCA2_HW_VIS,,,
CCA2_FW_LOADED,CCA2 Firmware Loaded,Milestone,FW,,0,CCA2,CCA2_FW_FPGA,2026-01-20,false,2
ASM1_INT,CCA-ASM1 Integrated,Milestone,System,,0,ASM1,,2026-02-01,false,3
ASM1_INT_PR1,ASM1 prereq CCA1 loaded,Task,System,0,1,ASM1_INT,CCA1_FW_LOADED,,,
ASM1_INT_PR2,ASM1 prereq CCA2 loaded,Task,System,0,1,ASM1_INT,CCA2_FW_LOADED,,,


(Notes: milestones have PercentComplete blank; Level is optional hint.)

10. Instructions to the Coding Agent (one-shot guidance)

Implement exactly the data model and rules above first.

Prioritize correctness + clean MVVM + deterministic layout.

Start with:

CSV parsing/validation + unit tests for calculations and cycle detection

Progress/status computation

Simple layout + graph rendering + selection/inspector

Filters/search

Dashboard view

Export PNG/PDF

Keep rendering custom but straightforward (Canvas + ItemsControl for nodes, draw edges in an overlay).