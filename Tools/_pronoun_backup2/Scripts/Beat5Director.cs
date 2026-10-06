using System;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Beat 5 Director")]
    public sealed class Beat5Director : MonoBehaviour, IResettable
    {
    	public enum Phase
    	{
    		Waiting,
    		Rising,
    		Engaging,
    		Done
    	}

    	[Header("Parts")]
    	[Tooltip("The crawlers. Found under this beat if left empty. A beat with one entry is a duel, not this beat — the number is reported by the setup tool either way.")]
    	[SerializeField]
    	private List<InkCrawler> crawlers = new List<InkCrawler>(3);

    	[Tooltip("Ari's brush. Found on Ari if left empty.")]
    	[SerializeField]
    	private BrushPainter brush;

    	[Tooltip("Mono, for the commentary. He is awake by now.")]
    	[SerializeField]
    	private MonoCompanion mono;

    	[Header("The beat")]
    	[Tooltip("Start the beat the moment Mono opens his eyes, wherever he is standing. This is the trigger the brief asks for, and it is on by default: an entrance tied to a place is an entrance he can walk past without ever seeing.")]
    	[SerializeField]
    	private bool huntOnMonoWake = true;

    	[Tooltip("Fallback only, used when there is no Mono in the level to wake. He has entered the yard past this x.")]
    	[SerializeField]
    	private float enterX = 38f;

    	[Tooltip("He has finished the beat past this x.")]
    	[SerializeField]
    	private float exitX = 51f;

    	[Tooltip("Staggers in total before Mono concedes that the brush works. One teaches that it happened; two teaches that it is the answer. He says nothing after that.")]
    	[Min(1f)]
    	[SerializeField]
    	private int staggersToExplain = 2;

    	[Header("The entrance")]
    	[Tooltip("Rise them this many seconds apart. Simultaneous is a wall coming up; staggered is three things arriving, which is legible.")]
    	[Min(0f)]
    	[SerializeField]
    	private float emergeStagger = 0.35f;

    	[Tooltip("Wait this long after he crosses enterX before the first one rises. Long enough for him to see an empty yard and start walking into it — the beat's premise is that it looks safe.")]
    	[Min(0f)]
    	[SerializeField]
    	private float emergeDelay = 1.2f;

    	[SerializeField]
    	private bool log = true;

    	private const float YardMargin = 12f;

    	private Vector3 _entry;

    	private bool _saidStagger;

    	private bool _saidNoKill;

    	private bool _saidLunge;

    	private bool _saidUnseen;

    	private bool _beganOnWake;

    	private BrushPainter _subscribed;

    	public Phase Now { get; private set; }

    	public InkCrawler Crawler
    	{
    		get
    		{
    			if (crawlers == null || crawlers.Count <= 0)
    			{
    				return null;
    			}
    			return crawlers[0];
    		}
    	}

    	public int CrawlerCount
    	{
    		get
    		{
    			if (crawlers != null)
    			{
    				return crawlers.Count;
    			}
    			return 0;
    		}
    	}

    	public IReadOnlyList<InkCrawler> Crawlers => crawlers;

    	public float EnterX => enterX;

    	public float ExitX => exitX;

    	public int Staggers => Sum((InkCrawler c) => c.Staggers);

    	public int Lunges => Sum((InkCrawler c) => c.Lunges);

    	public bool EverNoticed => Any((InkCrawler c) => c.EverNoticed);

    	public bool BeatStarted { get; private set; }

    	public float Seconds { get; private set; }

    	public int Emerged
    	{
    		get
    		{
    			int num = 0;
    			if (crawlers == null)
    			{
    				return num;
    			}
    			for (int i = 0; i < crawlers.Count; i++)
    			{
    				InkCrawlerEmerge inkCrawlerEmerge = (((Object)(object)crawlers[i] != (Object)null) ? ((Component)crawlers[i]).GetComponent<InkCrawlerEmerge>() : null);
    				if ((Object)(object)inkCrawlerEmerge != (Object)null && inkCrawlerEmerge.IsReady)
    				{
    					num++;
    				}
    			}
    			return num;
    		}
    	}

    	public string Route
    	{
    		get
    		{
    			if (Now != Phase.Done)
    			{
    				if (!crawlerRetired())
    				{
    					if (!EverNoticed)
    					{
    						return "in progress, unseen";
    					}
    					return "in progress, they have seen him";
    				}
    				return "in progress, they have given him up";
    			}
    			int staggers = Staggers;
    			int lunges = Lunges;
    			if (staggers == 0 && lunges == 0)
    			{
    				return "stealth — he was never in a fight";
    			}
    			if (lunges == 0)
    			{
    				return "brush — " + staggers + " stagger(s), never touched";
    			}
    			return "brush — " + staggers + " stagger(s), " + lunges + " lunge(s) taken";
    		}
    	}

    	public bool WentUnfought
    	{
    		get
    		{
    			if (Now == Phase.Done && Staggers == 0)
    			{
    				return Lunges == 0;
    			}
    			return false;
    		}
    	}

    	public bool BeganOnWake => _beganOnWake;

    	public void SetCrawlers(IEnumerable<InkCrawler> set)
    	{
    		crawlers = new List<InkCrawler>(3);
    		if (set == null)
    		{
    			return;
    		}
    		foreach (InkCrawler item in set)
    		{
    			if (!((Object)(object)item == (Object)null) && !crawlers.Contains(item))
    			{
    				crawlers.Add(item);
    			}
    		}
    	}

    	private int Sum(Func<InkCrawler, int> read)
    	{
    		int num = 0;
    		if (crawlers == null)
    		{
    			return num;
    		}
    		for (int i = 0; i < crawlers.Count; i++)
    		{
    			if ((Object)(object)crawlers[i] != (Object)null)
    			{
    				num += read(crawlers[i]);
    			}
    		}
    		return num;
    	}

    	private bool Any(Func<InkCrawler, bool> read)
    	{
    		if (crawlers == null)
    		{
    			return false;
    		}
    		for (int i = 0; i < crawlers.Count; i++)
    		{
    			if ((Object)(object)crawlers[i] != (Object)null && read(crawlers[i]))
    			{
    				return true;
    			}
    		}
    		return false;
    	}

    	private bool crawlerRetired()
    	{
    		if (crawlers == null)
    		{
    			return false;
    		}
    		for (int i = 0; i < crawlers.Count; i++)
    		{
    			if ((Object)(object)crawlers[i] != (Object)null && crawlers[i].Retired)
    			{
    				return true;
    			}
    		}
    		return false;
    	}

    	private void Awake()
    	{
    		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
    		if (crawlers == null)
    		{
    			crawlers = new List<InkCrawler>(3);
    		}
    		if (crawlers.Count == 0)
    		{
    			InkCrawler[] componentsInChildren = ((Component)this).GetComponentsInChildren<InkCrawler>(true);
    			for (int i = 0; i < componentsInChildren.Length; i++)
    			{
    				crawlers.Add(componentsInChildren[i]);
    			}
    		}
    		if (crawlers.Count == 0)
    		{
    			InkCrawler[] array = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
    			for (int j = 0; j < array.Length; j++)
    			{
    				float x = ((Component)array[j]).transform.position.x;
    				if (!(x < enterX - 12f) && !(x > exitX + 12f))
    				{
    					crawlers.Add(array[j]);
    				}
    			}
    			if (crawlers.Count > 0)
    			{
    				Debug.LogWarning((object)("[Echoes] beat 5: nothing is a child of " + ((Object)this).name + ", so the crawlers in the yard (x " + (enterX - 12f).ToString("0") + " to " + (exitX + 12f).ToString("0") + ") were adopted by position instead. Run Tools/Echoes/Beat 5 - Three Crawlers so the list is set explicitly."), (Object)(object)this);
    			}
    		}
    		List<InkCrawler> list = new List<InkCrawler>(crawlers.Count);
    		for (int k = 0; k < crawlers.Count; k++)
    		{
    			InkCrawler inkCrawler = crawlers[k];
    			if ((Object)(object)inkCrawler == (Object)null)
    			{
    				continue;
    			}
    			if (list.Contains(inkCrawler))
    			{
    				if (log)
    				{
    					Debug.LogWarning((object)("[Echoes] beat 5: '" + ((Object)inkCrawler).name + "' was in the list twice; using it once"), (Object)(object)this);
    				}
    			}
    			else
    			{
    				list.Add(inkCrawler);
    			}
    		}
    		if (list.Count != crawlers.Count)
    		{
    			if (log && list.Count != crawlers.Count)
    			{
    				Debug.Log((object)("[Echoes] beat 5: " + crawlers.Count + " listed, " + list.Count + " usable"), (Object)(object)this);
    			}
    			crawlers = list;
    		}
    		if ((Object)(object)mono == (Object)null)
    		{
    			mono = MonoCompanion.FindInLevel();
    		}
    		if (crawlers.Count == 0)
    		{
    			Debug.LogError((object)("[Echoes] beat 5 has NO crawlers. It will never start, they will never rise, and nothing in the yard will attack. Run Tools/Echoes/Beat 5 - Three Crawlers, and check that the three InkCrawler components are between x " + (enterX - 12f).ToString("0") + " and " + (exitX + 12f).ToString("0") + "."), (Object)(object)this);
    		}
    		else if (log)
    		{
    			Debug.Log((object)("[Echoes] beat 5 has " + crawlers.Count + " crawler(s) — this beat is written for three"), (Object)(object)this);
    		}
    	}

    	private void OnEnable()
    	{
    		Subscribe();
    	}

    	private void OnDisable()
    	{
    		Unsubscribe();
    	}

    	private void Subscribe()
    	{
    		if ((Object)(object)brush == (Object)null)
    		{
    			AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    			if ((Object)(object)ariMover != (Object)null)
    			{
    				brush = ((Component)ariMover).GetComponent<BrushPainter>();
    			}
    		}
    		if (!((Object)(object)brush == (Object)null) && !((Object)(object)_subscribed == (Object)(object)brush))
    		{
    			brush.Stroked += OnStroke;
    			_subscribed = brush;
    		}
    	}

    	private void Unsubscribe()
    	{
    		if (!((Object)(object)_subscribed == (Object)null))
    		{
    			_subscribed.Stroked -= OnStroke;
    			_subscribed = null;
    		}
    	}

    	private void Update()
    	{
    		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
    		if (Now == Phase.Done)
    		{
    			return;
    		}
    		Seconds += Time.deltaTime;
    		AriMover ariMover = AriNow();
    		if ((Object)(object)ariMover == (Object)null)
    		{
    			return;
    		}
    		float x = ((Component)ariMover).transform.position.x;
    		if (!BeatStarted)
    		{
    			bool flag = huntOnMonoWake && (Object)(object)mono != (Object)null && mono.IsAwake;
    			bool flag2 = !huntOnMonoWake && x >= enterX;
    			if (!flag && !flag2)
    			{
    				return;
    			}
    			BeatStarted = true;
    			_entry = ((Component)ariMover).transform.position;
    			_beganOnWake = flag;
    			Now = Phase.Rising;
    			BeginEntrance();
    			if (log)
    			{
    				Debug.Log((object)("[Echoes] beat 5 begun at " + _entry.ToString("F2") + (flag ? " — Mono is awake, so they are coming up now" : (" — he reached x " + x.ToString("0.0"))) + "; the yard looks empty for " + emergeDelay.ToString("0.0") + " s, then they come up"), (Object)(object)this);
    			}
    		}
    		if (Now == Phase.Rising && Emerged >= CrawlerCount)
    		{
    			Now = Phase.Engaging;
    		}
    		if (x >= exitX)
    		{
    			Now = Phase.Done;
    			Say(mono, "beat5.done");
    			if (log)
    			{
    				Debug.Log((object)("[Echoes] beat 5 done after " + Seconds.ToString("0.0") + " s — " + Route), (Object)(object)this);
    			}
    		}
    		else
    		{
    			Commentary();
    		}
    	}

    	private void BeginEntrance()
    	{
    		if (crawlers == null || crawlers.Count == 0)
    		{
    			return;
    		}
    		for (int i = 0; i < crawlers.Count; i++)
    		{
    			if ((Object)(object)crawlers[i] == (Object)null)
    			{
    				continue;
    			}
    			InkCrawlerEmerge component = ((Component)crawlers[i]).GetComponent<InkCrawlerEmerge>();
    			if ((Object)(object)component == (Object)null)
    			{
    				Debug.LogWarning((object)("[Echoes] beat 5: '" + ((Object)crawlers[i]).name + "' has no InkCrawlerEmerge, so it cannot come out of the ground. It will be visible from the moment he walks in."), (Object)(object)crawlers[i]);
    				continue;
    			}
    			InkCrawler mine = crawlers[i];
    			InkCrawlerEmerge mineEmerge = component;
    			LevelRunner.RunAfter(emergeDelay + emergeStagger * (float)i, () =>
    			{
    				RiseOne(mine, mineEmerge);
    			});
    		}
    	}

    	private void RiseOne(InkCrawler c, InkCrawlerEmerge e)
    	{
    		if ((Object)(object)c == (Object)null || (Object)(object)e == (Object)null)
    		{
    			return;
    		}
    		if (e.Now != InkCrawlerEmerge.Phase.Up)
    		{
    			Debug.LogWarning((object)("[Echoes] beat 5: '" + ((Object)c).name + "' was due to rise and finish hunting but its emerge phase is " + e.Now.ToString() + ", not Up. It will be told to hunt late, or not at all."), (Object)(object)c);
    			return;
    		}
    		c.Aggro();
    		if (log)
    		{
    			Debug.Log((object)("[Echoes] beat 5: '" + ((Object)c).name + "' is up and hunting, " + c.MeasuredSpeed.ToString("0.00") + " m/s so far, " + c.AnimatorSpeed.ToString("0.00") + " m/s on the Animator"), (Object)(object)c);
    		}
    	}

    	private void Commentary()
    	{
    		SayOnce(Lunges > 0 && anyReachedHer(), ref _saidLunge, "beat5.hits_back");
    		SayOnce(crawlerRetired(), ref _saidUnseen, "beat5.unseen");
    	}

    	private bool anyReachedHer()
    	{
    		if (crawlers == null)
    		{
    			return false;
    		}
    		for (int i = 0; i < crawlers.Count; i++)
    		{
    			if ((Object)(object)crawlers[i] != (Object)null && crawlers[i].LastPushMetres > 0f)
    			{
    				return true;
    			}
    		}
    		return false;
    	}

    	private AriMover AriNow()
    	{
    		return Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    	}

    	private void OnStroke(Vector3 point, int painted)
    	{
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		if (Now == Phase.Done || crawlers == null)
    		{
    			return;
    		}
    		AriMover ariMover = AriNow();
    		if ((Object)(object)ariMover == (Object)null)
    		{
    			return;
    		}
    		int num = 0;
    		for (int i = 0; i < crawlers.Count; i++)
    		{
    			if (!((Object)(object)crawlers[i] == (Object)null) && crawlers[i].Splash(point, ((Component)ariMover).transform.position))
    			{
    				num++;
    			}
    		}
    		if (num != 0)
    		{
    			if (!_saidStagger && Staggers >= Mathf.Max(1, staggersToExplain))
    			{
    				_saidStagger = true;
    				Say(mono, "beat5.push");
    			}
    			if (!_saidNoKill && Staggers >= 1)
    			{
    				_saidNoKill = true;
    				Say(mono, "beat5.nokill");
    			}
    		}
    	}

    	private void SayOnce(bool when, ref bool said, string line)
    	{
    		if (!(!when | said))
    		{
    			said = true;
    			Say(mono, line);
    		}
    	}

    	private static void Say(MonoCompanion speaker, string id)
    	{
    		if ((Object)(object)speaker != (Object)null && !string.IsNullOrEmpty(id))
    		{
    			speaker.SayBeat(id);
    		}
    	}

    	public void ResetForCheckpoint()
    	{
    		ResetBeat();
    	}

    	public void ResetBeat()
    	{
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		Now = Phase.Waiting;
    		BeatStarted = false;
    		Seconds = 0f;
    		_entry = Vector3.zero;
    		_saidStagger = (_saidNoKill = (_saidLunge = (_saidUnseen = false)));
    		if (crawlers == null)
    		{
    			return;
    		}
    		for (int i = 0; i < crawlers.Count; i++)
    		{
    			InkCrawler inkCrawler = crawlers[i];
    			if (!((Object)(object)inkCrawler == (Object)null))
    			{
    				inkCrawler.ResetCrawler();
    				InkCrawlerEmerge component = ((Component)inkCrawler).GetComponent<InkCrawlerEmerge>();
    				if ((Object)(object)component != (Object)null)
    				{
    					component.ResetForCheckpoint();
    				}
    			}
    		}
    		LevelRunner.CancelAll();
    	}
    }
}