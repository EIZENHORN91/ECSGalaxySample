using Unity.Entities;
using UnityEngine;
using Galaxy;

public class ShipTurretAuthoring : MonoBehaviour
{
    public TurretDataScriptableObject TurretData;
    
    class Baker : Baker<ShipTurretAuthoring>
    {
        public override void Bake(ShipTurretAuthoring authoring)
        {
            Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Turret
            {
                TurretData = authoring.TurretData == null ? default : BlobAuthoringUtility.BakeToBlob(this, authoring.TurretData, authoring.TurretData),
            });
            AddComponent(entity, new Team
            {
                Index = -1,
            });
        }
    }
}
