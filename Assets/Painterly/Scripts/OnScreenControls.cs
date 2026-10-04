using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/On Screen Controls")]
    public sealed class OnScreenControls : MonoBehaviour
    {
    	[Header("Layout")]
    	[Tooltip("Side of the D-pad buttons, in units of the short screen edge.")]
    	[Min(0.05f)]
    	[SerializeField]
    	private float buttonSize = 0.13f;

    	[Tooltip("How far the D-pad sits from the corner, as a fraction of height.")]
    	[Range(0f, 0.4f)]
    	[SerializeField]
    	private float edgeMargin = 0.06f;

    	[Tooltip("Gap between the D-pad buttons, as a fraction of the button size.")]
    	[Range(0f, 0.5f)]
    	[SerializeField]
    	private float gap = 0.06f;

    	[Header("Behaviour")]
    	[Tooltip("Also drive Ari from these buttons, ignoring the keyboard.")]
    	[SerializeField]
    	private bool driveAri = true;

    	[Tooltip("Start visible. Turn off if the buttons are in the way of the scene; the whole canvas is disabled, not just the graphics.")]
    	[SerializeField]
    	private bool visibleOnStart = true;

    	private static readonly Color PadColour = new Color(0.92f, 0.92f, 0.92f, 0.55f);

    	private static readonly Color StopColour = new Color(0.85f, 0.3f, 0.28f, 0.7f);

    	private static readonly Color RunColour = new Color(0.95f, 0.8f, 0.35f, 0.65f);

    	private static readonly Color HeldColour = new Color(1f, 1f, 1f, 0.85f);

    	private Canvas _canvas;

    	private Button _up;

    	private Button _down;

    	private Button _left;

    	private Button _right;

    	private Button _run;

    	private Button _stop;

    	private HoldButton _upHold;

    	private HoldButton _downHold;

    	private HoldButton _leftHold;

    	private HoldButton _rightHold;

    	private void Awake()
    	{
    		Build();
    	}

    	private void Update()
    	{
    		Tick();
    	}

    	public void Tick()
    	{
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)_canvas == (Object)null || !((Component)_canvas).gameObject.activeInHierarchy)
    		{
    			AriMover.ScreenMove = Vector2.zero;
    			return;
    		}
    		Vector2 val = Vector2.zero;
    		if (Pressing(_upHold))
    		{
    			val += Vector2.up;
    		}
    		if (Pressing(_downHold))
    		{
    			val += Vector2.down;
    		}
    		if (Pressing(_rightHold))
    		{
    			val += Vector2.right;
    		}
    		if (Pressing(_leftHold))
    		{
    			val += Vector2.left;
    		}
    		AriMover.ScreenMove = ((val.sqrMagnitude > 1f) ? val.normalized : val);
    	}

    	private static bool Pressing(HoldButton b)
    	{
    		if ((Object)(object)b != (Object)null)
    		{
    			return b.Held;
    		}
    		return false;
    	}

    	private void OnDisable()
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		AriMover.ScreenMove = Vector2.zero;
    	}

    	private void OnDestroy()
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		AriMover.ScreenMove = Vector2.zero;
    		AriMover.ScreenRun = false;
    	}

    	private void Build()
    	{
    		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
    		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
    		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0285: Expected Obj, but got Unknown
    		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a1: Expected Obj, but got Unknown
    		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)EventSystem.current == (Object)null)
    		{
    			new GameObject("EventSystem", new Type[2]
    			{
    				typeof(EventSystem),
    				typeof(InputSystemUIInputModule)
    			}).transform.SetParent(((Component)this).transform, false);
    		}
    		_canvas = ((Component)this).gameObject.AddComponent<Canvas>();
    		_canvas.renderMode = (RenderMode)0;
    		CanvasScaler canvasScaler = ((Component)this).gameObject.AddComponent<CanvasScaler>();
    		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
    		canvasScaler.matchWidthOrHeight = 0.5f;
    		((Component)this).gameObject.AddComponent<GraphicRaycaster>();
    		((Component)_canvas).gameObject.SetActive(visibleOnStart);
    		float num = edgeMargin;
    		float num2 = buttonSize;
    		float num3 = num2 * gap;
    		_leftHold = PadHold("Left", new Vector2(num, num), new Vector2(0f, 0f), Vector2.left, out _left);
    		_downHold = PadHold("Down", new Vector2(num + num2 + num3, num), new Vector2(0f, 0f), Vector2.down, out _down);
    		_rightHold = PadHold("Right", new Vector2(num + (num2 + num3) * 2f, num), new Vector2(0f, 0f), Vector2.right, out _right);
    		_upHold = PadHold("Up", new Vector2(num + num2 + num3, num + num2 + num3), new Vector2(0f, 0f), Vector2.up, out _up);
    		PadHold("Run", new Vector2(0f, num), new Vector2(1f, 0f), Vector2.zero, out _run, RunColour);
    		PadHold("STOP", new Vector2(1f, num), new Vector2(1f, 0f), Vector2.zero, out _stop, StopColour);
    		Label(_run, "RUN");
    		Label(_stop, "STOP");
    		((UnityEvent)_run.onClick).AddListener((UnityAction)ToggleRun);
    		((UnityEvent)_stop.onClick).AddListener((UnityAction)Stop);
    		if (!driveAri)
    		{
    			return;
    		}
    		GameObject val = GameObject.Find("Ari");
    		if ((Object)(object)val != (Object)null)
    		{
    			AriMover component = val.GetComponent<AriMover>();
    			if ((Object)(object)component != (Object)null)
    			{
    				component.UseScreenControls = true;
    			}
    		}
    	}

    	private HoldButton PadHold(string name, Vector2 anchor, Vector2 pivot, Vector2 direction, out Button button, Color? colour = null)
    	{
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0034: Expected Obj, but got Unknown
    		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
    		GameObject val = new GameObject(name, new Type[3]
    		{
    			typeof(RectTransform),
    			typeof(Image),
    			typeof(Button)
    		});
    		val.transform.SetParent(((Component)_canvas).transform, false);
    		RectTransform val2 = (RectTransform)val.transform;
    		val2.anchorMin = anchor;
    		val2.anchorMax = anchor;
    		val2.pivot = pivot;
    		val2.anchoredPosition = Vector2.zero;
    		val2.sizeDelta = new Vector2(buttonSize, buttonSize);
    		Image component = val.GetComponent<Image>();
    		component.color = colour ?? PadColour;
    		button = val.GetComponent<Button>();
    		button.targetGraphic = component;
    		button.transition = Selectable.Transition.None;
    		HoldButton holdButton = val.AddComponent<HoldButton>();
    		holdButton.Configure(direction, component.color);
    		return holdButton;
    	}

    	private void Label(Button button, string text)
    	{
    		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		GameObject val = new GameObject("Label", new Type[2]
    		{
    			typeof(RectTransform),
    			typeof(Text)
    		});
    		val.transform.SetParent(((Component)button).transform, false);
    		RectTransform val2 = (RectTransform)val.transform;
    		val2.anchorMin = Vector2.zero;
    		val2.anchorMax = Vector2.one;
    		val2.offsetMin = Vector2.zero;
    		val2.offsetMax = Vector2.zero;
    		Text component = val.GetComponent<Text>();
    		component.text = text;
    		component.font = BuiltinFont();
    		component.fontSize = 28;
    		component.alignment = (TextAnchor)4;
    		component.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    		component.raycastTarget = false;
    	}

    	private static Font BuiltinFont()
    	{
    		try
    		{
    			return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    		}
    		catch
    		{
    			return Resources.GetBuiltinResource<Font>("Arial.ttf");
    		}
    	}

    	private void Stop()
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		AriMover.ScreenMove = Vector2.zero;
    		AriMover.ScreenRun = false;
    	}

    	private void ToggleRun()
    	{
    		AriMover.ScreenRun = !AriMover.ScreenRun;
    	}

    	static OnScreenControls()
    	{
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}