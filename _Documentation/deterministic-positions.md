# Deterministic Positions Overview
Enable the feature in the `Assets/Resources/Config.prefab` asset, and set `UseFixedSimulationDeltaTime` to true.

## How Determinism works in Galaxy Sample
- Fixed timestep with no catch-up cap via a `RateManager` set on SimulationSystemGroup.
- Deterministic random using `Unity.Mathematics.Random` with seeds derived from stable identifiers (e.g., Entity.Index) and a single World managing random usage.
- Burst on all simulation involved systems/jobs with `FloatPrecision.High` and `FloatMode.Deterministic`.
- Multithreaded logic structured to avoid race conditions and order-dependent writes; ensure predictable results regardless of worker count.
- Avoid unsafe collection patterns unless all dependencies are explicitly completed at sync points.

## Fixed Frame Rate and RateManager
The simulation uses a fixed time step of `0.0333333` seconds on SimulationSystemGroup.
Example setup in SimulationRateSystem:

```csharp
public partial class SimulationRateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var simulationSystemGroup = World.GetExistingSystemManaged<SimulationSystemGroup>();
        if (simRate.UseFixedRate)
        {
            simulationSystemGroup.RateManager =
                new RateUtils.FixedRateSimpleManager(simRate.FixedTimeStep); // 0.0333333f
        }
        else
        {
            simulationSystemGroup.RateManager = null;
        }
        simRate.Update = false;
    }
}
```
> Important:
> Use a fixed update count and delta. Do not rely on variable deltaTime.
> No cap on catch-up updates; if the game falls behind, consider failure modes rather than loosening determinism.

## Deterministic Random
- Use `Unity.Mathematics.Random` for all randomness.
- Seed generation should be deterministic (e.g., based on `Entity.Index` and a global seed).
- Test the sequence of `Random.Next()` calls is stable and occurs in a predictable order.
- Centralize random usage within the Default World; avoid multiple Worlds with random state.

## Burst Compilation Settings
Apply deterministic Burst settings to all systems that affects the simulation.

### Entry point rule:

The outermost `Burst-compiled` method dictates the Burst settings for all methods it calls.
Example:

```csharp
[BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
public partial struct HighPrecisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        A(ref state);
    }

    // Even if B is annotated differently, calls from OnUpdate inherit High+Deterministic
    [BurstCompile(FloatPrecision.Low)]
    static void B(ref SystemState state) { /* ... */ }
}
```


> Notes:
> - `OnUpdate(ref SystemState)` uses generated static wrappers as Burst entry points.
> - Galaxy sample sets all decision-making to run under Burst Deterministic code.

## Execution Order
Galaxy runs with a fixed, predefined by default order within ECS groups. If customization is needed, use ordering attributes:

```csharp
[UpdateInGroup(typeof(BuildSpatialDatabaseGroup))]
[UpdateAfter(typeof(PlanetSystem))]
[UpdateBefore(typeof(LateSimulationSystemGroup))]
public partial struct SomeSystem : ISystem
{
    public void OnUpdate(ref SystemState state) { /* ... */ }
}
```
> Guidelines:
> - Stabilize execution order on a single platform first.
> - Use consistent seeds and ordering before cross-platform validation.

## Determinism in Multithreaded Spatial Database
### Design
- World divided into uniform grid cells.
- SpatialDatabase contains:
    - Component data
    - UnsafeList<SpatialDatabaseCell> with start/capacity info
    - UnsafeList<SpatialDatabaseElement> representing concatenated sub-lists per cell

- DB is rebuilt each frame:
    - ClearSpatialDatabaseSystem
- BuildSpatialDatabasesSystem adds ships/buildings to cells

### Constraint:
- Cell element lists cannot grow during build. Overflows are recorded to resize capacity next frame.

## Parallel Build Strategy
Single parallel IJobEntity iterates ships; multiple threads write to shared buffers:

- Use `NativeDisableParallelForRestriction` to allow parallel writes.
- Use `Interlocked.Increment` on a per-cell element counter to reserve a unique slot.
- Insert element at index (elementsCount - 1).
- After insertion, schedule a sort per cell by `Entity.Index` to normalize order.


### Capacity Overflows and Invalid Cells
#### **Issue:**

If capacity is exceeded, the last writes are dropped. Different thread write orders can produce different “kept” elements before sorting, breaking determinism.

#### **Fix:**

If a cell’s required count exceeds capacity, mark the cell invalid for queries for that frame.
Next frame, resize capacity as recorded, rebuild DB, and restore validity.

#### **Rule:**

Only query a cell if all intended elements were added this frame.

## Alternative Strategy
Per-thread private collections, then deterministic merge:

- Each thread writes to its own cell-local list.
- Merge lists in a single-thread or deterministically ordered pass. Pros:
- Avoids atomic increments and shared contention. Cons:
- Higher memory footprint and a merge cost.
- Performance tradeoffs vs the Interlocked + sort approach are context-dependent.