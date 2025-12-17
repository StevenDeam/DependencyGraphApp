Dependency Progress Dashboard — Specification
1. Purpose

The Dependency Progress Dashboard is a visual planning and status-tracking tool for complex engineering programs involving:

Multiple assemblies (CCAs, sub-assemblies, final assemblies)

Multiple disciplines (FW, HW, System, Test, etc.)

Strict dependency relationships

Integration milestones that behave like a tournament bracket, where assemblies are progressively merged until a final system is built and tested

This tool is not date-driven like a Gantt chart.
Instead, it emphasizes:

dependency order

integration structure

progress roll-ups

blockers and risk

Dates are used only for milestone health, not layout.

2. Conceptual Model (Hierarchy)

The visualization and data model have three explicit structural layers:

2.1 Phase (highest level — vertical columns)

A Phase represents a major program stage or integration boundary.

Examples:

Phase 1: HW Integration and Pre-Reqs

Phase 2: Sub-Assembly Integration

Phase 3: Final Assembly Integration

Phase 4: Software, Test, Demo Prep

Phases are conceptual, not time-based.
They indicate when assemblies connect, not how long they take.

2.2 Assembly Group (within a Phase — stacked vertically)

An Assembly Group represents a meaningful integration unit, typically a milestone such as:

CCA1

CCA2

ASM1

Sub-Assembly A

Final Assembly

Test Suite

Each Assembly Group:

Is represented by a Milestone work item

Contains tasks and possibly sub-milestones

Rolls up progress and health from its children

Assembly Groups are stacked top-to-bottom within a Phase column.

2.3 Discipline Rows (inside an Assembly Group)

Inside each Assembly Group:

Tasks are arranged by Discipline

Each discipline has its own horizontal row

Examples: FW, HW, System, Test

All tasks of the same discipline within a group appear on the same row.

Dependencies may cross discipline rows (e.g., HW task blocked by FW task).

3. CSV Input Specification
3.1 Required Columns
Column	Description
Id	Unique identifier for the work item
Title	Human-readable name
Type	Task or Milestone
Discipline	Logical discipline (FW, HW, System, Test, etc.)
Weight	Non-negative integer used for roll-ups
3.2 Optional / Conditional Columns
Column	Description
PercentComplete	Required for Tasks (0–100), optional for Milestones
ParentId	Hierarchical parent (used to group tasks under milestones)
PrereqId	Dependency: this item cannot start until prereq is Done
TargetDate	Required for Milestones (used for health computation)
IsNA	Boolean flag to exclude item from rollups
Phase	Optional but strongly recommended. Defines the Phase this item belongs to
Level	Optional hint for layout ordering (lower = earlier)
Phase inheritance rules

If a Milestone has a Phase, all descendants inherit it unless overridden.

If no Phase exists anywhere, all items default to a single Phase.

4. Computation Rules
4.1 Progress

Task progress = PercentComplete

Milestone progress = weighted average of all descendant Tasks

IsNA items are excluded

If total weight = 0, fall back to equal weighting

4.2 Status
Status	Rule
Done	PercentComplete ≥ 100
Blocked	Has Prereq and prereq is not Done
InProgress	0 < PercentComplete < 100 and not Blocked
NotStarted	PercentComplete = 0 and not Blocked
NotApplicable	IsNA = true

Blocked items may still show partial progress.

4.3 Health (Milestones only)

Health is derived from TargetDate and status:

Health	Rule
Green	Done OR (not Blocked AND >14 days remaining)
Yellow	Blocked OR 7–14 days remaining
Red	≤6 days remaining and not Done
NoDate	TargetDate missing
5. Visualization Specification
5.1 Layout Overview (Phase Matrix)

X-Axis: Phase columns
Y-Axis: Assembly Groups within each Phase
Inside Groups: Discipline rows with Tasks

This creates a grid-like structure:

Phase 1 | Phase 2 | Phase 3 | Phase 4
-------------------------------------
Group A | Group C | Group F | ...
  FW    |   FW    |   FW
  HW    |   HW    |   HW
-------------------------------------
Group B | Group D | Group G | ...

5.2 Phase Columns

Rendered as vertical swimlanes

Have visible headers (Phase name)

Fixed width

Items never overlap columns

Cross-phase dependencies are allowed

5.3 Assembly Group Containers

Each Assembly Group renders as a container box:

Header shows:

Title / Id

Percent complete

Status

Health indicator

Target date (if applicable)

Children render inside the container

Groups stack vertically within a phase

5.4 Discipline Rows

One row per discipline present in the group

Discipline label on the left

Tasks laid out left-to-right within the row

Horizontal position does NOT represent time

5.5 Dependency Edges

Prereq → Dependent

May cross discipline rows, group boundaries, and phases

Prefer Manhattan routing (horizontal then vertical)

Merge points should be visually clear (spines or bundled joins)

Provide toggles:

Show all task-level edges

Show milestone-only edges (decluttered view)

5.6 Navigation (Non-Negotiable)

Graph is hosted in a ScrollViewer

Horizontal and vertical scrollbars enabled

Canvas size always expands to content bounds

Zoom and pan supported

No visual clipping under any circumstance

6. UI Requirements
6.1 Controls

Open CSV

Reload CSV

Export PNG

Export PDF

Layout selector:

Existing layouts

Phase Matrix

Filters:

Discipline

Status

Health

Toggles:

Milestones only

Blocked paths only

Show task-level edges

6.2 Inspector Panel

Selecting any item shows:

Id, Title, Type

Discipline

Status, Progress, Health

TargetDate (if milestone)

Prerequisite

Dependents

Children

7. Export
PNG

Exports current view at current zoom

Includes title, filename, timestamp

Includes legend

PDF

Landscape

Fit to page

Same content as PNG

8. Acceptance Criteria

The system is acceptable when:

All content is visible via scroll/zoom

Phases clearly segment major integration stages

Assemblies are visually grouped and understandable

FW/HW/Test tasks align by discipline within assemblies

Dependencies clearly show integration flow

Blockers are obvious

Progress and health match CSV input

Final assembly path is visually traceable end-to-end

9. Non-Goals

No scheduling or duration estimation

No automatic timeline generation

No drag-to-reschedule behavior

No multi-user editing


Addendum: Phase Matrix Layout Visual Rules
Phase Matrix Layout Rendering Rules (Visual Contract)

Graph area must use full available tab space

The Phase Matrix view must render starting at the top-left of the tab content area (below any error panel), with no inner framed/centered viewport.

The graph background must be uniform across the entire available area (no faint “box” or inset region).

Scrollbars are responsible for navigation; the canvas itself should not be visually bounded by a separate background rectangle unless it’s explicitly the same as the tab background.

Phase columns must auto-size horizontally

Each phase column width must be computed dynamically based on its contents.

Minimum: PhaseMinWidth (configurable constant).

Actual width: max(PhaseMinWidth, widestAssemblyGroupWidth + left/right padding).

The phase header bar must stretch to the computed phase width.

Assembly group width must be content-driven

Within a phase, each assembly group (milestone container) must expand horizontally to fit its widest discipline row.

Discipline rows lay out tasks horizontally with consistent spacing.

No clipping is allowed. If content exceeds visible space, the ScrollViewer must enable navigation.

No overlap / no forced compression

The layout engine must not “squeeze” tasks by shrinking card widths to fit a fixed phase width.

If content grows, the computed layout bounds must grow (canvas width/height), and the ScrollViewer should handle it.

Performance guardrail

Measuring widths should be deterministic and based on constants (card width, spacing) and counts of items, not runtime WPF “measure passes” per element.