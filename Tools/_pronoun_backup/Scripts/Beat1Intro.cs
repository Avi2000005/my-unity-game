using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Beat 1 Intro")]
    public sealed class Beat1Intro : MonoBehaviour, IResettable
    {
    	public enum Phase
    	{
    		Card,
    		Teaching,
    		Done
    	}

    	private const int RowMove = 0;

    	private const int RowJump = 1;

    	private const int RowBrush = 2;

    	private const int RowInteract = 3;

    	[Header("The introduction")]
    	[Tooltip("Shown when the level opens. Dismissed with Interact.")]
    	[TextArea(2, 6)]
    	[SerializeField]
    	private string introText = "Ari has never seen a colour.\n\nEverything here is black or white, and she has a brush anyway — because everything she likes, she tries to paint.";

    	[Tooltip("Shown while the card is up, telling her how to dismiss it.")]
    	[SerializeField]
    	private string introPrompt = "Press E (or F) to begin.";

    	[Header("The buttons")]
    	[Tooltip("One row per button, in teaching order. Five rows, five arrays — the lengths are checked and a mismatch is reported rather than silently truncating.")]
    	[SerializeField]
    	private string[] keyGlyphs = new string[5] { "W A S D", "SPACE", "LEFT CLICK", "E / F", "S" };

    	[SerializeField]
    	private string[] keyLabels = new string[5] { "move", "jump", "swing the brush", "interact", "skip a line" };

    	[Header("Behaviour")]
    	[Tooltip("Seconds the intro card waits before it can be dismissed. Long enough to actually be read; there is nothing to dismiss it with except the button it is introducing.")]
    	[Min(0f)]
    	[SerializeField]
    	private float introLockSeconds = 0.6f;

    	[SerializeField]
    	private bool log = true;

    	private Vector3 _entry;

    	public Phase Now { get; private set; }

    	public bool CardUp => Now == Phase.Card;

    	public float Seconds { get; private set; }

    	public Vector3 EntryPoint
    	{
    		get
    		{
    			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    			return _entry;
    		}
    	}

    	private void Awake()
    	{
    		ControlPrompts.Load(keyGlyphs, keyLabels);
    		Now = Phase.Card;
    		int num = Mathf.Min(keyGlyphs.Length, keyLabels.Length);
    		if (num != keyGlyphs.Length || num != keyLabels.Length)
    		{
    			Debug.LogError((object)("[Echoes] Beat 1 tutorial: " + keyGlyphs.Length + " key glyphs but " + keyLabels.Length + " labels. Rows past the shorter array are dropped, so the player is never taught a button with no description."), (Object)(object)this);
    		}
    	}

    	private void OnEnable()
    	{
    		ControlPrompts.ResetAll();
    		ControlPrompts.Load(keyGlyphs, keyLabels);
    	}

    	private void Update()
    	{
    		Seconds += Time.deltaTime;
    		if (Now == Phase.Card)
    		{
    			if (!(Seconds < introLockSeconds) && Interact())
    			{
    				Now = Phase.Teaching;
    				if (log)
    				{
    					Debug.Log((object)("[Echoes] beat 1 card dismissed, teaching " + ControlPrompts.Count + " buttons"), (Object)(object)this);
    				}
    			}
    		}
    		else if (Now == Phase.Teaching)
    		{
    			Teach();
    		}
    	}

    	public string CardText()
    	{
    		if (Now != Phase.Card)
    		{
    			return "";
    		}
    		return introText;
    	}

    	public string CardPrompt()
    	{
    		if (Now != Phase.Card || !(Seconds >= introLockSeconds))
    		{
    			return "";
    		}
    		return introPrompt;
    	}

    	private void Teach()
    	{
    		if ((MoveKey() || MoveKeyAxis()) && ControlPrompts.MarkUsed(0) && log)
    		{
    			Debug.Log((object)"[Echoes] tutorial: move", (Object)(object)this);
    		}
    		if (JumpKey() && ControlPrompts.MarkUsed(1) && log)
    		{
    			Debug.Log((object)"[Echoes] tutorial: jump", (Object)(object)this);
    		}
    		if (BrushKey() && ControlPrompts.MarkUsed(2) && log)
    		{
    			Debug.Log((object)"[Echoes] tutorial: brush", (Object)(object)this);
    		}
    		if (AriInteract.Near != null && Interact() && ControlPrompts.MarkUsed(3) && log)
    		{
    			Debug.Log((object)"[Echoes] tutorial: interact", (Object)(object)this);
    		}
    		if (ControlPrompts.AllUsed)
    		{
    			Now = Phase.Done;
    			if (log)
    			{
    				Debug.Log((object)"[Echoes] beat 1 tutorial complete", (Object)(object)this);
    			}
    		}
    	}

    	private static bool Interact()
    	{
    		Keyboard current = Keyboard.current;
    		if (current != null && (current.eKey.wasPressedThisFrame || current.fKey.wasPressedThisFrame))
    		{
    			return true;
    		}
    		return Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false;
    	}

    	private static bool MoveKey()
    	{
    		Keyboard current = Keyboard.current;
    		if (current == null)
    		{
    			return false;
    		}
    		if (!current.wKey.wasPressedThisFrame && !current.aKey.wasPressedThisFrame && !current.sKey.wasPressedThisFrame && !current.dKey.wasPressedThisFrame && !current.upArrowKey.wasPressedThisFrame && !current.downArrowKey.wasPressedThisFrame && !current.leftArrowKey.wasPressedThisFrame)
    		{
    			return current.rightArrowKey.wasPressedThisFrame;
    		}
    		return true;
    	}

    	private static bool MoveKeyAxis()
    	{
    		Keyboard current = Keyboard.current;
    		if (current == null)
    		{
    			return false;
    		}
    		if (!current.wKey.isPressed && !current.aKey.isPressed && !current.sKey.isPressed)
    		{
    			return current.dKey.isPressed;
    		}
    		return true;
    	}

    	private static bool JumpKey()
    	{
    		Keyboard current = Keyboard.current;
    		if (current != null && current.spaceKey.wasPressedThisFrame)
    		{
    			return true;
    		}
    		return Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false;
    	}

    	private static bool BrushKey()
    	{
    		Mouse current = Mouse.current;
    		if (current != null && current.leftButton.wasPressedThisFrame)
    		{
    			return true;
    		}
    		return Gamepad.current?.buttonWest.wasPressedThisFrame ?? false;
    	}

    	public void ResetForCheckpoint()
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		Now = Phase.Card;
    		Seconds = 0f;
    		_entry = Vector3.zero;
    		ControlPrompts.ResetAll();
    		ControlPrompts.Load(keyGlyphs, keyLabels);
    	}
    }
}