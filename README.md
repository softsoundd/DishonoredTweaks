# Dishonored Tweaks

A tool for patch management and tweaking setings for Dishonored, with additional features for speedrunning.

## Features
- Patch management for `1.2 - Base Game`, `1.3 - Knife of Dunwall`, `1.4 - Brigmore Witches`, `1.4 - DaudHonored`, and `1.5 - Latest`
- Optional Boyle RNG and Timsh RNG fix installation
- Engine tweak controls (framerate limiter, FPS cap, pause on unfocus, and other visual options)
- Input tweak controls (scroll binds, FPS keys, console key, cheat key)
- Dish2Macro integration (download/install, launch, and `Dish2Macro.ini` bind/interval editing)

## Requirements
- Windows 10 or later
- .NET 8.0 or later
- Dishonored installed on PC

## Build
```powershell
dotnet restore
dotnet build DishonoredTweaks.sln
```

## Run (dev)
```powershell
dotnet run --project DishonoredTweaks/DishonoredTweaks.csproj
```

## Publish
```powershell
dotnet publish DishonoredTweaks/DishonoredTweaks.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

## Notes
- Changing patches can make existing save files unstable in-game; if that happens, start a new save on the target patch.

## Acknowledgements
- lurven and Som1Lse for the providing the design documentation and assisting with testing efforts
- HardLife for helping with Russian localisation

