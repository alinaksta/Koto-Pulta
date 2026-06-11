# Contribution Guidelines

This document explains how the team should use Git, GitHub, and Unity when working on **Koto Pulta**.

For folder layout and asset placement rules, see [Project Structure](STRUCTURE.md).

---

## Contents

* [1. Branches](#1-branches)
* [2. Starting Work](#2-starting-work)
* [3. Saving Work](#3-saving-work)
* [4. Keeping Your Branch Updated](#4-keeping-your-branch-updated)
* [5. Pull Requests](#5-pull-requests)
* [6. Unity Workflow Rules](#6-unity-workflow-rules)
* [7. Naming Prefixes](#7-naming-prefixes)
* [8. Team Rules](#8-team-rules)

---

## 1. Branches

This repository uses two long-term branches:

| Branch | Purpose                                                        |
| ------ | -------------------------------------------------------------- |
| `main` | Stable builds, milestone versions, presentations, and releases |
| `dev`  | Active development branch where completed work is combined     |

Normal workflow:

```text
task branch → dev → main
```

Do not push directly to `main`.

Usually, do not push directly to `dev`. Use a pull request instead.

Use task-based branch names:

```text
feature/player-movement
bugfix/menu-crash
art/player-sprites
design/game-settings
docs/update-readme
```

Recommended prefixes:

| Prefix     | Use for                             |
| ---------- | ----------------------------------- |
| `feature/` | New gameplay features or systems    |
| `bugfix/`  | Bug fixes                           |
| `art/`     | Art assets                          |
| `design/`  | Levels, balance, dialogue, settings |
| `docs/`    | Documentation                       |

Avoid vague names:

```text
my-branch
changes
stuff
test
```

---

## 2. Starting Work

Always start from the latest `dev`.

```bash
# Switch to the development branch
git checkout dev

# Download the latest changes from GitHub
git pull origin dev

# Create and switch to your own task branch
git checkout -b <your-branch-name>
```

Examples:

```bash
git checkout -b art/player-sprites
git checkout -b feature/player-movement
git checkout -b design/game-settings
```

---

## 3. Saving Work

Check changed files:

```bash
git status
```

Add changes:

```bash
git add .
```

Create a commit with a short message:

```bash
git commit -m "Add player sprites"
```

Or use:

```bash
git commit
```

This opens a text editor where you can write a longer commit message.

Suggested format:

```text
Short summary of the change

Optional details:
- What changed
- Why it changed
- Anything the team should know
```

Push your branch:

```bash
git push origin <your-branch-name>
```

Example:

```bash
git push origin art/player-sprites
```

---

## 4. Keeping Your Branch Updated

If other work has been merged into `dev`, update your branch before opening a pull request.

First, commit your current work.

Then run:

```bash
# Update local dev
git checkout dev
git pull origin dev

# Return to your branch
git checkout <your-branch-name>

# Merge latest dev into your branch
git merge dev
```

Example:

```bash
git checkout dev
git pull origin dev
git checkout art/player-sprites
git merge dev
```

If Git reports a conflict, ask for help before continuing.

---

## 5. Pull Requests

After pushing your branch:

1. Open the repository on GitHub.
2. Click **Compare & pull request**.
3. Make sure the target branch is `dev`.
4. Write a short description of your changes.
5. Create the pull request.

The pull request should merge:

```text
your branch → dev
```

After review and testing, the pull request can be merged into `dev`.

---

## 6. Unity Workflow Rules

Unity scene and prefab files can cause merge conflicts.

Follow these rules:

* Avoid multiple people editing the same scene at the same time.
* Use Prefabs instead of making many direct scene changes.
* For testing, duplicate the scene and place it in `Assets/_Project/Scenes/Sandbox/`.
* Do not delete `.meta` files manually.
* Move and rename assets inside Unity when possible.
* Do not rename, move, or delete another team member's files without checking first.
* Ask for help if Unity scene, prefab, or `.meta` conflicts appear.

Developer placeholder art should go in:

```text
Assets/_Project/Art/Dev/
```

Final art assets should go in the correct `Art/Characters`, `Art/Environment`, `Art/UI`, or `Art/Shared` folder.

---

## 7. Naming Prefixes

Use these prefixes for Unity assets:

| Prefix | Asset type                 | Example                  |
| ------ | -------------------------- | ------------------------ |
| `PF_`  | Prefab                     | `PF_Player.prefab`       |
| `M_`   | Material                   | `M_Player.mat`           |
| `T_`   | Texture                    | `T_Player_BaseColor.png` |
| `SO_`  | ScriptableObject asset     | `SO_PlayerStats.asset`   |
| `AC_`  | Animator Controller        | `AC_Player.controller`   |
| `AN_`  | Animation Clip             | `AN_Player_Run.anim`     |
| `SG_`  | Shader Graph               | `SG_ToonLit.shadergraph` |
| `SH_`  | Shader                     | `SH_CustomWater.shader`  |
| `VFX_` | Visual effect prefab/asset | `VFX_HitSpark.prefab`    |

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

## 8. Team Rules

* Pull latest `dev` before starting work.
* Create a branch for each task.
* Use clear commit messages.
* Open pull requests into `dev`.
* Do not push directly to `main`.
* Avoid pushing directly to `dev`.
* Keep changes focused on one task when possible.
* Test your changes before opening a pull request.
* Do not use `git push --force` unless the team agrees.

Common workflow:

```bash
git checkout dev
git pull origin dev
git checkout -b feature/example-task

# Make changes

git status
git add .
git commit
git push origin feature/example-task
```

Then create a pull request on GitHub:

```text
feature/example-task → dev
```
