using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/L1 HUD Overlay")]
    public sealed class AriHudOverlay : MonoBehaviour
    {
    	[Header("Health bar")]
    	[Tooltip("Top-left corner, as a fraction of the screen. Top-left because the fragment slot is bottom-right and the prompt is low-centre, so the three never overlap.")]
    	[SerializeField]
    	private Vector2 barAt = new Vector2(0.03f, 0.9f);

    	[Tooltip("Bar width, as a fraction of screen width.")]
    	[Range(0.1f, 0.45f)]
    	[SerializeField]
    	private float barWidth = 0.22f;

    	[Min(4f)]
    	[SerializeField]
    	private float barHeight = 14f;

    	[Tooltip("Draw the bar at all. Off once real UI arrives.")]
    	[SerializeField]
    	private bool drawBar = true;

    	[Tooltip("Show the health as a number as well as a bar. Off by default: the brief asks for a bar, and a number invites the player to do arithmetic instead of dodging.")]
    	[SerializeField]
    	private bool showNumber;

    	[Header("Damage")]
    	[Tooltip("Tint the screen edges red on a hit. The brief asks for an edge tint; in this level it is a brightening of the edges, because there is no red yet.")]
    	[SerializeField]
    	private bool drawFlash = true;

    	[Min(0f)]
    	[SerializeField]
    	private float flashPeak = 0.55f;

    	[Tooltip("Pulse the bar while health is low. The health-low stinger is an audio cue and this is the visual one.")]
    	[SerializeField]
    	private bool pulseWhenLow = true;

    	[Header("Fragment")]
    	[Tooltip("Show the inventory slot once Ari is carrying the fragment.")]
    	[SerializeField]
    	private bool drawSlot = true;

    	[SerializeField]
    	private Vector2 slotAt = new Vector2(0.94f, 0.1f);

    	[Min(16f)]
    	[SerializeField]
    	private float slotSize = 52f;

    	public static readonly Color FragmentBlue = new Color(0.3f, 0.55f, 0.95f);

    	private GUIStyle _label;

    	private GUIStyle _small;

    	private int _styleForHeight = -1;

    	private Texture2D _white;

    	private GUIStyle _card;

    	[Header("Death")]
    	[Tooltip("Draw the card when Ari's health reaches zero. Off only once a real lose screen exists — and nothing in this project read LevelState.Lost, so with this off Ari reached zero and nothing at all happened.")]
    	[SerializeField]
    	private bool drawLost = true;

    	[Tooltip("Seconds after zero before the card appears. Long enough for the damage flash to be seen, short enough that a player who is losing does not think the game has hung.")]
    	[Min(0f)]
    	[SerializeField]
    	private float lostDelay = 0.6f;

    	[TextArea(2, 4)]
    	[SerializeField]
    	private string lostText = "Ari is out.";

    	[TextArea(1, 3)]
    	[SerializeField]
    	private string retryText = "Press  ENTER  to get up again.";

    	private float _lostAt = -1f;

    	private int _retried;

    	public static bool CarryingFragment { get; set; }

    	public static bool ForceBar { get; set; }

    	public int Retries => _retried;

    	private void OnGUI()
    	{
    		if ((Object)(object)_white == (Object)null)
    		{
    			_white = Solid();
    		}
    		EnsureStyles();
    		if (drawFlash)
    		{
    			Flash();
    		}
    		if (drawBar)
    		{
    			Bar();
    		}
    		if (drawSlot)
    		{
    			Slot();
    		}
    		Lost();
    	}

    	private void EnsureStyles()
    	{
    		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
    		if (_label == null || _styleForHeight != Screen.height)
    		{
    			_styleForHeight = Screen.height;
    			int hudTarget = BeatText.HudTarget;
    			_label = BeatText.Make((TextAnchor)3, hudTarget, wordWrap: false, new Color(0.86f, 0.86f, 0.84f));
    			_small = BeatText.Make((TextAnchor)3, Mathf.Max(10, hudTarget / 2), wordWrap: false, new Color(0.8f, 0.8f, 0.78f));
    			_card = BeatText.Make((TextAnchor)4, BeatText.PromptTarget, wordWrap: true, BeatText.InkBright);
    		}
    	}

    	private static Texture2D Solid()
    	{
    		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Expected Obj, but got Unknown
    		Texture2D val = new Texture2D(1, 1, (TextureFormat)4, false)
    		{
    			name = "__hud_solid",
    			hideFlags = (HideFlags)61
    		};
    		val.SetPixel(0, 0, Color.white);
    		val.Apply();
    		return val;
    	}

    	private void Bar()
    	{
    		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
    		AriHealth i = AriHealth.I;
    		if (!((Object)(object)i == (Object)null) && (!(i.Fraction >= 0.999f) || ForceBar))
    		{
    			float num = (float)Screen.width * barWidth;
    			float num2 = barHeight * Mathf.Max(0.7f, (float)Screen.height / 720f);
    			Rect val = new Rect((float)Screen.width * barAt.x, (float)Screen.height * barAt.y, num, num2);
    			float num3 = ((i.IsLow && pulseWhenLow) ? (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : 0f);
    			Color color = GUI.color;
    			GUI.color = new Color(0f, 0f, 0f, 0.55f);
    			GUI.DrawTexture(val, (Texture)(object)_white);
    			float num4 = 2f;
    			Rect val2 = new Rect(val.x + num4, val.y + num4, Mathf.Max(0f, (val.width - num4 * 2f) * i.Fraction), val.height - num4 * 2f);
    			GUI.color = Color.Lerp(new Color(0.92f, 0.92f, 0.9f), Color.white, num3);
    			GUI.DrawTexture(val2, (Texture)(object)_white);
    			if (!i.IsDead)
    			{
    				float num5 = val.x + num4 + (val.width - num4 * 2f) * i.LowAt;
    				GUI.color = new Color(0f, 0f, 0f, 0.8f);
    				GUI.DrawTexture(new Rect(num5 - 1f, val.y - 2f, 2f, val.height + 4f), (Texture)(object)_white);
    			}
    			GUI.color = color;
    			if (showNumber)
    			{
    				float num6 = _small.lineHeight + 4f;
    				GUI.Label(new Rect(val.x, val.y - num6 - 2f, val.width, num6), "health " + Mathf.RoundToInt(i.Fraction * 100f) + "%", _small);
    			}
    		}
    	}

    	private void Flash()
    	{
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
    		float flashLeft = AriHealth.FlashLeft;
    		if (!(flashLeft <= 0f))
    		{
    			float num = Mathf.Clamp01(flashLeft / 0.35f) * flashPeak;
    			Color color = GUI.color;
    			GUI.color = new Color(1f, 1f, 1f, num);
    			float num2 = Mathf.Max(24f, (float)Screen.height * 0.1f);
    			float num3 = Mathf.Max(24f, (float)Screen.width * 0.07f);
    			GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, num2), (Texture)(object)_white);
    			GUI.DrawTexture(new Rect(0f, (float)Screen.height - num2, (float)Screen.width, num2), (Texture)(object)_white);
    			GUI.DrawTexture(new Rect(0f, 0f, num3, (float)Screen.height), (Texture)(object)_white);
    			GUI.DrawTexture(new Rect((float)Screen.width - num3, 0f, num3, (float)Screen.height), (Texture)(object)_white);
    			GUI.color = color;
    		}
    	}

    	private void Slot()
    	{
    		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0158: Expected Obj, but got Unknown
    		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
    		if (CarryingFragment)
    		{
    			float num = _small.lineHeight + 4f;
    			float num2 = Mathf.Max(slotSize * BeatText.ScreenScale, Mathf.Max(num, slotSize));
    			Rect val = new Rect((float)Screen.width * slotAt.x - num2, (float)Screen.height * slotAt.y, num2, num2);
    			Color color = GUI.color;
    			GUI.color = new Color(0f, 0f, 0f, 0.55f);
    			GUI.DrawTexture(val, (Texture)(object)_white);
    			float num3 = num2 * 0.5f;
    			Vector2 val2 = new Vector2(val.x + num3, val.y + num3);
    			float num4 = num2 * 0.26f;
    			GUI.color = FragmentBlue;
    			GUI.DrawTexture(new Rect(val2.x - num4, val2.y - num4 * 0.35f, num4 * 2f, num4 * 0.7f), (Texture)(object)_white);
    			GUI.DrawTexture(new Rect(val2.x - num4 * 0.35f, val2.y - num4, num4 * 0.7f, num4 * 2f), (Texture)(object)_white);
    			GUI.color = color;
    			float num5 = _small.CalcSize(new GUIContent("blue")).x + 4f;
    			float num6 = _small.lineHeight + 4f;
    			GUI.Label(new Rect(val.x + num2 - num5, val.y + num2 + 2f, num5, num6), "blue", _small);
    		}
    	}

    	private void Lost()
    	{
    		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
    		//IL_011c: Expected Obj, but got Unknown
    		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
    		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
    		if (!drawLost)
    		{
    			return;
    		}
    		AriHealth i = AriHealth.I;
    		if ((!((Object)(object)i != (Object)null) || !i.IsDead) && !LevelState.Lost)
    		{
    			_lostAt = -1f;
    		}
    		else if (_lostAt < 0f)
    		{
    			_lostAt = Time.time + lostDelay;
    		}
    		else if (!(Time.time < _lostAt))
    		{
    			if (RetryPressed())
    			{
    				_lostAt = -1f;
    				Retry();
    				return;
    			}
    			float num = BeatText.PromptWidth(0.86f);
    			float num2 = (float)Screen.height * 0.5f;
    			BeatText.Block(_card, lostText, (float)Screen.width * 0.5f, num2, num, BeatText.MaxBlock * 0.4f, BeatText.PromptTarget, out var _);
    			GUIStyle val = BeatText.Fitted(_small, retryText, num, 400f, BeatText.RowTarget);
    			Rect val2 = new Rect(((float)Screen.width - num) * 0.5f, num2 + 24f, num, BeatText.Height(val, retryText, num) + 8f);
    			GUIStyle val3 = new GUIStyle(val);
    			val3.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
    			GUI.Label(new Rect(val2.x + 2f, val2.y + 2f, val2.width, val2.height), retryText, val3);
    			GUI.Label(val2, retryText, val);
    		}
    	}

    	private static bool RetryPressed()
    	{
    		Keyboard current = Keyboard.current;
    		if (current != null && (current.enterKey.wasPressedThisFrame || current.eKey.wasPressedThisFrame || current.fKey.wasPressedThisFrame || current.numpadEnterKey.wasPressedThisFrame))
    		{
    			return true;
    		}
    		return Gamepad.current?.startButton.wasPressedThisFrame ?? false;
    	}

    	private void Retry()
    	{
    		LevelCheckpoint levelCheckpoint = Object.FindAnyObjectByType<LevelCheckpoint>((FindObjectsInactive)1);
    		if ((Object)(object)levelCheckpoint != (Object)null)
    		{
    			levelCheckpoint.Retry();
    		}
    		else
    		{
    			int num = LevelCheckpoint.ResetEveryBeat();
    			AriHealth i = AriHealth.I;
    			if ((Object)(object)i != (Object)null)
    			{
    				i.ResetHealth();
    			}
    			LevelState.Lost = false;
    			Debug.LogError((object)("[Echoes] Ari is out and there is no LevelCheckpoint in the level. " + num + " beat(s) and her health were restored, but she has not been moved anywhere — add a checkpoint, or this retry is not a retry."));
    		}
    		_retried++;
    	}

    	public AriHudOverlay()
    	{
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
    	}

    	static AriHudOverlay()
    	{
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}