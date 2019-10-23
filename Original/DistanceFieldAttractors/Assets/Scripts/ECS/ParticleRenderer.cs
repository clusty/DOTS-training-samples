using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS
{
    
    [DisableAutoCreation]
    class RenderSystem : ComponentSystem
    {
	    public Mesh particleMesh;
	    public Material particleMaterial;
        const int instancesPerBatch = 1023;
        private Matrix4x4[] matricesM ;
        private Vector4[] colorsM ;
        private MaterialPropertyBlock matProps;
        private int ColorID = Shader.PropertyToID("_Color");

        protected override void OnCreate()
        {
            matricesM = new Matrix4x4[instancesPerBatch];
            colorsM = new Vector4[instancesPerBatch];
            matProps = new MaterialPropertyBlock();
        }

        protected override unsafe void  OnUpdate()
        {
            var blah = World.Active.EntityManager.CreateEntityQuery(typeof(LocalToWorld),typeof(ColorData)); // readonly
            var chunkArray = blah.CreateArchetypeChunkArray(Allocator.TempJob);
            var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>();
            var colorType = GetArchetypeChunkComponentType<ColorData>();
            for (int i = 0; i < chunkArray.Length; ++i)
            {
                var chunk = chunkArray[i];
                var localToWorlds = chunk.GetNativeArray(localToWorldType);
                var colors = chunk.GetNativeArray(colorType);
                for (int j = 0; j < localToWorlds.Length; j+= instancesPerBatch)
                {
	                var count = Math.Min(instancesPerBatch, localToWorlds.Length);
	                var src = localToWorlds.GetUnsafeReadOnlyPtr()  ;
	                fixed (void* dst = matricesM)
	                {
		                UnsafeUtility.MemCpy(dst,src,count * UnsafeUtility.SizeOf<Matrix4x4>());
	                }

	                src = colors.GetUnsafeReadOnlyPtr()  ;
	                fixed (void* dst = colorsM)
	                {
		                UnsafeUtility.MemCpy(dst,src,count * UnsafeUtility.SizeOf<float4>());
	                }
	                
					matProps.SetVectorArray(ColorID, colorsM);
					Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, matricesM, count, matProps);
				}
            }
            
            chunkArray.Dispose();
        }
    }
    public class ParticleRenderer : MonoBehaviour
    {
	    public Mesh particleMesh;
	    public Material particleMaterial;
        private RenderSystem render;
        private void Start()
        {
	        render = World.Active.GetOrCreateSystem<RenderSystem>(); //new RenderSystem {particleMesh = particleMesh, particleMaterial = particleMaterial};
	        render.particleMaterial = particleMaterial;
	        render.particleMesh = particleMesh;
        }

        private void Update()
        {
	        render.Update();
        }
    }
}