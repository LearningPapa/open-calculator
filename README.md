# Open Calculator

A cross-platform scientific calculator with 2D and 3D graphing, built with Avalonia UI and OpenGL. Free, open source (MIT), no installation required.

## Features

- **Scientific calculator** — trig, log, exp, power, modulo, with full expression input
- **2D graphing** — plot any equation `y = f(x)` with pan and zoom
- **3D graphing** — GPU-accelerated surface plots `z = f(x, y)` with Phong lighting, mouse-orbit rotation, and scroll-wheel zoom
- **Persistent history** — all calculations saved between sessions
- **Cross-platform** — runs on Windows, Linux, and macOS

## Download

Pre-built portable binaries are available on the [Releases](../../releases) page. Download the file for your OS, mark it executable if needed, and run — no installation required.

| Platform | File | Size |
|---|---|---|
| Windows x64 | `OpenCalculator-win-x64.exe` | ~80 MB |
| Linux x64 | `OpenCalculator-linux-x64` | ~85 MB |
| macOS Apple Silicon | `OpenCalculator-osx-arm64` | ~80 MB |
| macOS Intel | `OpenCalculator-osx-x64` | ~80 MB |

### Linux first run
```bash
chmod +x OpenCalculator-linux-x64
./OpenCalculator-linux-x64
```

### macOS first run
macOS will block unsigned binaries by default. Right-click the file → Open → confirm. Or from terminal:
```bash
chmod +x OpenCalculator-osx-arm64
xattr -d com.apple.quarantine OpenCalculator-osx-arm64
./OpenCalculator-osx-arm64
```

## Building from source

You'll need [.NET 10 SDK](https://dotnet.microsoft.com/download) (or newer) and any code editor.

```bash
git clone https://github.com/YOUR_USERNAME/open-calculator.git
cd open-calculator
dotnet restore
dotnet run
```

That's it. To build portable binaries for all platforms, run:

```bash
# Windows (PowerShell)
./build.ps1

# Linux / macOS
./build.sh
```

Output goes to `./publish/` — one self-contained binary per platform.

## Tech stack

- **[Avalonia UI 11](https://avaloniaui.net/)** — cross-platform XAML framework (MIT)
- **[ScottPlot 5](https://scottplot.net/)** — 2D plotting (MIT)
- **[Silk.NET](https://github.com/dotnet/Silk.NET)** — OpenGL bindings for the 3D renderer (MIT)
- **[NCalc](https://github.com/ncalc/ncalc)** — math expression parser (MIT)
- Custom GLSL shader-based 3D surface renderer in `SurfacePlot3D.cs`

## Known limitations

- 3D rendering does not work in WSLg (Windows Subsystem for Linux GUI). This is a WSLg limitation, not a code issue — the app works on real Linux. Use the Windows binary on Windows, or a real Linux distro.
- The 3D mesh is built at 60×60 resolution. Plotting a fundamentally discontinuous function (e.g. `tan(x*y)`) may show interpolation artifacts at the discontinuities.

## Contributing

Pull requests welcome. See [`AI_CONTEXT.md`](AI_CONTEXT.md) for an architectural overview suitable for handing to an AI coding assistant if you want help making changes.

## License

MIT — see [`LICENSE`](LICENSE).
