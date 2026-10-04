using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.Painterly
{

    [RequireComponent(typeof(Button))]
    public sealed class HoldButton : MonoBehaviour
    {
    	private Button _button;

    	private Image _image;

    	private Color _resting;

	// ILSpy could not name this property's backing store and emitted the bare word 'field' instead, which is not a member of anything.
	// The get is public and the set is private, so it is an auto-property with a private setter.
	public Vector2 Direction { get; private set; }

    	public bool Held
    	{
    		get
    		{
    			if ((Object)(object)_button != (Object)null)
    			{
    				return _button.IsPressed();
    			}
    			return false;
    		}
    	}

    	public void Configure(Vector2 direction, Color resting)
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    		Direction = direction;
    		_resting = resting;
    	}

    	private void Awake()
    	{
    		_button = ((Component)this).GetComponent<Button>();
    		_image = ((Component)this).GetComponent<Image>();
    	}

    	private void LateUpdate()
    	{
    		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    		if (!((Object)(object)_image == (Object)null))
    		{
    			_image.color = (Color)(Held ? new Color(1f, 1f, 1f, 0.9f) : _resting);
    		}
    	}
    }
}