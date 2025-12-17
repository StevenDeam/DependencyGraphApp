# Dependency Progress Dashboard

A Windows desktop application (WPF) that visualizes task dependencies, milestone progress, and project health for complex HW/FW integration programs.

## Features

- **Three Layout Modes**: Bracket (tournament-style), Swimlane (milestone-grouped), or Phase Matrix (phase-organized grid)
- **Tournament/Bracket-style Dependency Graph**: Visualizes how tasks and milestones depend on each other
- **Swimlane Layout**: Groups work items by parent milestone in horizontal lanes
- **Phase Matrix Layout**: Organizes work by Phase columns, Assembly Groups, and Discipline rows
- **Milestone Dashboard**: Table view of all milestones with progress, health, and blocker information
- **Inspector Panel**: Drill-down view of selected items showing dependencies, children, and rollup breakdowns
- **Computed Progress**: Automatic weighted average calculation of milestone progress from descendant tasks
- **Health Indicators**: Visual indicators (Green/Yellow/Red) based on target dates and blocked status
- **Blocked Status Detection**: Automatically identifies blocked items based on incomplete prerequisites
- **Filtering & Search**: Filter by discipline, status, health; search by ID or title
- **Export**: Export graph view to PNG or PDF (XPS)

## Requirements

- .NET 9.0 or later
- Windows 10/11

## How to Run

1. Build the solution:
   ```
   dotnet build DependencyDashboard.sln
   ```

2. Run the application:
   ```
   dotnet run --project DependencyDashboard.App
   ```

   Or run the built executable directly:
   ```
   DependencyDashboard.App\bin\Debug\net9.0-windows\DependencyDashboard.App.exe
   ```

## How to Load CSV

1. Click **Open CSV** button in the top toolbar, or press **Ctrl+O**
2. Select your work items CSV file
3. The graph will render automatically if the CSV is valid
4. If there are validation errors, they will be displayed in the error panel

### CSV Format

The CSV file must contain these columns:

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| Id | string | yes | Unique stable identifier |
| Title | string | yes | Human-readable name |
| Type | enum | yes | `Task` or `Milestone` |
| Discipline | string | yes | e.g., HW, FW, Test, ME |
| PercentComplete | int | tasks yes | 0-100. For milestones, ignored |
| Weight | int | yes | Story points / effort weight (>=0) |
| ParentId | string | no | Hierarchy grouping reference |
| PrereqId | string | no | Single prerequisite dependency |
| TargetDate | date | milestones yes | YYYY-MM-DD format |
| IsNA | bool | no | true/false. Default false |
| Level | int | no | Optional: hints bracket layout (0..N) |
| Phase | string | no | Phase name (e.g., "Phase 1 HW integration"). Inherited from parent if not set |

A sample CSV file (`sample_workitems.csv`) is included in the repository.

## How to Export Images

### Export to PNG
1. Load a CSV file
2. Navigate to the **Dependency Graph** tab
3. Click **Export PNG** button
4. Choose the save location
5. The current graph view will be saved as a PNG image

### Export to PDF
1. Load a CSV file
2. Navigate to the **Dependency Graph** tab
3. Click **Export PDF** button
4. Choose the save location
5. The graph will be saved as an XPS file (use a virtual PDF printer for PDF conversion)

## Keyboard Shortcuts

- **Ctrl+O**: Open CSV file
- **F5**: Reload current CSV file
- **Ctrl++**: Zoom in
- **Ctrl+-**: Zoom out
- **Ctrl+0**: Reset zoom and pan

## Graph Navigation

The graph can be navigated using multiple methods:

### Scrollbars
- **Horizontal/Vertical Scrollbars**: Scroll the graph view when content exceeds the viewport
- Scrollbars appear automatically when the graph is larger than the visible area

### Zoom and Pan
- **Mouse Wheel**: Zoom in/out (zooms toward cursor position)
- **Click and Drag**: Pan the graph by dragging on empty space
- **Ctrl++/-**: Zoom in/out via keyboard
- **Ctrl+0**: Reset zoom and pan to default

### Selection
- **Click Node**: Select node and view details in Inspector panel
- **Hover Node**: Show tooltip with full details

## Layout Modes

The application supports three graph layout modes, selectable via radio buttons in the toolbar:

### Bracket Mode (Default)
Traditional tournament/bracket-style layout where:
- Nodes are arranged in vertical columns by dependency level
- Level 0 contains items with no prerequisites
- Higher levels contain items that depend on lower-level items
- Dependencies flow left-to-right

### Swimlane Mode
Horizontal swimlane layout where:
- Items are grouped by their parent milestone (ParentId)
- Each milestone gets its own horizontal lane
- Lane header displays milestone title, ID, progress %, status pill, and health indicator
- Within each lane, items are arranged left-to-right by dependency order
- Cross-lane edges (dependencies between items in different lanes) are rendered with thicker lines and a gate indicator
- Lanes are nested by milestone hierarchy depth (child milestones are indented)
- Items without a parent are placed in a separate "Unassigned Items" lane

### Phase Matrix Mode
Grid-based layout organized by project phases:
- **X-axis (columns)**: Phase columns, each representing a major program stage
- **Y-axis (rows)**: Assembly groups stacked vertically within each phase
- **Inside groups**: Tasks organized by discipline rows (HW, FW, System, Test)

Features:
- Phase headers show the phase name
- Assembly group containers display milestone summary (title, ID, %, status, health, target date)
- Discipline labels on the left of each row
- Tasks arranged left-to-right by local dependency depth within each discipline row
- Manhattan-routed edges (horizontal-then-vertical)
- Cross-phase edges highlighted with thicker strokes
- Cross-group edges shown with distinct styling

Phase inheritance:
- If a milestone has a Phase, all descendants inherit it unless overridden
- If no Phase exists anywhere, all items default to "Phase 1"
- Phases are sorted numerically if they start with a number (e.g., "Phase 1", "Phase 2")

Dynamic sizing rules:
- **Discipline row width**: Computed based on task count
  - `RowWidth = DisciplineLabelWidth + RowLeftPadding + (taskCount × NodeWidth) + ((taskCount - 1) × NodeSpacingX) + RowRightPadding`
- **Assembly group width**: Grows to fit the widest discipline row
  - `GroupWidth = max(MinGroupWidth, maxRowWidth + GroupHorizontalPadding × 2)`
- **Phase column width**: Grows to fit the widest assembly group
  - `PhaseWidth = max(MinPhaseWidth, maxGroupWidth + PhasePadding × 2)`
- Task cards maintain consistent width (160px) and spacing (20px)
- No clipping: scrollbars enable full navigation when content exceeds viewport

Edge toggle controls:
- **Show Task Edges**: Toggle visibility of task-level dependency edges (default: ON)
- **Milestone Edges Only**: Show only milestone-to-milestone edges for a decluttered view (default: OFF)

**When to use each mode:**
- **Bracket**: Best for seeing the overall dependency flow and identifying critical paths
- **Swimlane**: Best for understanding milestone ownership and tracking work by parent grouping
- **Phase Matrix**: Best for viewing work organized by program phases with discipline alignment

## Visual Cues

### Level Swimlanes (Bracket Mode)
The graph displays faint vertical bands behind nodes, organized by dependency level:
- Each column represents a "Level" in the dependency hierarchy
- Level 0 contains nodes with no prerequisites
- Higher levels contain nodes that depend on lower-level nodes
- Alternating subtle shading helps distinguish adjacent levels
- Level labels appear at the top of each column (e.g., "Level 0", "Level 1")

### Merge Spines (Bracket Mode)
Nodes that receive multiple incoming dependencies (merge nodes) display a visual "spine":
- A vertical gray line appears to the left of the merge node
- Incoming dependency edges route to this spine
- A horizontal connector leads from the spine into the node
- A small blue circle marks the merge point
- This visualization makes it immediately obvious where multiple dependencies converge (e.g., assembly integration points)

**Example**: In the sample CSV, `ASM1_INT` is a merge node because both `ASM1_INT_PR1` and `ASM1_INT_PR2` depend on prerequisites that flow into it

### Cross-Lane Edges (Swimlane Mode)
Dependencies that cross between swimlanes are highlighted:
- Thicker stroke (2.5px vs normal 1.5px)
- Darker color for visibility
- Small rectangular gate indicator at the connection point
- These help identify external dependencies that may affect milestone timelines

## Project Structure

```
/DependencyDashboard
  /DependencyDashboard.Core     # Models, parsing, computation, graph algorithms
    Models/                     # WorkItem, DependencyEdge, enums
    Parsing/                    # CSV parsing
    Computation/                # Progress/status/health calculation
    Graph/                      # Graph layout engine
    Validation/                 # Validation rules, cycle detection
  /DependencyDashboard.App      # WPF UI
    Views/                      # XAML views
    ViewModels/                 # MVVM view models
    Controls/                   # Custom controls (GraphCanvas)
    Resources/                  # Converters, styles
  Specs.md                      # Full specification
  sample_workitems.csv          # Sample data
  README.md                     # This file
```

## Status and Health Definitions

### Status
- **Not Started**: 0% complete, not blocked
- **In Progress**: 1-99% complete, not blocked
- **Blocked**: Has incomplete prerequisite
- **Done**: 100% complete
- **N/A**: Explicitly marked as not applicable

### Health (Milestones only)
- **Green**: Done, or not blocked with >14 days to target
- **Yellow**: Not done and (blocked or 7-14 days to target)
- **Red**: Not done and <=6 days to target
- **No Date**: No target date specified

## Quick Manual Test (Graph Navigation & Visual Cues)

### Bracket Mode Testing
1. **Load CSV**: Open `sample_workitems.csv`
2. **Verify Scrollbars**: If the graph is larger than the viewport, scrollbars should appear
3. **Scroll Left/Right**: Use horizontal scrollbar to navigate between levels
4. **Scroll Up/Down**: Use vertical scrollbar to see all nodes at each level
5. **Verify Swimlanes**: Faint vertical blue bands should appear behind nodes, with "Level X" labels at the top
6. **Find Merge Node**: Look for `ASM1_INT` - it should have a vertical spine with edges merging into it
7. **Verify Merge Spine**: The spine appears as a gray vertical line with a blue dot, edges connect to it
8. **Test Zoom**: Mouse wheel should still zoom in/out correctly
9. **Test Pan**: Click and drag on empty space should still pan the graph
10. **Test Selection**: Clicking a node should still select it and update the Inspector panel

### Swimlane Mode Testing
1. **Load CSV**: Open `sample_workitems.csv`
2. **Switch Layout**: Click the "Swimlanes" radio button in the toolbar
3. **Verify Lanes**: Horizontal lanes should appear, one for each milestone with children
4. **Verify Headers**: Lane headers should show milestone title, ID, progress %, status, and health
5. **Verify Item Grouping**: Items should be grouped under their parent milestone's lane
6. **Verify Dependency Order**: Within each lane, items should flow left-to-right based on prerequisites
7. **Look for Cross-Lane Edges**: Dependencies crossing lanes should be thicker with a gate indicator
8. **Switch Back**: Click "Bracket" to verify mode switching works correctly
9. **Test Zoom/Pan**: Both should work in swimlane mode
10. **Test Selection**: Click a node in swimlane mode - Inspector should update correctly

### Phase Matrix Mode Testing
1. **Load CSV**: Open `sample_workitems.csv` (includes Phase column)
2. **Switch Layout**: Click the "Phase Matrix" radio button in the toolbar
3. **Verify Phase Columns**: Vertical phase columns should appear (Phase 1, Phase 2, Phase 3, Phase 4)
4. **Verify Assembly Groups**: Each phase should contain assembly group containers (e.g., CCA1, CCA2, ASM1)
5. **Verify Group Headers**: Group headers should show milestone title, ID, progress %, status, health, and target date
6. **Verify Discipline Rows**: Inside each group, tasks should be organized by discipline (HW, FW, System, Test)
7. **Verify Task Ordering**: Tasks within each discipline row should flow left-to-right by dependency
8. **Check Edge Routing**: Edges should use Manhattan routing (horizontal then vertical)
9. **Cross-Phase Edges**: Dependencies crossing phases should be thicker and darker
10. **Toggle "Show Task Edges"**: Uncheck to hide all edges
11. **Toggle "Milestone Edges Only"**: Check to show only milestone-to-milestone dependencies
12. **Test Zoom/Pan**: Both should work in Phase Matrix mode
13. **Test Selection**: Click a node - Inspector should update correctly
14. **Verify Scrollbars**: If the matrix is large, scrollbars should appear

## License

Internal use only.
