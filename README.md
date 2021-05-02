# LeoEcsLite - Lightweight C# Entity Component System framework
Performance, zero/small memory allocations/footprint, no dependencies on any game engine - main goals of this project.

> **Important!** Don't forget to use `DEBUG` builds for development and `RELEASE` builds in production: all internal error checks / exception throwing works only in `DEBUG` builds and eleminated for performance reasons in `RELEASE`.

> **Important!** LeoEcsLite API **not tread safe** and will never be! If you need multithread-processing - you should implement it on your side as part of ecs-system.

# Table of content
* [Socials](#socials)
* [Installation](#installation)
    * [As unity module](#as-unity-module)
    * [As source](#as-source)
* [Main parts of ecs](#main-parts-of-ecs)
    * [Entity](#entity)
    * [Component](#component)
    * [System](#system)
* [Data sharing](#data-sharing)
* [Special classes](#special-classes)
    * [EcsPool](#ecspool)
    * [EcsFilter](#ecsfilter)
    * [EcsWorld](#ecsworld)
    * [EcsSystems](#ecssystems)
* [Engine integration](#engine-integration)
    * [Unity](#unity)
    * [Custom engine](#custom-engine)
* [License](#license)
* [FAQ](#faq)

# Socials
[![discord](https://img.shields.io/discord/404358247621853185.svg?label=enter%20to%20discord%20server&style=for-the-badge&logo=discord)](https://discord.gg/5GZVde6)

# Installation

## As unity module
This repository can be installed as unity module directly from git url. In this way new line should be added to `Packages/manifest.json`:
```
"com.leopotam.ecslite": "https://github.com/Leopotam/ecslite.git",
```
By default last released version will be used. If you need trunk / developing version then `develop` name of branch should be added after hash:
```
"com.leopotam.ecslite": "https://github.com/Leopotam/ecslite.git#develop",
```

## As source
If you can't / don't want to use unity modules, code can be downloaded as sources archive of required release from [Releases page](`https://github.com/Leopotam/ecslite/releases`).

# Main parts of ecs

## Entity
Сontainer for components. Implemented as `int`:
```csharp
// Creates new entity in world context.
int entity = _world.NewEntity ();

// Any entity can be destroyed. All component will be removed first, then entity will be destroyed. 
world.DelEntity (entity);
```

> **Important!** Entities can't live without components and will be killed automatically after last component removement.

## Component
Container for user data without / with small logic inside:
```csharp
struct Component1 {
    public int Id;
    public string Name;
}
```
Components can be added / requested / removed through [component pools](#ecspool).

## System
Сontainer for logic for processing filtered entities. User class should implements `IEcsInitSystem`, `IEcsDestroySystem`, `IEcsRunSystem` (or other supported) interfaces:
```csharp
class UserSystem : IEcsInitSystem, IEcsRunSystem, IEcsDestroySystem {
    public void Init (EcsSystems systems) {
        // Will be called once during EcsSystems.Init() call.
    }
    
    public void Run (EcsSystems systems) {
        // Will be called on each EcsSystems.Run() call.
    }

    public void Destroy (EcsSystems systems) {
        // Will be called once during EcsSystems.Destroy() call.
    }
}
```

# Data sharing
Instance of any custom type can be shared between all systems:
```csharp
class SharedData {
    public string PrefabsPath;
}
...
var sharedData = new SharedData { PrefabsPath = "Items/{0}" };
var systems = new EcsSystems (world, sharedData);
systems
    .Add (new TestSystem1 ())
    .Init ();
...
class TestSystem1 : IEcsInitSystem {
    public void Init(EcsSystems systems) {
        var shared = systems.GetShared<SharedData> (); 
        var prefabPath = string.Format (shared.PrefabsPath, 123);
        // prefabPath = "Items/123" here.
    } 
}
```

# Special classes

## EcsPool
Container for components, provides api for adding / requesting / removing components on entity:
```csharp
int entity = world.NewEntity ();
EcsPool<Component1> pool = world.GetPool<Component1> (); 

// Add() adds component to entity. If component already exists - exception will be raised in DEBUG.
ref Component1 c1 = ref pool.Add (entity);

// Get() returns exist component on entity. If component not exists - exception will be raised in DEBUG.
ref Component1 c1 = ref pool.Get (entity);

// Del() removes component from entity. If it was last component - entity will be removed automatically too.
pool.Del (entity);
```

> **Important!** After removing component will be pooled and can be reused later. All fields will be reset to default values automatically.

## EcsFilter
Container for keeping filtered entities with specified component list:
```csharp
class WeaponSystem : IEcsInitSystem, IEcsRunSystem {
    EcsPool<Weapon> _weapons = null;    
    EcsFilter  _filter = null;

    public void Init (EcsSystems systems) {
        var world = systems.GetWorld ();
        // We wants to cache pool for Weapon components for later use.
        _weapons = world.GetPool<Weapon>();
        
        // We wants to get entities with "Weapon" and without "Health".
        // Better to cache filter somehow.
        _filter = EcsFilter.New (world).Inc<Weapon> ().Exc<Health> ().End ();
        
        // creating test entity.
        int entity = _world.NewEntity ();
        _weapons.Add (entity);
    }

    public void Run (EcsSystems systems) {
        foreach (int entity in _filter) {
            ref Weapon weapon = ref weapons.Get (entity);
            weapon.Ammo = System.Math.Max (0, weapon.Ammo - 1);
        }
    }
}
```

> Important: Any filter supports any amount of components, include and exclude list can't intersects and should be unique.

## EcsWorld
Root level container for all entities / components, works like isolated environment.

> Important: Do not forget to call `EcsWorld.Destroy()` method when instance will not be used anymore.

## EcsSystems
Group of systems to process `EcsWorld` instance:
```csharp
class Startup : MonoBehaviour {
    EcsWorld _world;
    EcsSystems _systems;

    void Start () {
        // create ecs environment.
        _world = new EcsWorld ();
        _systems = new EcsSystems (_world)
            .Add (new WeaponSystem ());
        _systems.Init ();
    }
    
    void Update () {
        // process all dependent systems.
        _systems?.Run ();
    }

    void OnDestroy () {
        // destroy systems logical group.
        if (_systems != null) {
            _systems.Destroy ();
            _systems = null;
        }
        // destroy world.
        if (_world != null) {
            _world.Destroy ();
            _world = null;
        }
    }
}
```

> Important: Do not forget to call `EcsSystems.Destroy()` method when instance will not be used anymore.

# Engine integration

## Unity
> Tested on unity 2020.3 (not dependent on it) and contains assembly definition for compiling to separate assembly file for performance reason.

Not ready yet.

## Custom engine
> C#7.3 or above required for this framework.

Code example - each part should be integrated in proper place of engine execution flow.
```csharp
using Leopotam.EcsLite;

class EcsStartup {
    EcsWorld _world;
    EcsSystems _systems;

    // Initialization of ecs world and systems.
    void Init () {        
        _world = new EcsWorld ();
        _systems = new EcsSystems (_world);
        _systems
            // register additional worlds here.
            // .AddWorld (customWorldInstance, "events")
            // register your systems here, for example:
            // .Add (new TestSystem1 ())
            // .Add (new TestSystem2 ())
            
            // register components for removing here
            // position in registration is important,
            // should be after all AddWorld() registration, for example:
            // .DelHere<TestComponent1> ()
            // .DelHere<TestComponent2> ("events")
            
            .Init ();
    }

    // Engine update loop.
    void UpdateLoop () {
        _systems?.Run ();
    }

    // Cleanup.
    void Destroy () {
        if (_systems != null) {
            _systems.Destroy ();
            _systems = null;
        }
        if (_world != null) {
            _world.Destroy ();
            _world = null;
        }
    }
}
```

# License
The software released under the terms of the [MIT license](./LICENSE.md).

No personal support or any guarantees.

# FAQ

### I want to process one system at MonoBehaviour.Update() and another - at MonoBehaviour.FixedUpdate(). How I can do it?

For splitting systems by `MonoBehaviour`-method multiple `EcsSystems` logical groups should be used:
```csharp
EcsSystems _update;
EcsSystems _fixedUpdate;

void Start () {
    var world = new EcsWorld ();
    _update = new EcsSystems (world).Add (new UpdateSystem ());
    _update.Init ();
    _fixedUpdate = new EcsSystems (world).Add (new FixedUpdateSystem ());
    _fixedUpdate.Init ();
}

void Update () {
    _update.Run ();
}

void FixedUpdate () {
    _fixedUpdate.Run ();
}
```

### I copy&paste my reset components code again and again. How I can do it in other manner?

If you want to simplify your code and keep reset/init code at one place, you can setup custom handler to process cleanup / initialization for component:
```csharp
struct MyComponent : IEcsAutoReset<MyComponent> {
    public int Id;
    public object LinkToAnotherComponent;

    public void AutoReset (ref MyComponent c) {
        c.Id = 2;
        c.LinkToAnotherComponent = null;
    }
}
```
This method will be automatically called for brand new component instance and after component removing from entity and before recycling to component pool.
> Important: With custom `AutoReset` behaviour there are no any additional checks for reference-type fields, you should provide correct cleanup/init behaviour without possible memory leaks.

### I use components as events that works only one frame, then remove it at last system in execution sequence. It's boring, how I can automate it?

If you want to remove one-frame components without additional custom code, you can register them at `EcsSystems`:
```csharp
struct MyOneFrameComponent { }

EcsSystems _update;

void Start () {
    var world = new EcsWorld ();
    _update = new EcsSystems (world);
    _update
        .Add (new CalculateSystem ())
        .Add (new UpdateSystem ())
        .DelHere<MyOneFrameComponent> ()
        .Init ();
}

void Update () {
    _update.Run ();
}
```

> important: All one-frame components should be registered with `DelHere()` after all worlds registration through `AddWorld()`.
> Important: All one-frame components with specified type will be removed at position in execution flow where this component was registered with `DelHere()` call.

### I want to keep references to entities in components, but entity can be killed at any system and I need protection from reuse same ID. How I can do it?

For keeping entity somewhere you should pack it to special `EcsPackedEntity` or `EcsPackedEntityWithWorld` types:
```csharp
EcsWorld world = new EcsWorld ();
int entity = world.NewEntity ();
EcsPackedEntity packed = world.PackEntity (entity);
EcsPackedEntityWithWorld packedWithWorld = world.PackEntityWithWorld (entity);
...
if (packed.Unpack (world, out int unpacked)) {
    // unpacked is valid and can be used.
}
if (packedWithWorld.Unpack (out EcsWorld unpackedWorld, out int unpackedWithWorld)) {
    // unpackedWithWorld is valid and can be used.
}
```
