using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(BuildingAuthoring))]
public class FactoryAuthoring : MonoBehaviour
{
    public FactoryDataObject FactoryData;
    
    class Baker : Baker<FactoryAuthoring>
    {
        public override void Bake(FactoryAuthoring authoring)
        {
            Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Factory
            {
                FactoryData = authoring.FactoryData.BakeToBlob(this),
            });
        }
    }
}
