using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable once InconsistentNaming
[RequiresEntityConversion]
public class SpawnerBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public Mesh mesh;
    public Material material;
    public int particleCount;

    // The MonoBehaviour data is converted to ComponentData on the entity.
    // We are specifically transforming from a good editor representation of the data (Represented in degrees)
    // To a good runtime representation (Represented in radians)
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new Spawner
        {
            //mesh = mesh,
            //material = material,
            particleCount = particleCount
        };
        
        dstManager.AddSharedComponentData(entity, data);
    }
}

