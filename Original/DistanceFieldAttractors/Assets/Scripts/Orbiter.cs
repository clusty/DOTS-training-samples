using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct Orbiter {
	public float3 position;
	public float3 velocity;
	public float4 color;

	public Orbiter(float3 pos) {
		position = pos;
		velocity=Vector3.zero;
		color = new float4(0,0,0,1);
	}
}
