using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{

    public static class BeatPrompt
    {
    	private static string _text = "";

    	private static float _until = -1f;

    	private static bool _sticky;

    	private static readonly List<Action> _alsoClear = new List<Action>(2);

    	public static bool Skippable { get; private set; }

    	public static bool HasSomethingToSkip
    	{
    		get
    		{
    			if (Skippable)
    			{
    				return Current.Length > 0;
    			}
    			return false;
    		}
    	}

    	public static string Current
    	{
    		get
    		{
    			if (!_sticky && !(Time.time < _until))
    			{
    				return "";
    			}
    			return _text;
    		}
    	}

    	public static void SetSkippable(bool on)
    	{
    		Skippable = on;
    	}

    	public static void AlsoClearOnSkip(Action what)
    	{
    		if (what == null)
    		{
    			_alsoClear.Clear();
    		}
    		else if (!_alsoClear.Contains(what))
    		{
    			_alsoClear.Add(what);
    		}
    	}

    	public static bool PollSkip()
    	{
    		if (!Skippable)
    		{
    			return false;
    		}
    		if (Current.Length == 0)
    		{
    			return false;
    		}
    		bool flag = false;
    		if (!((Keyboard.current == null) ? Input.GetKeyDown((KeyCode)115) : Keyboard.current.sKey.wasPressedThisFrame))
    		{
    			return false;
    		}
    		for (int i = 0; i < _alsoClear.Count; i++)
    		{
    			_alsoClear[i]?.Invoke();
    		}
    		Clear();
    		return true;
    	}

    	public static void Show(string text, float seconds = 0f, bool skippable = false)
    	{
    		_text = text ?? "";
    		_sticky = seconds <= 0f;
    		_until = (_sticky ? (-1f) : (Time.time + seconds));
    		Skippable = skippable;
    	}

    	public static void Clear()
    	{
    		_text = "";
    		_until = -1f;
    		_sticky = false;
    		Skippable = false;
    	}

    	public static void ResetAll()
    	{
    		Clear();
    	}
    }
}