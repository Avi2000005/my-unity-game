using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class AriStandOnGround
    {
    	[MenuItem("Tools/Echoes/Stand Ari On The Square", priority = 63)]
    	public static void Run()
    	{
    		try
    		{
    			RunInner();
    		}
    		catch (Exception ex)
    		{
    			File.WriteAllText("Temp/ari_stand_error.txt", ex.ToString());
    			Debug.LogError((object)("[Echoes] Stand Ari failed\n" + ex));
    		}
    	}

    	private static void RunInner()
    	{
    		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0059: Expected Obj, but got Unknown
    		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
    		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
    		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
    		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
    		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
    		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
    		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
    		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05c7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04a1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04b6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04db: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_050b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_056a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			Debug.LogError((object)"[Echoes] stop play mode first. Placement made in play mode is thrown away when you exit, so it would look like it worked and then vanish.");
    			return;
    		}
    		GameObject val = GameObject.Find("Ari");
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] no Ari in the scene. Run Tools/Echoes/Place Ari.");
    			return;
    		}
    		SkinnedMeshRenderer componentInChildren = val.GetComponentInChildren<SkinnedMeshRenderer>(true);
    		if ((Object)(object)componentInChildren == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] Ari has no SkinnedMeshRenderer");
    			return;
    		}
    		Mesh val2 = new Mesh();
    		MeasureSilhouette(componentInChildren, val2, out var lowestWorldY, out var highestWorldY);
    		Object.DestroyImmediate((Object)(object)val2);
    		float num = highestWorldY - lowestWorldY;
    		stringBuilder.AppendLine("--- measured silhouette (baked skin, not bounds) ---");
    		stringBuilder.AppendLine($"  sole  y = {lowestWorldY:F4}   (with root at {val.transform.position.y:F4})");
    		stringBuilder.AppendLine($"  crown y = {highestWorldY:F4}");
    		stringBuilder.AppendLine($"  height   = {num:F4}");
    		if (TryFindSquare(out var surfaceY, out var standAt, stringBuilder))
    		{
    			stringBuilder.AppendLine("--- the market square ---");
    			stringBuilder.AppendLine($"  paving top y = {surfaceY:F4}  (tiles are placed at " + $"{surfaceY:F2}, so that is the walking surface)");
    		}
    		else
    		{
    			surfaceY = 0f;
    			standAt = val.transform.position;
    			stringBuilder.AppendLine("--- no MarketSquare found; standing at the current position ---");
    		}
    		float num2 = surfaceY - lowestWorldY;
    		float num3 = val.transform.position.y + num2;
    		Vector3 val3 = new Vector3(standAt.x, num3, standAt.z);
    		Camera main = Camera.main;
    		if ((Object)(object)main != (Object)null)
    		{
    			Bounds b = new Bounds(val3 + Vector3.up * (num * 0.5f), new Vector3(0.8f, num, 0.8f));
    			if (!InFrustum(main, b))
    			{
    				Vector3 val4 = new Vector3(val3.x - ((Component)main).transform.position.x, 0f, val3.z - ((Component)main).transform.position.z);
    				if (val4.sqrMagnitude < 0.0001f)
    				{
    					val4 = new Vector3(0f, 0f, -1f);
    				}
    				val4.Normalize();
    				Vector3 position = val3 - val4 * 7f + Vector3.up * 3.2f;
    				((Component)main).transform.position = position;
    				LookAt(((Component)main).transform, val3 + Vector3.up * (num * 0.5f));
    				stringBuilder.AppendLine("  she was out of frame, so the camera was reframed: " + $"pos {((Component)main).transform.position} rot {((Component)main).transform.eulerAngles}");
    				stringBuilder.AppendLine($"  in frustum after reframing: {InFrustum(main, b)}");
    			}
    		}
    		val.transform.position = val3;
    		val.transform.rotation = Quaternion.Euler(0f, FaceCamera(val, main), 0f);
    		stringBuilder.AppendLine("--- placed ---");
    		stringBuilder.AppendLine($"  position {val.transform.position}  (lift applied {num2:F4})");
    		AriMover component = val.GetComponent<AriMover>();
    		bool flag = false;
    		if ((Object)(object)component == (Object)null)
    		{
    			val.AddComponent<AriMover>();
    			flag = true;
    		}
    		stringBuilder.AppendLine("--- mover ---");
    		stringBuilder.AppendLine(flag ? "  added AriMover (WASD or arrows to walk, LeftShift to run)" : "  AriMover already present");
    		Animator val5 = val.GetComponent<Animator>() ?? ((Component)componentInChildren).GetComponent<Animator>();
    		if ((Object)(object)val5 == (Object)null)
    		{
    			val5 = val.AddComponent<Animator>();
    		}
    		((Behaviour)val5).enabled = true;
    		val5.applyRootMotion = false;
    		val5.cullingMode = (AnimatorCullingMode)0;
    		val5.Rebind();
    		val5.Update(0f);
    		stringBuilder.AppendLine("--- animator ---");
    		stringBuilder.AppendLine($"  enabled={((Behaviour)val5).enabled} active={((Behaviour)val5).isActiveAndEnabled} " + "controller=" + (((Object)(object)val5.runtimeAnimatorController == (Object)null) ? "<NONE>" : ((Object)val5.runtimeAnimatorController).name) + " avatar=" + (((Object)(object)val5.avatar == (Object)null) ? "<NONE>" : ((Object)val5.avatar).name));
    		stringBuilder.AppendLine("  state after a forced evaluate: " + DescribeState(val5) + "  (Speed=0 should be the idle)");
    		if ((Object)(object)main != (Object)null)
    		{
    			Bounds bounds = ((Renderer)componentInChildren).bounds;
    			float num4 = Vector3.Distance(((Component)main).transform.position, bounds.center);
    			stringBuilder.AppendLine("--- visibility ---");
    			stringBuilder.AppendLine($"  camera {((Component)main).transform.position} -> Ari {bounds.center}, {num4:F2} units");
    			stringBuilder.AppendLine($"  in frustum: {InFrustum(main, bounds)}");
    			stringBuilder.AppendLine($"  layer {val.layer} in cullingMask: " + $"{(main.cullingMask & (1 << val.layer)) != 0}");
    			if (!InFrustum(main, bounds))
    			{
    				LookAt(((Component)main).transform, bounds.center);
    				stringBuilder.AppendLine($"  aimed the camera at her: rot now {((Component)main).transform.eulerAngles}");
    				stringBuilder.AppendLine($"  in frustum after aiming: {InFrustum(main, bounds)}");
    			}
    		}
    		EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    		AssetDatabase.SaveAssets();
    		string text = stringBuilder.ToString();
    		File.WriteAllText("Temp/ari_stand.txt", text);
    		Debug.Log((object)("[Echoes] Ari stood on the square\n" + text));
    	}

    	private static void MeasureSilhouette(SkinnedMeshRenderer skin, Mesh baked, out float lowestWorldY, out float highestWorldY)
    	{
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
    		skin.BakeMesh(baked);
    		Vector3[] vertices = baked.vertices;
    		Matrix4x4 localToWorldMatrix = ((Component)skin).transform.localToWorldMatrix;
    		lowestWorldY = float.MaxValue;
    		highestWorldY = float.MinValue;
    		for (int i = 0; i < vertices.Length; i++)
    		{
    			float y = localToWorldMatrix.MultiplyPoint3x4(vertices[i]).y;
    			if (y < lowestWorldY)
    			{
    				lowestWorldY = y;
    			}
    			if (y > highestWorldY)
    			{
    				highestWorldY = y;
    			}
    		}
    		if (vertices.Length == 0)
    		{
    			lowestWorldY = ((Component)skin).transform.position.y;
    			highestWorldY = ((Component)skin).transform.position.y;
    		}
    	}

    	private static bool TryFindSquare(out float surfaceY, out Vector3 standAt, StringBuilder sb)
    	{
    		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
    		surfaceY = 0f;
    		standAt = Vector3.zero;
    		GameObject val = GameObject.Find("MarketSquare");
    		if ((Object)(object)val == (Object)null)
    		{
    			GameObject val2 = GameObject.Find("Village_Grey");
    			if ((Object)(object)val2 != (Object)null)
    			{
    				Transform val3 = val2.transform.Find("MarketSquare");
    				if ((Object)(object)val3 != (Object)null)
    				{
    					val = ((Component)val3).gameObject;
    				}
    			}
    		}
    		if ((Object)(object)val == (Object)null)
    		{
    			return false;
    		}
    		Renderer[] array = (from r in val.GetComponentsInChildren<Renderer>(true)
    			where ((Object)r).name.StartsWith("Floor_")
    			select r).ToArray();
    		if (array.Length == 0)
    		{
    			sb.AppendLine("  no Floor_* paving found under MarketSquare");
    			return false;
    		}
    		Bounds bounds = array[0].bounds;
    		Renderer[] array2 = array;
    		foreach (Renderer val4 in array2)
    		{
    			bounds.Encapsulate(val4.bounds);
    		}
    		surfaceY = bounds.max.y;
    		Renderer[] tower = (from r in val.GetComponentsInChildren<Renderer>(true)
    			where (Object)(object)((Component)r).transform.parent != (Object)null && ((Object)((Component)r).transform.parent).name.StartsWith("House_")
    			select r).ToArray();
    		Bounds footprint = ((tower.Length != 0) ? tower[0].bounds : default(Bounds));
    		array2 = tower;
    		foreach (Renderer val5 in array2)
    		{
    			footprint.Encapsulate(val5.bounds);
    		}
    		Vector3[] array3 = array.Select((Renderer r) =>
    		{
    			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    			Bounds bounds2 = r.bounds;
    			return bounds2.center;
    		}).Where((Vector3 c) =>
    		{
    			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    			return tower.Length == 0 || Mathf.Abs(c.x - footprint.center.x) > footprint.extents.x + 0.2f || Mathf.Abs(c.z - footprint.center.z) > footprint.extents.z + 0.2f;
    		}).ToArray();
    		sb.AppendLine($"  {array.Length} paving tile(s), top y = {surfaceY:F4}, " + $"spanning {bounds.size.x:F2} x {bounds.size.z:F2}");
    		sb.AppendLine($"  landmark tower footprint {footprint.size.x:F2} x " + $"{footprint.size.z:F2} at {footprint.center}");
    		if (array3.Length == 0)
    		{
    			sb.AppendLine("  every tile is inside the tower's footprint; falling back to the square's edge");
    			standAt = new Vector3(bounds.center.x, surfaceY, bounds.max.z - 0.5f);
    			return true;
    		}
    		Camera cam = Camera.main;
    		standAt = (((Object)(object)cam == (Object)null) ? array3[0] : array3.OrderBy((Vector3 c) =>
    		{
    			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    			return Vector3.Distance(c, ((Component)cam).transform.position);
    		}).First());
    		sb.AppendLine($"  {array3.Length} clear tile(s); standing at {standAt}");
    		return true;
    	}

    	private static bool InFrustum(Camera cam, Bounds b)
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), b);
    	}

    	private static float FaceCamera(GameObject ari, Camera cam)
    	{
    		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)cam == (Object)null)
    		{
    			return 0f;
    		}
    		Vector3 val = ((Component)cam).transform.position - ari.transform.position;
    		val.y = 0f;
    		if (val.sqrMagnitude < 0.0001f)
    		{
    			return 0f;
    		}
    		Quaternion val2 = Quaternion.LookRotation(val, Vector3.up);
    		return val2.eulerAngles.y;
    	}

    	private static void LookAt(Transform t, Vector3 target)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = target - t.position;
    		if (!(val.sqrMagnitude < 0.0001f))
    		{
    			t.rotation = Quaternion.LookRotation(val, Vector3.up);
    		}
    	}

    	private static string DescribeState(Animator anim)
    	{
    		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    		AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
    		var anon = anim.runtimeAnimatorController.animationClips.Select((AnimationClip c) => new
    		{
    			((Object)c).name,
    			c.length
    		}).FirstOrDefault(c => Animator.StringToHash(c.name) == info.shortNameHash);
    		return $"hash={info.shortNameHash} normalizedTime={info.normalizedTime:F2} " + "clip=" + ((anon == null) ? "?" : anon.name) + " " + $"clipLen={anon?.length ?? 0f:F2}s";
    	}
    }
}