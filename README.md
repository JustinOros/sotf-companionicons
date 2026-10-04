# Companion Icons

Shows Kelvin's K and Virginia's heart above their heads in Sons of the Forest, so
you can tell them apart from cannibals and not shoot them by accident.

The icons are the same ones the game uses on the GPS. They stay visible through
walls and trees, shrink and fade with distance, and hide when you are standing
right next to them.

## Multiplayer

Client side only. Nothing gets installed on a dedicated server. Every player who
wants the icons needs the mod, and players without it are not affected.

## Installation

### Step 1: Install RedLoader

[RedLoader](https://github.com/ToniMacaroni/RedLoader/releases/latest) is the mod
loader for Sons of the Forest. The game cannot load any mod without it, including
this one, so install it first. You only have to do this once.

1. Download `RedLoader.zip` from the
   [latest RedLoader release](https://github.com/ToniMacaroni/RedLoader/releases/latest)
2. Extract it into your Sons of the Forest folder, the one containing
   `SonsOfTheForest.exe`, usually
   `C:\Program Files (x86)\Steam\steamapps\common\Sons Of The Forest`
3. You should now see `version.dll` and a `_Redloader` folder sitting next to
   `SonsOfTheForest.exe`
4. Launch the game once and wait until you reach the main menu. The first launch
   takes a few minutes while RedLoader processes the game files
5. Check that `MODS` appears on the main menu, then quit

### Step 2: Install Companion Icons

1. Download `CompanionIcons.zip` from the
   [latest release](https://github.com/JustinOros/sotf-companionicons/releases/latest)
2. Open the `Mods` folder inside your Sons of the Forest install, usually
   `C:\Program Files (x86)\Steam\steamapps\common\Sons Of The Forest\Mods`
3. Extract the contents of the zip into that `Mods` folder

When you are done it should look like this:

```
Sons Of The Forest/
  Mods/
    CompanionIcons.dll
    CompanionIcons/
      manifest.json
```

## Updating

Run the installer again. It replaces the old version and keeps your settings.

## Usage

The icons are on by default. Press F1 to open the console and use the
`companionicons` command to turn them on or off:

| Command | What it does |
| --- | --- |
| `companionicons` | Toggles the icons |
| `companionicons on` | Turns the icons on |
| `companionicons off` | Turns the icons off |

Settings are saved to `UserData/CompanionIcons.txt` in your Sons of the Forest
folder. Edit it with the game closed.

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `True` | Whether the icons are shown |
| `MaxDistance` | `150` | Icons are hidden beyond this many meters |
| `FadeStart` | `100` | Icons start fading out at this many meters |
| `MinDistance` | `3` | Icons fade out when you are closer than this |
| `NearSize` | `48` | Icon size in pixels up close, at 1080p |
| `FarSize` | `20` | Icon size in pixels at `MaxDistance`, at 1080p |
| `HeadOffset` | `0.45` | Height above the head in meters |

## Building

This section is only for those who want to modify the source or build it
yourself.

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and a
[RedLoader](https://github.com/ToniMacaroni/RedLoader/releases/latest) install
that has been launched at least once, so the interop assemblies exist. The build
script offers to install both if they are missing.

```
.\build.ps1 -Install
```

The game folder is found through Steam. Override it with `-GameDir "path"` or the
`SOTF_PATH` environment variable.

To produce a release zip:

```
.\build.ps1 -Package
```

## Credits

Built against RedLoader by Toni Macaroni. Rig and asset data from
[sotf-rigprobe](https://github.com/JustinOros/sotf-rigprobe).
