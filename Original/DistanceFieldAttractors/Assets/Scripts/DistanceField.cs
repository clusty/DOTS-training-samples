using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class DistanceField:MonoBehaviour {
	public bool preview;
	[Space(10)]
	public DistanceFieldModel model;
	float switchTimer = 0f;
	int modelCount;

	public static DistanceField instance;

	public static float timeStatic;

	// Smooth-Minimum, from Media Molecule's "Dreams"
	static float SmoothMin(float a, float b, float radius) {
		float e = math.max(radius - Mathf.Abs(a - b),0);
		return math.min(a,b) - e * e * 0.25f / radius;
	}

	static float Sphere(float x, float y, float z,float radius) {
		return math.sqrt(x * x + y * y + z * z) - radius;
	}

	// what's the shortest distance from a given point to the isosurface?
	public static float GetDistance(DistanceFieldModel model,  float time,float x, float y, float z, out float3 normal) {
		float distance = float.MaxValue;
		normal = float3.zero;
		if (model == DistanceFieldModel.Metaballs) {
			for (int i = 0; i < 5; i++) {
				float orbitRadius = i * .5f + 2f;
				float angle1 = time * 4f * (1f + i * .1f);
				float angle2 = time * 4f * (1.2f + i * .117f);
				float angle3 = time * 4f * (1.3f + i * .1618f);
				float cx = math.cos(angle1) * orbitRadius;
				float cy = math.sin(angle2) * orbitRadius;
				float cz = math.sin(angle3) * orbitRadius;

				float newDist = SmoothMin(distance,Sphere(x - cx,y - cy,z - cz,2f),2f);
				if (newDist < distance) {
					normal = new float3(x - cx,y - cy,z - cz);
					distance = newDist;
				}
			}
		} else if (model == DistanceFieldModel.SpinMixer) {
			for (int i = 0; i < 6; i++) {
				float orbitRadius = (i / 2 + 2) * 2;
				float angle = time * 20f * (1f + i * .1f);
				float cx = math.cos(angle) * orbitRadius;
				float cy = math.sin(angle);
				float cz = math.sin(angle) * orbitRadius;

				float newDist = Sphere(x - cx,y - cy,z - cz,2f);
				if (newDist < distance) {
					normal = new float3(x - cx,y - cy,z - cz);
					distance = newDist;
				}
			}
		} else if (model == DistanceFieldModel.SpherePlane) {
			float sphereDist = Sphere(x,y,z,5f);
			var sphereNormal = new float3(x,y,z);
			sphereNormal /= math.length(sphereNormal);

			float planeDist = y;
			var planeNormal = new float3(0f,1f,0f);

			float t = math.sin(time * 8f) * .4f + .4f;
			distance = math.lerp(sphereDist,planeDist,t);
			normal = math.lerp(sphereNormal,planeNormal,t);
		} else if (model==DistanceFieldModel.SphereField) {
			float spacing = 5f + math.sin(time*5f) * 2f;
			x += spacing * .5f;
			y += spacing * .5f;
			z += spacing * .5f;
			x -= math.floor(x / spacing) * spacing;
			y -= math.floor(y / spacing) * spacing;
			z -= math.floor(z / spacing) * spacing;
			x -= spacing * .5f;
			y -= spacing * .5f;
			z -= spacing * .5f;
			distance = Sphere(x,y,z,5f);
			normal = new float3(x,y,z);
		} else if (model==DistanceFieldModel.FigureEight) {
			float ringRadius = 4f;
			float flipper = 1f;
			if (z<0f) {
				z = -z;
				flipper = -1f;
			}
			float3 point = new float3(x,0f,z - ringRadius);
			point /= math.length(point) * ringRadius;
			float angle = math.atan2(point.z,point.x)+time*8f;
			point+=new float3(0,0,1)*ringRadius;
			normal = new float3(x - point.x,y - point.y,(z - point.z)*flipper);
			float wave = math.cos(angle*flipper*3f) * .5f + .5f;
			wave *= wave*.5f;
			distance = math.sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z) - (.5f + wave);
		} else if (model==DistanceFieldModel.PerlinNoise) {
			float perlin = noise.cnoise(new float2(x*.2f,z*.2f));
			distance = y - perlin*6f;
			normal = new float3(0,1,0);
		}
		
		return distance;
	}

	// what's the slope of the distance field at a given point?
	/*public static Vector3 GetGradient(float x, float y, float z,float baseValue) {
		float eps = 0.01f;
		float dx = GetDistance(x + eps,y,z) - baseValue;
		float dy = GetDistance(x,y + eps,z) - baseValue;
		float dz = GetDistance(x,y,z + eps) - baseValue;

		return new Vector3(dx / eps,dy / eps,dz / eps);
	}*/

	private void Start() {
		instance = this;
		modelCount = System.Enum.GetValues(typeof(DistanceFieldModel)).Length;
	}

	private void FixedUpdate() {
		timeStatic = Time.time*.1f;
	}

	private void Update() {
		switchTimer += Time.deltaTime*.1f;
		if (switchTimer>1f) {
			switchTimer -= 1f;
			int newModel = Random.Range(0,modelCount-1);
			if (newModel>=(int)model) {
				newModel++;
			}
			model = (DistanceFieldModel)newModel;
		}

		if (preview) {
			float3 normal;
			for (int i = 0; i < 3000; i++) {
				float x = Random.Range(-10f,10f);
				float y = Random.Range(-10f,10f);
				float z = Random.Range(-10f,10f);
				float dist = GetDistance(instance.model, timeStatic,x,y,z,out normal);
				if (dist < 0f) {
					Debug.DrawRay(new Vector3(x,y,z),Vector3.up * .1f,Color.red,1f);
				}
			}
		}
	}
}
