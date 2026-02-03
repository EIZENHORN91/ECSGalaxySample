using Unity.Entities;
using UnityEngine;

namespace Galaxy
{
    public class UIInputPresenter : MonoBehaviour
    {
        private void OnEnable()
        {
            UIEvents.IgnoreCameraInput += IgnoreCameraInput;
        }

        private void OnDisable()
        {
            UIEvents.IgnoreCameraInput -= IgnoreCameraInput;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIEvents.TogglePause?.Invoke();
            }
        }

        private void IgnoreCameraInput(bool value)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (GameUtilities.TryGetSingletonRW(entityManager, out RefRW<GameCamera> config))
            {
                config.ValueRW.IgnoreInput = value;
            }
        }
    }
}