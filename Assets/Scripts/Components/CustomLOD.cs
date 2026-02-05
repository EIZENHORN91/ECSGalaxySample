using Unity.Entities;
using UnityEngine;

public struct InitializeLOD : IComponentData, IEnableableComponent
{ }

[InternalBufferCapacity(0)]
public struct CustomLOD : IBufferElementData
{
    public Entity LODEntity;
    public float DistanceSq;

    public int MeshIndex;
}
