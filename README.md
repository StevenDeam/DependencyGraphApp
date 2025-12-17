# Dependency Progress Dashboard

A minimal Windows desktop application (WPF) that visualizes task dependencies and project health using a Phase Matrix layout.

## Features

- **Phase Matrix Layout**: Organizes work by Phase columns, Assembly Groups, and Discipline rows
- **Computed Progress**: Automatic weighted average calculation of milestone progress from descendant tasks
- **Health Indicators**: Visual indicators (Green/Yellow/Red) based on target dates and blocked status
- **Status Colors**: Visual status indicators (Not Started, In Progress, Blocked, Done)
- **CSV Import**: Load work items from CSV files

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

## How to Use

1. Click **Open CSV** button or press **Ctrl+O**
2. Select your work items CSV file
3. The Phase Matrix view will render automatically
4. Use scrollbars to navigate the view
5. Press **F5** to reload the current CSV file

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
| Phase | string | no | Phase name (e.g., "Phase 1 HW integration") |

A sample CSV file (`sample_workitems.csv`) is included in the repository.

## Phase Matrix Layout

The Phase Matrix organizes work items in a grid:

- **X-axis (columns)**: Phase columns representing major program stages
- **Y-axis (rows)**: Assembly groups stacked vertically within each phase
- **Inside groups**: Tasks organized by discipline rows (HW, FW, System, Test)

Features:
- Phase headers show the phase name
- Assembly group containers display milestone summary (title, ID, %, status, health, target date)
- Discipline labels on the left of each row
- Tasks arranged left-to-right by local dependency depth within each discipline row

Phase inheritance:
- If a milestone has a Phase, all descendants inherit it unless overridden
- If no Phase exists anywhere, all items default to "Phase 1"
- Phases are sorted numerically if they start with a number

## Visual Cues

### Status Colors
- **Gray**: Not Started (0% complete)
- **Blue**: In Progress (1-99% complete)
- **Orange-Red**: Blocked (has incomplete prerequisite)
- **Green**: Done (100% complete)

### Health (Milestones only)
- **Green**: Done, or not blocked with >14 days to target
- **Yellow**: Not done and (blocked or 7-14 days to target)
- **Red**: Not done and <=6 days to target
- **Gray**: No target date specified

## Project Structure

```
/DependencyDashboard
  /DependencyDashboard.Core     # Models, parsing, computation, layout
    Models/                     # WorkItem, enums, collection
    Parsing/                    # CSV parsing
    Computation/                # Progress/status/health calculation
    Graph/                      # Phase Matrix layout engine
    Validation/                 # Validation rules, cycle detection
  /DependencyDashboard.App      # WPF UI
    Views/                      # XAML views
    ViewModels/                 # MVVM view models
    Controls/                   # GraphCanvas control
  sample_workitems.csv          # Sample data
  README.md                     # This file
```

## Keyboard Shortcuts

- **Ctrl+O**: Open CSV file
- **F5**: Reload current CSV file

## License

Internal use only.
