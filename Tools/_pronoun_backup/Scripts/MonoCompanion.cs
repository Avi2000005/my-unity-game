using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{

    [RequireComponent(typeof(Animator))]
    public sealed class MonoCompanion : MonoBehaviour
    {
    	private static readonly int SpeedId = Animator.StringToHash("Speed");

    	private static readonly int TalkId = Animator.StringToHash("Talk");

    	private static readonly int AwakeId = Animator.StringToHash("Awake");

    	[Header("Who he is with")]
    	[Tooltip("Ari. Left empty he will find her by name at Awake.")]
    	[SerializeField]
    	private Transform ari;

    	[Header("Following")]
    	[Tooltip("How far behind Ari he stops. Close enough to talk over her shoulder, far enough that he is not under her feet.")]
    	[Min(0.3f)]
    	[SerializeField]
    	private float followDistance = 2f;

    	[Tooltip("How fast he closes a gap. Deliberately slower than Ari's run: if he can keep up at a sprint he is not being left behind, and the chase stops being a chase.")]
    	[Min(0.2f)]
    	[SerializeField]
    	private float followSpeed = 3f;

    	[Tooltip("How fast he turns. Matches Ari's, for the same reason — two characters rotating at different rates is the first thing that makes a pair look like two characters.")]
    	[Min(30f)]
    	[SerializeField]
    	private float turnRate = 720f;

    	[Tooltip("Whether he walks at all. Off for a beat where he should only hover and comment.")]
    	[SerializeField]
    	private bool follows = true;

    	[Header("Feet")]
    	[Tooltip("Keep him standing on whatever is under him. Off means he keeps whatever height he was authored at, and he was authored floating — a follow that preserves Y preserves that floating for the whole level.")]
    	[SerializeField]
    	private bool walkOnGround = true;

    	[Tooltip("How far below him to look for the floor.")]
    	[Min(0.2f)]
    	[SerializeField]
    	private float groundProbe = 3f;

    	[Tooltip("How far above the floor he may be placed and still be pulled down onto it. Generous, because a beat that parks him on a wall ledge should keep him there rather than drag him off it onto the cobbles underneath.")]
    	[Min(0f)]
    	[SerializeField]
    	private float groundTolerance = 1.2f;

    	[Header("Size")]
    	[Tooltip("Scale him once, on waking, so he stands as tall as Ari's chest. One, because the brief is 'up to her chest', and his authored size is 0.57 m — a thing at Ari's ankle.")]
    	[Min(0f)]
    	[SerializeField]
    	private float chestFraction = 1f;

    	[Tooltip("Where her chest is, as a fraction of her body height. Not a number invented here: it is the same 0.6 that InkCrawler measures her reach from, so there is one chest in this project and not two.")]
    	[Range(0.3f, 0.9f)]
    	[SerializeField]
    	private float chestAt = 0.6f;

    	[SerializeField]
    	private bool logSize = true;

    	[Header("Errands")]
    	[Tooltip("How close he has to get to an errand's destination before he stops and hands control back to following. Small, because he is 0.57 m tall and the switch he is sent to is the size of a fist.")]
    	[Min(0.1f)]
    	[SerializeField]
    	private float errandRadius = 0.7f;

    	[Header("Speech")]
    	[Tooltip("The line list. Left empty he still wakes and still follows, he just has nothing to say.")]
    	[SerializeField]
    	private MonoHintLines lines;

    	[Tooltip("How long a line stays up, per character. A typewriter reveal is added on top of this. Four seconds is a long time to read two words of dialogue in a game where nobody else is speaking.")]
    	[Min(1f)]
    	[SerializeField]
    	private float lineSeconds = 4f;

    	[Tooltip("Characters per second for the reveal. Slow enough to be read by a player who is also walking, which is most of the time.")]
    	[Min(5f)]
    	[SerializeField]
    	private float revealPerSecond = 28f;

    	[Tooltip("Where the subtitle sits, in normalised screen space, as the distance from the BOTTOM of the screen to the bottom of the text. (0.5, 0.10) is just under centre, which is where a player already is looking. Anchored to the bottom rather than the top because the block is as tall as the text needs it to be, and that height depends on the font size — which is now five times what it was.")]
    	[SerializeField]
    	private Vector2 subtitleAt = new Vector2(0.5f, 0.1f);

    	[Tooltip("How wide the subtitle is allowed to be, as a fraction of the screen. Wider than it used to be: a narrower box wraps the same words into more lines, and more lines is what makes a big font impossible to fit at all.")]
    	[Range(0.3f, 0.98f)]
    	[SerializeField]
    	private float subtitleWidth = 0.88f;

    	[Tooltip("Draw the subtitle. On by default; off once real UI exists, at which point this whole block goes.")]
    	[SerializeField]
    	private bool drawSubtitle = true;

    	private Animator _animator;

    	private Transform _target;

    	private Transform _errand;

    	private bool _awake;

    	private bool _hasController;

    	private string _current = "";

    	private string _showing = "";

    	private float _reveal;

    	private float _lineUntil = -1f;

    	private readonly List<string> _queued = new List<string>(4);

    	private const int MaxQueued = 4;

    	[SerializeField]
    	private bool log = true;

    	private GUIStyle _style;

    	private int _styleForHeight = -1;

    	public bool IsAwake => _awake;

    	public int Queued => _queued.Count;

    	public bool IsFollowing
    	{
    		get
    		{
    			if (_awake)
    			{
    				return follows;
    			}
    			return false;
    		}
    	}

    	public bool OnErrand => (Object)(object)_errand != (Object)null;

    	public float ErrandDistance
    	{
    		get
    		{
    			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    			if ((Object)(object)_errand == (Object)null)
    			{
    				return -1f;
    			}
    			Vector3 position = ((Component)this).transform.position;
    			Vector3 position2 = _errand.position;
    			position.y = 0f;
    			position2.y = 0f;
    			return Vector3.Distance(position, position2);
    		}
    	}

    	public bool ErrandDone
    	{
    		get
    		{
    			if ((Object)(object)_errand != (Object)null)
    			{
    				return ErrandDistance <= errandRadius;
    			}
    			return false;
    		}
    	}

    	public float ErrandRadius => errandRadius;

    	public float DistanceToAri
    	{
    		get
    		{
    			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    			if (!((Object)(object)ari == (Object)null))
    			{
    				return Vector3.Distance(((Component)this).transform.position, ari.position);
    			}
    			return float.PositiveInfinity;
    		}
    	}

    	public string CurrentLine => _current;

    	public void SendTo(Transform where)
    	{
    		if ((Object)(object)where == (Object)null)
    		{
    			Recall();
    		}
    		else if (_awake)
    		{
    			_errand = where;
    		}
    	}

    	public void Recall()
    	{
    		_errand = null;
    	}

    	public void WarpTo(Vector3 where)
    	{
    		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    		_errand = null;
    		((Component)this).transform.position = where;
    		Physics.SyncTransforms();
    	}

    	public static MonoCompanion FindInLevel()
    	{
    		GameObject val = GameObject.Find("Mono");
    		if ((Object)(object)val != (Object)null)
    		{
    			MonoCompanion component = val.GetComponent<MonoCompanion>();
    			if ((Object)(object)component != (Object)null)
    			{
    				return component;
    			}
    		}
    		GameObject val2 = GameObject.Find("L1_Cast");
    		if ((Object)(object)val2 != (Object)null)
    		{
    			Transform val3 = val2.transform.Find("Mono");
    			if ((Object)(object)val3 != (Object)null)
    			{
    				return ((Component)val3).GetComponent<MonoCompanion>();
    			}
    		}
    		return null;
    	}

    	private void Awake()
    	{
    		Bind();
    		if ((Object)(object)ari == (Object)null)
    		{
    			GameObject val = GameObject.Find("Ari");
    			if ((Object)(object)val != (Object)null)
    			{
    				ari = val.transform;
    			}
    		}
    	}

    	public void Bind()
    	{
    		if ((Object)(object)_animator == (Object)null)
    		{
    			_animator = ((Component)this).GetComponent<Animator>();
    		}
    		if (!((Object)(object)_animator == (Object)null))
    		{
    			_hasController = (Object)(object)_animator.runtimeAnimatorController != (Object)null;
    			if (!_hasController && Application.isPlaying)
    			{
    				Debug.LogWarning((object)"[Echoes] Mono has an Animator but no controller. He will stand in his bind pose, which is arms out and legs together — a scarecrow, not a character. Run Tools/Echoes/Build Cast Controllers.", (Object)(object)this);
    			}
    		}
    	}

    	public void Wake()
    	{
    		Bind();
    		if (!_awake)
    		{
    			_awake = true;
    			((Component)this).gameObject.SetActive(true);
    			BeatPrompt.AlsoClearOnSkip(SkipLine);
    			FitToAribust();
    			if ((Object)(object)_animator != (Object)null && _hasController)
    			{
    				_animator.SetTrigger(AwakeId);
    			}
    		}
    	}

    	private void FitToAribust()
    	{
    		if (chestFraction <= 0f || (Object)(object)ari == (Object)null)
    		{
    			return;
    		}
    		float num = AriChest();
    		Renderer val = SkinHeight.BodyOf(((Component)this).gameObject);
    		if ((Object)(object)val == (Object)null)
    		{
    			return;
    		}
    		float num2 = SkinHeight.Measure(val, out var _, out var _);
    		if (num2 > 0.0001f && Mathf.Abs(num2 - num) < 0.02f)
    		{
    			if (logSize)
    			{
    				Debug.Log((object)("[Echoes] Mono is " + num2.ToString("0.00") + " m, already at Ari's chest (" + num.ToString("0.00") + " m) — not rescaled"), (Object)(object)this);
    			}
    		}
    		else
    		{
    			float num3 = SkinHeight.FitToHeight(((Component)this).transform, val, num, out var why2);
    			if (logSize)
    			{
    				Debug.Log((object)("[Echoes] Mono sized to Ari's chest: " + why2), (Object)(object)this);
    			}
    			if (num3 <= 0f)
    			{
    				Debug.LogWarning((object)("[Echoes] Mono could not be sized — " + why2 + ". He stays at his authored size, which is about Ari's ankle."), (Object)(object)this);
    			}
    		}
    	}

    	public float AriChest()
    	{
    		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		return (((Object)(object)ariMover != (Object)null) ? ariMover.BodyHeight : 1.8f) * chestAt * chestFraction;
    	}

    	public void Unwake()
    	{
    		_awake = false;
    		((Component)this).gameObject.SetActive(false);
    		ClearLine();
    		_queued.Clear();
    		Recall();
    		if ((Object)(object)lines != (Object)null)
    		{
    			lines.ResetMemory();
    		}
    	}

    	public void Say(MonoHintLines.Line line)
    	{
    		if (line == null || !_awake || string.IsNullOrEmpty(line.text))
    		{
    			return;
    		}
    		if ((Object)(object)lines != (Object)null)
    		{
    			lines.Consume(line);
    		}
    		if ((Object)(object)_animator != (Object)null && _hasController)
    		{
    			_animator.SetTrigger(TalkId);
    		}
    		if (_current != "" && Time.time < _lineUntil)
    		{
    			if (_queued.Contains(line.text))
    			{
    				return;
    			}
    			if (_queued.Count >= 4)
    			{
    				if (log)
    				{
    					Debug.LogWarning((object)("[Echoes] Mono's speech queue is full at " + 4 + "; dropping '" + line.id + "'. Something is firing lines faster than he can say them."), (Object)(object)this);
    				}
    			}
    			else
    			{
    				_queued.Add(line.text);
    			}
    		}
    		else
    		{
    			Begin(line.text);
    			AriAnim.PlayTalk();
    		}
    	}

    	public void SaySomething()
    	{
    		if (!((Object)(object)lines == (Object)null) && _awake)
    		{
    			MonoHintLines.Line line = lines.NextAmbient(Time.time);
    			if (line != null)
    			{
    				Say(line);
    			}
    		}
    	}

    	public void SayBeat(string id)
    	{
    		if (!((Object)(object)lines == (Object)null))
    		{
    			Say(lines.Beat(id));
    		}
    	}

    	public bool SayBeatIfPresent(string id)
    	{
    		if (string.IsNullOrEmpty(id))
    		{
    			return false;
    		}
    		if ((Object)(object)lines == (Object)null)
    		{
    			if (log)
    			{
    				Debug.LogWarning((object)("[Echoes] Mono was asked to say '" + id + "' but has no line list at all, so nothing was said."), (Object)(object)this);
    			}
    			return false;
    		}
    		MonoHintLines.Line line = lines.Beat(id);
    		if (line == null)
    		{
    			if (log)
    			{
    				Debug.LogWarning((object)("[Echoes] Mono was asked to say '" + id + "' and his line list has no beat line with that id. Nothing was said."), (Object)(object)this);
    			}
    			return false;
    		}
    		Say(line);
    		return true;
    	}

    	public bool SayNextHint()
    	{
    		if ((Object)(object)lines == (Object)null)
    		{
    			return false;
    		}
    		MonoHintLines.Line line = lines.NextHint();
    		if (line == null)
    		{
    			return false;
    		}
    		Say(line);
    		return true;
    	}

    	private void Begin(string text)
    	{
    		_current = text;
    		_showing = "";
    		_reveal = 0f;
    		_lineUntil = Time.time + lineSeconds + (float)text.Length / revealPerSecond;
    	}

    	public void SkipLine()
    	{
    		if (!(_current == ""))
    		{
    			ClearLine();
    		}
    	}

    	private void ClearLine()
    	{
    		_current = "";
    		_showing = "";
    		_reveal = 0f;
    		_lineUntil = -1f;
    	}

    	private void NextQueued()
    	{
    		if (_queued.Count != 0 && (!(_current != "") || !(Time.time < _lineUntil)))
    		{
    			string text = _queued[0];
    			_queued.RemoveAt(0);
    			Begin(text);
    			if ((Object)(object)_animator != (Object)null && _hasController)
    			{
    				_animator.SetTrigger(TalkId);
    			}
    			AriAnim.PlayTalk();
    		}
    	}

    	private void OnDisable()
    	{
    		BeatPrompt.AlsoClearOnSkip(null);
    	}

    	private void Update()
    	{
    		if (!_awake)
    		{
    			return;
    		}
    		if ((Object)(object)_target == (Object)null && (Object)(object)ari != (Object)null)
    		{
    			_target = ari;
    		}
    		if (_current != "")
    		{
    			_reveal += revealPerSecond * Time.deltaTime;
    			int length = Mathf.Clamp(Mathf.FloorToInt(_reveal), 0, _current.Length);
    			_showing = _current.Substring(0, length);
    			if (Time.time >= _lineUntil)
    			{
    				ClearLine();
    				NextQueued();
    			}
    		}
    		else
    		{
    			NextQueued();
    		}
    		if (IsFollowing && ((Object)(object)_errand != (Object)null || (Object)(object)_target != (Object)null))
    		{
    			Follow(Time.deltaTime);
    		}
    		else
    		{
    			ReportSpeed(0f);
    		}
    	}

    	private void Follow(float dt)
    	{
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
    		Transform transform = ((Component)this).transform;
    		if ((Object)(object)_errand != (Object)null)
    		{
    			Vector3 val = _errand.position - transform.position;
    			val.y = 0f;
    			if (val.magnitude <= errandRadius)
    			{
    				Recall();
    			}
    			else
    			{
    				Move(transform, val.normalized, followSpeed, dt);
    			}
    			return;
    		}
    		if ((Object)(object)_target == (Object)null)
    		{
    			ReportSpeed(0f);
    			return;
    		}
    		Vector3 val2 = _target.position - transform.position;
    		val2.y = 0f;
    		float magnitude = val2.magnitude;
    		if (magnitude < followDistance * 0.7f)
    		{
    			float num = Mathf.Clamp01((magnitude - followDistance * 0.35f) / Mathf.Max(0.001f, followDistance * 0.35f));
    			Move(transform, val2.normalized, followSpeed * num, dt);
    		}
    		else if (magnitude < 0.05f)
    		{
    			ReportSpeed(0f);
    		}
    		else
    		{
    			Move(transform, val2.normalized, followSpeed, dt);
    		}
    	}

    	private void Move(Transform t, Vector3 direction, float speed, float dt)
    	{
    		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		direction.y = 0f;
    		if (direction.sqrMagnitude < 1E-06f)
    		{
    			ReportSpeed(0f);
    			return;
    		}
    		direction.Normalize();
    		t.position += direction * (speed * dt);
    		StandOnGround(t);
    		Quaternion val = Quaternion.LookRotation(direction, Vector3.up);
    		t.rotation = Quaternion.RotateTowards(t.rotation, val, turnRate * dt);
    		ReportSpeed(speed);
    	}

    	private void StandOnGround(Transform t)
    	{
    		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
    		RaycastHit val = default;
    		if (walkOnGround && Physics.Raycast(t.position + Vector3.up * 0.6f, Vector3.down, out val, groundProbe, -1, (QueryTriggerInteraction)1))
    		{
    			float num = t.position.y - val.point.y;
    			if (!(num < 0f) && !(num > groundTolerance))
    			{
    				Vector3 position = t.position;
    				position.y = val.point.y;
    				t.position = position;
    			}
    		}
    	}

    	private void ReportSpeed(float speed)
    	{
    		if (!((Object)(object)_animator == (Object)null) && _hasController)
    		{
    			_animator.SetFloat(SpeedId, speed);
    		}
    	}

    	private void OnGUI()
    	{
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0079: Expected Obj, but got Unknown
    		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b2: Expected Obj, but got Unknown
    		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
    		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0173: Expected Obj, but got Unknown
    		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
    		if (drawSubtitle && !string.IsNullOrEmpty(_current) && _awake)
    		{
    			EnsureStyle();
    			float num = BeatText.PromptWidth(subtitleWidth);
    			float num2 = (float)Screen.height * (1f - subtitleAt.y);
    			int num3 = BeatText.Fit(_style, _showing, num, BeatText.MaxBlock, BeatText.SubtitleTarget);
    			GUIStyle val = new GUIStyle(_style)
    			{
    				fontSize = num3
    			};
    			float num4 = BeatText.Height(val, _showing, num);
    			Rect val2 = new Rect(((float)Screen.width - num) * subtitleAt.x, num2 - num4, num, num4);
    			GUIStyle val3 = new GUIStyle(val);
    			val3.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
    			GUI.Label(new Rect(val2.x + 3f, val2.y + 3f, val2.width, val2.height), _showing, val3);
    			GUI.Label(val2, _showing, val);
    			if (_showing.Length > 0)
    			{
    				int num5 = Mathf.Max(10, num3 / 2);
    				float num6 = (float)num5 * 1.5f;
    				Rect val4 = new Rect(val2.x, val2.y - num6, val2.width, num6);
    				GUIStyle val5 = new GUIStyle(val)
    				{
    					fontSize = num5
    				};
    				val5.normal.textColor = new Color(0.8f, 0.8f, 0.77f);
    				GUI.Label(val4, "Mono", val5);
    			}
    		}
    	}

    	private void EnsureStyle()
    	{
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		if (_style == null || _styleForHeight != Screen.height)
    		{
    			_styleForHeight = Screen.height;
    			_style = BeatText.Make((TextAnchor)4, BeatText.SubtitleTarget, wordWrap: true, new Color(0.94f, 0.94f, 0.92f));
    		}
    	}

    	public MonoCompanion()
    	{
    		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}