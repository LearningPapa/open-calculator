# Open Calculator Roadmap

A living document of planned features and improvements. Items are loosely ordered by priority but subject to change. Contributions welcome — see [`AI_CONTEXT.md`](AI_CONTEXT.md) for an architectural overview.

## Near-term (small additions)

These are well-scoped and don't require major architectural changes.

### Unit converter tab
A 4th tab for converting between common units (length, mass, temperature, volume, time, energy, pressure). The `Convert_Click` stub already exists in `MainWindow.axaml.cs` waiting to be wired up.
- *Effort:* small
- *Files affected:* `MainWindow.axaml`, `MainWindow.axaml.cs`

### Equation history with click-to-restore
History panel currently displays past calculations as read-only text. Make each entry clickable to restore the equation into the active input box.
- *Effort:* small
- *Files affected:* `MainWindow.axaml`, `MainWindow.axaml.cs`

### Polar coordinates mode for 2D graph
Toggle between Cartesian `y = f(x)` and polar `r = f(θ)`. Polar plotting is just sweeping θ from 0 to 2π and converting to (x, y) before plotting.
- *Effort:* small
- *Files affected:* `MainWindow.axaml`, `MainWindow.axaml.cs`

### Multiple equation overlay on 2D graph
Plot 2–3 equations simultaneously in different colors with a small legend.
- *Effort:* small-medium
- *Files affected:* `MainWindow.axaml`, `MainWindow.axaml.cs`

### Export 3D plot to PNG
Use Avalonia's `RenderTargetBitmap` to capture the OpenGL output and save it as an image file.
- *Effort:* small-medium
- *Files affected:* `SurfacePlot3D.cs`, `MainWindow.axaml.cs`

### Save/load workspace
Serialize current equations, view angles, and history to a JSON file users can save and reopen.
- *Effort:* small-medium
- *Files affected:* `MainWindow.axaml.cs`

## Medium-term (engineering improvements)

### Performance: precompile NCalc expressions
Currently NCalc compiles the formula 3,600 times when plotting a 3D surface (once per grid point). Compile once, then swap parameters per evaluation. Should make 3D plotting noticeably faster, especially for higher resolutions.
- *Effort:* small
- *Files affected:* `MainWindow.axaml.cs`

### Higher 3D resolution option
Add a quality slider (low/medium/high) to choose 30×30, 60×60, or 120×120 mesh resolution. The GPU can handle far more than 60×60, the CPU evaluation is the bottleneck — pairs naturally with the precompile improvement above.
- *Effort:* small
- *Files affected:* `MainWindow.axaml`, `MainWindow.axaml.cs`

### Code signing for binaries
Get a code signing certificate so Windows SmartScreen and macOS Gatekeeper stop warning users on first run. ~$100–500/year per platform — currently not justified for a free project, but worth revisiting if the user base grows.
- *Effort:* logistical, not technical

### CI/CD with GitHub Actions
Automate the cross-platform build on every release tag. Push a `vX.Y.Z` tag → binaries get built and attached to the release automatically.
- *Effort:* small-medium
- *Files affected:* new `.github/workflows/release.yml`

## Long-term (major undertakings)

### Android version
Avalonia 11 supports Android targets, so the calculator and 2D graph should port cleanly. The 3D renderer is the wildcard — Android uses GLES 3.0 directly (no ANGLE), and the existing shaders should work, but mesh upload and the `OpenGlControlBase` lifecycle behave differently. Likely needs:
- New `Open Calculator.Android` project alongside the desktop one
- Touch-friendly UI scaling and larger button sizes
- Replace right-click drag with two-finger drag for 3D rotation
- Replace scroll wheel zoom with pinch-to-zoom
- Test 3D rendering on actual hardware (emulators have limited GPU support)
- *Effort:* large

### iOS version
Avalonia 11 also supports iOS via Metal. iOS doesn't expose OpenGL anymore — Apple deprecated it years ago. The 3D renderer would need a Metal shader port, or the Silk.NET layer would need to translate. Realistic options:
- Port shaders to MSL (Metal Shading Language)
- Use a higher-level Avalonia 3D abstraction if one emerges
- Build using ANGLE for iOS (translates GLES → Metal under the hood)
- Apple Developer membership ($99/year) required to distribute via App Store
- *Effort:* large, with platform-specific licensing overhead

### WebAssembly version (browser)
Avalonia supports WASM. The calculator and 2D graph should work; the 3D renderer would need a WebGL2 path (close to GLES 3.0 — much of the shader code should port directly). Would let people use the calculator from any browser without installing anything.
- *Effort:* medium-large

### Plugin/extension system
Let advanced users define custom functions, unit conversions, or graph types via a plugin folder. Significant architectural work, but would make the project genuinely extensible.
- *Effort:* very large

## Ideas under consideration

Not yet committed to but worth thinking about:

- Light/dark theme toggle (currently dark only)
- Keyboard shortcuts overlay (help screen showing all hotkeys)
- Equation graphing animations (slider for a parameter, watch the curve change)
- Statistical functions and basic data plotting (mean, median, regression lines)
- Matrix calculator tab
- Complex number support
- Localization (currently English only)

## Want to contribute?

Pick anything from the **Near-term** section, open an issue to claim it, and submit a PR. The architecture is documented in [`AI_CONTEXT.md`](AI_CONTEXT.md) and most features touch only one or two files.

For larger items (mobile ports, plugin system), please open a discussion first to align on approach before investing significant time.
