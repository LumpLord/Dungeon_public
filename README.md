# Dungeon Public – Modular Combat & Tooling Systems

This repository showcases modular systems developed for a third-person dungeon RPG in Unity. It focuses on reusable, extensible tools for melee combat, editor-driven attack animation design, and AI behavior coordination.

# The BIG idea of the project as a whole - A dungeon crawler that learns and adapts from the player(s)
- Will include:
   - A wide diversity of dungeon-themed challenges
   - A data capture system to track and normalize data realted to player play styles, strengths, and weakness, with the end point being to have the dungeon adapt to any individual play style - an organic and challenging AI
   - Data modeling to predict what will challenge a given player most 
   - Data compression / simplization to make AI decision making lightweight on the user side (no local modeling while the game is running)


> 🔒 This project powers a private, in-development RPG with full player, enemy, and animation systems.  
> For details or collaboration inquiries, reach out via GitHub or @LordLump.

---

## What's Included (Public - May 2025)

### ✅ Attack Phase System (ScriptableObject-Based)
- Author attacks using **modular animation phases**
- Each phase defines:
  - Position/rotation offset
  - Duration
  - Custom interpolation curve
  - Damage curve (e.g. sweet spots)
  - Combo window and behavior

### ✅ Equipped Weapon Controller
- Interprets `AttackAsset` data to animate weapons in code
- Supports:
  - **Chained combos**
  - Damage triggering via collider
  - Returning to a guard pose
  - Auto-queue of next attack on input

### ✅ Custom Weapon Pose Editor
- Unity Editor window to:
  - Pose a weapon in-scene
  - Save that pose to an attack phase
  - Preview per-phase animation steps
- Streamlines animation design for non-animator designers

### ✅ Lightweight Enemy Combat AI
- Example `EnemyCombatController` and modular behavior states for AI coordination
- Includes basic attack and movement logic:
  - Rush, Stalk, Attack, and Retreat
- Built to demonstrate how enemy AI can use the **same AttackAssets** as the player

---

## 📁 Repo Structure

- `Assets/Combat/`
  - `AttackAssets/` – Attack combos and phases
  - `AttackPhases/` – Individual attack phase data
  - `EquippedWeaponController.cs` – Controls weapon behavior
  - `DamageSource.cs` – Core dependency for combat logic
- `Assets/Editor/`
  - `WeaponPoseEditor.cs` – Editor tool for posing and authoring attacks
- `Assets/Enemy/`
  - Enemy-related scripts and AI logic (Rush, Attack, Stalk, Retreat, etc.)
- `Assets/Player/`
  - Player control and combat handling scripts

---

## Designed For:
- Solo and indie developers needing flexible melee combat tools
- Designers who want to author weapon motion paths without keyframe animation
- RPG, Soulslike, and action prototype builders
- Developers looking to extend from a basic AI state system using ScriptableObjects

---

## 🧙 Author

Created by **LordLump** (April 2025–present)  
Follow the development of the full game on the [private Dungeon repository].

---

## License

This repository is licensed under the MIT License.  
Feel free to use, modify, and share – attribution appreciated!

---

## Changelog

### June 30, 2025
- Modular AI combat behaviors stabilized with PursuitState improvements (navmesh validation, movement smoothing)
- Timed pursuit behavior with override for unreachable targets
- New debug and behavior diagnostics added to `EnemyCombatController.cs`
- Created `WeightedRandomSelector.cs` for adaptive behavior weighting
- Time control utility added for debugging (slow motion toggle and pause)
- Updated public repo asset list to remove internal-only scripts
- Improved movement logic and fixed weapon drop bug during pursuit

### May–June 2025
- Initial public release of:
  - `AttackPhase` and `AttackAsset` modular animation system
  - `EquippedWeaponController` for real-time phase-based melee attacks
  - Unity Editor tooling for attack design (`WeaponPoseEditor`)
  - Modular enemy behavior system with Rush, Attack, Retreat, and Stalk states