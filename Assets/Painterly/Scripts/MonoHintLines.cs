using System;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [CreateAssetMenu(fileName = "MonoHintLines", menuName = "Echoes/Mono Hint Lines", order = 40)]
    public sealed class MonoHintLines : ScriptableObject
    {
    	public enum LineKind
    	{
    		Beat,
    		HintRung,
    		Ambient
    	}

    	[Serializable]
    	public sealed class Line
    	{
    		[Tooltip("Stable id beats refer to, e.g. 'beat3.wake'. Never reused.")]
    		public string id = "line";

    		[TextArea(2, 4)]
    		[Tooltip("What Mono says. Mono is the only speaker in Level 1.")]
    		public string text = "...";

    		public LineKind kind;

    		[Tooltip("For HintRung lines: 1 plays first, 2 second, and so on. Anything already played is skipped.")]
    		public int rung = 1;

    		[Tooltip("For Ambient lines: minimum seconds of quiet before this one may play at all.")]
    		public float minGapSeconds = 25f;

    		[Tooltip("Muted lines stay in the list so ids keep meaning the same thing, but never play. How the rest of Level 1 is switched off.")]
    		public bool muted;
    	}

    	[Tooltip("Lines in play order. The order here is the order they fire.")]
    	public List<Line> lines = new List<Line>();

    	private readonly HashSet<string> _played = new HashSet<string>();

    	private int _nextRung = 1;

    	private float _lastAmbientAt = -999f;

    	public IEnumerable<Line> All => lines;

    	public int RungsPlayed => Mathf.Max(0, _nextRung - 1);

    	public Line NextHint()
    	{
    		foreach (Line line in lines)
    		{
    			if (!line.muted && line.kind == LineKind.HintRung && line.rung == _nextRung && !_played.Contains(line.id))
    			{
    				_nextRung++;
    				return line;
    			}
    		}
    		return null;
    	}

    	public Line Beat(string id)
    	{
    		foreach (Line line in lines)
    		{
    			if (line.kind == LineKind.Beat && line.id == id)
    			{
    				return line.muted ? null : line;
    			}
    		}
    		Debug.LogWarning((object)("[Echoes] Mono has no beat line called '" + id + "'."));
    		return null;
    	}

    	public Line NextAmbient(float now)
    	{
    		if (now - _lastAmbientAt < 20f)
    		{
    			return null;
    		}
    		if (NextHint() != null)
    		{
    			return null;
    		}
    		foreach (Line line in lines)
    		{
    			if (!line.muted && line.kind == LineKind.Ambient && !_played.Contains(line.id) && !(now - _lastAmbientAt < line.minGapSeconds))
    			{
    				_lastAmbientAt = now;
    				return line;
    			}
    		}
    		return null;
    	}

    	public void Consume(Line line)
    	{
    		if (line != null)
    		{
    			_played.Add(line.id);
    		}
    	}

    	public bool HasPlayed(string id)
    	{
    		return _played.Contains(id);
    	}

    	public void ResetMemory()
    	{
    		_played.Clear();
    		_nextRung = 1;
    		_lastAmbientAt = -999f;
    	}

    	public void Tally(out int beat, out int rung, out int ambient, out int muted)
    	{
    		beat = (rung = (ambient = (muted = 0)));
    		foreach (Line line in lines)
    		{
    			if (line.muted)
    			{
    				muted++;
    				continue;
    			}
    			switch (line.kind)
    			{
    			case LineKind.Beat:
    				beat++;
    				break;
    			case LineKind.HintRung:
    				rung++;
    				break;
    			default:
    				ambient++;
    				break;
    			}
    		}
    	}
    }
}