using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace JustFight.Skill {

    class ShadowAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
        public void Convert (Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
            dstManager.AddComponent<Shadow> (entity);
            dstManager.RemoveComponent<Translation> (entity);
            dstManager.RemoveComponent<Rotation> (entity);
        }
    }
}