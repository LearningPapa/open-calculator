# AI Context — TI DESTROYER 9000

This document is intended to be **handed verbatim to an AI coding assistant** (Claude, ChatGPT, Copilot, etc.) when you want help modifying or extending this project. Paste this whole file as your first message, then describe what you want to do.

---

## What this project is

**TI DESTROYER 9000** is a cross-platform scientific calculator with 2D and 3D graphing, written in C# using Avalonia UI 11. It targets .NET 8+ (typically built against .NET 10) and ships as a self-contained single-file binary on Windows, Linux, and macOS.

The name is a parody — not affiliated with Texas Instruments. The vibe is intentionally absurd; the code underneath is serious.

## Architecture overview

```
TI DESTROYER 9000/
├── TIDestroyer9000.csproj    # Project file — packages, target framework, AssemblyName
├── Program.cs                # Avalonia entry point (Main + AppBuilder)
├── App.axaml                 # Application-level resources: button ControlThemes
├── App.axaml.cs              # App lifecycle (loads MainWindow on startup)
├── MainWindow.axaml          # Main UI: 3 tabs (CALC, 2D GRAPH, 3D GRAPH) + history + display
├── MainWindow.axaml.cs       # Code-behind: calculator logic, equation parsing, plot orchestration
└── SurfacePlot3D.cs          # Custom GPU-accelerated 3D surface renderer (OpenGL via Silk.NET)
```

**Important:** The C# namespace is still `ScientificCalculator` internally even though the public name is "TI DESTROYER 9000." This is intentional — renaming the namespace touches every code file and adds risk for zero user benefit. Only the assembly name (`AssemblyName` in csproj), window title, and external docs use the public name.

### Key technologies

| Library | Version | Purpose | License |
|---|---|---|---|
| Avalonia | 11.2.3 | Cross-platform XAML UI framework | MIT |
| ScottPlot.Avalonia | 5.0.53 | 2D plot widget | MIT |
| Silk.NET.OpenGL | 2.23.0 | OpenGL bindings for the 3D surface renderer | MIT |
| NCalcSync | 5.2.0 | Math expression parser and evaluator | MIT |
| Tmds.DBus.Protocol | 0.21.0 | Pinned to override a vulnerable transitive dep on Linux | MIT |

### Migration history

This started as a WPF app and was migrated to Avalonia for cross-platform support:
- WPF → Avalonia (XAML stays ~95% similar; uses `ControlTheme` instead of `Style`, `Theme=` instead of `Style=`, `.axaml` extension)
- ScottPlot.WPF → ScottPlot.Avalonia (`WpfPlot` → `AvaPlot`, identical API otherwise)
- HelixToolkit.Wpf 3D viewport → custom `SurfacePlot3D` using OpenGL via Silk.NET (HelixToolkit has no Avalonia port)
- WPF `Style` keyed resources → Avalonia `ControlTheme` keyed resources
- WPF `Style.Triggers` → Avalonia CSS-like `Style Selectors` (e.g. `Selector="TabItem:selected"`)

## How the 3D surface renderer works

`SurfacePlot3D.cs` is a custom `OpenGlControlBase` subclass — Avalonia's hook for direct OpenGL rendering inside the control tree.

- **Mesh**: built CPU-side once per PLOT 3D click. 60×60 grid → 7,200 triangles, interleaved float buffer `[x,y,z, nx,ny,nz]` per vertex with normals computed via central differences for smooth shading.
- **Upload**: mesh goes into a GPU VBO + EBO via `BufferData(DynamicDraw)`. Once per formula change.
- **Rotation**: only the MVP matrix uniform (16 floats) is updated. The mesh is never re-uploaded for view changes. This is why rotation is GPU-smooth.
- **Shaders**: GLSL `#version 150` — desktop OpenGL 3.2 core profile. This is the lowest common denominator that works on Windows ANGLE, Linux Mesa, and macOS Apple OpenGL. We initially used `#version 300 es` but macOS doesn't support GLES.
- **Depth buffer**: Avalonia's framebuffer has no depth attachment by default. We allocate our own depth renderbuffer and attach it on first render. **Without this, depth testing silently produces a blank screen** — significant bug to track down.
- **Camera**: orbit camera around the surface centre, parameterized by azimuth (0–360°), elevation (5–85°), and zoom (camera distance multiplier 0.2–5.0).

### Public API of `SurfacePlot3D`

```csharp
void UpdateMesh(float[] vertices, uint[] indices, float zMin, float zMax, float range);
void SetViewAngles(float azimuth, float elevation);
void AdjustZoom(float scrollDelta);
void ResetZoom();
void ClearMesh();
```

## How equation parsing works

`MainWindow.NormalizeEquation()` translates user-friendly syntax into NCalc syntax:

- `sin/cos/tan/sqrt/log/ln/exp` → capitalized (NCalc convention)
- `log` → `Log10`, `ln` → `Log`
- `^` → `Pow(base, exponent)` via regex
- `×`/`÷` → `*`/`/`

NCalc evaluates with `x`, `y`, `z`, `Pi` as parameters. For 3D plotting, the formula is evaluated 3,600 times (60×60 grid points), so a fresh `NCalc.Expression` is constructed per call. *Optimization opportunity: precompile once and only swap parameters per evaluation.*

## How history persistence works

History is saved to the OS-standard user data folder (resolved via `Environment.SpecialFolder.ApplicationData`):

- **Windows:** `%APPDATA%\OpenCalculator\history.txt`
- **macOS:** `~/Library/Application Support/OpenCalculator/history.txt`
- **Linux:** `~/.config/OpenCalculator/history.txt`

The folder name is still `OpenCalculator` rather than `TIDestroyer9000` — this is a known cosmetic inconsistency from the rename. Updating it would orphan existing users' history. If migrating, copy old history file to new location before changing the path constant.

## How to ship a release

```bash
./build.sh        # Linux/macOS
./build.ps1       # Windows
```

Both produce 4 self-contained single-file binaries in `./publish/`:
- `TIDestroyer9000-win-x64.exe`
- `TIDestroyer9000-linux-x64`
- `TIDestroyer9000-osx-x64`
- `TIDestroyer9000-osx-arm64`

Upload these to a GitHub Release. Users download and run — no .NET install needed.

## Known limitations and gotchas

1. **WSLg does not work for the 3D renderer.** WSLg's graphics stack doesn't expose a usable OpenGL context to Avalonia's compositor. Develop in WSL but run in PowerShell on Windows, or use a real Linux distro.
2. **Window title bar font** may render blank on first run on some Linux systems if `fonts-dejavu` and `fontconfig` are not installed: `sudo apt-get install -y libfontconfig1 fonts-dejavu fonts-liberation`.
3. **`Tmds.DBus.Protocol`** must be pinned to ≥0.21.0 to avoid GHSA-xrw6-gwf8-vvr9. The csproj already does this.
4. **`AllowUnsafeBlocks`** must be `true` in the csproj because Silk.NET uses pointer offsets in `VertexAttribPointer` and `DrawElements`.
5. **`x:Name="Plot3D"`** in MainWindow.axaml is intentionally different from the class name `SurfacePlot3D` to avoid generated-code-behind name ambiguity.
6. **Internal namespace is `ScientificCalculator`** despite the public rename — this is intentional, not a bug.

## When making changes

- Calculator/2D/3D logic: edit `MainWindow.axaml.cs`
- Layout/styling/buttons: edit `MainWindow.axaml`
- Button colours and themes: edit `App.axaml` (use `ControlTheme` not `Style`, apply with `Theme="{StaticResource X}"`)
- 3D rendering pipeline (shaders, lighting, projection): edit `SurfacePlot3D.cs`
- Adding new packages: edit `TIDestroyer9000.csproj`, then `dotnet restore`

After any change run `dotnet run` to verify it still builds and behaves correctly.

## Suggested feature ideas

- **Unit converter tab** (the `Convert_Click` stub already exists in MainWindow.axaml.cs)
- **Equation history with click-to-restore**
- **Export 3D plot to PNG** via Avalonia's `RenderTargetBitmap`
- **Multiple plot overlay** on the 2D graph
- **Polar coordinates mode**
- **Save/load workspace** via JSON
- **NCalc precompilation** for faster 3D plotting

## Asking for help

When asking an AI assistant for help, please specify which file you want changed. Best practice is to ask for the **full updated file** as a drop-in replacement rather than diffs — eliminates merge ambiguity. The files are short enough to handle this comfortably.
