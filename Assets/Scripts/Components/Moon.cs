using Unity.Entities;
using Galaxy;

public struct Moon : IComponentData
{
    public Entity PlanetEntity;

    public float CummulativeBuildSpeed;
    public float BuildProgress;
    public Entity BuiltPrefab;

    public Team PreviousTeam;
}

public struct BuildingReference : IComponentData
{
    public Entity BuildingEntity;
}