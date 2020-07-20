using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class Manager : Singleton<Manager>
{
    public GameObject prefab;

    EntityManager entityManager;
    Entity ball;

    BlobAssetStore blob;

    private void Awake()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blob = new BlobAssetStore();

        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blob);
        ball = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);

        //blob.Dispose();
    }

    private void Update()
    {
        for(int i = 0; i < 10; i++)
        {
            Entity copy = entityManager.Instantiate(ball);
            entityManager.SetComponentData(copy, new Translation { Value = new float3(UnityEngine.Random.Range(-10f, 10f), 10f, UnityEngine.Random.Range(-10f, 10f)) });
        }
    }

    private void OnDestroy()
    {
        blob.Dispose();
    }
}
