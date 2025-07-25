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

# Project plan and timelines
| # | Work‑stream | Start | Finish | Work Days |
|---|-------------|-------|--------|-----------|
| **1** | ✔ Playability polish (done) | 21 Jul 2025 | **24 Jul 2025** | 4 |
| **2** | Character model & basic anims | 28 Jul 2025 | 01 Aug 2025 | 5 |
| **3** | Animancer Pro migration (partial) | 04 Aug 2025 | 08 Aug 2025 | 5 |
| **4** | Block / Parry mechanic | 11 Aug 2025 | 19 Aug 2025 | 7 |
| **5** | Damage‑Type & R‑P‑S system | 20 Aug 2025 | 02 Sep 2025 | 10 |
| **6** | Instrumentation & Logger v1 | 03 Sep 2025 | 09 Sep 2025 | 5 |
| **7** | Simple analytics dashboard | 10 Sep 2025 | 11 Sep 2025 | 2 |
| **8** | Versioned balance data | 12 Sep 2025 | 15 Sep 2025 | 2 |
| **9** | Automated test harness | 16 Sep 2025 | 25 Sep 2025 | 8 |
| **10** | DunGen procedural arenas | 26 Sep 2025 | 30 Sep 2025 | 3 |
| **11** | Variable‑sweep tool | 01 Oct 2025 | 10 Oct 2025 | 8 |
| **12** | Enemy repertoire upgrade | 13 Oct 2025 | 20 Oct 2025 | 6 |
| **13** | Data‑capture arena (Modular Castle kit) | 21 Oct 2025 | 23 Oct 2025 | 3 |
---

## What's Included (Public – July 2025)

### Core Gameplay
- **Third-person controller** with free orbit camera & optional **lock-on mode** (target cycle, auto-unlock on death)
- Player movement: idle ⇄ walk/run, jump, **directional dodge** (Left Alt)
- **Projectile & launcher** system – cooldown, physics/hitscan, damage + hit-point forwarding
- Cursor-lock helper and aim/lock **UI reticle**

### Modular Combat Toolkit
- **Attack Phase / Attack Asset** ScriptableObject pipeline  
  *Pose → phase → combo* authoring with custom curves & damage windows
- **Equipped Weapon Controller** → real-time phase blending, combo queue, collider-based hits
- **Weapon Pose / Phase Editors** – in-scene handles, curve preview, batch save

### Enemy AI Framework
- **EnemyCombatController** with weighted, modular **state machine**  
  _States shipped_: Pursuit · Rush · Stalk · Attack · Retreat · **Investigate** (on distant hit)
- Cooldowns, “allowed-previous” gates, fail-safe re-targeting & rich debug logging
- NavMesh-aware path validation and auto-charge when far from player

### Health & Damage
- `HealthComponent` ( `OnDamaged(amount,type,source,hitPoint)`  · `OnDeath`  · `IsAlive` )
- `DamageType` enum + `IDamageable` interface – ready for rock-paper-scissors extensions
- Tiny‐health variant for props & projectiles

### Camera & UX
- Cinemachine shoulder cam with roll-free orbit; smooth blends on mode switch
- **Lock-on virtual camera** (TargetGroup + Position/Rotation composers) keeps player foregrounded
- In-world **Billboard UI** health bars

### Utilities & Tooling
- **TimeToggle** (pause / 0.25 × slow-mo), auto-bake NavMesh helper, patrol-point & dungeon-tile spawners
- Public-safe **sync script** with interactive new-file prompt
- Consistent, timestamped debug logs for state transitions and damage events

## Current Features In-Progress

---

## Next Phase — Character Model + Basic Animations (28 Jul → 01 Aug 2025)

| Goal | Detail |
|------|--------|
| 1: Replace capsule | Swap player prefab mesh with Bozo Modular humanoid, preserve sockets |
| 2: Core locomotion | Idle ↔ Walk/Run, jump, dodge, death wired via **Animancer** |
| 3: Light / heavy attacks | Hook RPG AnimPack clips (One‑Hand Slash A, Overhead) into existing combo system |
| 4: Hit‑react & stagger | Blend‑tree for flinch ⇄ stagger, interrupt on damage |
| 5: Enemy baseline | Convert one enemy prefab to new avatar, confirm retargeting |

_Target: playable scene with full humanoid animations by **Fri 01 Aug 2025***_

---

## 📁 Repo Structure (July 2025)

```
Assets/
├─ Combat/
│  ├─ AttackAssets/            # ScriptableObject combos (e.g. OneHandGuard*)
│  ├─ AttackPhases/            # Individual phase SOs
│  ├─ DamageSource.cs          # Base damage provider
│  ├─ DamageType.cs            # Enum & helpers
│  ├─ Projectile.cs            # Hit‑scan & physics projectiles
│  ├─ AttackPhase.cs           # Serializable phase data container
│  ├─ EquippedWeaponController.cs
│  └─ ProjectileLauncher.cs
├─ Editor/
│  ├─ WeaponPoseEditor.cs      # In‑scene pose & save
│  ├─ AttackPhaseSetterEditor.cs
│  ├─ TimeToggle.cs            # Pause / slo‑mo toggle
│  └─ WeaponPoseEditor_AutoAssign.cs
├─ Enemy/
│  ├─ EnemyCombatController.cs
│  ├─ EnemyRoamer.cs
│  ├─ PursuitState.cs · RushState.cs · RetreatState.cs · …
│  ├─ InvestigateState.asset   # New AI state ScriptableObject
│  └─ EnemyCombatBehaviorProfile.cs
├─ Player/
│  ├─ PlayerController.cs
│  ├─ PlayerCombatController.cs
│  ├─ PlayerMovementStats.asset
│  ├─ PickupPromptUI.cs
│  └─ WeaponPickup.cs
├─ Scripts/
│  ├─ Combat/                  # Mirror of Assets/Combat for test‑only logic
│  ├─ Utilities/
│  │   ├─ CameraUtil.cs
│  │   ├─ LockOnController.cs · LockOnCameraRig.cs · LockOnCameraAlign.cs
│  │   └─ CursorLockHelper.cs
│  ├─ DungeonTileSpawner.cs
│  ├─ PatrolPointSpawnerManager.cs
│  ├─ HealthComponent.cs
│  ├─ Tiny_HealthComponent.cs  # Minimal health for props
│  ├─ WeaponManager.cs
│  └─ BillboardUI.cs
└─ Instances/                  # ScriptableObject singletons (stats, states)
```
(Only key files shown.)

---

## Designed For:
- Solo and indie developers needing flexible melee combat tools
- Designers who want to author weapon motion paths without keyframe animation
- RPG, Soulslike, and action prototype builders
- Developers looking to extend from a basic AI state system using ScriptableObjects

---

## Author

Created by **LordLump** (April 2025–present)  
Follow the development of the full game on the [private Dungeon repository].

---

## License

This repository is licensed under the MIT License.  
Feel free to use, modify, and share – attribution appreciated!

---

## Changelog

### July 24 – 25, 2025  (playability‑polish milestone)
- Lock‑on camera system (VCam + target‑group) with auto‑unlock on death
- Projectile & launcher framework with cooldown, hit‑point forwarding
- Directional dodge (Left Alt) and player‑relative movement refactor
- InvestigateState + stay‑engaged timer; enemies react to distant hits
- HealthComponent expanded (OnDamaged⟨amount,type,source,hit⟩, IsAlive, OnDeath)
- NavMeshAgent rotation restored on combat exit; zero‑roll shoulder cam

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