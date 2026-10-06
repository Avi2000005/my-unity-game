using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class L1CastProbe
    {
    	private const string Report = "Temp/l1_cast_probe.txt";

    	[MenuItem("Tools/Echoes/Probe L1 Cast", priority = 96)]
    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] cast probe");
    		stringBuilder.AppendLine("ground check — every number below is read off the hierarchy, none is a design constant");
    		Crawlers(stringBuilder);
    		Mono(stringBuilder);
    		Tree(stringBuilder);
    		Ari(stringBuilder);
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_cast_probe.txt"), stringBuilder.ToString());
    		Debug.Log((object)"[Echoes] cast probe written to Temp/l1_cast_probe.txt");
    	}

    	private static void Crawlers(StringBuilder sb)
    	{
    		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0635: Unknown result type (might be due to invalid IL or missing references)
    		//IL_063a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0655: Unknown result type (might be due to invalid IL or missing references)
    		//IL_065a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
    		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0553: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
    		//IL_055c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0561: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0576: Unknown result type (might be due to invalid IL or missing references)
    		//IL_057b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_057f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0584: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== INK CRAWLERS ===");
    		InkCrawler[] array = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1, (FindObjectsSortMode)0);
    		sb.AppendLine("found " + array.Length + " InkCrawler component(s)");
    		foreach (InkCrawler inkCrawler in array)
    		{
    			GameObject gameObject = ((Component)inkCrawler).gameObject;
    			sb.AppendLine();
    			string[] array2 = new string[6]
    			{
    				"-- ",
    				((Object)gameObject).name,
    				"  at ",
    				null,
    				null,
    				null
    			};
    			Vector3 val = gameObject.transform.position;
    			array2[3] = val.ToString("F2");
    			array2[4] = "  active=";
    			array2[5] = gameObject.activeSelf.ToString();
    			sb.AppendLine(string.Concat(array2));
    			sb.AppendLine("   parent      : " + (((Object)(object)gameObject.transform.parent == (Object)null) ? "(none — a root)" : ((Object)gameObject.transform.parent).name));
    			val = gameObject.transform.localScale;
    			string text = val.ToString("F4");
    			val = gameObject.transform.localPosition;
    			sb.AppendLine("   localScale  : " + text + "   localPos    : " + val.ToString("F3"));
    			sb.AppendLine("   declared    : bodyHeight " + inkCrawler.BodyHeight.ToString("F2") + " m, splashRadius " + inkCrawler.SplashRadius.ToString("F2") + ", notice " + inkCrawler.NoticeRadius.ToString("F1"));
    			Collider component = gameObject.GetComponent<Collider>();
    			sb.AppendLine("   collider    : " + Describe(component));
    			Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
    			sb.AppendLine("   renderers   : " + componentsInChildren.Length);
    			if (componentsInChildren.Length == 0)
    			{
    				sb.AppendLine("      ^ NO RENDERER ANYWHERE UNDER THIS OBJECT. Nothing is drawn for this crawler — which is a different problem from an untextured one, and the fix is not a texture.");
    			}
    			for (int j = 0; j < componentsInChildren.Length; j++)
    			{
    				Renderer val2 = componentsInChildren[j];
    				sb.AppendLine("      [" + j + "] " + ((object)val2).GetType().Name + " '" + ((Object)val2).name + "' enabled=" + val2.enabled + " path=" + PathOf(gameObject.transform, ((Component)val2).transform));
    				SkinnedMeshRenderer val3 = (SkinnedMeshRenderer)(object)((val2 is SkinnedMeshRenderer) ? val2 : null);
    				if ((Object)(object)val3 != (Object)null)
    				{
    					sb.AppendLine("          skinned, blendShapeCount " + val3.sharedMesh.blendShapeCount);
    				}
    				MeshFilter component2 = ((Component)val2).GetComponent<MeshFilter>();
    				Bounds bounds;
    				if ((Object)(object)component2 != (Object)null && (Object)(object)component2.sharedMesh != (Object)null)
    				{
    					string[] array3 = new string[6]
    					{
    						"          mesh       : '",
    						((Object)component2.sharedMesh).name,
    						"' ",
    						component2.sharedMesh.vertexCount.ToString(),
    						" verts, bounds ",
    						null
    					};
    					bounds = component2.sharedMesh.bounds;
    					val = bounds.size;
    					array3[5] = val.ToString("F3");
    					sb.AppendLine(string.Concat(array3));
    					sb.AppendLine("          uv0        : " + ((component2.sharedMesh.uv.Length != 0) ? (component2.sharedMesh.uv.Length + " coords") : "NONE — a texture assigned here would sample nothing and the mesh would render as flat colour"));
    				}
    				Material[] sharedMaterials = val2.sharedMaterials;
    				int num = sharedMaterials?.Length ?? 0;
    				sb.AppendLine("          materials  : " + num);
    				for (int k = 0; k < num; k++)
    				{
    					Material val4 = sharedMaterials[k];
    					if ((Object)(object)val4 == (Object)null)
    					{
    						sb.AppendLine("             [" + k + "] NULL — this slot draws pink");
    						continue;
    					}
    					sb.AppendLine("             [" + k + "] '" + ((Object)val4).name + "' shader=" + (((Object)(object)val4.shader != (Object)null) ? ((Object)val4.shader).name : "NULL SHADER") + " path=" + AssetDatabase.GetAssetPath((Object)(object)val4));
    					if (val4.HasProperty("_BaseMap"))
    					{
    						Texture texture = val4.GetTexture("_BaseMap");
    						Texture2D val5 = (Texture2D)(object)((texture is Texture2D) ? texture : null);
    						sb.AppendLine("                   _BaseMap = " + (((Object)(object)val5 == (Object)null) ? "NONE  <-- the flat look" : (((Object)val5).name + " " + ((Texture)val5).width + "x" + ((Texture)val5).height + " @" + AssetDatabase.GetAssetPath((Object)(object)val5))));
    					}
    				}
    				bounds = val2.bounds;
    				val = bounds.size;
    				string text2 = val.ToString("F3");
    				bounds = val2.bounds;
    				val = bounds.center;
    				sb.AppendLine("          worldBounds: " + text2 + " at " + val.ToString("F2"));
    			}
    			int childCount = gameObject.transform.childCount;
    			sb.AppendLine("   children    : " + childCount);
    			for (int l = 0; l < childCount; l++)
    			{
    				Transform child = gameObject.transform.GetChild(l);
    				int num2 = ((Component)child).GetComponentsInChildren<Renderer>(true).Length;
    				string[] array4 = new string[12]
    				{
    					"      ",
    					((Object)child).name,
    					" (",
    					((object)child).GetType().Name,
    					") localPos ",
    					null,
    					null,
    					null,
    					null,
    					null,
    					null,
    					null
    				};
    				val = child.localPosition;
    				array4[5] = val.ToString("F3");
    				array4[6] = " localScale ";
    				val = child.localScale;
    				array4[7] = val.ToString("F3");
    				array4[8] = " renderers=";
    				array4[9] = num2.ToString();
    				array4[10] = " active=";
    				array4[11] = ((Component)child).gameObject.activeSelf.ToString();
    				sb.AppendLine(string.Concat(array4));
    			}
    		}
    	}

    	private static void Mono(StringBuilder sb)
    	{
    		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
    		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
    		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04ab: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04b0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== MONO ===");
    		MonoCompanion[] array = Object.FindObjectsByType<MonoCompanion>((FindObjectsInactive)1, (FindObjectsSortMode)0);
    		sb.AppendLine("found " + array.Length);
    		for (int i = 0; i < array.Length; i++)
    		{
    			GameObject gameObject = ((Component)array[i]).gameObject;
    			Transform transform = gameObject.transform;
    			sb.AppendLine();
    			string[] array2 = new string[6]
    			{
    				"-- ",
    				((Object)gameObject).name,
    				" at ",
    				null,
    				null,
    				null
    			};
    			Vector3 val = transform.position;
    			array2[3] = val.ToString("F3");
    			array2[4] = " active=";
    			array2[5] = gameObject.activeSelf.ToString();
    			sb.AppendLine(string.Concat(array2));
    			val = transform.localScale;
    			string text = val.ToString("F4");
    			val = transform.lossyScale;
    			sb.AppendLine("   localScale  : " + text + "   localScale3 : " + val.ToString("F4"));
    			val = transform.localPosition;
    			sb.AppendLine("   localPos    : " + val.ToString("F3"));
    			val = transform.eulerAngles;
    			sb.AppendLine("   rotation    : " + val.ToString("F1"));
    			Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
    			sb.AppendLine("   renderers   : " + componentsInChildren.Length);
    			Bounds val2 = default;
    			bool flag = true;
    			foreach (Renderer val3 in componentsInChildren)
    			{
    				SkinnedMeshRenderer val4 = (SkinnedMeshRenderer)(object)((val3 is SkinnedMeshRenderer) ? val3 : null);
    				sb.AppendLine("      " + ((object)val3).GetType().Name + " '" + ((Object)val3).name + " enabled=" + val3.enabled);
    				Bounds val5;
    				if ((Object)(object)val4 != (Object)null)
    				{
    					val5 = ((Renderer)val4).localBounds;
    					val = val5.size;
    					sb.AppendLine("         animationBounds " + val.ToString("F3") + "  <- every pose it reaches, NOT its current height");
    					float num = SkinHeight.Measure(val3, out var worldFeet, out var why);
    					sb.AppendLine("         measured    : " + ((num > 0.0001f) ? (num.ToString("F3") + " m tall, feet at y " + worldFeet.y.ToString("F3") + "  (" + why + ")") : ("COULD NOT MEASURE — " + why)));
    				}
    				else
    				{
    					MeshFilter component = ((Component)val3).GetComponent<MeshFilter>();
    					val5 = val3.bounds;
    					val = val5.size;
    					sb.AppendLine("         meshRenderer bounds " + val.ToString("F3") + (((Object)(object)component != (Object)null && (Object)(object)component.sharedMesh != (Object)null) ? (" (mesh " + ((Object)component.sharedMesh).name + ")") : ""));
    				}
    				if (flag)
    				{
    					val2 = val3.bounds;
    					flag = false;
    				}
    				else
    				{
    					val2.Encapsulate(val3.bounds);
    				}
    			}
    			if (!flag)
    			{
    				val = val2.size;
    				string text2 = val.y.ToString("F3");
    				val = val2.min;
    				sb.AppendLine("   MEASURED    : Mono stands " + text2 + " m tall, bottom at y " + val.y.ToString("F3"));
    				string[] array3 = new string[6] { "   his root y  : ", null, null, null, null, null };
    				val = transform.position;
    				array3[1] = val.y.ToString("F3");
    				array3[2] = "  -> bottom is ";
    				array3[3] = (val2.min.y - transform.position.y).ToString("F3");
    				array3[4] = " m ";
    				array3[5] = ((val2.min.y > transform.position.y + 0.02f) ? "ABOVE his root, so he is hovering" : "at or below his root, so he is on the floor");
    				sb.AppendLine(string.Concat(array3));
    			}
    			int childCount = transform.childCount;
    			sb.AppendLine("   children    : " + childCount);
    			for (int k = 0; k < childCount; k++)
    			{
    				Transform child = transform.GetChild(k);
    				string[] array4 = new string[8]
    				{
    					"      ",
    					((Object)child).name,
    					" (",
    					((object)child).GetType().Name,
    					") localPos ",
    					null,
    					null,
    					null
    				};
    				val = child.localPosition;
    				array4[5] = val.ToString("F3");
    				array4[6] = " renderers=";
    				array4[7] = ((Component)child).GetComponentsInChildren<Renderer>(true).Length.ToString();
    				sb.AppendLine(string.Concat(array4));
    			}
    		}
    		sb.AppendLine();
    		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		if ((Object)(object)ariMover != (Object)null)
    		{
    			sb.AppendLine("Ari's bodyHeight  : " + ariMover.BodyHeight.ToString("F3") + " m  (collider authority, unchanged)");
    			sb.AppendLine("Ari's chest, as InkCrawler measures it : " + (ariMover.BodyHeight * 0.6f).ToString("F3") + " m");
    			sb.AppendLine("  ^ that is the number to size Mono to. It is the project's existing definition of her chest, so using it means two scripts cannot disagree about where her chest is.");
    		}
    	}

    	private static void Tree(StringBuilder sb)
    	{
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
    		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== SLEEPING TREE / COLOUR STATE ===");
    		SleepingTree sleepingTree = Object.FindAnyObjectByType<SleepingTree>((FindObjectsInactive)1);
    		if ((Object)(object)sleepingTree == (Object)null)
    		{
    			sb.AppendLine("no SleepingTree in the scene");
    		}
    		else
    		{
    			string name = ((Object)((Component)sleepingTree).gameObject).name;
    			Vector3 val = ((Component)sleepingTree).transform.position;
    			sb.AppendLine("tree " + name + " at " + val.ToString("F2"));
    			sb.AppendLine("  burstRadius : " + sleepingTree.BurstRadius.ToString("F2") + " m");
    			sb.AppendLine("  awoken      : " + sleepingTree.IsAwoken);
    			val = sleepingTree.TouchPoint;
    			sb.AppendLine("  _point      : " + val.ToString("F2"));
    		}
    		ColorRestoreTarget[] array = Object.FindObjectsByType<ColorRestoreTarget>((FindObjectsInactive)1, (FindObjectsSortMode)0);
    		sb.AppendLine();
    		sb.AppendLine("ColorRestoreTarget components: " + array.Length);
    		float num = 0f;
    		float num2 = 0f;
    		int num3 = 0;
    		for (int i = 0; i < array.Length; i++)
    		{
    			num += array[i].Restore;
    			if (array[i].Restore > 0.001f)
    			{
    				num3++;
    			}
    			if (array[i].Restore > num2)
    			{
    				num2 = array[i].Restore;
    			}
    		}
    		if (array.Length != 0)
    		{
    			sb.AppendLine("  mean restore " + (num / (float)array.Length).ToString("0.000") + ", highest " + num2.ToString("0.000") + ", coloured now " + num3 + " of " + array.Length);
    			sb.AppendLine("  " + ((num3 == 0) ? "the whole village is grey, which is what Beat 7 needs to change" : (num3 + " target(s) are ALREADY partly coloured. The brief says no colour until the fountain, so something is restoring early — most likely BrushPainter.PaintAt, which calls RestoreInRadius(1f) on every stroke.")));
    		}
    		sb.AppendLine();
    		sb.AppendLine("targets within 15 m of the tree:");
    		for (int j = 0; j < array.Length; j++)
    		{
    			if ((Object)(object)sleepingTree == (Object)null)
    			{
    				break;
    			}
    			float num4 = Vector3.Distance(((Component)array[j]).transform.position, sleepingTree.TouchPoint);
    			if (!(num4 > 15f))
    			{
    				sb.AppendLine("   " + ((Object)array[j]).name.PadRight(28) + " " + num4.ToString("F1") + " m  restore " + array[j].Restore.ToString("0.000"));
    			}
    		}
    	}

    	private static void Ari(StringBuilder sb)
    	{
    		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== ARI ===");
    		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		if ((Object)(object)ariMover == (Object)null)
    		{
    			sb.AppendLine("none in the scene");
    			return;
    		}
    		Vector3 val = ((Component)ariMover).transform.position;
    		sb.AppendLine("at " + val.ToString("F3"));
    		sb.AppendLine("  bodyHeight " + ariMover.BodyHeight.ToString("F3") + "  bodyRadius " + ariMover.BodyRadius.ToString("F3"));
    		val = ((Component)ariMover).transform.localScale;
    		sb.AppendLine("  localScale " + val.ToString("F4"));
    		AriHealth component = ((Component)ariMover).GetComponent<AriHealth>();
    		sb.AppendLine("  AriHealth   : " + (((Object)(object)component == (Object)null) ? "NOT ON HER — beats 5-7 have no health" : ("present, at " + component.Fraction.ToString("0.00"))));
    		AriInteract component2 = ((Component)ariMover).GetComponent<AriInteract>();
    		sb.AppendLine("  AriInteract : " + (((Object)(object)component2 == (Object)null) ? "NOT ON HER — there is no E button in the level yet" : "present"));
    	}

    	private static string Describe(Collider c)
    	{
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)c == (Object)null)
    		{
    			return "NONE";
    		}
    		string[] array = new string[7]
    		{
    			((object)c).GetType().Name,
    			" size=",
    			null,
    			null,
    			null,
    			null,
    			null
    		};
    		Bounds bounds = c.bounds;
    		Vector3 val = bounds.size;
    		array[2] = val.ToString("F2");
    		array[3] = " enabled=";
    		array[4] = c.enabled.ToString();
    		array[5] = " trigger=";
    		array[6] = c.isTrigger.ToString();
    		string text = string.Concat(array);
    		BoxCollider val2 = (BoxCollider)(object)((c is BoxCollider) ? c : null);
    		if (val2 != null)
    		{
    			val = val2.center;
    			return text + " centre=" + val.ToString("F2");
    		}
    		SphereCollider val3 = (SphereCollider)(object)((c is SphereCollider) ? c : null);
    		if (val3 != null)
    		{
    			string[] array2 = new string[5] { text, " centre=", null, null, null };
    			val = val3.center;
    			array2[2] = val.ToString("F2");
    			array2[3] = " radius=";
    			array2[4] = val3.radius.ToString("F2");
    			return string.Concat(array2);
    		}
    		CapsuleCollider val4 = (CapsuleCollider)(object)((c is CapsuleCollider) ? c : null);
    		if (val4 != null)
    		{
    			string[] array3 = new string[9] { text, " centre=", null, null, null, null, null, null, null };
    			val = val4.center;
    			array3[2] = val.ToString("F2");
    			array3[3] = " r=";
    			array3[4] = val4.radius.ToString("F2");
    			array3[5] = " h=";
    			array3[6] = val4.height.ToString("F2");
    			array3[7] = " dir=";
    			array3[8] = val4.direction.ToString();
    			return string.Concat(array3);
    		}
    		return text + " centre=not on this collider type";
    	}

    	private static string PathOf(Transform from, Transform to)
    	{
    		if ((Object)(object)from == (Object)(object)to)
    		{
    			return ".";
    		}
    		List<string> list = new List<string>();
    		Transform val = to;
    		while ((Object)(object)val != (Object)null && (Object)(object)val != (Object)(object)from)
    		{
    			list.Add(((Object)val).name);
    			val = val.parent;
    		}
    		if ((Object)(object)val == (Object)null)
    		{
    			return "(not under " + ((Object)from).name + ")";
    		}
    		list.Reverse();
    		return ((Object)from).name + "/" + string.Join("/", list.ToArray());
    	}
    }
}