# TI DESTROYER 9000 Roadmap

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
Get a code signing certificate so Windows SmartScreen and macOS Gatekeeper stop warning users on first run. ~$100–500/year per platform — currently not justified.
- *Effort:* logistical, not technical

### CI/CD with GitHub Actions
Automate the cross-platform build on every release tag. Push a `vX.Y.Z` tag → binaries get built and attached to the release automatically.
- *Effort:* small-medium
- *Files affected:* new `.github/workflows/release.yml`

## Long-term (major undertakings)

### Android version
Avalonia 11 supports Android targets. Calculator and 2D graph should port cleanly. The 3D renderer is the wildcard — Android uses GLES 3.0 directly. Likely needs:
- New Android project alongside the desktop one
- Touch-friendly UI scaling
- Two-finger drag for 3D rotation, pinch-to-zoom
- Test on actual hardware (emulators have limited GPU support)
- *Effort:* large

### iOS version
Avalonia 11 supports iOS via Metal. iOS doesn't expose OpenGL — Apple deprecated it. Options:
- Port shaders to MSL (Metal Shading Language)
- Use ANGLE for iOS (translates GLES → Metal)
- Apple Developer membership ($99/year) required for App Store
- *Effort:* large

### WebAssembly version (browser)
Avalonia supports WASM. Calculator and 2D graph should work; the 3D renderer would need a WebGL2 path (close to GLES 3.0 — much of the shader code should port directly).
- *Effort:* medium-large

### Plugin/extension system
Let advanced users define custom functions, unit conversions, or graph types via a plugin folder. Significant architectural work, but would make the project genuinely extensible.
- *Effort:* very large

## Ideas under consideration

- Light/dark theme toggle (currently dark only)
- Keyboard shortcuts overlay
- Animation slider for parameterized equations
- Statistical functions and basic data plotting
- Matrix calculator tab
- Complex number support
- Localization

## Want to contribute?

Pick anything from the **Near-term** section, open an issue to claim it, and submit a PR. The architecture is documented in [`AI_CONTEXT.md`](AI_CONTEXT.md) and most features touch only one or two files.

For larger items (mobile ports, plugin system), please open a discussion first to align on approach before investing significant time.
