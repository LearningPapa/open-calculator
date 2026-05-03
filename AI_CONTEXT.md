# AI Context — Open Calculator

This document is intended to be **handed verbatim to an AI coding assistant** (Claude, ChatGPT, Copilot, etc.) when you want help modifying or extending Open Calculator. Paste this whole file as your first message, then describe what you want to do.

---

## What this project is

**Open Calculator** is a cross-platform scientific calculator with 2D and 3D graphing, written in C# using Avalonia UI 11. It targets .NET 8+ (typically built against .NET 10) and ships as a self-contained single-file binary on Windows, Linux, and macOS.

## Architecture overview

```
Open Calculator/
├── Open Calculator.csproj    # Project file — defines packages and target framework
├── Program.cs                # Avalonia entry point (Main + AppBuilder)
├── App.axaml                 # Application-level resources: button ControlThemes
├── App.axaml.cs              # App lifecycle (loads MainWindow on startup)
├── MainWindow.axaml          # Main UI: 3 tabs (CALC, 2D GRAPH, 3D GRAPH) + history panel + display
├── MainWindow.axaml.cs       # Code-behind: calculator logic, equation parsing, plot orchestration
└── SurfacePlot3D.cs          # Custom GPU-accelerated 3D surface renderer (OpenGL via Silk.NET)
```

### Key technologies and why each is used

| Library | Version | Purpose | License |
|---|---|---|---|
| Avalonia | 11.2.3 | Cross-platform XAML UI framework (replaces WPF) | MIT |
| ScottPlot.Avalonia | 5.0.53 | 2D plot widget (replaced ScottPlot.WPF) | MIT |
| Silk.NET.OpenGL | 2.23.0 | OpenGL bindings for the 3D surface renderer | MIT |
| NCalcSync | 5.2.0 | Math expression parser and evaluator (e.g. `sin(x)*cos(y)`) | MIT |
| Tmds.DBus.Protocol | 0.21.0 | Pinned manually to avoid a vulnerable transitive dependency from Avalonia on Linux | MIT |

### What replaced what (migration history)

This started as a WPF app and was migrated to Avalonia for cross-platform support:
- WPF → Avalonia (XAML stays ~95% similar, but uses `ControlTheme` instead of `Style`, `Theme=` instead of `Style=`, and `.axaml` extension)
- ScottPlot.WPF → ScottPlot.Avalonia (`WpfPlot` → `AvaPlot`, identical API otherwise)
- HelixToolkit.Wpf 3D viewport → custom `SurfacePlot3D` class using OpenGL via Silk.NET (HelixToolkit has no Avalonia port)
- WPF `Style` keyed resources → Avalonia `ControlTheme` keyed resources
- WPF `Style.Triggers` → Avalonia CSS-like `Style Selectors` (e.g. `Selector="TabItem:selected"`)

## How the 3D surface renderer works

`SurfacePlot3D.cs` is a custom `OpenGlControlBase` subclass — Avalonia's hook for direct OpenGL rendering inside the control tree.

- **Mesh**: built CPU-side once per PLOT 3D click. 60×60 grid → 7,200 triangles, interleaved float buffer `[x,y,z, nx,ny,nz]` per vertex with normals computed via central differences for smooth shading.
- **Upload**: mesh goes into a GPU VBO + EBO via `BufferData(DynamicDraw)`. This happens once per formula change.
- **Rotation**: only the MVP matrix uniform (16 floats) is updated. The mesh is never re-uploaded for view changes. This is why rotation is GPU-smooth.
- **Shaders**: GLSL `#version 300 es` (the lowest common denominator that works on both ANGLE/Windows and Mesa/Linux). Vertex shader transforms with MVP; fragment shader does Phong-ish two-sided lighting plus a z-based blue→teal→cyan colour gradient.
- **Depth buffer**: Avalonia's framebuffer has no depth attachment by default. We allocate our own depth renderbuffer and attach it on first render. **Without this, depth testing silently produces a blank screen** — this was a significant bug to track down.
- **Camera**: orbit camera around the surface centre, parameterized by azimuth (0–360°), elevation (5–85°), and zoom (camera distance multiplier 0.2–5.0).

### Public API of `SurfacePlot3D`

```csharp
void UpdateMesh(float[] vertices, uint[] indices, float zMin, float zMax, float range);
void SetViewAngles(float azimuth, float elevation);
void AdjustZoom(float scrollDelta);   // positive = zoom in
void ResetZoom();
void ClearMesh();
```

## How equation parsing works

`MainWindow.NormalizeEquation()` translates user-friendly syntax into NCalc syntax:

- `sin/cos/tan/sqrt/log/ln/exp` → capitalized (NCalc convention)
- `log` → `Log10`, `ln` → `Log` (NCalc's natural log)
- `^` → `Pow(base, exponent)` via regex
- `×`/`÷` → `*`/`/`

NCalc evaluates each expression with `x`, `y`, `z`, `Pi` as parameters. For 3D plotting, the formula is evaluated 3,600 times (60×60 grid points), so a fresh `NCalc.Expression` is constructed per call. (Optimization opportunity: precompile once and only swap parameters per evaluation.)

## How to ship a release

```bash
./build.sh        # Linux/macOS
./build.ps1       # Windows
```

Both produce 4 self-contained single-file binaries in `./publish/`:
- `OpenCalculator-win-x64.exe`
- `OpenCalculator-linux-x64`
- `OpenCalculator-osx-x64`
- `OpenCalculator-osx-arm64`

Upload these to a GitHub Release. Users download and run — no .NET install needed.

## Known limitations and gotchas

1. **WSLg does not work for the 3D renderer.** WSLg's graphics stack doesn't expose a usable OpenGL context to Avalonia's compositor. The app runs in WSLg, but `OnOpenGlInit` never fires and the 3D panel stays blank. Develop in WSL but run in PowerShell, or use a real Linux distro. The Windows binary works fine on Windows.
2. **Window title bar font** may render blank on first run on some Linux systems if `fonts-dejavu` and `fontconfig` are not installed. Standard install commands: `sudo apt-get install -y libfontconfig1 fonts-dejavu fonts-liberation`.
3. **The `Tmds.DBus.Protocol` package** must be pinned to ≥0.21.0 to avoid the GHSA-xrw6-gwf8-vvr9 vulnerability that comes in transitively via Avalonia on Linux. The csproj already does this.
4. **`AllowUnsafeBlocks`** must be `true` in the csproj because Silk.NET uses pointer offsets in `VertexAttribPointer` and `DrawElements`.
5. **`x:Name="Plot3D"`** in MainWindow.axaml is intentionally different from the class name `SurfacePlot3D` to avoid a generated-code-behind name ambiguity.

## When making changes

- Calculator/2D/3D logic: edit `MainWindow.axaml.cs`
- Layout/styling/buttons: edit `MainWindow.axaml`
- Button colours and themes: edit `App.axaml` (use `ControlTheme` not `Style`, apply with `Theme="{StaticResource X}"`)
- 3D rendering pipeline (shaders, lighting, projection): edit `SurfacePlot3D.cs`
- Adding new packages: edit `Open Calculator.csproj`, then `dotnet restore`

After any change run `dotnet run` to verify it still builds and behaves correctly.

## Suggested feature ideas

- **Unit converter tab** (the stub `Convert_Click` method already exists in MainWindow.axaml.cs)
- **Equation history with click-to-restore** — currently history is read-only text
- **Export 3D plot to PNG** — Avalonia's `RenderTargetBitmap` can capture the OpenGL output
- **Multiple plot overlay** — 2D graph could plot multiple equations in different colours
- **Polar coordinates mode** for the 2D graph
- **Save/load workspace** — serialize current equations, view angles, history to JSON

## File-by-file changes welcome

When asking for help, please specify which file you want changed. Best practice is to ask for the **full updated file** as a drop-in replacement rather than diffs — it eliminates merge ambiguity. The files are short enough to handle this comfortably.
