# Dependency Progress Dashboard

A Windows desktop application (WPF) that visualizes task dependencies, milestone progress, and project health for complex HW/FW integration programs.

## Features

- **Tournament/Bracket-style Dependency Graph**: Visualizes how tasks and milestones depend on each other
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

- **Mouse Wheel**: Zoom in/out
- **Click and Drag**: Pan the graph
- **Click Node**: Select node and view in Inspector panel
- **Hover Node**: Show tooltip with full details

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

## License

Internal use only.
