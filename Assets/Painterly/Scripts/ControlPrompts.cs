using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{

    public static class ControlPrompts
    {
    	public struct Row
    	{
    		public string Keys;

    		public string Label;

    		public bool Used;
    	}

    	private static readonly List<Row> _rows = new List<Row>(4);

    	private static bool _anyUsed;

    	private static bool _allUsed;

    	public static IReadOnlyList<Row> Rows => _rows;

    	public static bool AnyUsed => _anyUsed;

    	public static bool AllUsed => _allUsed;

    	public static int Count => _rows.Count;

    	public static bool Visible
    	{
    		get
    		{
    			if (_rows.Count > 0)
    			{
    				return !_allUsed;
    			}
    			return false;
    		}
    	}

    	public static void Load(string[] keys, string[] labels)
    	{
    		_rows.Clear();
    		_anyUsed = false;
    		_allUsed = false;
    		if (keys != null && labels != null)
    		{
    			int num = Mathf.Min(keys.Length, labels.Length);
    			for (int i = 0; i < num; i++)
    			{
    				_rows.Add(new Row
    				{
    					Keys = keys[i],
    					Label = labels[i],
    					Used = false
    				});
    			}
    			_allUsed = _rows.Count == 0;
    		}
    	}

    	public static bool MarkUsed(int index)
    	{
    		if (index < 0 || index >= _rows.Count)
    		{
    			return false;
    		}
    		Row value = _rows[index];
    		if (value.Used)
    		{
    			return false;
    		}
    		value.Used = true;
    		_rows[index] = value;
    		_anyUsed = true;
    		bool allUsed = true;
    		for (int i = 0; i < _rows.Count; i++)
    		{
    			if (!_rows[i].Used)
    			{
    				allUsed = false;
    				break;
    			}
    		}
    		_allUsed = allUsed;
    		return true;
    	}

    	public static bool IsUsed(int index)
    	{
    		if (index >= 0 && index < _rows.Count)
    		{
    			return _rows[index].Used;
    		}
    		return false;
    	}

    	public static void ResetAll()
    	{
    		_rows.Clear();
    		_anyUsed = false;
    		_allUsed = false;
    	}
    }
}