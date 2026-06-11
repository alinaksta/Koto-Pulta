# Project Structure

This document describes the recommended Unity folder structure for **Koto Pulta**.

The goal is to keep files organized, reduce merge conflicts, and make it clear where programmers, artists, and designers should place their work.

---

## Contents

* [1. Main Project Folder](#1-main-project-folder)
* [2. Recommended Folder Structure](#2-recommended-folder-structure)
* [3. General Rule](#3-general-rule)
* [4. Art Folder](#4-art-folder)

  * [Characters](#characters)
  * [Environment](#environment)
  * [UI](#ui)
  * [Developer Placeholder Art](#developer-placeholder-art)
  * [Shared Art Assets](#shared-art-assets)
* [5. Models, Textures, and Materials](#5-models-textures-and-materials)
* [6. Sprites](#6-sprites)
* [7. Animations](#7-animations)
* [8. Shaders and VFX](#8-shaders-and-vfx)
* [9. Audio](#9-audio)
* [10. Data](#10-data)
* [11. Prefabs](#11-prefabs)
* [12. Scenes](#12-scenes)
* [13. Scripts](#13-scripts)
* [14. Settings](#14-settings)
* [15. Naming Prefixes](#15-naming-prefixes)
* [16. Summary](#16-summary)

---

## 1. Main Project Folder

All project-specific assets should be placed inside:

```text
Assets/_Project/
```

Unity packages, Asset Store imports, and third-party tools may create their own folders inside `Assets/`. Keeping our files inside `_Project` makes the project easier to navigate.

---

## 2. Recommended Folder Structure (Approximation)

```text
Assets/
  _Project/
    Art/
      Characters/
        Player/
          Models/
          Textures/
          Sprites/
          Materials/
          Animations/
          Prefabs/
        Enemies/
          EnemyName/
            Models/
            Textures/
            Sprites/
            Materials/
            Animations/
            Prefabs/

      Environment/
        Props/
          PropName/
            Models/
            Textures/
            Sprites/
            Materials/
            Prefabs/
        Tiles/
        Backgrounds/

      UI/
        Sprites/
        Icons/
        Fonts/
        Materials/

      Dev/
        Models/
        Textures/
        Sprites/
        Materials/
        Prefabs/

      Shared/
        Materials/
        Textures/
        Sprites/
        Shaders/
          ShaderGraphs/
          Includes/
        AnimationClips/
        VFX/

    Audio/
      Music/
      SFX/
      Ambience/

    Data/
      Player/
      Balance/
      Dialogue/

    Prefabs/
      Gameplay/
      Systems/
      UI/

    Scenes/
      MainMenu/
      Level01/
      Sandbox/

    Scripts/
      Runtime/
      Editor/

    Settings/
```

This structure can be expanded as the project grows.

---

## 3. General Rule

Use these rules when deciding where to put files:

```text
If an asset belongs to one object, keep it with that object.
If many objects use it, put it in Shared.
If it is temporary programmer-made placeholder art, put it in Art/Dev.
Sprites and Textures are treated as separate asset categories.
```

Examples:

| Asset                           | Location                                  |
| ------------------------------- | ----------------------------------------- |
| Barrel texture                  | `Art/Environment/Props/Barrel/Textures/`  |
| Barrel material                 | `Art/Environment/Props/Barrel/Materials/` |
| Barrel sprite                   | `Art/Environment/Props/Barrel/Sprites/`   |
| Generic wood material           | `Art/Shared/Materials/`                   |
| Shared UI sprite                | `Art/Shared/Sprites/`                     |
| Player run animation            | `Art/Characters/Player/Animations/`       |
| Reusable humanoid run animation | `Art/Shared/AnimationClips/`              |
| Toon shader graph               | `Art/Shared/Shaders/ShaderGraphs/`        |
| Programmer placeholder sprite   | `Art/Dev/Sprites/`                        |

---

## 4. Art Folder

The `Art/` folder contains visual assets such as models, textures, sprites, materials, animations, UI art, shaders, and visual effects.

### Characters

Use `Art/Characters/` for the player, enemies, NPCs, and character-related assets.

Example:

```text
Art/
  Characters/
    Player/
      Models/
      Textures/
      Sprites/
      Materials/
      Animations/
      Prefabs/
```

Character-specific models, textures, sprites, materials, animations, and prefabs should stay inside the character's folder.

### Environment

Use `Art/Environment/` for props, tiles, backgrounds, and level art.

Example:

```text
Art/
  Environment/
    Props/
      Barrel/
        Models/
        Textures/
        Sprites/
        Materials/
        Prefabs/
    Tiles/
    Backgrounds/
```

For object-specific assets, create a folder for the object and keep its related files together.

### UI

Use `Art/UI/` for visual UI assets.

```text
Art/
  UI/
    Sprites/
    Icons/
    Fonts/
    Materials/
```

Examples:

* Button sprites
* Icons
* UI backgrounds
* Fonts
* UI-specific materials

### Developer Placeholder Art

Use `Art/Dev/` for temporary art made by programmers or designers during development.

```text
Art/
  Dev/
    Models/
    Textures/
    Sprites/
    Materials/
    Prefabs/
```

Use this folder for placeholder assets only.

Examples:

```text
SPR_Dev_PlayerPlaceholder.png
SPR_Dev_EnemyPlaceholder.png
T_Dev_TestPattern.png
M_Dev_TestRed.mat
PF_Dev_TargetDummy.prefab
```

When final art is ready, replace or move the asset to the correct `Characters`, `Environment`, `UI`, or `Shared` folder.

### Shared Art Assets

Use `Art/Shared/` for assets intentionally reused by multiple characters, props, scenes, or systems.

```text
Art/
  Shared/
    Materials/
    Textures/
    Sprites/
    Shaders/
      ShaderGraphs/
      Includes/
    AnimationClips/
    VFX/
```

Only place assets here if they are intentionally shared.

---

## 5. Models, Textures, and Materials

For object-specific assets, keep the model, textures, and materials close together.

Recommended:

```text
Art/
  Environment/
    Props/
      Barrel/
        Models/
          Barrel.fbx
        Textures/
          T_Barrel_BaseColor.png
          T_Barrel_Normal.png
        Materials/
          M_Barrel.mat
        Prefabs/
          PF_Barrel.prefab
```

Avoid placing every model, texture, and material into large global folders unless they are shared.

Less recommended:

```text
Art/
  Models/
  Textures/
  Materials/
```

This becomes harder to manage as the project grows because files related to one object are spread across multiple folders.

Shared materials and shared textures belong here:

```text
Art/Shared/Materials/
Art/Shared/Textures/
```

Do not edit a shared material unless you know which objects use it.

---

## 6. Sprites

Sprites are treated separately from Textures.

Even though Unity imports sprites from image files, sprites usually have a different purpose from model textures.

Use Sprites for:

```text
UI icons
HUD elements
Portraits
Inventory icons
2D gameplay graphics
Sprite atlases
Placeholder 2D graphics
```

Use Textures for:

```text
Model materials
Base color maps
Normal maps
Roughness maps
Metallic maps
Terrain textures
Shader inputs
```

Character-specific sprites should stay with the character.

```text
Art/
  Characters/
    Player/
      Sprites/
        SPR_PlayerPortrait.png
        SPR_PlayerIcon.png
```

Environment-specific sprites should stay with the prop, tile, or background they belong to.

```text
Art/
  Environment/
    Props/
      Barrel/
        Sprites/
          SPR_BarrelIcon.png
```

UI sprites should go in `Art/UI/Sprites/`.

```text
Art/
  UI/
    Sprites/
      SPR_HealthBar.png
      SPR_InventorySword.png
      SPR_ButtonPlay.png
```

Shared sprites should go in `Art/Shared/Sprites/`.

```text
Art/
  Shared/
    Sprites/
      SPR_GenericInteractionIcon.png
      SPR_CommonButtonFrame.png
```

Developer placeholder sprites should go in `Art/Dev/Sprites/`.

```text
Art/
  Dev/
    Sprites/
      SPR_Dev_PlayerPlaceholder.png
      SPR_Dev_EnemyPlaceholder.png
```

Sprite atlases should also use the `SPR_` prefix.

```text
SPR_UI_Icons.png
SPR_PlayerAtlas.png
SPR_ItemsAtlas.png
```

---

## 7. Animations

Use local animation folders for animations that belong to one character or object.

```text
Art/Characters/Player/Animations/
Art/Characters/Enemies/Slime/Animations/
```

Example:

```text
Art/
  Characters/
    Player/
      Animations/
        AC_Player.controller
        AN_Player_Idle.anim
        AN_Player_Run.anim
        AN_Player_Jump.anim
```

Use shared animation folders only for reusable animation clips.

```text
Art/Shared/AnimationClips/
```

Animator Controllers should usually stay with the character or object they control.

---

## 8. Shaders and VFX

Put shaders and shader-related files in:

```text
Art/Shared/Shaders/
```

Recommended structure:

```text
Art/
  Shared/
    Shaders/
      ShaderGraphs/
        SG_ToonLit.shadergraph
        SG_Water.shadergraph
      Includes/
        CommonLighting.hlsl
      SH_CustomWater.shader
```

Use `ShaderGraphs/` for Shader Graph assets.

Use `Includes/` for shared HLSL include files.

Use `Art/Shared/Materials/` for materials that use these shaders.

Put reusable visual effects in:

```text
Art/Shared/VFX/
```

Examples:

```text
VFX_HitSpark.prefab
VFX_DustPoof.prefab
VFX_Explosion.prefab
```

---

## 9. Audio

Use `Audio/` for music, sound effects, and ambience.

```text
Audio/
  Music/
  SFX/
  Ambience/
```

| Folder      | Use for                         |
| ----------- | ------------------------------- |
| `Music/`    | Background music and themes     |
| `SFX/`      | Gameplay sound effects          |
| `Ambience/` | Environmental background sounds |

---

## 10. Data

Use `Data/` for non-code game data.

```text
Data/
  ScriptableObjects/
  Balance/
  Dialogue/
```

| Folder               | Use for                         |
| -------------------- | ------------------------------- |
| `Balance/`           | Gameplay tuning values          |
| `Dialogue/`          | Dialogue files or dialogue data |

Script code should not go in `Data/`. Scripts belong in `Scripts/`.

---

## 11. Prefabs

There are two acceptable prefab locations.

Use local prefab folders for art-object prefabs:

```text
Art/Environment/Props/Barrel/Prefabs/PF_Barrel.prefab
Art/Characters/Player/Prefabs/PF_Player.prefab
```

This keeps the prefab close to the model, textures, sprites, and materials it uses.

Use `_Project/Prefabs/` for gameplay, system, and UI prefabs:

```text
Prefabs/
  Gameplay/
  Systems/
  UI/
```

Examples:

```text
Prefabs/Gameplay/PF_PlayerSpawner.prefab
Prefabs/Systems/PF_GameManager.prefab
Prefabs/UI/PF_PauseMenu.prefab
```

---

## 12. Scenes

Use `Scenes/` for Unity scene files.

Each important scene should have its own folder.

```text
Scenes/
  MainMenu/
    LV_MainMenu.unity
  Level01/
    LV_Level01.unity
  Sandbox/
```

Scene folders may also contain scene-specific lighting data or related scene files.

Use `Scenes/Sandbox/` for testing scenes.

```text
Scenes/
  Sandbox/
    Isako/
      LV_CombatTest_Isako.unity
    ArtistName/
      LV_LightingTest_ArtistName.unity
    DesignerName/
      LV_EnemyBalanceTest_DesignerName.unity
```

If you need to test changes in a scene, create a copy instead of modifying a shared production scene directly.

---

## 13. Scripts

Use `Scripts/` for C# code.

```text
Scripts/
  Runtime/
  Editor/
```

Use `Runtime/` for normal gameplay code.

Use `Editor/` only for Unity editor scripts.

Do not place non-code data assets inside `Scripts/`.

Do not prefix script file names.

Good:

```text
PlayerController.cs
EnemyHealth.cs
```

Avoid:

```text
SCR_PlayerController.cs
CS_EnemyHealth.cs
```

---

## 14. Settings

Use `Settings/` for project-specific configuration assets.

Examples:

```text
Input settings
Render settings
Game configuration assets
Custom project settings
```

Do not move Unity's built-in `ProjectSettings/` folder into `_Project`. This section is only for additional project-specific assets created by the team.

---

## 15. Naming Prefixes

Use these prefixes for Unity assets:

| Prefix | Asset type                 | Example                  |
| ------ | -------------------------- | ------------------------ |
| `PF_`  | Prefab                     | `PF_Player.prefab`       |
| `M_`   | Material                   | `M_Player.mat`           |
| `T_`   | Texture                    | `T_Player_BaseColor.png` |
| `SPR_` | Sprite                     | `SPR_HealthBar.png`      |
| `SO_`  | ScriptableObject asset     | `SO_PlayerStats.asset`   |
| `AC_`  | Animator Controller        | `AC_Player.controller`   |
| `AN_`  | Animation Clip             | `AN_Player_Run.anim`     |
| `SG_`  | Shader Graph               | `SG_ToonLit.shadergraph` |
| `SH_`  | Shader                     | `SH_CustomWater.shader`  |
| `VFX_` | Visual effect prefab/asset | `VFX_HitSpark.prefab`    |
| `LV_`  | Scene                      | `LV_MainMenu.unity`      |

Use clear names that describe the asset's purpose.

For temporary developer art, include `Dev` in the name:

```text
SPR_Dev_PlayerPlaceholder.png
SPR_Dev_EnemyPlaceholder.png
T_Dev_TestPattern.png
M_Dev_TestRed.mat
PF_Dev_TargetDummy.prefab
```

Do not prefix C# scripts.

Good:

```text
PlayerController.cs
EnemyHealth.cs
```

Avoid:

```text
SCR_PlayerController.cs
CS_EnemyHealth.cs
```

---

## 16. Summary

Main rules:

```text
Object-specific files stay with the object.
Shared files go in Art/Shared.
Developer placeholder art goes in Art/Dev.
Sprites and Textures are treated as separate asset categories.
Code goes in Scripts.
Data assets go in Data.
Scenes go in their own scene folders.
Temporary test scenes go in Scenes/Sandbox.
```

This structure should help the team avoid confusion, reduce accidental edits, and make project files easier to find.
