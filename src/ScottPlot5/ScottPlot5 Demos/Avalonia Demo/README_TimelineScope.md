# Timeline Scope Control Demo

This demo demonstrates a timeline with draggable scope selection that controls multiple synchronized detail views in a vertical scrollable layout.

## Features

1. **Timeline View**: A main timeline plot showing overview data (always visible at top)
   - Y-axis is locked (cannot pan vertically)
   - X-axis minimum is constrained to 0
   - Contains a draggable scope selection area

2. **Multiple Detail Views**: Five synchronized detail plots in vertical layout
   - X-axes are synchronized between all detail views
   - Display different data sets (cosine wave, exponential decay, random walk, sine+cosine, polynomial)
   - Automatically zoom to the selected scope range
   - Arranged vertically with individual scroll support

3. **Scrollable Interface**:
   - Timeline remains fixed at the top for easy access
   - Detail views are in a scrollable area below
   - Vertical scrollbar appears when content exceeds visible area
   - Each detail view has a fixed height of 200 pixels

4. **Interactive Scope Selection**:
   - Drag the scope boundaries to resize the selection
   - Drag the entire scope area to move it
   - Real-time updates to all detail views as scope changes

5. **Control Features**:
   - Reset View button: Returns to default scope selection
   - Generate New Data button: Creates new random data sets for all plots

## Implementation Details

### Key Components

- **TimelineScopeWindow.axaml**: UI layout with timeline and detail view areas
- **TimelineScopeWindow.axaml.cs**: Main logic for plot setup and interaction
- **TimelineScopeViewModel.cs**: ViewModel for data binding

### Key Features Implemented

1. **Y-axis Locking**: Timeline plot uses `UserInputProcessor` with `LockY = true` for pan actions
2. **X-axis Constraint**: Timeline X-axis minimum is set to 0, enforced in all drag operations
3. **Scope Visualization**: Uses `VerticalSpan` with built-in `IsDraggable` and `IsResizable` properties
4. **Synchronized Views**: Detail plots use `Plot.Axes.Link()` for X-axis synchronization
5. **Built-in Dragging**: Uses ScottPlot's native `AxisSpanUnderMouse` functionality for smooth interactions
6. **Smart Cursor**: Automatically changes cursor based on interaction area (resize vs move)
7. **Real-time Updates**: Updates detail views in real-time during dragging operations
8. **Boundary Constraints**: Enforces X >= 0 and data bounds during all operations

### Data Generation

The demo generates six different data sets:
- Timeline: Sine wave with noise
- Detail View 1: Cosine wave with different frequency
- Detail View 2: Exponential decay with sine modulation
- Detail View 3: Random walk data
- Detail View 4: Sine + cosine combination
- Detail View 5: Polynomial function with noise

## Usage

1. Run the Avalonia Demo application
2. Select "Timeline Scope Control" from the demo list
3. Interact with the timeline scope selection:
   - Click and drag the scope boundaries to resize
   - Click and drag within the scope to move it
   - Observe real-time updates in all detail views below
4. Use the vertical scrollbar to navigate through the detail views
5. Use the control buttons to reset or generate new data

This demo showcases advanced ScottPlot features including:
- Multi-plot coordination with 5+ synchronized plots
- Custom mouse interactions
- Axis constraints and linking
- Real-time plot updates
- Scrollable UI layout for handling multiple plots
- Fixed timeline with scrollable detail views
