using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

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
        public int particleCount  = 50000;
        private DistanceFieldModel model;
        private float switchTimer = 0;
        private int modelCount;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var gravityConfigData = new OrbiterSimmulationParams
            {
                particleCount = particleCount,
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

        private void Start()
        {
            modelCount = System.Enum.GetValues(typeof(DistanceFieldModel)).Length;
        }


        void Update()
        {

            switchTimer += Time.deltaTime * .1f;
            if (switchTimer > 1f)
            {
                switchTimer -= 1f;
                int newModel = Random.Range(0, modelCount - 1);
                if (newModel >= (int) model)
                {
                    newModel++;
                }

                model = (DistanceFieldModel) newModel;
            }
            
            World.Active.EntityManager.CreateEntityQuery(typeof(OrbiterSimmulationParams)).SetSingleton(
                new OrbiterSimmulationParams
                {
                    particleCount = particleCount,
                    attraction = attraction,
                    speedStretch = speedStretch,
                    jitter = jitter,
                    surfaceColor = new float4(surfaceColor.r, surfaceColor.g, surfaceColor.b, surfaceColor.a),
                    interiorColor = new float4(interiorColor.r, interiorColor.g, interiorColor.b, interiorColor.a),
                    exteriorColor = new float4(exteriorColor.r, exteriorColor.g, exteriorColor.b, exteriorColor.a),
                    exteriorColorDist = exteriorColorDist,
                    interiorColorDist = interiorColorDist,
                    colorStiffness = colorStiffness,
                    model = model
                });
        }

    }
    
    public struct OrbiterSimmulationParams : IComponentData
    {
        public int particleCount;
        public float attraction;
        public float speedStretch;
        public float jitter;
        public float4 surfaceColor;
        public float4 interiorColor;
        public float4 exteriorColor;
        public float exteriorColorDist;
        public float interiorColorDist;
        public float colorStiffness;
        public DistanceFieldModel model;
    }
}