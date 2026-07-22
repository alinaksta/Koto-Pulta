# Project Documentation

This document reflects current runtime code under `Assets/_Project/Scripts/Runtime` and the local item framework under `Assets/Itemworks`.

---

## Contents

* [High-Level Overview](#high-level-overview)
* [Important Runtime Areas](#important-runtime-areas)
* [Bootstrap Flow](#bootstrap-flow)

  * [What happens on startup](#what-happens-on-startup)
  * [Important notes](#important-notes)
* [Services](#services)

  * [How services work](#how-services-work)
  * [Current bootstrapped services](#current-bootstrapped-services)
  * [Practical rule](#practical-rule)
* [Progression and Shift Flow](#progression-and-shift-flow)

  * [Core runtime roles](#core-runtime-roles)
  * [Current shift flow](#current-shift-flow)
* [Main Menu and Loading Flow](#main-menu-and-loading-flow)

  * [Main menu runtime pieces](#main-menu-runtime-pieces)
  * [Scene transition flow](#scene-transition-flow)
* [Input Flow](#input-flow)

  * [How it works](#how-it-works)
  * [Current consumers](#current-consumers)
* [Player Movement and Camera](#player-movement-and-camera)

  * [Movement](#movement)
  * [Camera](#camera)
* [Interaction System](#interaction-system)

  * [Core idea](#core-idea)
  * [Interaction flow](#interaction-flow)
  * [Current behavior details](#current-behavior-details)
  * [Container-based world interaction](#container-based-world-interaction)
  * [Implementing a new interactable](#implementing-a-new-interactable)
* [Focus System](#focus-system)

  * [How focus works](#how-focus-works)
  * [Current computer flow](#current-computer-flow)
  * [Important side effects](#important-side-effects)
* [Hands](#hands)

  * [Runtime model](#runtime-model)
  * [What hands do](#what-hands-do)
  * [Current throw behavior](#current-throw-behavior)
  * [Hand visuals](#hand-visuals)
* [Containers and Item Transfer](#containers-and-item-transfer)

  * [Core idea](#core-idea-1)
  * [Transfer flow](#transfer-flow)
  * [Container interfaces](#container-interfaces)
  * [Current container implementations](#current-container-implementations)
  * [Current important rule](#current-important-rule)
  * [Using containers in new code](#using-containers-in-new-code)
* [Itemworks](#itemworks)

  * [Main Itemworks types](#main-itemworks-types)
  * [Unity authoring flow](#unity-authoring-flow)
  * [How items are used in-game](#how-items-are-used-in-game)
  * [Creating a new item](#creating-a-new-item)
  * [Adding custom item behavior](#adding-custom-item-behavior)
* [Waiters, Customers, and Tables](#waiters-customers-and-tables)

  * [Customer flow](#customer-flow)
  * [Table flow](#table-flow)
  * [Waiter flow](#waiter-flow)
  * [Waiter as both character and item](#waiter-as-both-character-and-item)
  * [Carrying items with waiters](#carrying-items-with-waiters)
  * [Current service behavior](#current-service-behavior)
* [How to Extend the Current Project](#how-to-extend-the-current-project)

  * [Add a new service](#add-a-new-service)
  * [Add a new interactable object](#add-a-new-interactable-object)
  * [Add a new focusable interaction](#add-a-new-focusable-interaction)
  * [Add a new item type](#add-a-new-item-type)
  * [Add a new item-holding system](#add-a-new-item-holding-system)
* [Current Quirks and Limitations](#current-quirks-and-limitations)
* [Suggested Mental Model](#suggested-mental-model)

---

## High-Level Overview

The project is built around a few simple runtime ideas:

- Global services are created during startup and resolved through `ServiceLocator`.
- A persistent `ServiceRoot` survives scene loads and now also owns shared UI such as the loading screen.
- Player input is polled every frame and routed into movement, camera, and interaction systems.
- Interactions are driven by `DualHandInteractor`, which treats both hands as item containers.
- Items use the local `Itemworks` framework: definitions are authored as assets, registered at startup, and optionally instantiated with runtime components.
- Most gameplay is expressed as item transfers between containers.
- Focus interactions, such as computers, temporarily switch the camera into a dedicated focused mode.
- Progression is organized around runtime services (`RunSessionService`, `BalanceService`, `GameModeService`) plus game-mode implementations such as `ShiftService`.
- Main menu flow is now a first-class runtime path with its own loading screen service and scene transition scripts.

## Important Runtime Areas

- Bootstrap and service setup: `Assets/_Project/Scripts/Runtime/Bootstrap`
- Global service locator: `Assets/_Project/Scripts/Runtime/Services/ServiceLocator.cs`
- Input: `Assets/_Project/Scripts/Runtime/Input`
- Player movement/camera/hands: `Assets/_Project/Scripts/Runtime/Player`
- Interactions: `Assets/_Project/Scripts/Runtime/Interaction`
- Items and containers: `Assets/_Project/Scripts/Runtime/Items`
- Waiters/customers/tables: `Assets/_Project/Scripts/Runtime/Characters`
- Progression and shift systems: `Assets/_Project/Scripts/Runtime/Progression`
- UI and main menu flow: `Assets/_Project/Scripts/Runtime/UI`
- Itemworks framework: `Assets/Itemworks`

## Bootstrap Flow

Startup begins in `Assets/_Project/Scripts/Runtime/Bootstrap/Bootstraper.cs`.

### What happens on startup

1. Unity calls the static bootstrap method before scene load.
2. The code checks whether an `IBootstrapService` is already registered.
3. If not, it loads `Resources/ServiceRoot`.
4. It instantiates the prefab as `[Services]`.
5. The instance is marked `DontDestroyOnLoad`.
6. The bootstrapper calls `Bootstrap()` on every `IBootstrapable` found on:
   - the root object itself
   - each immediate child of the root

### Important notes

- Bootstrap currently only scans the root and one child level. It does not recursively walk the full hierarchy.
- If a required service is missing from `ServiceRoot`, systems that call `ServiceLocator.Get<T>()` during `Awake()` will fail immediately.

## Services

Global services are registered in `Assets/_Project/Scripts/Runtime/Services/ServiceLocator.cs`.

### How services work

- Services are stored globally in a dictionary keyed by type.
- A service registers itself by calling `ServiceLocator.Register<T>(this)` during bootstrap.
- Consumers resolve services with either:
  - `ServiceLocator.Get<T>()` when the service must exist
  - `ServiceLocator.TryGet<T>(out var service)` when the dependency is optional

### Current bootstrapped services

The current runtime relies on these bootstrapable systems:

- `BootstrapService`: registers the root bootstrap service
- `InputService`: exposes gameplay input through `IInputService`
- `PhysicsItemService`: spawns pooled physics items into the world
- `CustomerService`: tracks tables and customer spawning
- `WaiterService`: tracks waiter registration and assignments
- `WaiterQueueService`: tracks meal points and the service counter origin used by waiters
- `BalanceService`: stores runtime money/progression balance
- `RunSessionService`: tracks whether the current run is idle, running, succeeded, or failed
- `GameModeService`: owns the active runtime game mode and ticks it every frame
- `ShiftService`: implements the current `shift` game mode and shift progression loop
- `LoadingService`: exposes the persistent curtain-style loading screen
- `ItemRegistryBootstrap`: loads item definitions into `ItemRegistry`

### Practical rule

If you add a new global gameplay system, it should usually:

1. implement `IBootstrapable`
2. live on the `ServiceRoot` prefab or one of its immediate children
3. register itself inside `Bootstrap()`

## Progression and Shift Flow

Progression is no longer just a couple of counters. It is a service-driven layer that sits on top of the customer/waiter loop.

### Core runtime roles

- `RunSessionService`: owns the high-level run state (`None`, `Running`, `Succeeded`, `Failed`)
- `BalanceService`: stores current money and raises change events
- `GameModeService`: owns the currently active runtime mode, builds a `GameModeContext`, and ticks the active mode every frame
- `ShiftService`: the current gameplay mode implementation with id `shift`
- `ShiftStatisticsCollector`: aggregates served/unsatisfied customer counts, money earned, and average delivery time for the active shift
- `ComputerShiftView`: presents shift start, in-progress, and end-of-shift statistics on the computer UI

### Current shift flow

At runtime, the current progression loop works like this:

1. `GameModeService` activates `ShiftService`
2. `ShiftService.Enter(...)` resets mode-local state, hooks balance/customer events, installs a shift-aware random order giver, and starts the run session
3. `TryStartNextShift()` starts the next configured shift, resets revenue tracking, and starts the shift timer/spawn timer
4. While a shift is active, `ShiftService.Tick(...)`:
   - decreases the remaining shift timer
   - spawns customers on a repeating delay
5. Served customers increase balance through `FoodProperty.UnitPrice`, and shift revenue is derived from balance deltas
6. When the timer expires, `ShiftService` first forces all still-waiting customers to time out, then resolves shift success/failure and computes final statistics
7. `OnShiftEnded` exposes the finished statistics to UI such as `ComputerShiftView`

Important current detail:

- `ShiftService` installs a `ShiftRandomItemDefinitionGiver`, so customer orders are now filtered by the current shift index instead of using a hardcoded item id.

## Main Menu and Loading Flow

The project now has a dedicated main menu scene plus a persistent loading screen that survives scene changes.

### Main menu runtime pieces

- `LV_MainMenu` is the authored main menu scene
- `LoadingService` lives on `ServiceRoot`, not in the menu scene, so it survives scene transitions
- `LoadingScreenUI` animates the curtain-style loading screen and exposes awaitable show/hide transitions
- `PlayButton` starts a scene transition into gameplay
- `ExitButton` quits the application
- `UIVerticalLoopMotion`, `UIHorizontalLoopMotion`, and `UIContinuousSpin` are lightweight reusable UI motion scripts used by menu presentation

### Scene transition flow

The current scene loading path is:

1. `PlayButton.EnterNextScene()` awaits `LoadingService.StartLoadingAsync()`
2. the curtain closes before the actual scene activation begins
3. `SceneManager.LoadSceneAsync(..., LoadSceneMode.Single)` loads the target scene
4. `allowSceneActivation` is held until the async operation reaches `0.9`
5. after activation and a small delay, `LoadingService.StopLoadingAsync()` reopens the curtain

Important current limitation:

- scene activation still happens on Unity's main thread, so a heavy gameplay scene can still briefly freeze the spinning loading icon even though the loading service itself persists correctly across scenes.

## Input Flow

Input is centralized in `Assets/_Project/Scripts/Runtime/Input/InputService.cs`.

### How it works

- `InputService` creates and enables the generated `InputActions` asset.
- It exposes input through `IInputService`.
- Button-like actions use the `ButtonState` struct with `Pressed`, `Held`, and `Released`.

### Current consumers

- `PlayerController` reads movement and jump.
- `CameraController` reads mouse delta.
- `DualHandInteractor` reads left/right interaction, left/right drop, and cancel.

This project currently uses polling, not event-driven input.

## Player Movement and Camera

### Movement

`Assets/_Project/Scripts/Runtime/Player/Movement/PlayerController.cs` drives player locomotion.

- Uses a `CharacterController`
- Reads directional input from `IInputService`
- Uses `MovementMath` for acceleration, friction, and jump-force calculations
- Uses an `IOrientation` source to convert input into world-space movement

The orientation source is usually the camera, but any `IOrientation` implementation can be used.

### Camera

`Assets/_Project/Scripts/Runtime/Player/Camera/CameraController.cs` is responsible for:

- yaw/pitch look rotation
- smoothed mouse input
- cursor locking
- focus transitions
- exposing orientation data through `IOrientation`

This means the camera is both a look controller and part of the gameplay interaction system.

## Interaction System

The interaction system is defined primarily by:

- `Assets/_Project/Scripts/Runtime/Interaction/IInteractable.cs`
- `Assets/_Project/Scripts/Runtime/Interaction/InteractionContext.cs`
- `Assets/_Project/Scripts/Runtime/Player/Interaction/DualHandInteractor.cs`
- `Assets/_Project/Scripts/Runtime/Player/Interaction/Hand.cs`

### Core idea

Interactions are initiated by the player, but they are always contextualized by:

- player head position and forward
- current raycast hit
- active hand/container
- current focus handler
- current dual-hand interactor

That data is passed around inside `InteractionContext`.

### Interaction flow

When the player presses interact on either hand:

1. `DualHandInteractor` creates an `InteractionContext`.
2. It asks the active `Hand` whether the held item wants to consume the interaction first.
3. If not consumed, it raycasts forward.
4. If something is hit:
   - the hand receives `OnInteractionStarted(...)`
   - if the hit object implements `IInteractable` and `CanInteract(...)` returns true, the object also receives `OnInteractionStarted(...)`

### Current behavior details

- Held and released input are forwarded to the hand, but world interactables are not currently tracked through a full started/held/stopped lifecycle.
- In practice, the system currently uses `OnInteractionStarted(...)` as the main world interaction entry point.

### Container-based world interaction

The most important current interaction rule is that the hand handles container exchange itself.

When the raycast hits something, `Hand.OnInteractionStarted(...)` checks whether the hit object is:

- an `IContainer`
- or an `IContainerHolder`

If a container is found:

- empty hand -> try to pull the item into the hand
- full hand -> try to push the held item into the target container

This means many world objects do not need custom interaction logic at all if exposing a container is enough.

### Implementing a new interactable

To create a new interactable object:

1. implement `IInteractable`
2. decide whether it should validate the current hand/context in `CanInteract(...)`
3. place the logic in `OnInteractionStarted(...)`

Use `InteractionContext.ActiveHand` whenever the interaction depends on the player's current item.

## Focus System

The focus system is used for interactions that temporarily take over the camera, such as computers.

Main files:

- `Assets/_Project/Scripts/Runtime/Interaction/IFocusable.cs`
- `Assets/_Project/Scripts/Runtime/Player/Camera/IFocusHandler.cs`
- `Assets/_Project/Scripts/Runtime/Player/Camera/CameraController.cs`
- `Assets/_Project/Scripts/Runtime/Interaction/ComputerInteractable.cs`
- `Assets/_Project/Scripts/Runtime/Player/Camera/FocusTarget.cs`

### How focus works

- A focusable object exposes a `FocusTarget` plus enter/exit transition durations.
- `CameraController` acts as the runtime focus handler.
- When a focus interaction begins, the camera enters one of three states:
  - `Unfocused`
  - `InTransition`
  - `Focused`
- During focus, the camera blends toward the target transform and FOV.
- While focused, `OnFocusHeld(delta)` is called every frame on the focus target.
- Cancel input exits focus.

### Current computer flow

`ComputerInteractable` works like this:

1. player interacts with the computer
2. computer stores the current interactor and focus handler
3. computer asks the focus handler to begin focus
4. camera unlocks the cursor while focused
5. cancel exits focus and restores locked mouse behavior

Current important computer-specific UI pieces built on top of that flow:

- `SiteActivator` now acts as a tab controller, not a random site picker
- tabs are represented by the `ComputerSiteTab` enum (`ShiftStatistics`, `Shop`, `Website`)
- the active tab is restored when the player reopens the computer
- all tabs are hidden when focus ends
- `ComputerController` handles item distribution from shop-style computer screens
- `ComputerShiftView` handles shift start/end presentation on the progression computer

### Important side effects

- While focused, `DualHandInteractor` hides both hand visuals.
- Focus currently blocks normal world interaction until the player exits focus.

## Hands

Hands are not just UI. They are gameplay containers.

Main files:

- `Assets/_Project/Scripts/Runtime/Player/Interaction/Hand.cs`
- `Assets/_Project/Scripts/Runtime/Player/Interaction/DualHandInteractor.cs`
- `Assets/_Project/Scripts/Runtime/Player/Interaction/HandsView.cs`
- `Assets/_Project/Scripts/Runtime/Player/Interaction/HandsSway.cs`

### Runtime model

- The player has two `Hand` instances: left and right.
- Each hand implements `IContainer`.
- A hand can hold exactly one `Item`.
- Each hand can be shown/hidden independently.

### What hands do

- hold items
- participate in transfers
- handle hold-to-throw behavior
- drop items into the world
- act as the `ActiveHand` during interactions

### Current throw behavior

When the player holds and releases interaction with an item in hand:

- the hand accumulates hold time
- if the item has `ThrowableProperty` and the hold time is long enough, releasing throws the item

### Hand visuals

`HandsView` is presentation only. It reacts to hand state and displays:

- held item sprite
- hand sprite overrides
- waiter carried-item sprite
- waiter table number

`HandsSway` offsets the hand UI based on camera angular velocity.

## Containers and Item Transfer

Containers are one of the most important concepts in the project.

Main files:

- `Assets/_Project/Scripts/Runtime/Items/Containers/IContainer.cs`
- `Assets/_Project/Scripts/Runtime/Items/ItemTransferUtility.cs`
- `Assets/_Project/Scripts/Runtime/Items/TransferRequest.cs`

### Core idea

A container is anything that can hold one item and participate in transfers.

The project currently models many systems this way:

- player hands
- item slots
- shelf storage
- waiter carry slots
- world physics items

### Transfer flow

All transfers go through `ItemTransferUtility.TryTransfer(source, destination)`.

The utility:

1. validates both containers
2. prevents transferring to the same container
3. checks whether the source is empty
4. creates a `TransferRequest`
5. asks the source `CanRemove(...)`
6. asks the destination `CanInsert(...)`
7. removes the item from source and inserts it into destination

### Container interfaces

- `IContainer`: actual storage + transfer rules
- `IContainerHolder`: wrapper for components that expose a container indirectly

### Current container implementations

- `ItemContainer`: basic single-slot container
- `Hand`: player-held single-slot container
- `PhysicsContainer`: pooled world object that holds an item in the scene
- `WaiterContainer.CarryContainer`: waiter meal/item slot with custom rules
- shelf storage via `ShelveContainer` + internal `ItemContainer`

### Current important rule

`WaiterContainer` rejects items that contain `WaiterComponent`, so waiters cannot carry other waiters.

### Using containers in new code

If a new system should hold or exchange items, first ask whether it should simply be an `IContainer`.

Usually you only need to implement:

- `Item`
- `IsEmpty`
- `CanInsert(...)`
- `CanRemove(...)`
- `Insert(...)`
- `Remove()`
- `OnItemChanged`

If you do this, the system can immediately participate in the existing transfer pipeline.

## Itemworks

`Assets/Itemworks` is the local item-definition framework used by the game.

It separates item data into two layers:

- immutable item definitions
- optional runtime item instances

### Main Itemworks types

#### `ItemRegistry`

Global registry of item definitions by string id.

- startup registration happens in `ItemRegistryBootstrap`
- game code looks up definitions by id through `ItemRegistry.Instance`

#### `ItemDefinition`

Immutable definition of an item.

Contains:

- `Id`
- a list of `ItemProperty` values

Properties are queried by exact runtime type.

#### `ItemProperty`

Static data stored on the definition.

Current project properties include:

- `FoodProperty`
- `HandSpriteProperty`
- `ThrowableProperty`
- `WaiterProperty`
- `ShiftProperty`

#### `ItemInstance`

Optional runtime state for a specific item.

An instance can have runtime `ItemComponent`s attached to it.

#### `ItemComponent`

Runtime-only state attached to an `ItemInstance`.

Current important example:

- `WaiterComponent`, which stores a reference to the actual `Waiter`

#### `IItemInitializer`

Hook that lets a property modify a new runtime instance when it is created.

Current important example:

- `WaiterProperty` creates a `WaiterComponent` during instance creation

### Unity authoring flow

Runtime definitions are authored as Unity assets:

- `ItemDefinitionAsset`: one item definition asset
- `ItemListAsset`: list of assets to register at startup
- `ItemDefinitionAssetSerializer`: converts authoring assets into runtime `ItemDefinition`s

### How items are used in-game

The gameplay layer uses `Assets/_Project/Scripts/Runtime/Items/Item.cs`.

`Item` is a lightweight wrapper around:

- a definition-only item
- or a runtime `ItemInstance`

This distinction matters:

- many normal items are definition-only
- special runtime-backed items, like waiters, use `ItemInstance`

### Creating a new item

Typical flow:

1. create a new `ItemDefinitionAsset`
2. set a valid lowercase id
3. add the needed properties
4. include the asset in `ItemListAsset`
5. make sure the `ItemRegistryBootstrap` service bootstraps before anything needs the item

### Adding custom item behavior

There are two main options:

- add a new `ItemProperty` for static data
- add a new `ItemComponent` plus an `IItemInitializer` property when the item needs runtime behavior/state

Use a property when the behavior is just data.

Use an instance + component when the item needs a live runtime reference or mutable state.

## Waiters, Customers, and Tables

These systems are tightly connected and are worth understanding together.

### Customer flow

`CustomerService`:

- tracks registered tables
- tracks free tables
- spawns customers
- chooses an order
- subscribes to customer outcome events
- asks `WaiterService` to assign a free waiter when possible
- can query the next seated customer who still needs a waiter
- can force all active waiting customers to time out at shift end

### Table flow

`Table`:

- stores seat transforms
- tracks current seated customers
- exposes seat availability
- raises add/remove/free events

### Waiter flow

`WaiterService`:

- tracks registered waiters
- assigns customers to unassigned waiters
- keeps waiter/customer lookup maps
- immediately reassigns newly freed waiters to already seated unassigned customers when possible

`WaiterQueueService`:

- tracks waiter meal points
- tracks the service counter origin used by waiter routing
- provides free meal points to waiters

### Waiter as both character and item

`Waiter` is a key pattern in this codebase.

It is simultaneously:

- a world character
- an interactable object
- an item definition/instance bridge

On startup, a waiter:

1. reads its waiter item definition id
2. looks the definition up in `ItemRegistry`
3. creates an `ItemInstance`
4. expects `WaiterProperty` to add `WaiterComponent`
5. stores a self-item in a private container

When the player picks a waiter up:

- the waiter transfers its self-item into the player's hand
- the world GameObject hides itself

When the waiter is dropped or thrown:

- the self-item is moved back into the waiter
- the world object reappears
- the waiter enters ragdoll/recovery flow

### Carrying items with waiters

Waiters have their own separate carry container for meals or other items.

That means the waiter gameplay model has two different item concepts:

- the waiter-as-item itself
- the item currently carried by the waiter

### Current service behavior

The current waiter service flow is roughly:

1. customer is spawned
2. free waiter is assigned if one is available
3. waiter goes to the customer
4. waiter waits through an ask duration
5. waiter transitions into meal-point/service-counter logic
6. once carrying an item, waiter tries to deliver it to the assigned table
7. when a waiter is freed by delivery, timeout, or wrong-item flow, the service immediately tries to rematch that waiter to the next waiting customer

## How to Extend the Current Project

### Add a new service

- create a `MonoBehaviour` implementing `IBootstrapable`
- register it in `Bootstrap()`
- place it on `ServiceRoot` or one of its immediate children
- resolve it elsewhere via `ServiceLocator`

### Add a new interactable object

- implement `IInteractable`
- read `InteractionContext.ActiveHand` if the interaction depends on the player's held item
- place most logic in `OnInteractionStarted(...)`

### Add a new focusable interaction

- implement both `IInteractable` and `IFocusable`
- provide a `FocusTarget`
- start focus through the current `IFocusHandler`
- use `OnFocusStarted`, `OnFocusHeld`, and `OnFocusEnded` for lifecycle behavior

### Add a new item type

- author an `ItemDefinitionAsset`
- give it properties
- add it to `ItemListAsset`
- use `Item.FromId(id)` or `ItemRegistry.Instance.Get(id)` at runtime

### Add a new item-holding system

- implement `IContainer` if the system should hold or exchange items
- or use `IContainerHolder` if the system uses already existing container
- use `ItemTransferUtility.TryTransfer(...)` instead of manually moving items between objects
- hands usually transfer objects automatically between containers. If you don't need custom transfer logic, don't use `TryTransfer`

## Current Quirks and Limitations

These are important when modifying the current codebase:

- Bootstrap only walks the `ServiceRoot` root and one child level.
- Missing services often fail hard because many systems call `ServiceLocator.Get<T>()` in `Awake()`.
- Item property lookup is by exact type only.
- `ItemPropertyRegistry` exists but is not currently part of the main runtime flow.
- `ItemDefinitionAssetSerializer.Serialize(...)` is not a well-tested runtime path and should be treated carefully.
- World interactables mainly receive `OnInteractionStarted(...)`; held/stop routing is not fully implemented yet.
- `Hand.TryStartInteractionWithItemInHand(...)` is still effectively a placeholder.
- `CustomerService` depends on a valid `IRandomItemDefinitionGiver` being installed before spawning customers.
- Heavy scene activation still freezes the loading icon briefly because Unity scene activation blocks the main thread.
- `SiteActivator` keeps a compatibility fallback for its legacy `sites` list while the newer enum-tab binding system is phased in.
- Some debug and prototype systems exist alongside production code, so always confirm whether a class is gameplay-critical before extending it.

## Suggested Mental Model

When reading or extending this project, this mental model works well:

- Startup creates global systems.
- Services expose the shared runtime state.
- The player interacts through two hands.
- Hands are containers.
- World gameplay is mostly item transfer between containers.
- Some objects can temporarily take over the camera through focus.
- Itemworks defines what items are, while gameplay code defines what those items do in the scene.
