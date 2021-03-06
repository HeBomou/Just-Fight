using Unity.Entities;
using UnityEngine;

namespace JustFight.Bullet {

    class WaveBulletAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
        public void Convert (Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
            dstManager.AddComponentData (entity, new WaveBulletState { recoveryTime = 0.4f });
        }
    }
}