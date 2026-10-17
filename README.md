# Among Us - Trollmate Custom Role Mod

<img width="1280" height="720" alt="image" src="https://github.com/user-attachments/assets/24b6a012-8a27-4748-be26-0efb8f1b9195" />

A fully functional, high-complexity custom role implementation for **Among Us** utilizing C#, HarmonyX hooks, BepInEx IL2CPP, and the Reactor framework. This mod injects the **Trollmate** role directly into the game's native state machine.

---

## 🎭 About the Trollmate Role

The **Trollmate** is a specialized Crewmate sub-role assigned at the start of the match. While they win alongside the regular Crewmates, their primary secondary objective is to cause absolute, harmless psychological chaos by deploying ridiculous pranks against other players. 

To win with an **Ultimate Prank Victory**, the Trollmate must successfully hit a target threshold of pranks before the match concludes.

### 🛠️ Harmless Pranks & Abilities:
* **Throw Cake (`ThrowCake`)**: Throws a massive visual cake directly into a player's face, blocking their immediate vision with a comedic UI splatter popup.
* **Squeaky Shoes (`SqueakyShoes`)**: Curses a target player's boots, forcing loud, annoying squeaking sounds to blast from their positioning whenever they move.
* **Confetti Bomb (`ConfettiBomb`)**: Detonates a colorful surprise party bomb right over a player to completely startle them and block their sightlines.
* **Fake Venting (`FakeVent`)**: Triggers the game's native vent audio structures and mechanical entry animations to make the Trollmate look incredibly suspicious without actually changing rooms.
* **Screen Glitch (`FakeSabotage`)**: Spams an artificial reactor/critical hazard warning pop-up solely onto nearby players' screens to force unneeded panic.
* **Dizzy Hex (`ReverseControls`)**: Inverts the directional input vectors (Keyboard or Analog Sticks) of the nearest target for 5 chaotic seconds.

---

## 🔧 Technical Architecture & Network Sync

Unlike basic client-side mods, this repository contains a complete architecture designed for **Multiplayer Network Stability**:

1. **Custom RPC Layer (`RpcManager.cs`)**: Intercepts native InnerNet data streams using unique byte identifiers (`150` and `151`). This ensures animations, status effects, and audio cues sync flawlessly across host and client computers without causing vanilla desyncs or game crashes.
2. **State Overrides (`Patches.cs`)**: Overrides native Unity GUI presentation methods (`IntroCutscene` and `EndGameManager`) to present seamless visual animations for **"Your Role is Trollmate"** and dedicated **Ultimate Victory Summary Screens**.

---

## 💻 Repository Directory Structure

```text
├── .gitignore               # Keeps compilation junk out of the repository
├── nuget.config             # Points compilation dependencies to the Reactor feeds
├── README.md                # General project documentation
├── TrollmateMod.csproj      # MSBuild configuration detailing IL2CPP dependencies
├── Plugin.cs                # Mod entry-point registering BepInEx parameters
├── TrollmateRole.cs         # Core role state machine and prank behaviors
├── Patches.cs               # Harmony prefix/postfix hooks mapping native game triggers
├── RpcManager.cs            # Network communication layer handling byte deserialization
└── Properties/
    └── AssemblyInfo.cs      # Assembly identity metadata and version control
```

---

## 🛠️ How to Compile & Install

### Requirements for Developers:
* **.NET 6.0 SDK** or newer.
* An IDE supporting C# 10+ (Visual Studio 2022, JetBrains Rider, or VS Code).

### Build Pipeline Instructions:
1. Clone this repository into your machine:
   ```bash
   git clone https://github.com
   ```
2. Open your terminal inside the root folder and restore dependencies using the custom NuGet feed:
   ```bash
   dotnet restore
   ```
3. Compile the production DLL file:
   ```bash
   dotnet build -c Release
   ```
4. Grab the generated `TrollmateRoleMod.dll` from the `bin/Release/net6.0/` folder.
5. Drop the DLL directly into your local `Among Us/BepInEx/plugins/` directory.
6. Launch the game via Steam/Epic Games!

---

## Gameplay Simulated Screenshot

<img width="1024" height="559" alt="image" src="https://github.com/user-attachments/assets/be8ac89a-9c95-4895-8b2b-e4eeee8c0523" />

---

## 🎹 Default Keybinds
* **[ F ] Key**: While active as the Trollmate, dynamically targets the closest living player within interaction range and rolls a random prank from your arsenal. (Has an active 20-second internal cooldown).
