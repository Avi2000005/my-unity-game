using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class AriVisibilityProbe
    {
    	[MenuItem("Tools/Echoes/Probe Ari Visible", priority = 96)]
    	public static void Run()
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0525: Unknown result type (might be due to invalid IL or missing references)
    		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
    		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
    		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
    		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0548: Unknown result type (might be due to invalid IL or missing references)
    		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0592: Unknown result type (might be due to invalid IL or missing references)
    		//IL_059e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_061c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0635: Unknown result type (might be due to invalid IL or missing references)
    		//IL_067f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_06c7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0736: Unknown result type (might be due to invalid IL or missing references)
    		//IL_073d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0742: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0749: Unknown result type (might be due to invalid IL or missing references)
    		//IL_077f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0786: Unknown result type (might be due to invalid IL or missing references)
    		//IL_07cf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_07f8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_07ff: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0808: Unknown result type (might be due to invalid IL or missing references)
    		//IL_09d1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_09f7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a87: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		Scene activeScene = SceneManager.GetActiveScene();
    		stringBuilder.AppendLine($"scene '{activeScene.name}' path={activeScene.path} loaded={activeScene.isLoaded} " + $"dirty={activeScene.isDirty}");
    		GameObject val = GameObject.Find("Ari");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("\nNO GameObject named 'Ari' in the open scene.");
    			stringBuilder.AppendLine("Roots in this scene:");
    			GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
    			foreach (GameObject val2 in rootGameObjects)
    			{
    				stringBuilder.AppendLine("  " + ((Object)val2).name + " (" + (val2.activeSelf ? "active" : "INACTIVE") + ")");
    			}
    			Finish(stringBuilder);
    			return;
    		}
    		Transform transform = val.transform;
    		stringBuilder.AppendLine($"\nAri: activeInHierarchy={val.activeInHierarchy} layer={val.layer} " + "tag=" + val.tag);
    		stringBuilder.AppendLine($"  world pos {transform.position}  localScale {transform.localScale}  " + $"children={val.transform.childCount}");
    		Renderer[] componentsInChildren = val.GetComponentsInChildren<Renderer>(true);
    		stringBuilder.AppendLine($"  {componentsInChildren.Length} renderer(s)");
    		Renderer[] array = componentsInChildren;
    		foreach (Renderer val3 in array)
    		{
    			stringBuilder.AppendLine($"    {((object)val3).GetType().Name} '{((Object)val3).name}' enabled={val3.enabled} " + $"activeInHierarchy={((Component)val3).gameObject.activeInHierarchy} layer={((Component)val3).gameObject.layer}");
    			stringBuilder.AppendLine($"      activeInHierarchy={((Component)val3).gameObject.activeInHierarchy} " + $"isVisible={val3.isVisible}");
    			SkinnedMeshRenderer val4 = (SkinnedMeshRenderer)(object)((val3 is SkinnedMeshRenderer) ? val3 : null);
    			Bounds val5;
    			if (val4 != null)
    			{
    				Transform rootBone = val4.rootBone;
    				string text = $"      skin={((rootBone != null) ? ((Object)rootBone).name : null)} quality={val4.quality} ";
    				string text2 = $"updateWhenOffscreen={val4.updateWhenOffscreen} ";
    				val5 = ((Renderer)val4).localBounds;
    				object arg = val5.center;
    				val5 = ((Renderer)val4).localBounds;
    				stringBuilder.AppendLine(text + text2 + $"bounds(local) centre={arg} size={val5.size}");
    				stringBuilder.AppendLine("      mesh=" + (((Object)(object)val4.sharedMesh == (Object)null) ? "<none>" : (((Object)val4.sharedMesh).name + " " + val4.sharedMesh.vertexCount + "v")));
    			}
    			else if (val3 is MeshRenderer)
    			{
    				val5 = val3.bounds;
    				object arg2 = val5.center;
    				val5 = val3.bounds;
    				stringBuilder.AppendLine($"      bounds(world) centre={arg2} size={val5.size}");
    			}
    			Material[] sharedMaterials = val3.sharedMaterials;
    			foreach (Material val6 in sharedMaterials)
    			{
    				if ((Object)(object)val6 == (Object)null)
    				{
    					stringBuilder.AppendLine("      material: <NULL>");
    					continue;
    				}
    				Shader shader = val6.shader;
    				stringBuilder.AppendLine("      material '" + ((Object)val6).name + "' shader='" + ((shader != null) ? ((Object)shader).name : null) + "' " + $"supported={(Object)(object)shader != (Object)null && shader.isSupported} " + $"renderQueue={val6.renderQueue} passCount={((shader != null) ? new int?(shader.passCount) : ((int?)null))}");
    				if ((Object)(object)shader != (Object)null && !shader.isSupported)
    				{
    					stringBuilder.AppendLine("      ^ SHADER NOT SUPPORTED: this is what makes his invisible");
    				}
    				Texture texture = val6.GetTexture("_BaseMap");
    				stringBuilder.AppendLine("      _BaseMap=" + (((Object)(object)texture == (Object)null) ? "<none>" : ((Object)texture).name) + " " + $"renderQueue={val6.renderQueue} " + "keywords=" + string.Join(",", val6.shaderKeywords));
    			}
    		}
    		Bounds bounds = new Bounds(transform.position, Vector3.zero);
    		bool flag = false;
    		array = componentsInChildren;
    		foreach (Renderer val7 in array)
    		{
    			if (!flag)
    			{
    				bounds = val7.bounds;
    				flag = true;
    			}
    			else
    			{
    				bounds.Encapsulate(val7.bounds);
    			}
    		}
    		if (flag)
    		{
    			stringBuilder.AppendLine($"\nAri world bounds: centre={bounds.center} " + $"size={bounds.size} minY={bounds.min.y:F3} " + $"maxY={bounds.max.y:F3}");
    		}
    		Camera main = Camera.main;
    		if ((Object)(object)main == (Object)null)
    		{
    			stringBuilder.AppendLine("\nNO Camera.main in the scene.");
    			Finish(stringBuilder);
    			return;
    		}
    		Transform transform2 = ((Component)main).transform;
    		stringBuilder.AppendLine($"\nCamera.main '{((Object)main).name}' pos={transform2.position} " + $"rot={transform2.eulerAngles} fov={main.fieldOfView} " + $"near={main.nearClipPlane} far={main.farClipPlane} " + $"clearFlags={main.clearFlags} cullingMask={main.cullingMask} " + $"enabled={((Behaviour)main).enabled}");
    		stringBuilder.AppendLine($"  forward={transform2.forward}");
    		if (flag)
    		{
    			stringBuilder.AppendLine($"  Ari layer {val.layer} visible to this camera: " + $"{(main.cullingMask & (1 << val.layer)) != 0}");
    			stringBuilder.AppendLine("  Ari bounds centre in front of camera: " + $"{Vector3.Dot(bounds.center - transform2.position, transform2.forward) >= 0f}");
    			stringBuilder.AppendLine("  distance camera->Ari = " + $"{Vector3.Distance(transform2.position, bounds.center):F3} " + $"(near={main.nearClipPlane}, far={main.farClipPlane})");
    			stringBuilder.AppendLine($"  in camera frustum: {TestInFrustum(main, bounds)}");
    		}
    		if (flag)
    		{
    			Collider[] array2 = new Collider[16];
    			int num = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, array2, transform2.rotation, -1, (QueryTriggerInteraction)1);
    			string[] array3 = (from c in array2
    				where (Object)(object)c != (Object)null
    				select ((Object)c).name + "(" + ((object)c).GetType().Name + ")").Distinct().ToArray();
    			stringBuilder.AppendLine($"\n  colliders overlapping Ari's bounds: {num}");
    			string[] array4 = array3;
    			foreach (string text3 in array4)
    			{
    				stringBuilder.AppendLine("    " + text3);
    			}
    			if (num == 0)
    			{
    				stringBuilder.AppendLine("    none — nothing is in him space");
    			}
    		}
    		Animator val8 = val.GetComponent<Animator>();
    		if ((Object)(object)val8 == (Object)null)
    		{
    			val8 = val.GetComponentInChildren<Animator>();
    		}
    		if ((Object)(object)val8 == (Object)null)
    		{
    			stringBuilder.AppendLine("\nNO Animator on Ari or his children.");
    		}
    		else
    		{
    			stringBuilder.AppendLine("\nAnimator on '" + ((Object)val8).name + "':");
    			stringBuilder.AppendLine($"  enabled={((Behaviour)val8).enabled} " + $"activeInHierarchy={((Behaviour)val8).isActiveAndEnabled} " + "controller=" + (((Object)(object)val8.runtimeAnimatorController == (Object)null) ? "<NONE>" : ((Object)val8.runtimeAnimatorController).name) + " avatar=" + (((Object)(object)val8.avatar == (Object)null) ? "<NONE>" : ((Object)val8.avatar).name) + " " + $"applyRootMotion={val8.applyRootMotion} " + $"culling={val8.cullingMode} speed={val8.speed} " + $"updateMode={val8.updateMode}");
    			if ((Object)(object)val8.avatar != (Object)null)
    			{
    				stringBuilder.AppendLine($"  avatar valid={val8.avatar.isValid} human={val8.avatar.isHuman}");
    			}
    			if ((Object)(object)val8.runtimeAnimatorController != (Object)null)
    			{
    				AnimatorControllerParameter[] parameters = val8.parameters;
    				foreach (AnimatorControllerParameter val9 in parameters)
    				{
    					stringBuilder.AppendLine($"  param {val9.name} {val9.type} default={val9.defaultFloat}");
    				}
    			}
    		}
    		Finish(stringBuilder);
    	}

    	private static bool TestInFrustum(Camera cam, Bounds bounds)
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), bounds);
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		string text = sb.ToString();
    		File.WriteAllText("Temp/ari_visible.txt", text);
    		Debug.Log((object)("[Echoes] Ari visibility probe\n" + text));
    	}
    }
}