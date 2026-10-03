# Quiet, Pls 🤫

**Quiet, Pls** is an ultra-lightweight, zero-lag gamer voice awareness tool for Windows. It monitors your microphone volume in the background while you play and alerts you when you start shouting or getting excessively loud, without ever minimizing your game or stealing window focus.

---

## ✨ Features

- **🎮 Zero Game Impact & Near-Zero Footprint**:
  - Monitors audio using Windows CoreAudio / WASAPI.
  - Uses virtually **0% CPU** and under **25MB RAM**.
  - No background recording or audio encoding.
- **👻 Ghost Screen Border Flash (Level 1 Alert)**:
  - Transparent, click-through, non-activating Win32 layered overlay (`WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_LAYERED`).
  - Flashes a sleek red perimeter vignette across your monitor without taking mouse focus, keyboard input, or alt-tabbing your borderless game.
- **🔔 Headset Chime Escalation (Level 2 Alert)**:
  - If shouting continues past the escalation threshold (e.g., >1.2 seconds), it plays a discrete dual-tone audio chime directly into your headphones.
- **⏱️ Smart Debounce & Hang Time**:
  - Ignores mechanical keyboard clacks, mouse slams, or brief coughs (< 150ms).
  - Maintains speech continuity across brief pauses between words.
- **🎛️ Live Calibration Dashboard**:
  - Real-time VU meter with color zones.
  - Adjustable shout sensitivity threshold slider.
  - Device selector with hot-plug support.
- **📌 System Tray Resident**:
  - Minimizes to the system tray with right-click menu to quick-mute alerts, test pulses, or reopen the dashboard.

---

## 🛠️ Architecture

The project follows clean architecture principles:
- **`QuietPls.Core`**: Pure, cross-platform domain logic containing the state machine, threshold configs, and detection pipeline with 100% test coverage.
- **`QuietPls`**: Windows presentation and I/O layer utilizing WPF, WASAPI audio capture, and Win32 interop for transparent non-activating overlays.
- **`QuietPls.Tests`**: Automated unit tests verifying threshold transitions, transient noise rejection, syllable dips, and cooldown behavior.

---

## 🚀 Getting Started

### Prerequisites
- Windows 10 / 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Building and Running
```powershell
# Clone the repository
git clone https://github.com/BabyNejii/quiet-please.git
cd quiet-please

# Run unit tests
dotnet test

# Run Quiet, Pls
dotnet run --project src/QuietPls
```

### Publishing a Portable Single-File Executable
```powershell
dotnet publish src/QuietPls/QuietPls.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
The output `.exe` will be located in `src/QuietPls/bin/Release/net10.0-windows/win-x64/publish/QuietPls.exe`.

---

## 📄 License
MIT License.
