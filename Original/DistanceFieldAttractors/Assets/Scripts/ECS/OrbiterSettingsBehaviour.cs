using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS
{

    public class OrbiterSettingsBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float attraction = 0.003f;
        public float speedStretch = 0.55f;
        public float jitter = 0.001f;
        public Color surfaceColor = new Color(82f / 255, 62f / 255, 161f / 255, 1);
        public Color interiorColor = new Color(231f / 255, 117f / 255, 117f / 255, 1);
        public Color exteriorColor = new Color(133f / 255, 215f / 255, 242f / 255, 1);
        public float exteriorColorDist = 5f;
        public float interiorColorDist = 1.5f;
        public float colorStiffness = 4;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var gravityConfigData = new OrbiterSimmulationParams
            {
                attraction = attraction,
                speedStretch = speedStretch,
                jitter = jitter,
                surfaceColor = new float4(surfaceColor.r, surfaceColor.g, surfaceColor.b, surfaceColor.a),
                interiorColor = new float4(interiorColor.r, interiorColor.g, interiorColor.b, interiorColor.a),
                exteriorColor = new float4(exteriorColor.r, exteriorColor.g, exteriorColor.b, exteriorColor.a),
                exteriorColorDist = exteriorColorDist,
                interiorColorDist = interiorColorDist,
                colorStiffness = colorStiffness
            };
            var e = dstManager.CreateEntity();
            dstManager.AddComponentData(e, gravityConfigData);
        }


        void Update()
        {
            World.Active.EntityManager.CreateEntityQuery(typeof(OrbiterSimmulationParams)).SetSingleton(
                new OrbiterSimmulationParams
                {
                    attraction = attraction,
                    speedStretch = speedStretch,
                    jitter = jitter,
                    surfaceColor = new float4(surfaceColor.r, surfaceColor.g, surfaceColor.b, surfaceColor.a),
                    interiorColor = new float4(interiorColor.r, interiorColor.g, interiorColor.b, interiorColor.a),
                    exteriorColor = new float4(exteriorColor.r, exteriorColor.g, exteriorColor.b, exteriorColor.a),
                    exteriorColorDist = exteriorColorDist,
                    interiorColorDist = interiorColorDist,
                    colorStiffness = colorStiffness
                });
        }
    }

    public struct OrbiterSimmulationParams : IComponentData
    {
        public float attraction;
        public float speedStretch;
        public float jitter;
        public float4 surfaceColor;
        public float4 interiorColor;
        public float4 exteriorColor;
        public float exteriorColorDist;
        public float interiorColorDist;
        public float colorStiffness;
        // Model
    }
}