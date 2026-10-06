using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class Beat5Crawlers
    {
    	private struct Under
    	{
    		public bool HasFloor;

    		public float FloorY;

    		public bool Blocked;

    		public string BlockedBy;

    		public float BlockTopY;
    	}

    	private const string Report = "Temp/beat5_crawlers.txt";

    	private const float OctantGuardMetres = 2f;

    	private const float EntryMarginMetres = 1.5f;

    	private static float YardMinX
    	{
    		get
    		{
    			Beat5Director beat5Director = Object.FindAnyObjectByType<Beat5Director>((FindObjectsInactive)1);
    			if (!((Object)(object)beat5Director != (Object)null))
    			{
    				return 38f;
    			}
    			return beat5Director.EnterX;
    		}
    	}

    	private static float YardMaxX
    	{
    		get
    		{
    			Beat5Director beat5Director = Object.FindAnyObjectByType<Beat5Director>((FindObjectsInactive)1);
    			if (!((Object)(object)beat5Director != (Object)null))
    			{
    				return 51f;
    			}
    			return beat5Director.ExitX;
    		}
    	}

    	[MenuItem("Tools/Echoes/Beat 5 — Three Crawlers", priority = 50)]
    	public static void Run()
    	{
    		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
    		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
    		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0414: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
    		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
    		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
    		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04e1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04e6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0689: Unknown result type (might be due to invalid IL or missing references)
    		//IL_068e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05f7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05fc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0609: Unknown result type (might be due to invalid IL or missing references)
    		//IL_07fa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0809: Unknown result type (might be due to invalid IL or missing references)
    		//IL_09b4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_095e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0963: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] beat 5 — three crawlers");
    		if (EditorApplication.isPlaying)
    		{
    			stringBuilder.AppendLine("  refused: in play mode");
    			Finish(stringBuilder);
    			return;
    		}
    		Transform val = Root("L1_Beat5");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("  FATAL: no root called L1_Beat5.");
    			Finish(stringBuilder);
    			return;
    		}
    		if ((Object)(object)Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1) == (Object)null)
    		{
    			stringBuilder.AppendLine("  FATAL: no AriMover.");
    			Finish(stringBuilder);
    			return;
    		}
    		InkCrawler componentInChildren = ((Component)val).GetComponentInChildren<InkCrawler>(true);
    		if ((Object)(object)componentInChildren == (Object)null)
    		{
    			stringBuilder.AppendLine("  FATAL: L1_Beat5 has no InkCrawler to clone. Run Beat5Setup first — it builds the yard and the walls.");
    			Finish(stringBuilder);
    			return;
    		}
    		string name = ((Object)componentInChildren).name;
    		Vector3 val2 = ((Component)componentInChildren).transform.position;
    		stringBuilder.AppendLine("  prototype   : '" + name + "' at " + val2.ToString("F2"));
    		stringBuilder.AppendLine("  measured    : " + MeasuredHeight(componentInChildren) + " m tall, collider " + DescribeCollider(((Component)componentInChildren).GetComponent<Collider>()));
    		stringBuilder.AppendLine();
    		bool flag = TryMeasureYard(out var yard);
    		string text;
    		if (!flag)
    		{
    			text = "COULD NOT BE MEASURED — no floor under this beat at all, so there is no yard to place into";
    		}
    		else
    		{
    			string[] array = new string[13]
    			{
    				"x ", null, null, null, null, null, null, null, null, null,
    				null, null, null
    			};
    			val2 = yard.min;
    			array[1] = val2.x.ToString("F1");
    			array[2] = " .. ";
    			val2 = yard.max;
    			array[3] = val2.x.ToString("F1");
    			array[4] = "   z ";
    			val2 = yard.min;
    			array[5] = val2.z.ToString("F1");
    			array[6] = " .. ";
    			val2 = yard.max;
    			array[7] = val2.z.ToString("F1");
    			array[8] = "   (";
    			val2 = yard.size;
    			array[9] = val2.x.ToString("F1");
    			array[10] = " x ";
    			val2 = yard.size;
    			array[11] = val2.z.ToString("F1");
    			array[12] = " m of WALKABLE FLOOR, found by raycast)";
    			text = string.Concat(array);
    		}
    		stringBuilder.AppendLine("  yard        : " + text);
    		stringBuilder.AppendLine("               not renderer bounds: an earlier version unioned the renderers under L1_Beat5 and got 13.0 x 4.5 m, which is the size of the props, not of the yard, and it produced a 'too small, widen it' verdict that was measuring paint instead of ground.");
    		stringBuilder.AppendLine();
    		Vector3 val3 = EntryPoint();
    		stringBuilder.AppendLine("  entry       : " + val3.ToString("F2") + "  (x from Beat5Director.EnterX, z from the gate he walks through — not a constant)");
    		if (flag)
    		{
    			bool flag2 = val3.x >= yard.min.x && val3.x <= yard.max.x && val3.z >= yard.min.z && val3.z <= yard.max.z;
    			string[] array2 = new string[15]
    			{
    				"  sanity      : entry is ",
    				flag2 ? "INSIDE the measured yard" : "OUTSIDE the measured yard — so the yard is the wrong piece of ground and every verdict below it is about somewhere else. STOPPING.",
    				"  (entry x ",
    				val3.x.ToString("F1"),
    				" vs yard x ",
    				null,
    				null,
    				null,
    				null,
    				null,
    				null,
    				null,
    				null,
    				null,
    				null
    			};
    			val2 = yard.min;
    			array2[5] = val2.x.ToString("F1");
    			array2[6] = "..";
    			val2 = yard.max;
    			array2[7] = val2.x.ToString("F1");
    			array2[8] = "; entry z ";
    			array2[9] = val3.z.ToString("F1");
    			array2[10] = " vs yard z ";
    			val2 = yard.min;
    			array2[11] = val2.z.ToString("F1");
    			array2[12] = "..";
    			val2 = yard.max;
    			array2[13] = val2.z.ToString("F1");
    			array2[14] = ")";
    			stringBuilder.AppendLine(string.Concat(array2));
    			if (!flag2)
    			{
    				stringBuilder.AppendLine();
    				stringBuilder.AppendLine("  STOPPED. The yard was measured somewhere the player never walks, so a spot search over it would report a shortage that does not exist — which is the failure this tool was rewritten to stop.");
    				stringBuilder.AppendLine();
    				stringBuilder.AppendLine(SaveAfter.Save("nothing — yard measurement refused"));
    				Finish(stringBuilder);
    				return;
    			}
    		}
    		Vector3[] array3 = new Vector3[0];
    		string why = "";
    		if (!flag)
    		{
    			why = "the yard could not be measured, so there is nothing to search inside";
    		}
    		else
    		{
    			array3 = DeriveSpots(yard, componentInChildren, val3, out why);
    		}
    		stringBuilder.AppendLine("  spots       : " + ((array3.Length == 3) ? "searched inside the yard against the measured constraints" : ("COULD NOT SATISFY (" + why + ")")));
    		stringBuilder.AppendLine();
    		if (array3.Length != 3)
    		{
    			stringBuilder.AppendLine("  STOPPED. Placing two crawlers would pass the count check while failing the beat, and placing three at hand-picked points is what produced the wrong verdict last time.");
    			stringBuilder.AppendLine(SaveAfter.Save("NOTHING — placement refused"));
    			Finish(stringBuilder);
    			return;
    		}
    		for (int i = 0; i < array3.Length; i++)
    		{
    			stringBuilder.AppendLine("    spot " + i + ": " + array3[i].ToString("F2"));
    		}
    		List<InkCrawler> list = new List<InkCrawler>(3);
    		string[] array4 = "Beat5_Crawler_A,Beat5_Crawler_B,Beat5_Crawler_C".Split(',', StringSplitOptions.None);
    		Vector3 position = ((Component)componentInChildren).transform.position;
    		((Component)componentInChildren).transform.position = array3[0];
    		if (((Object)componentInChildren).name != array4[0])
    		{
    			string name2 = ((Object)componentInChildren).name;
    			((Object)((Component)componentInChildren).gameObject).name = array4[0];
    			stringBuilder.AppendLine("  " + name2 + " -> adopted as " + array4[0]);
    		}
    		stringBuilder.AppendLine("  " + array4[0] + " moved " + position.ToString("F2") + " -> " + array3[0].ToString("F2") + " (derived, not kept where it was found)");
    		list.Add(componentInChildren);
    		EnsureEmerge(componentInChildren, stringBuilder, array4[0]);
    		for (int j = 1; j < array3.Length; j++)
    		{
    			string text2 = array4[j];
    			Transform val4 = ((Component)val).transform.Find(text2);
    			if ((Object)(object)val4 != (Object)null)
    			{
    				InkCrawler component = ((Component)val4).GetComponent<InkCrawler>();
    				if ((Object)(object)component != (Object)null)
    				{
    					Vector3 position2 = ((Component)component).transform.position;
    					((Component)component).transform.position = array3[j];
    					stringBuilder.AppendLine("  " + text2 + " already here, reused and moved " + position2.ToString("F2") + " -> " + array3[j].ToString("F2"));
    					EnsureEmerge(component, stringBuilder, text2);
    					list.Add(component);
    					continue;
    				}
    			}
    			GameObject val5 = Object.Instantiate<GameObject>(((Component)componentInChildren).gameObject, array3[j], Quaternion.identity, ((Component)val).transform);
    			((Object)val5).name = text2;
    			SkinnedMeshRenderer componentInChildren2 = val5.GetComponentInChildren<SkinnedMeshRenderer>(true);
    			stringBuilder.AppendLine("  " + text2 + " placed at " + array3[j].ToString("F2") + ", skin " + (((Object)(object)componentInChildren2 == (Object)null) ? "MISSING — this one would be invisible" : ("'" + ((Object)componentInChildren2).name + "' mesh " + (((Object)(object)componentInChildren2.sharedMesh == (Object)null) ? "NULL" : "ok"))));
    			InkCrawler component2 = val5.GetComponent<InkCrawler>();
    			EnsureEmerge(component2, stringBuilder, text2);
    			list.Add(component2);
    		}
    		int num = 0;
    		InkCrawler[] array5 = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
    		for (int k = 0; k < array5.Length; k++)
    		{
    			num++;
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("crawlers total in the level: " + num + " (wanted 3" + ((num != 3) ? "  <-- MISMATCH" : "") + ")");
    		for (int l = 0; l < array5.Length; l++)
    		{
    			for (int m = l + 1; m < array5.Length; m++)
    			{
    				float num2 = Vector3.Distance(((Component)array5[l]).transform.position, ((Component)array5[m]).transform.position);
    				if (!(num2 >= 1.5f))
    				{
    					stringBuilder.AppendLine("  OVERLAPPING: " + ((Object)array5[l]).name + " and " + ((Object)array5[m]).name + " are " + num2.ToString("F2") + " m apart, inside one brush swing. One of these is a duplicate and must go — left for you to delete, because deleting an object a beat still references is not this tool's decision to take.");
    				}
    			}
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("=== TELLING THE BEAT ===");
    		Beat5Director beat5Director = Object.FindAnyObjectByType<Beat5Director>((FindObjectsInactive)1);
    		if ((Object)(object)beat5Director == (Object)null)
    		{
    			stringBuilder.AppendLine("  no Beat5Director in the scene, so there is nothing to tell. The crawlers are placed but nothing will start them.");
    		}
    		else
    		{
    			beat5Director.SetCrawlers(list);
    			EditorUtility.SetDirty((Object)(object)beat5Director);
    			stringBuilder.AppendLine("  '" + ((Object)beat5Director).name + "' now holds " + beat5Director.CrawlerCount + " crawler(s):");
    			for (int n = 0; n < beat5Director.CrawlerCount; n++)
    			{
    				string name3 = ((Object)beat5Director.Crawlers[n]).name;
    				val2 = ((Component)beat5Director.Crawlers[n]).transform.position;
    				stringBuilder.AppendLine("    " + name3 + " at " + val2.ToString("F2"));
    			}
    			if (beat5Director.CrawlerCount != 3)
    			{
    				stringBuilder.AppendLine("  <-- the beat is written for three. It will log an error at Awake and never start.");
    			}
    			else
    			{
    				stringBuilder.AppendLine("  read back off the director, not off the variable that was just assigned.");
    			}
    		}
    		Measure(stringBuilder, list, val3);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine(SaveAfter.Save("three crawlers + emerge components + the beat's list of them"));
    		Finish(stringBuilder);
    	}

    	private static void Measure(StringBuilder sb, List<InkCrawler> crawlers, Vector3 entry)
    	{
    		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
    		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03f2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_040e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
    		//IL_041c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0473: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
    		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_049e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== IS THE YARD BIG ENOUGH? ===");
    		sb.AppendLine("(these decide it. Nothing here is a design number.)");
    		if (crawlers.Count != 3)
    		{
    			sb.AppendLine("  NOT MEASURED — " + crawlers.Count + " crawlers were placed, not 3.");
    			return;
    		}
    		float num = 0f;
    		float num2 = 0f;
    		for (int i = 0; i < crawlers.Count; i++)
    		{
    			num = Mathf.Max(num, crawlers[i].SplashRadius);
    			num2 = Mathf.Max(num2, crawlers[i].NoticeRadius);
    		}
    		sb.AppendLine();
    		sb.AppendLine("thresholds, read off the crawlers themselves:");
    		sb.AppendLine("  brush stagger reach : " + num.ToString("F2") + " m   (InkCrawler.SplashRadius)");
    		sb.AppendLine("  notice radius       : " + num2.ToString("F2") + " m   (InkCrawler.NoticeRadius)");
    		sb.AppendLine();
    		sb.AppendLine("spacing between crawlers:");
    		float num3 = float.MaxValue;
    		for (int j = 0; j < crawlers.Count; j++)
    		{
    			for (int k = j + 1; k < crawlers.Count; k++)
    			{
    				float num4 = Vector3.Distance(((Component)crawlers[j]).transform.position, ((Component)crawlers[k]).transform.position);
    				num3 = Mathf.Min(num3, num4);
    				sb.AppendLine("  " + ((Object)crawlers[j]).name + " <-> " + ((Object)crawlers[k]).name + " : " + num4.ToString("F2") + " m");
    			}
    		}
    		sb.AppendLine("  closest pair: " + num3.ToString("F2") + " m. " + ((num3 < num) ? "UNDER one brush swing — staggering one hits both, so the third crawler is free and the yard is NOT big enough" : "over one brush swing, so each costs a separate swing"));
    		sb.AppendLine();
    		sb.AppendLine("from the yard entrance at " + entry.ToString("F2") + ":");
    		float num5 = float.MaxValue;
    		for (int l = 0; l < crawlers.Count; l++)
    		{
    			float num6 = Vector3.Distance(((Component)crawlers[l]).transform.position, entry);
    			num5 = Mathf.Min(num5, num6);
    			sb.AppendLine("  " + ((Object)crawlers[l]).name + " : " + num6.ToString("F2") + " m  (its notice radius is " + crawlers[l].NoticeRadius.ToString("F1") + " m)");
    		}
    		StringBuilder stringBuilder = sb;
    		string text = num5.ToString("F2");
    		string text2;
    		if (num5 < num2)
    		{
    			text2 = "  <- already noticed before he is in the yard";
    		}
    		else
    		{
    			text2 = ((num5 < num2 + 1.5f) ? ("  <- clear of the " + num2.ToString("F1") + " m radius, but only by " + (num5 - num2).ToString("F2") + " m, and the margin wanted is " + 1.5f.ToString("F1") + " m. He is barely asleep when he arrives.") : ("  <- all still dormant when he walks in, by " + (num5 - num2).ToString("F2") + " m over the radius (margin wanted: " + 1.5f.ToString("F1") + " m)"));
    		}
    		stringBuilder.AppendLine("  nearest crawler at entry: " + text + " m" + text2);
    		sb.AppendLine();
    		Vector3 val = Vector3.zero;
    		for (int m = 0; m < crawlers.Count; m++)
    		{
    			val += ((Component)crawlers[m]).transform.position;
    		}
    		val /= (float)crawlers.Count;
    		sb.AppendLine("encirclement — bearings from the crawlers' own centre (" + val.ToString("F2") + ") to each:");
    		sb.AppendLine("  (not from Ari: where he will be standing when they close is unknown at edit time, and the version that measured it from him spawn reported the yard's layout as his starting position)");
    		SortedSet<int> sortedSet = new SortedSet<int>();
    		List<float> list = new List<float>(3);
    		int num7 = 0;
    		for (int n = 0; n < crawlers.Count; n++)
    		{
    			Vector3 val2 = ((Component)crawlers[n]).transform.position - val;
    			val2.y = 0f;
    			if (!(val2.sqrMagnitude < 0.0001f))
    			{
    				float num8 = Mathf.Atan2(val2.x, val2.z) * 57.29578f;
    				if (num8 < 0f)
    				{
    					num8 += 360f;
    				}
    				if (val2.magnitude < 2f)
    				{
    					sb.AppendLine("  " + ((Object)crawlers[n]).name + " : " + val2.magnitude.ToString("F2") + " m from the centre — INSIDE the " + 2f.ToString("0.0") + " m guard, so it has no bearing and is not counted. It is standing in the middle of the other two.");
    					num7++;
    				}
    				else
    				{
    					int item = (int)Mathf.Floor((num8 + 22.5f) / 45f) % 8;
    					sortedSet.Add(item);
    					list.Add(num8);
    					sb.AppendLine("  " + ((Object)crawlers[n]).name + " : " + num8.ToString("F0") + " deg, " + val2.magnitude.ToString("F2") + " m out, octant " + item);
    				}
    			}
    		}
    		float num9 = 360f;
    		list.Sort();
    		for (int num10 = 0; num10 < list.Count; num10++)
    		{
    			float num11 = list[num10];
    			float num12 = list[(num10 + 1) % list.Count];
    			float num13 = ((list.Count < 2) ? 360f : (num12 - num11));
    			if (num13 < 0f)
    			{
    				num13 += 360f;
    			}
    			num9 = Mathf.Min(num9, num13);
    		}
    		sb.AppendLine("  distinct octants: " + sortedSet.Count + " of 3" + ((num7 > 0) ? ("  (with " + num7 + " on the centre and not counted)") : "") + "   <- a report, not the test");
    		sb.AppendLine("  smallest gap between two bearings: " + num9.ToString("F0") + " deg  " + ((num9 >= 90f) ? "<- all three point genuinely different ways" : "<- two of them point the same way however many octants they landed in. This is a line, not a pincer. NEEDS 90"));
    		float num14 = (((Object)(object)crawlers[0] != (Object)null) ? (crawlers[0].LungeRange * 3f) : 6f);
    		sb.AppendLine("  spacing      : closest pair " + num3.ToString("F2") + " m, target band around " + num14.ToString("F2") + " m (three lunge ranges — far enough that one swing cannot hit two, near enough that two can be on him at once)");
    		bool flag = num3 > num14 * 2f;
    		if (flag)
    		{
    			sb.AppendLine("  <-- the pair is more than twice the band. They will be met one at a time, which is the duel this beat exists to avoid.");
    		}
    		bool flag2 = num3 < num || num5 < num2 + 1.5f || num9 < 90f;
    		sb.AppendLine();
    		StringBuilder stringBuilder2 = sb;
    		string text3;
    		if (flag2)
    		{
    			text3 = "the yard as it stands does NOT fit three crawlers readably. Specifically: " + ((num5 < num2 + 1.5f) ? ("the nearest crawler is " + num5.ToString("F2") + " m from the entrance, inside the " + (num2 + 1.5f).ToString("F1") + " m it needs (" + num2.ToString("F1") + " m notice radius plus a " + 1.5f.ToString("F1") + " m margin), so he is spotted before he is in the yard. ") : "") + ((num3 < num) ? ("the closest pair is " + num3.ToString("F2") + " m apart, inside one " + num.ToString("F1") + " m brush swing, so one swing staggers two at once. ") : "") + ((num9 < 90f) ? ("two of the three point " + num9.ToString("F0") + " degrees apart from the trio's own centre, which is a line and not an encirclement.") : "") + "Widening the yard is the remedy only for the first two. For the angle, re-run this tool to re-derive the positions.";
    		}
    		else
    		{
    			text3 = (flag ? "the yard fits three crawlers, but this placement spreads them too far to be a pincer. Re-run this tool to re-derive the positions; the yard itself needs no change." : "the yard as it stands fits three crawlers readably. No dimension changes, so all 44 existing checks still hold.");
    		}
    		stringBuilder2.AppendLine("VERDICT: " + text3);
    		if (!flag2 && !flag)
    		{
    			sb.AppendLine("  This is the finding that reverses the earlier 'widen to 28 m' plan. It was decided before anything was measured.");
    		}
    	}

    	private static bool TryMeasureYard(out Bounds yard)
    	{
    		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
    		yard = WalkableBounds(out var walkableCells);
    		return walkableCells > 20;
    	}

    	private static Bounds WalkableBounds(out int walkableCells)
    	{
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
    		walkableCells = 0;
    		Bounds result = default;
    		float num = YardMinX - 6f;
    		float num2 = YardMaxX + 6f;
    		float num3 = EntryPoint().z - 18f;
    		float num4 = EntryPoint().z + 18f;
    		bool flag = false;
    		for (float num5 = num; num5 <= num2; num5 += 0.5f)
    		{
    			for (float num6 = num3; num6 <= num4; num6 += 0.5f)
    			{
    				Vector3 val = new Vector3(num5, 0f, num6);
    				if (!IsWall(val) && HasFloorUnder(val))
    				{
    					walkableCells++;
    					if (!flag)
    					{
    						result = new Bounds(val, Vector3.zero);
    						flag = true;
    					}
    					else
    					{
    						result.Encapsulate(val);
    					}
    				}
    			}
    		}
    		if (flag)
    		{
    			result.Encapsulate(result.center + new Vector3(0.5f, 0f, 0.5f));
    		}
    		return result;
    	}

    	private static Under Probe(Vector3 at)
    	{
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
    		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
    		Under result = new Under
    		{
    			FloorY = 0f,
    			BlockedBy = ""
    		};
    		RaycastHit[] array = Physics.RaycastAll(at + Vector3.up * 30f, Vector3.down, 60f, -1, (QueryTriggerInteraction)1);
    		Collider val = null;
    		float num = 0f;
    		Bounds bounds;
    		for (int i = 0; i < array.Length; i++)
    		{
    			Collider collider = array[i].collider;
    			if (!((Object)(object)collider == (Object)null) && !((Object)(object)((Component)collider).GetComponentInParent<InkCrawler>() != (Object)null) && !((Object)(object)((Component)collider).GetComponentInParent<AriMover>() != (Object)null))
    			{
    				bounds = collider.bounds;
    				Vector3 size = bounds.size;
    				float num2 = size.x * size.z;
    				if ((Object)(object)val == (Object)null || num2 > num)
    				{
    					val = collider;
    					num = num2;
    				}
    			}
    		}
    		if ((Object)(object)val == (Object)null)
    		{
    			return result;
    		}
    		result.HasFloor = true;
    		bounds = val.bounds;
    		result.FloorY = bounds.max.y;
    		for (int j = 0; j < array.Length; j++)
    		{
    			Collider collider2 = array[j].collider;
    			if ((Object)(object)collider2 == (Object)null || (Object)(object)collider2 == (Object)(object)val || (Object)(object)((Component)collider2).GetComponentInParent<InkCrawler>() != (Object)null || (Object)(object)((Component)collider2).GetComponentInParent<AriMover>() != (Object)null)
    			{
    				continue;
    			}
    			bounds = collider2.bounds;
    			float y = bounds.max.y;
    			if (!(y > result.FloorY + 0.9f) && !(y <= result.FloorY + 0.05f))
    			{
    				bounds = collider2.bounds;
    				if (!(bounds.size.y <= 0.6f))
    				{
    					result.Blocked = true;
    					string[] array2 = new string[6]
    					{
    						((Object)collider2).name,
    						" (",
    						null,
    						null,
    						null,
    						null
    					};
    					bounds = collider2.bounds;
    					Vector3 size2 = bounds.size;
    					array2[2] = size2.ToString("F2");
    					array2[3] = ", top at y ";
    					array2[4] = y.ToString("F2");
    					array2[5] = ")";
    					result.BlockedBy = string.Concat(array2);
    					result.BlockTopY = y;
    					break;
    				}
    			}
    		}
    		return result;
    	}

    	private static float FloorHeightAt(float x, float z)
    	{
    		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    		return Probe(new Vector3(x, 0f, z)).FloorY;
    	}

    	private static bool HasFloorUnder(Vector3 at)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		return Probe(at).HasFloor;
    	}

    	private static bool IsWall(Vector3 at)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		return Probe(at).Blocked;
    	}

    	private static Vector3 EntryPoint()
    	{
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
    		Beat5Director beat5Director = Object.FindAnyObjectByType<Beat5Director>((FindObjectsInactive)1);
    		float num = (((Object)(object)beat5Director != (Object)null) ? beat5Director.EnterX : 38f);
    		float num2 = float.NaN;
    		string text = "NOTHING — defaulted to 0, so every distance below is measured from the wrong point";
    		BeatGate beatGate = Object.FindAnyObjectByType<BeatGate>((FindObjectsInactive)1);
    		if ((Object)(object)beatGate != (Object)null)
    		{
    			num2 = ((Component)beatGate).transform.position.z;
    			text = "the gate";
    		}
    		else
    		{
    			LevelCheckpoint levelCheckpoint = Object.FindAnyObjectByType<LevelCheckpoint>((FindObjectsInactive)1);
    			if ((Object)(object)levelCheckpoint != (Object)null)
    			{
    				num2 = ((Component)levelCheckpoint).transform.position.z;
    				text = "the checkpoint";
    			}
    		}
    		if (float.IsNaN(num2))
    		{
    			num2 = 0f;
    		}
    		else
    		{
    			Debug.Log((object)("[Echoes] yard entry z " + num2.ToString("F2") + " read off " + text));
    		}
    		return new Vector3(num, 0f, num2);
    	}

    	private static Vector3[] DeriveSpots(Bounds yard, InkCrawler proto, Vector3 entry, out string why)
    	{
    		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
    		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
    		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
    		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
    		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
    		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
    		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
    		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
    		why = "";
    		float splashRadius = proto.SplashRadius;
    		float noticeRadius = proto.NoticeRadius;
    		float num = proto.BodyHeight * 0.5f + 0.5f;
    		List<Vector3> list = new List<Vector3>();
    		for (float num2 = yard.min.x + num; num2 <= yard.max.x - num; num2 += 0.5f)
    		{
    			for (float num3 = yard.min.z + num; num3 <= yard.max.z - num; num3 += 0.5f)
    			{
    				Vector3 val = new Vector3(num2, FloorHeightAt(num2, num3), num3);
    				if (!(Vector3.Distance(val, entry) < noticeRadius + 1.5f))
    				{
    					list.Add(val);
    				}
    			}
    		}
    		if (list.Count < 3)
    		{
    			why = "only " + list.Count + " spot(s) inside the yard are further than " + (noticeRadius + 1.5f).ToString("F1") + " m from the entrance — that is the " + noticeRadius.ToString("F1") + " m notice radius plus a " + 1.5f.ToString("F1") + " m margin, because passing by one centimetre is not passing at all. Either the yard is shorter than that total or the entrance sits inside it. Both are a layout fact, not something to place around.";
    			return new Vector3[0];
    		}
    		list.Sort((Vector3 a, Vector3 b) =>
    		{
    			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
    			return Vector3.Distance(b, entry).CompareTo(Vector3.Distance(a, entry));
    		});
    		int num4 = Mathf.Min(list.Count, 14);
    		Vector3[] array = new Vector3[num4];
    		for (int num5 = 0; num5 < num4; num5++)
    		{
    			array[num5] = list[(num4 != 1) ? Mathf.RoundToInt((float)num5 / (float)(num4 - 1) * (float)(list.Count - 1)) : 0];
    		}
    		Vector3[] array2 = null;
    		float num6 = float.MinValue;
    		for (int num7 = 0; num7 < num4; num7++)
    		{
    			for (int num8 = num7 + 1; num8 < num4; num8++)
    			{
    				for (int num9 = num8 + 1; num9 < num4; num9++)
    				{
    					Vector3[] array3 = new Vector3[3]
    					{
    						array[num7],
    						array[num8],
    						array[num9]
    					};
    					float num10 = Vector3.Distance(array3[0], array3[1]);
    					float num11 = Vector3.Distance(array3[0], array3[2]);
    					float num12 = Vector3.Distance(array3[1], array3[2]);
    					float num13 = Mathf.Min(num10, Mathf.Min(num11, num12));
    					if (num13 <= splashRadius)
    					{
    						continue;
    					}
    					Vector3 val2 = (array3[0] + array3[1] + array3[2]) / 3f;
    					SortedSet<int> sortedSet = new SortedSet<int>();
    					float num14 = 0f;
    					float[] array4 = new float[3];
    					float num15 = float.MaxValue;
    					for (int num16 = 0; num16 < 3; num16++)
    					{
    						Vector3 val3 = array3[num16] - val2;
    						val3.y = 0f;
    						Vector2 val4 = new Vector2(val3.x, val3.z);
    						float magnitude = val4.magnitude;
    						num15 = Mathf.Min(num15, magnitude);
    						if (!(magnitude < 2f))
    						{
    							float num17 = Mathf.Atan2(val3.x, val3.z) * 57.29578f;
    							if (num17 < 0f)
    							{
    								num17 += 360f;
    							}
    							array4[num16] = num17;
    							sortedSet.Add((int)Mathf.Floor((num17 + 22.5f) / 45f) % 8);
    						}
    					}
    					if (sortedSet.Count < 3)
    					{
    						continue;
    					}
    					float num18 = 360f;
    					for (int num19 = 0; num19 < 3; num19++)
    					{
    						for (int num20 = num19 + 1; num20 < 3; num20++)
    						{
    							if (!(array4[num19] < 0f) && !(array4[num20] < 0f))
    							{
    								float num21 = Mathf.Abs(array4[num19] - array4[num20]);
    								if (num21 > 180f)
    								{
    									num21 = 360f - num21;
    								}
    								num18 = Mathf.Min(num18, num21);
    								num14 = Mathf.Max(num14, num21);
    							}
    						}
    					}
    					if (!(num18 < 90f))
    					{
    						float num22 = proto.LungeRange * 3f;
    						float num23 = 2f - Mathf.Abs(num13 - num22) / num22 + num14 / 180f;
    						if (!(num23 <= num6))
    						{
    							num6 = num23;
    							array2 = array3;
    						}
    					}
    				}
    			}
    		}
    		if (array2 == null)
    		{
    			why = "no triple inside the yard is " + splashRadius.ToString("F1") + " m apart (one swing each) AND spread so that no two of the three bear the same way from their own centre (90 degrees apart). The yard can satisfy one of those or the other, not both.";
    			return new Vector3[0];
    		}
    		Array.Sort(array2, (Vector3 a, Vector3 b) =>
    		{
    			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    			return a.z.CompareTo(b.z);
    		});
    		why = "best of " + num4 * (num4 - 1) * (num4 - 2) / 6 + " triples, from " + num4 + " candidates sampled evenly across the whole yard: closest pair " + Score(array2, splashRadius) + " m (target band " + (proto.LungeRange * 3f).ToString("F2") + " m), no two bearings within 90 degrees";
    		return array2;
    	}

    	private static string Score(Vector3[] t, float reach)
    	{
    		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		float num = Vector3.Distance(t[0], t[1]);
    		num = Mathf.Min(num, Vector3.Distance(t[0], t[2]));
    		num = Mathf.Min(num, Vector3.Distance(t[1], t[2]));
    		if (!(num > reach))
    		{
    			return "none over " + reach.ToString("F2");
    		}
    		return num.ToString("F2");
    	}

    	private static void EnsureEmerge(InkCrawler c, StringBuilder sb, string label)
    	{
    		InkCrawlerEmerge component = ((Component)c).GetComponent<InkCrawlerEmerge>();
    		if ((Object)(object)component == (Object)null)
    		{
    			component = ((Component)c).gameObject.AddComponent<InkCrawlerEmerge>();
    			sb.AppendLine("  " + label + " + InkCrawlerEmerge (rises " + component.EmergeSeconds.ToString("0.0") + " s, sink depth measured off the SKIN)");
    		}
    		else
    		{
    			sb.AppendLine("  " + label + " already has InkCrawlerEmerge");
    		}
    	}

    	private static string MeasuredHeight(InkCrawler c)
    	{
    		SkinnedMeshRenderer componentInChildren = ((Component)c).GetComponentInChildren<SkinnedMeshRenderer>(true);
    		if ((Object)(object)componentInChildren == (Object)null)
    		{
    			return "no skin — unknown";
    		}
    		float num = SkinHeight.Measure((Renderer)(object)componentInChildren, out var _, out var why);
    		if (!(num > 0.0001f))
    		{
    			return "unmeasurable (" + why + ")";
    		}
    		return num.ToString("F3");
    	}

    	private static string DescribeCollider(Collider c)
    	{
    		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)c == (Object)null)
    		{
    			return "NONE";
    		}
    		CapsuleCollider val = (CapsuleCollider)(object)((c is CapsuleCollider) ? c : null);
    		Vector3 val2;
    		if (val != null)
    		{
    			string[] array = new string[6]
    			{
    				"capsule r ",
    				val.radius.ToString("F2"),
    				" h ",
    				val.height.ToString("F2"),
    				" centre ",
    				null
    			};
    			val2 = val.center;
    			array[5] = val2.ToString("F2");
    			return string.Concat(array);
    		}
    		string name = ((object)c).GetType().Name;
    		Bounds bounds = c.bounds;
    		val2 = bounds.size;
    		return name + " bounds " + val2.ToString("F2");
    	}

    	private static string Bounds(Transform t)
    	{
    		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
    		Bounds val = new Bounds(t.position, Vector3.zero);
    		Renderer[] componentsInChildren = ((Component)t).GetComponentsInChildren<Renderer>(true);
    		Vector3 val2;
    		if (componentsInChildren.Length == 0)
    		{
    			val2 = t.position;
    			return "at " + val2.ToString("F2") + " but has NO renderer, so its size cannot be measured";
    		}
    		for (int i = 0; i < componentsInChildren.Length; i++)
    		{
    			val.Encapsulate(componentsInChildren[i].bounds);
    		}
    		string[] array = new string[13]
    		{
    			"x ", null, null, null, null, null, null, null, null, null,
    			null, null, null
    		};
    		val2 = val.min;
    		array[1] = val2.x.ToString("F1");
    		array[2] = " .. ";
    		val2 = val.max;
    		array[3] = val2.x.ToString("F1");
    		array[4] = "   z ";
    		val2 = val.min;
    		array[5] = val2.z.ToString("F1");
    		array[6] = " .. ";
    		val2 = val.max;
    		array[7] = val2.z.ToString("F1");
    		array[8] = "   (";
    		val2 = val.size;
    		array[9] = val2.x.ToString("F1");
    		array[10] = " x ";
    		val2 = val.size;
    		array[11] = val2.z.ToString("F1");
    		array[12] = " m)";
    		return string.Concat(array);
    	}

    	private static Transform Root(string n)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		Scene activeScene = SceneManager.GetActiveScene();
    		GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
    		for (int i = 0; i < rootGameObjects.Length; i++)
    		{
    			if (((Object)rootGameObjects[i]).name == n)
    			{
    				return rootGameObjects[i].transform;
    			}
    		}
    		return null;
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/beat5_crawlers.txt"), sb.ToString());
    		Debug.Log((object)"[Echoes] beat 5 crawlers — see Temp/beat5_crawlers.txt");
    	}
    }
}