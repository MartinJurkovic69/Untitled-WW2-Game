# Have Mercy on My Soul

A work-in-progress **WW2 first-person shooter** built in **Unity (HDRP)**.

> **Status:** early prototype, actively in development. 

## How to play the game

**[Download and play on itch.io](https://ingrysavec.itch.io/untitled-ww2-game)**

1. Open the itch.io page above and download the game (a `.zip` file).
2. **Extract** the zip to a folder. Don't run the game from inside the zip.
3. Open the folder and run **`Have Mercy on My Soul.exe`**.
4. If Windows shows a "Windows protected your PC" (SmartScreen) message, click **More info → Run anyway**. The game is unsigned, which is why it appears.

**Requirements**

- Windows 10/11, 64-bit
- A PC with a dedicated graphics card (the game uses HDRP and DirectX 12). Integrated graphics may run poorly.

## Controls

| Key | Action |
|---|---|
| **W A S D** | Move |
| **Mouse** | Look |
| **Left mouse** | Shoot |
| **Right mouse (hold)** | Aim down sights |
| **R** | Reload |
| **1 / 2** or **mouse wheel** | Switch between primary and secondary weapon |
| **E** | Pick up a weapon / restock ammo at an ammo box |
| **G (hold)** | Open the throwables wheel, move the mouse to choose, release to equip |
| **Q** | Throw the equipped throwable |

## How I built it

The game is made in **Unity 6** with the **High Definition Render Pipeline (HDRP)** and written in **C#**. All gameplay scripts are in `Assets/scripts`.

- **Weapons** (`Weapon.cs`, `AimController.cs`): semi-automatic, automatic and burst fire, per-weapon magazine and reserve ammo, reloading, dry-fire sound, aim down sights with zoom and tighter spread, and Animator-driven recoil.
- **Inventory** (`InteractionManager.cs`): primary and secondary weapon slots, pickup and drop with physics, quick switching, and ammo boxes.
- **Throwables** (`ThrowablesManager.cs`, `ThrowableProjectile.cs`): knife, frag, smoke, molotov and lure with their own stats and behaviour, chosen from a radial wheel built with Unity UI and TextMeshPro.
- **Enemy AI** (`EnemyAI.cs`, `EnemySquad.cs`, `OrderTrigger.cs`): NavMesh agents (Unity's AI Navigation package) with a state machine (hold, patrol, investigate, combat, assault), field-of-view and line-of-sight vision, hearing, and scripted orders so battles can be directed by the game.
- **Rendering and UI:** held weapons are drawn on top of the world with an HDRP Custom Pass so they don't clip into walls, and the HUD is built with Unity UI.
- **Input:** Unity's Input System package.
- **Tools:** Git and GitHub (with Git LFS for large assets), VS Code, and itch.io for publishing the Windows build.

## Running the project from source

1. Install **[Git LFS](https://git-lfs.com/)** and run `git lfs install` once.
2. Clone the repository and run `git lfs pull`.
3. Open the project folder with **Unity Hub** using **Unity 6**.
4. Open the main scene from `Assets/Scenes` and press **Play**. The enemies need a baked NavMesh to walk.

## Roadmap

- [x] Weapons, ammo, aim down sights and weapon switching
- [x] Throwables with a selection wheel
- [x] Enemy vision, hearing, patrol/hold, orders and squads
- [ ] Enemy weapons and classes, shooting, grenades and cover
- [ ] Allies on the player's team
- [ ] Real effects, animations, sound polish and levels
