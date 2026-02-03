using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Galaxy;
using Unity.Rendering;

public class CustomLODAuthoring : MonoBehaviour
{
    [System.Serializable]
    public struct CustomLODAuthoringData
    {
        public MeshFilter MeshFilter;
        public float Distance;
    }

    public List<CustomLODAuthoringData> LODs = new List<CustomLODAuthoringData>();
    
    class Baker : Baker<CustomLODAuthoring>
    {
        public override void Bake(CustomLODAuthoring authoring)
        {
            Entity entity = GetEntity(authoring, TransformUsageFlags.None);
            
            AddComponent(entity, new InitializeLOD());
            DynamicBuffer<CustomLOD> buffer = AddBuffer<CustomLOD>(entity);
            for (int i = 0; i < authoring.LODs.Count; i++)
            {
                CustomLODAuthoringData element = authoring.LODs[i];
                
                DependsOn(element.MeshFilter);
                
                Entity lodEntity = Entity.Null;
                if (element.MeshFilter != null)
                {
                    lodEntity = GetEntity(element.MeshFilter.gameObject, TransformUsageFlags.None);
                }
                
                buffer.Add(new CustomLOD
                {
                    LODEntity = lodEntity,
                    DistanceSq = element.Distance * element.Distance,
                    MeshIndex = 0,
                });
            }
        }
    }
}
