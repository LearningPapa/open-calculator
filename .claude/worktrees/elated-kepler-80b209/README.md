# TI DESTROYER 9000

A free, open-source scientific calculator with 2D and 3D graphing, built for engineers who refuse to spend $150 on a graphing calculator from 1996. Cross-platform, GPU-accelerated, MIT-licensed, no installation required.

## Features

- **Scientific calculator** — trig, log, exp, power, modulo, full expression parser
- **2D graphing** — plot any equation `y = f(x)` with pan and zoom
- **3D graphing** — GPU-accelerated surface plots `z = f(x, y)` with Phong lighting, mouse-orbit rotation, and scroll-wheel zoom
- **Persistent history** — calculations saved between sessions in your OS user data folder
- **Cross-platform** — runs on Windows, Linux, and macOS
- **No ads, no telemetry, no subscriptions** — just an executable

## Download

Pre-built portable binaries are on the [Releases](../../releases) page. Download the file for your OS and run — no installation required.

| Platform | File |
|---|---|
| Windows x64 | `TIDestroyer9000-win-x64.exe` |
| Linux x64 | `TIDestroyer9000-linux-x64` |
| macOS Apple Silicon | `TIDestroyer9000-osx-arm64` |
| macOS Intel | `TIDestroyer9000-osx-x64` |

### Windows first run

The binary is unsigned, so Windows SmartScreen will block it on first launch with a "Windows protected your PC" dialog.

1. Click **More info**
2. Click **Run anyway**

This is a one-time prompt — Windows remembers your choice for that file.

### Linux first run

```bash
chmod +x TIDestroyer9000-linux-x64
./TIDestroyer9000-linux-x64
```

### macOS first run

macOS will block unsigned binaries by default. Right-click the file → **Open** → confirm. Or from terminal:

```bash
chmod +x TIDestroyer9000-osx-arm64
xattr -cr TIDestroyer9000-osx-arm64
./TIDestroyer9000-osx-arm64
```

### Why aren't the binaries signed?

Code signing certificates cost $100–500/year per platform. This is a free side project. The OS warnings are real safety features, but they're warning you about the absence of a paid certificate, not anything malicious. The source is fully public — verify or rebuild it yourself if you want.

## Building from source

You'll need [.NET 10 SDK](https://dotnet.microsoft.com/download) (or newer).

```bash
git clone https://github.com/LearningPapa/ti-destroyer-9000.git
cd ti-destroyer-9000
dotnet restore
dotnet run
```

To build portable binaries for all platforms:

```bash
# Windows (PowerShell)
./build.ps1

# Linux / macOS
./build.sh
```

Output goes to `./publish/` — one self-contained binary per platform.

> **Windows PowerShell tip:** if `./build.ps1` fails with "cannot be loaded… not digitally signed", run `Unblock-File .\build.ps1` once, then retry. Or run with `powershell -ExecutionPolicy Bypass -File ./build.ps1`.

## Tech stack

- **[Avalonia UI 11](https://avaloniaui.net/)** — cross-platform XAML framework (MIT)
- **[ScottPlot 5](https://scottplot.net/)** — 2D plotting (MIT)
- **[Silk.NET](https://github.com/dotnet/Silk.NET)** — OpenGL bindings (MIT)
- **[NCalc](https://github.com/ncalc/ncalc)** — math expression parser (MIT)
- Custom GLSL shader-based 3D surface renderer in `SurfacePlot3D.cs`

## Known limitations

- 3D rendering does not work in WSLg (Windows Subsystem for Linux GUI). This is a WSLg limitation, not a code issue. Use the Windows binary directly on Windows, or a real Linux distro.
- The 3D mesh is built at 60×60 resolution. Plotting a discontinuous function (e.g. `tan(x*y)`) may show interpolation artifacts.

## Name

It's a joke. This isn't affiliated with, endorsed by, or in any way connected to Texas Instruments. The name is a parody — TI makes great hardware, but their software ecosystem and pricing for educational graphing calculators is the actual target. If you want a calculator for school, buy a TI. If you want a free toy that does the same things on your laptop, this is that.

## Contributing

PRs welcome. See [`AI_CONTEXT.md`](AI_CONTEXT.md) for an architectural overview suitable for handing to an AI coding assistant. See [`ROADMAP.md`](ROADMAP.md) for planned features.

## License

MIT — see [`LICENSE`](LICENSE).
