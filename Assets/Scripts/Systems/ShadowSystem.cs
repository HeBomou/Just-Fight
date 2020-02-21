using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace JustFight {

    [UpdateInGroup (typeof (TransformSystemGroup))]
    class ShadowSystem : JobComponentSystem {

        [BurstCompile]
        struct ShadowMoveJob : IJobChunk {
            [ReadOnly] public ComponentDataFromEntity<Translation> translationFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Rotation> rotationFromEntity;
            [ReadOnly] public ArchetypeChunkComponentType<Shadow> shadowType;
            public ArchetypeChunkComponentType<LocalToWorld> localToWorldType;
            public void Execute (ArchetypeChunk chunk, int chunkIndex, int entityOffset) {
                var chunkShadow = chunk.GetNativeArray (shadowType);
                var chunkLocalToWorld = chunk.GetNativeArray (localToWorldType);
                for (int i = 0; i < chunk.Count; i++) {
                    var translation = translationFromEntity[chunkShadow[i].translationEntity].Value;
                    var rotation = rotationFromEntity[chunkShadow[i].rotationEntity].Value;
                    chunkLocalToWorld[i] = new LocalToWorld { Value = math.mul (float4x4.Translate (chunkShadow[i].offset), new float4x4 (rotation, translation)) };
                }
            }
        }

        [BurstCompile]
        struct ShadowShootJob : IJobForEachWithEntity<ShadowTurret, GunBullet, GunState, LocalToWorld> {
            public EntityCommandBuffer.Concurrent ecb;
            [ReadOnly] public ComponentDataFromEntity<TankTurretTeam> tankTurretTeamFromEntity;
            [ReadOnly] public ComponentDataFromEntity<ShootInput> shootInputFromEntity;
            [ReadOnly] public float dT;
            public void Execute (Entity entity, int entityInQueryIndex, [ReadOnly] ref ShadowTurret shadowTurretCmpt, [ReadOnly] ref GunBullet bulletCmpt, ref GunState gunStateCmpt, [ReadOnly] ref LocalToWorld localToWorldCmpt) {
                var shootInputCmpt = shootInputFromEntity[shadowTurretCmpt.turretEntity];
                if (gunStateCmpt.recoveryLeftTime < 0) {
                    if (shootInputCmpt.isShoot) {
                        gunStateCmpt.recoveryLeftTime += gunStateCmpt.recoveryTime;
                        var teamId = tankTurretTeamFromEntity[shadowTurretCmpt.turretEntity].id;
                        var bulletEntity = ecb.Instantiate (entityInQueryIndex, bulletCmpt.bulletPrefab);
                        ecb.SetComponent (entityInQueryIndex, bulletEntity, new Rotation { Value = quaternion.LookRotation (shootInputCmpt.dir, math.up ()) });
                        ecb.SetComponent (entityInQueryIndex, bulletEntity, new Translation { Value = localToWorldCmpt.Position + localToWorldCmpt.Forward * 1.7f });
                        ecb.SetComponent (entityInQueryIndex, bulletEntity, new PhysicsVelocity { Linear = shootInputCmpt.dir * bulletCmpt.bulletShootSpeed });
                        ecb.SetComponent (entityInQueryIndex, bulletEntity, new BulletTeam { id = teamId });
                    }
                } else gunStateCmpt.recoveryLeftTime -= dT;
            }
        }

        private EntityQuery group;
        private BeginInitializationEntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate () {
            group = GetEntityQuery (new EntityQueryDesc {
                All = new ComponentType[] {
                    ComponentType.ReadOnly<Shadow> (),
                        typeof (LocalToWorld)
                }
            });
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem> ();
        }

        protected override JobHandle OnUpdate (Unity.Jobs.JobHandle inputDeps) {
            var moveJobHandle = new ShadowMoveJob {
                translationFromEntity = GetComponentDataFromEntity<Translation> (true),
                    rotationFromEntity = GetComponentDataFromEntity<Rotation> (true),
                    shadowType = GetArchetypeChunkComponentType<Shadow> (true),
                    localToWorldType = GetArchetypeChunkComponentType<LocalToWorld> ()
            }.Schedule (group, inputDeps);
            var shadowShootJobHandle = new ShadowShootJob {
                ecb = entityCommandBufferSystem.CreateCommandBuffer ().ToConcurrent (),
                    tankTurretTeamFromEntity = GetComponentDataFromEntity<TankTurretTeam> (),
                    shootInputFromEntity = GetComponentDataFromEntity<ShootInput> (),
                    dT = Time.DeltaTime
            }.Schedule (this, moveJobHandle);
            return shadowShootJobHandle;
        }
    }
}