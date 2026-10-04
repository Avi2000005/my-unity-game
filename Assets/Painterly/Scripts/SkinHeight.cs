using UnityEngine;

namespace Echoes.Painterly;

public static class SkinHeight
{
	public static float Measure(Renderer renderer, out Vector3 worldFeet, out string why)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected Obj, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		worldFeet = Vector3.zero;
		why = "";
		if ((Object)(object)renderer == (Object)null)
		{
			why = "no renderer";
			return 0f;
		}
		if (!renderer.enabled || !((Component)renderer).gameObject.activeInHierarchy)
		{
			why = "renderer is disabled" + (((Component)renderer).gameObject.activeInHierarchy ? "" : " (its GameObject is inactive)");
			return 0f;
		}
		SkinnedMeshRenderer val = (SkinnedMeshRenderer)(object)((renderer is SkinnedMeshRenderer) ? renderer : null);
		if ((Object)(object)val == (Object)null)
		{
			MeshFilter component = ((Component)renderer).GetComponent<MeshFilter>();
			if ((Object)(object)component == (Object)null || (Object)(object)component.sharedMesh == (Object)null)
			{
				why = "no MeshFilter mesh";
				return 0f;
			}
			Bounds bounds = renderer.bounds;
			worldFeet = bounds.min;
			why = "mesh renderer, bounds";
			return bounds.size.y;
		}
		Mesh sharedMesh = val.sharedMesh;
		if ((Object)(object)sharedMesh == (Object)null)
		{
			why = "skinned renderer has no shared mesh";
			return 0f;
		}
		if (sharedMesh.vertexCount == 0)
		{
			why = "shared mesh has no vertices";
			return 0f;
		}
		Mesh val2 = new Mesh();
		((Object)val2).name = "__skinheight_bake";
		try
		{
			val.BakeMesh(val2, true);
			if (val2.vertexCount == 0)
			{
				why = "baked mesh has no vertices";
				return 0f;
			}
			Bounds val3 = WorldBounds(((Component)val).transform, val2.bounds);
			worldFeet = val3.min;
			why = "baked skin, " + val2.vertexCount + " vertices";
			return val3.size.y;
		}
		finally
		{
			Object.DestroyImmediate((Object)(object)val2);
		}
	}

	public static float Measure(Renderer renderer)
	{
		Vector3 worldFeet;
		string why;
		return Measure(renderer, out worldFeet, out why);
	}

	public static float FitToHeight(Transform root, Renderer renderer, float wantMetres, out string why)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		why = "";
		if ((Object)(object)root == (Object)null)
		{
			why = "no root";
			return 0f;
		}
		if (wantMetres <= 0.0001f)
		{
			why = "wanted height is zero — refusing to scale to nothing";
			return 0f;
		}
		float num = Measure(renderer, out var worldFeet, out why);
		if (num <= 0.0001f)
		{
			why = "could not measure: " + why;
			return 0f;
		}
		float num2 = wantMetres / num;
		if (Mathf.Approximately(num2, 1f))
		{
			why = "already " + wantMetres.ToString("0.00") + " m tall";
			return 1f;
		}
		Vector3 localScale = root.localScale;
		root.position = worldFeet - num2 * (worldFeet - root.position);
		root.localScale = new Vector3(localScale.x * num2, localScale.y * num2, localScale.z * num2);
		why = "was " + num.ToString("0.000") + " m, scaled x" + num2.ToString("0.0000") + " to " + wantMetres.ToString("0.000") + " m";
		return num2;
	}

	public static Bounds WorldBounds(Transform t, Bounds local)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		Vector3 center = local.center;
		Vector3 extents = local.extents;
		Vector3[] array = new Vector3[8];
		int num = 0;
		for (int i = -1; i <= 1; i += 2)
		{
			for (int j = -1; j <= 1; j += 2)
			{
				for (int k = -1; k <= 1; k += 2)
				{
					array[num++] = t.TransformPoint(center + Vector3.Scale(extents, new Vector3((float)i, (float)j, (float)k)));
				}
			}
		}
		Bounds result = new Bounds(array[0], Vector3.zero);
		for (int l = 1; l < array.Length; l++)
		{
			result.Encapsulate(array[l]);
		}
		return result;
	}

	public static Renderer BodyOf(GameObject root)
	{
		if ((Object)(object)root == (Object)null)
		{
			return null;
		}
		Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i] is SkinnedMeshRenderer && componentsInChildren[i].enabled)
			{
				return componentsInChildren[i];
			}
		}
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			if (componentsInChildren[j].enabled && (Object)(object)((Component)componentsInChildren[j]).GetComponent<MeshFilter>() != (Object)null)
			{
				return componentsInChildren[j];
			}
		}
		if (componentsInChildren.Length == 0)
		{
			return null;
		}
		return componentsInChildren[0];
	}
}
