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
world.DelEntity(entity);
```

> **Important!** Entities without components on them will be automatically removed on last `EcsEntity.Del()` call.

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
    // auto-injected fields: EcsWorld instance and EcsFilter.
    EcsWorld _world = null;
    
    EcsFilter<Weapon>.Exclude<Health> _filter = null;

    public void Init (EcsSystems systems) {
        // We wants to get entities with "Weapon" and without "Health".
        // Better to cache filter somehow.
        _filter = EcsFilter.New (systems.GetWorld ()).Inc<Weapon> ().Exc<Health> ().End ();
        
        // creating test entity.
        _world.NewEntity ().Get<Weapon> ();
    }

    public void Run (EcsSystems systems) {
        // Better to cache pool somehow, or just not request it inside loop. 
        var weapons = systems.GetWorld ().GetPool<Weapon> ();
        
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
        _systems.Run ();
    }

    void OnDestroy () {
        // destroy systems logical group.
        _systems.Destroy ();
        // destroy world.
        _world.Destroy ();
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
            // .AddWorld (customWorldInstance)
            // register your systems here, for example:
            // .Add (new TestSystem1 ())
            // .Add (new TestSystem2 ())
            
            // register components for removing here
            // (position in registration is important), for example:
            // .KillHere<TestComponent1> ()
            // .KillHere<TestComponent2> ()
            
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
        .KillHere<MyOneFrameComponent> ()
        .Init ();
}

void Update () {
    _update.Run ();
}
```

> Important: All one-frame components with specified type will be removed at position in execution flow where this component was registered with KillHere() call.
