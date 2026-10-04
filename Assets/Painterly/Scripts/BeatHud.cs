using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Beat HUD")]
    public sealed class BeatHud : MonoBehaviour
    {
    	[Tooltip("Where the prompt sits, in normalised screen space. Low and centred: under the middle of the screen, clear of Ari and of the follow camera's horizon.")]
    	[SerializeField]
    	private Vector2 promptAt = new Vector2(0.5f, 0.22f);

    	[Tooltip("Draw the prompt at all. Off once real UI exists.")]
    	[SerializeField]
    	private bool drawPrompt = true;

    	private GUIStyle _style;

    	private GUIStyle _cardStyle;

    	private GUIStyle _rowStyle;

    	private GUIStyle _tagStyle;

    	private int _styleForHeight = -1;

    	private const float Padding = 14f;

    	[Range(0.3f, 0.98f)]
    	[SerializeField]
    	private float promptWidth = 0.9f;

    	[Range(0.3f, 0.98f)]
    	[SerializeField]
    	private float cardWidth = 0.82f;

    	private float MaxBlockHeight => (float)Screen.height * 0.72f;

    	private void Update()
    	{
    		BeatPrompt.PollSkip();
    	}

    	private void OnEnable()
    	{
    		BeatPrompt.ResetAll();
    	}

    	private void OnDisable()
    	{
    		BeatPrompt.ResetAll();
    	}

    	private void EnsureStyle()
    	{
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
    		if (_style == null || _styleForHeight != Screen.height)
    		{
    			_styleForHeight = Screen.height;
    			_style = BeatText.Make((TextAnchor)4, BeatText.PromptTarget, wordWrap: true, BeatText.Ink);
    			_cardStyle = BeatText.Make((TextAnchor)4, BeatText.PromptTarget, wordWrap: true, BeatText.InkBright);
    			_rowStyle = BeatText.Make((TextAnchor)3, BeatText.RowTarget, wordWrap: false, BeatText.InkDim);
    			_tagStyle = BeatText.Make((TextAnchor)3, Mathf.Max(10, BeatText.RowTarget / 2), wordWrap: false, BeatText.InkFaint);
    		}
    	}

    	private void DrawCard(string body, string prompt)
    	{
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0051: Expected Obj, but got Unknown
    		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e2: Expected Obj, but got Unknown
    		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
    		EnsureStyle();
    		float num = Mathf.Min((float)Screen.width * cardWidth, (float)Screen.width - 40f);
    		int fontSize = BeatText.Fit(_cardStyle, body, num, MaxBlockHeight, BeatText.PromptTarget);
    		GUIStyle val = new GUIStyle(_cardStyle)
    		{
    			fontSize = fontSize
    		};
    		float num2 = BeatText.Height(val, body, num) + 14f;
    		float num3 = (string.IsNullOrEmpty(prompt) ? 0f : ((float)Mathf.RoundToInt((float)BeatText.RowTarget * BeatText.ScreenScale) + 10f));
    		float num4 = (float)Screen.height * 0.86f - num2 - num3 - 8f;
    		Vector2 val2 = new Vector2(((float)Screen.width - num) * 0.5f, Mathf.Max(4f, num4));
    		Rect val3 = new Rect(val2.x, val2.y, num, num2);
    		GUIStyle val4 = new GUIStyle(val);
    		val4.normal.textColor = BeatText.Shadow;
    		GUI.Label(new Rect(val3.x + 3f, val3.y + 3f, val3.width, val3.height), body, val4);
    		GUI.Label(val3, body, val);
    		if (!(num3 <= 0f))
    		{
    			GUI.Label(new Rect(val3.x, val3.y + val3.height + 4f, val3.width, num3), prompt, _rowStyle);
    		}
    	}

    	private void DrawTutorial()
    	{
    		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b6: Expected Obj, but got Unknown
    		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
    		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
    		EnsureStyle();
    		IReadOnlyList<ControlPrompts.Row> rows = ControlPrompts.Rows;
    		int count = rows.Count;
    		if (count != 0)
    		{
    			int num = Mathf.Max(10, BeatText.RowTarget);
    			float num2 = (float)num * 1.35f;
    			float num3 = (float)Screen.width * 0.62f;
    			float num4 = (float)Screen.height - 16f - num2 * (float)count;
    			float num5 = (float)Screen.width * 0.02f;
    			for (int i = 0; i < count; i++)
    			{
    				ControlPrompts.Row row = rows[i];
    				string text = (row.Used ? "  " : "> ") + row.Keys + "   " + row.Label;
    				Color color = GUI.color;
    				GUIStyle val = new GUIStyle(_rowStyle)
    				{
    					fontSize = num
    				};
    				val.normal.textColor = (Color)(row.Used ? new Color(0.48f, 0.48f, 0.47f) : BeatText.InkDim);
    				GUI.color = (Color)(row.Used ? new Color(0.45f, 0.45f, 0.44f, 0.75f) : Color.white);
    				GUI.Label(new Rect(num5, num4, num3, num2), text, val);
    				GUI.color = color;
    				num4 += num2;
    			}
    		}
    	}

    	private void OnGUI()
    	{
    		if (!drawPrompt)
    		{
    			return;
    		}
    		Beat1Intro beat1Intro = Object.FindAnyObjectByType<Beat1Intro>((FindObjectsInactive)1);
    		string text = (((Object)(object)beat1Intro != (Object)null) ? beat1Intro.CardText() : "");
    		if (!string.IsNullOrEmpty(text))
    		{
    			DrawCard(text, beat1Intro.CardPrompt());
    			return;
    		}
    		DrawPrompt();
    		if (ControlPrompts.Visible)
    		{
    			DrawTutorial();
    		}
    	}

    	private void DrawPrompt()
    	{
    		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
    		string current = BeatPrompt.Current;
    		if (!string.IsNullOrEmpty(current))
    		{
    			EnsureStyle();
    			float width = Mathf.Min((float)Screen.width * promptWidth, (float)Screen.width - 24f);
    			BeatText.Block(_style, current, (float)Screen.width * promptAt.x, (float)Screen.height * (1f - promptAt.y) - 12f, width, MaxBlockHeight, BeatText.PromptTarget, out var _);
    		}
    	}

    	public BeatHud()
    	{
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}