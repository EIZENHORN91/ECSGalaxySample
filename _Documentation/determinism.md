# Determinism Overview

This sample supports deterministic simulation, where the outcome of the simulation will be identical every time it is run, even across different platforms.

In order to configure the sample for determinism, the following parameters need to be set in the in-game "Settings" menu or in `Assets/Resources/Config.prefab` asset before running the simulation:
* `UseFixedSimulationDeltaTime` must be ENABLED
* `UseNonDeterministicRandomSeed` must be DISABLED

## Determinism considerations

While many things could affect determinism, these are some of the main considerations to take into account in order to make determinism possible:

### Fixed timestep simulation
Everything that ends up affecting the game's simulation in any way must update at a fixed timestep. In this sample, this is done in the `SimulationRateSystem`, where we assign a `RateUtils.FixedRateCatchUpManager` to our `SimulationSystemGroup`'s `RateManager`.

If the framerate is not fixed, the frame deltaTimes will vary from one frame to another and from one simulation to another, which will change the outcome of all code that relies on deltaTime at any point.

### Deterministic randoms
All randoms must be deterministic. For this, we use `Unity.Mathematics.Random` for all of our randoms. This random is already deterministic as long as we guarantee that all the seeds are always the same, and all the `random.NextFloat()` are called in the same order/frequency.

### Deterministic burst compilaiton
All code that affects simulation and that deals with float values must be compiled with `[BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]`. This ensures floating point determinism even across platforms.

### Deterministic Entity creation
Since there is code in the game simulation that depends on Entity indexes (for random seeds, for sorting, etc...), we must ensure that entities will always be created deterministically in the same order. In order to make this problem simple, we wipe out and recreate the ECS world whenever we restart simulation.

### No race conditions
In a multithreaded context, you must ensure that the outcomes of the simulation will always be the same no matter how many threads there are, and no matter which threads finish first. When using the job system without disabling any safeties and without using more advanced concepts like thread index, this is ensured by default. However, if you choose to disable safeties (as is the case in this sample with the `[NativeDisableParallelForRestriction]` in `TeamAIJob` for example), you must make sure that the results will always be the same regradless of thread count, speed, or order.

The `BuildSpatialDatabasesSystem` demonstrates a more advanced example of ensuring determinism despite disabled safeties. This system tries to build a spatial database in parallel. It first iterates each ship entity, calculates what cell index it belongs to in the world grid, and adds the ship entity to a list of entities for that cell. We want this to happen in parallel, so we are highly likely to have multiple threads attempting to write ship entities to the same cell's list of entities. The way we ensure that this process remains deterministic, despite the disabled safeties in `CachedSpatialDatabaseUnsafeParallel`, is like this:
* In the `BuildSpatialDatabaseParallelJob`, when adding ship entities to the cell's list, we do it using `SpatialDatabase.AddToDataBaseParallel`. This method atomically increments a write index in the cell's list using `Interlocked.Increment`. This guarantees that each thread "reserves" their write index in the cell's list of entities, so no two threads can attempt to write at the same index in the list.
* At this point, we've ensured that all ships will be added to their corresponding cell without overwriting eachother due to race conditions, but we haven't yet ensured that the order of ships in the cell lists is the same every time. The fact that this order could change depending on thread counts and thread speeds makes this still non-deterministic.
* In order to make this deterministic, a `SortSpatialDatabaseCellElementsParallelJob` will sort the entities of each cell list by ascending order of their `Entity.Index`. With this, no matter in what order the entities were inserted into the cell's list of entities, their order will always be the same after the sorting is complete. This ensures determinism of ship query results for the game's AI.