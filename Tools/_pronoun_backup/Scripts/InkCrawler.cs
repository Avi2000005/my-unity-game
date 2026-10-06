using UnityEngine;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Ink Crawler")]
    public sealed class InkCrawler : MonoBehaviour
    {
    	public enum State
    	{
    		Dormant,
    		Alerted,
    		Closing,
    		Lunging,
    		Staggered,
    		Spent
    	}

    	[Header("Body")]
    	[Tooltip("Standing height in metres. Measured, not assumed — Beat5Setup reads this back off the placed model and fails the build if the figure the designer typed does not match.")]
    	[Min(0.1f)]
    	[SerializeField]
    	private float bodyHeight = 1.1f;

    	[Min(0.05f)]
    	[SerializeField]
    	private float bodyRadius = 0.34f;

    	[Tooltip("Where its eyes are, as a fraction of standing height. Below 1 so a low wall hides her and a tall one does not.")]
    	[Range(0.3f, 1f)]
    	[SerializeField]
    	private float eyeFraction = 0.85f;

    	[Header("Noticing")]
    	[Tooltip("Sees her inside this, with line of sight.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float noticeRadius = 7f;

    	[Tooltip("Loses her outside this. Larger than the notice radius on purpose, so it does not flicker on its own boundary.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float forgetRadius = 11f;

    	[Tooltip("Seconds without seeing her before it gives up and goes home. This is what makes the stealth route finish rather than turn into a chase that never ends.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float giveUpSeconds = 6f;

    	[Tooltip("Seconds it stands up straight before moving. Long enough to be noticed as a change, short enough not to be a cutscene.")]
    	[Min(0f)]
    	[SerializeField]
    	private float alertSeconds = 0.7f;

    	[Header("Moving")]
    	[Tooltip("Slower than Ari's 2.2 m/s walk.")]
    	[Min(0f)]
    	[SerializeField]
    	private float crawlSpeed = 1.5f;

    	[Tooltip("Faster than her walk, slower than her 3.6 m/s run.")]
    	[Min(0f)]
    	[SerializeField]
    	private float closeSpeed = 2.6f;

    	[Tooltip("Where it returns to when it gives her up.")]
    	[SerializeField]
    	private Vector3 home;

    	[Tooltip("It walks to home at this speed. Slower than closing, so giving up reads as leaving, not as repositioning.")]
    	[Min(0f)]
    	[SerializeField]
    	private float returnSpeed = 1.2f;

    	[Header("Striking")]
    	[Tooltip("Closer than this and it lunges.")]
    	[Min(0f)]
    	[SerializeField]
    	private float lungeRange = 2.2f;

    	[Tooltip("It walks no closer than this. Past here it would be inside the brush's reach with no room to swing.")]
    	[Min(0f)]
    	[SerializeField]
    	private float standoffRange = 1.9f;

    	[Min(0.1f)]
    	[SerializeField]
    	private float lungeSeconds = 0.55f;

    	[Tooltip("How far it comes in on the lunge.")]
    	[Min(0f)]
    	[SerializeField]
    	private float lungeReach = 1.1f;

    	[Header("The brush")]
    	[Tooltip("A stroke landing this close to her staggers it. Measured from Ari, not from where the click landed — the click can resolve a hundred metres down the lane and the reach is hers.")]
    	[Min(0.2f)]
    	[SerializeField]
    	private float splashRadius = 1.6f;

    	[Tooltip("Seconds knocked over.")]
    	[Min(0.1f)]
    	[SerializeField]
    	private float staggerSeconds = 1.4f;

    	[Tooltip("Metres pushed back per splash.")]
    	[Min(0f)]
    	[SerializeField]
    	private float knockback = 1.3f;

    	[Header("Animator")]
    	[SerializeField]
    	private string speedParameter = "Speed";

    	[SerializeField]
    	private string crawlingParameter = "Crawling";

    	[SerializeField]
    	private string attackTrigger = "Attack";

    	[Header("Wiring")]
    	[Tooltip("Ari. Searched if left empty, and searched inactive — she is never inactive, but Mono is, and this is the pattern that keeps working when someone copies it.")]
    	[SerializeField]
    	private AriMover ari;

    	[Tooltip("Optional. Cleared the moment it gives her up, so a spent crawler follows her instead of walking into the next beat.")]
    	[SerializeField]
    	private MonoCompanion mono;

    	[SerializeField]
    	private bool log = true;

    	private State _state;

    	private float _alertLeft;

    	private float _staggerLeft;

    	private float _lungeLeft;

    	private float _unseenSeconds;

    	private Vector3 _lungeFrom;

    	private Vector3 _lungeTo;

    	private Animator _anim;

    	private float _speedParam;

    	private int _speedHash;

    	private int _crawlHash;

    	private int _attackHash;

    	private bool _wired;

    	private bool _hunting;

    	private float _moved;

    	private AriHealth _health;

    	public State Now => _state;

    	public float BodyHeight => bodyHeight;

    	/// <summary>
    	/// Half-width of the capsule this crawler moves with.
    	/// </summary>
    	/// <remarks>
    	/// Read by the editor tools that have to ask whether a crawler fits at a
    	/// given spot. It was the one body dimension with no accessor, which meant
    	/// those tools had to hard-code 0.34 — the value on this field — and a
    	/// hard-coded copy of a serialized value is a number that goes stale the
    	/// moment somebody retunes the crawler in the inspector.
    	/// </remarks>
    	public float BodyRadius => bodyRadius;

    	public float NoticeRadius => noticeRadius;

    	public float ForgetRadius => forgetRadius;

    	public float SplashRadius => splashRadius;

    	public float StandoffRange => standoffRange;

    	public float LungeRange => lungeRange;

    	public Vector3 Home
    	{
    		get
    		{
    			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    			return home;
    		}
    	}

    	public int Staggers { get; private set; }

    	public int Lunges { get; private set; }

    	public float HuntingSeconds { get; private set; }

    	public bool EverNoticed { get; private set; }

    	public bool Retired { get; private set; }

    	public bool LastHitWasBrush { get; private set; }

    	public float LastPushMetres { get; private set; }

    	public float DistanceToAri
    	{
    		get
    		{
    			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    			if (!((Object)(object)ari == (Object)null))
    			{
    				return Vector3.Distance(((Component)this).transform.position, ((Component)ari).transform.position);
    			}
    			return float.PositiveInfinity;
    		}
    	}

    	public bool CanBeStaggered
    	{
    		get
    		{
    			if (_state != State.Staggered)
    			{
    				return !Retired;
    			}
    			return false;
    		}
    	}

    	public bool IsHunting => _hunting;

    	public Vector3 Eye
    	{
    		get
    		{
    			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
    			return ((Component)this).transform.position + Vector3.up * (bodyHeight * eyeFraction);
    		}
    	}

    	public Vector3 AriChest
    	{
    		get
    		{
    			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
    			if (!((Object)(object)ari == (Object)null))
    			{
    				return ((Component)ari).transform.position + Vector3.up * (ari.BodyHeight * 0.6f);
    			}
    			return ((Component)this).transform.position;
    		}
    	}

    	public float AnimatorSpeed
    	{
    		get
    		{
    			if (!((Object)(object)_anim == (Object)null))
    			{
    				return _anim.GetFloat(_speedHash);
    			}
    			return 0f;
    		}
    	}

    	public float MeasuredSpeed => _moved;

    	public float LastLungeTravel
    	{
    		get
    		{
    			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    			if (_lungeFrom.sqrMagnitude != 0f)
    			{
    				return Vector3.Distance(_lungeFrom, _lungeTo);
    			}
    			return 0f;
    		}
    	}

    	private void Awake()
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		if (home == Vector3.zero)
    		{
    			InkCrawlerEmerge component = ((Component)this).GetComponent<InkCrawlerEmerge>();
    			home = (((Object)(object)component != (Object)null) ? component.StandPoint : ((Component)this).transform.position);
    		}
    		Bind();
    	}

    	private void Bind()
    	{
    		if (!_wired)
    		{
    			_anim = ((Component)this).GetComponentInChildren<Animator>(true);
    			if ((Object)(object)_anim == (Object)null && log)
    			{
    				Debug.LogWarning((object)("[Echoes] " + ((Object)this).name + " has no Animator in its children, so it will move without animating"), (Object)(object)this);
    			}
    			if ((Object)(object)_anim != (Object)null)
    			{
    				_speedHash = Animator.StringToHash(speedParameter);
    				_crawlHash = Animator.StringToHash(crawlingParameter);
    				_attackHash = Animator.StringToHash(attackTrigger);
    			}
    			if ((Object)(object)ari == (Object)null)
    			{
    				ari = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    			}
    			_wired = true;
    		}
    	}

    	private void Update()
    	{
    		Bind();
    		float deltaTime = Time.deltaTime;
    		if (_state != State.Dormant && _state != State.Spent)
    		{
    			HuntingSeconds += deltaTime;
    		}
    		_moved = 0f;
    		switch (_state)
    		{
    		case State.Dormant:
    			Dormant(deltaTime);
    			break;
    		case State.Alerted:
    			Alerted(deltaTime);
    			break;
    		case State.Closing:
    			Closing(deltaTime);
    			break;
    		case State.Lunging:
    			Lunging(deltaTime);
    			break;
    		case State.Staggered:
    			Staggered(deltaTime);
    			break;
    		case State.Spent:
    			Spent(deltaTime);
    			break;
    		}
    		Animate();
    	}

    	public void Aggro()
    	{
    		bool hunting = _hunting;
    		_hunting = true;
    		if (!Retired)
    		{
    			if (_state == State.Dormant || _state == State.Spent)
    			{
    				_unseenSeconds = 0f;
    				Notice();
    			}
    			else if (log && !hunting)
    			{
    				Debug.Log((object)("[Echoes] crawler told to hunt; it is already " + _state.ToString() + " at " + DistanceToAri.ToString("0.0") + " m"), (Object)(object)this);
    			}
    		}
    	}

    	private void Dormant(float dt)
    	{
    		HoldStill();
    		if (SeesHer())
    		{
    			Notice();
    		}
    	}

    	private void Alerted(float dt)
    	{
    		HoldStill();
    		_alertLeft -= dt;
    		FaceHer();
    		if (_alertLeft <= 0f)
    		{
    			Enter(State.Closing);
    		}
    	}

    	private void Closing(float dt)
    	{
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
    		if (ChaseOrGiveUp(dt))
    		{
    			float distanceToAri = DistanceToAri;
    			if (distanceToAri <= lungeRange)
    			{
    				BeginLunge();
    				return;
    			}
    			float num = ((distanceToAri < 6f) ? crawlSpeed : closeSpeed);
    			Vector3 moved = Towards(((Component)ari).transform.position, distanceToAri, num * dt);
    			_moved = ((dt > 0f) ? (moved.magnitude / dt) : 0f);
    			FaceHer();
    			Face(moved);
    			SetCrawling(on: true);
    		}
    	}

    	private void Lunging(float dt)
    	{
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
    		_lungeLeft -= dt;
    		float num = Mathf.Max(0.01f, lungeSeconds);
    		float num2 = Mathf.Clamp01((1f - Mathf.Clamp01(_lungeLeft / num)) / 0.66f);
    		Vector3 val = _lungeFrom + (_lungeTo - _lungeFrom) * num2;
    		val.y = ((Component)this).transform.position.y;
    		Vector3 moved = val - ((Component)this).transform.position;
    		float magnitude = moved.magnitude;
    		if (magnitude > 0.0001f)
    		{
    			float num3 = Travel(moved.normalized, magnitude);
    			if (num3 > 0f)
    			{
    				Transform transform = ((Component)this).transform;
    				transform.position += moved.normalized * num3;
    				Face(moved);
    				_moved = ((dt > 0f) ? (num3 / dt) : 0f);
    			}
    		}
    		if (_lungeLeft <= 0f)
    		{
    			ResolveLunge();
    		}
    	}

    	private void ResolveLunge()
    	{
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
    		float num = lungeReach;
    		if ((Object)(object)ari != (Object)null && Vector3.Distance(((Component)ari).transform.position, _lungeTo) <= num && Vector3.Distance(((Component)ari).transform.position, ((Component)this).transform.position) <= num + bodyRadius + ari.BodyRadius)
    		{
    			Vector3 val = ((Component)ari).transform.position - ((Component)this).transform.position;
    			val.y = 0f;
    			if (val.sqrMagnitude < 0.0001f)
    			{
    				val = ((Component)this).transform.forward;
    			}
    			val.Normalize();
    			LastPushMetres = ari.TryPush(val * 0.9f);
    			AriHealth ariHealth = Health();
    			if ((Object)(object)ariHealth != (Object)null)
    			{
    				ariHealth.TakeHit();
    			}
    		}
    		Enter(State.Closing);
    		_unseenSeconds = 0f;
    	}

    	private AriHealth Health()
    	{
    		if ((Object)(object)_health == (Object)null)
    		{
    			_health = Object.FindAnyObjectByType<AriHealth>((FindObjectsInactive)1);
    		}
    		return _health;
    	}

    	private void Staggered(float dt)
    	{
    		HoldStill();
    		_staggerLeft -= dt;
    		if (_staggerLeft <= 0f)
    		{
    			Enter(State.Closing);
    			_unseenSeconds = 0f;
    		}
    	}

    	private void Spent(float dt)
    	{
    		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		SetCrawling(on: true);
    		float num = Vector3.Distance(((Component)this).transform.position, home);
    		if (num > 0.15f)
    		{
    			float num2 = Mathf.Min(returnSpeed, num * 2.5f);
    			Vector3 moved = Towards(home, num, num2 * dt);
    			Face(moved);
    			_moved = ((dt > 0f) ? (moved.magnitude / dt) : 0f);
    		}
    		else
    		{
    			HoldStill();
    			Enter(State.Dormant);
    		}
    	}

    	public bool BlockedFromPost(Vector3 eye, Vector3 ariPoint)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = ariPoint - eye;
    		if (val.sqrMagnitude < 0.0001f)
    		{
    			return false;
    		}
    		RaycastHit val2 = default;
    		if (!Physics.Raycast(eye, val.normalized, out val2, val.magnitude, -1, (QueryTriggerInteraction)1))
    		{
    			return false;
    		}
    		Collider collider = val2.collider;
    		if ((Object)(object)collider == (Object)null)
    		{
    			return false;
    		}
    		if (((Component)collider).transform.IsChildOf(((Component)this).transform))
    		{
    			return false;
    		}
    		if ((Object)(object)ari != (Object)null && ((Component)collider).transform.IsChildOf(((Component)ari).transform))
    		{
    			return false;
    		}
    		return true;
    	}

    	private bool SeesHer()
    	{
    		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)ari == (Object)null)
    		{
    			return false;
    		}
    		if (Retired)
    		{
    			return false;
    		}
    		float distanceToAri = DistanceToAri;
    		float num;
    		if (_hunting)
    		{
    			num = float.PositiveInfinity;
    		}
    		else
    		{
    			num = ((_state == State.Dormant || _state == State.Spent) ? noticeRadius : forgetRadius);
    		}
    		if (distanceToAri > num)
    		{
    			return false;
    		}
    		return !BlockedFromPost(Eye, AriChest);
    	}

    	private bool ChaseOrGiveUp(float dt)
    	{
    		if (SeesHer())
    		{
    			_unseenSeconds = 0f;
    			return true;
    		}
    		_unseenSeconds += dt;
    		if (_hunting)
    		{
    			return true;
    		}
    		if (_unseenSeconds >= giveUpSeconds)
    		{
    			if ((Object)(object)mono != (Object)null)
    			{
    				mono.Recall();
    			}
    			Retired = true;
    			Enter(State.Spent);
    			if (log)
    			{
    				Debug.Log((object)"[Echoes] crawler gave her up, and will not pick her up again this beat", (Object)(object)this);
    			}
    			return false;
    		}
    		return true;
    	}

    	private void Notice()
    	{
    		EverNoticed = true;
    		_alertLeft = alertSeconds;
    		_unseenSeconds = 0f;
    		Enter(State.Alerted);
    		if (log)
    		{
    			Debug.Log((object)("[Echoes] crawler noticed Ari at " + DistanceToAri.ToString("0.0") + " m"), (Object)(object)this);
    		}
    	}

    	private void BeginLunge()
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		_lungeLeft = lungeSeconds;
    		_lungeFrom = ((Component)this).transform.position;
    		_lungeTo = ((Component)ari).transform.position;
    		Lunges++;
    		Enter(State.Lunging);
    		LastHitWasBrush = false;
    		if ((Object)(object)_anim != (Object)null)
    		{
    			_anim.SetTrigger(_attackHash);
    		}
    	}

    	public bool Splash(Vector3 strokePoint, Vector3 fromAri)
    	{
    		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
    		if (!CanBeStaggered)
    		{
    			return false;
    		}
    		float num = Vector3.Distance(fromAri, strokePoint);
    		if (num > splashRadius)
    		{
    			return false;
    		}
    		_staggerLeft = staggerSeconds;
    		Staggers++;
    		LastHitWasBrush = true;
    		Enter(State.Staggered);
    		Vector3 val = ((Component)this).transform.position - fromAri;
    		val.y = 0f;
    		if (val.sqrMagnitude < 0.0001f)
    		{
    			val = -((Component)this).transform.forward;
    		}
    		Push(val.normalized * knockback);
    		if ((Object)(object)_anim != (Object)null)
    		{
    			_anim.SetTrigger(_attackHash);
    		}
    		if (log)
    		{
    			Debug.Log((object)("[Echoes] crawler staggered, stroke " + num.ToString("0.00") + " m from Ari (reach " + splashRadius.ToString("0.00") + " m)"), (Object)(object)this);
    		}
    		return true;
    	}

    	private Vector3 Towards(Vector3 to, float distance, float max)
    	{
    		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = new Vector3(to.x - ((Component)this).transform.position.x, 0f, to.z - ((Component)this).transform.position.z);
    		if (val.sqrMagnitude < 1E-06f || distance <= 0f)
    		{
    			return Vector3.zero;
    		}
    		Vector3 normalized = val.normalized;
    		float num = Travel(normalized, max);
    		if (num > 0f)
    		{
    			Transform transform = ((Component)this).transform;
    			transform.position += normalized * num;
    		}
    		return normalized * num;
    	}

    	private float Travel(Vector3 dir, float want)
    	{
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		if (want <= 0.0001f)
    		{
    			return 0f;
    		}
    		CapsuleAt(out var bottom, out var top);
    		RaycastHit val = default;
    		if (!Physics.CapsuleCast(bottom, top, bodyRadius, dir, out val, want, -1, (QueryTriggerInteraction)1))
    		{
    			return want;
    		}
    		return Mathf.Clamp(want - val.distance, 0f, want);
    	}

    	private void CapsuleAt(out Vector3 bottom, out Vector3 top)
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
    		float y = ((Component)this).transform.position.y;
    		bottom = new Vector3(((Component)this).transform.position.x, y + bodyRadius, ((Component)this).transform.position.z);
    		top = new Vector3(((Component)this).transform.position.x, y + bodyHeight - bodyRadius, ((Component)this).transform.position.z);
    		if (top.y < bottom.y)
    		{
    			top = bottom;
    		}
    	}

    	private void Push(Vector3 delta)
    	{
    		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		Towards(((Component)this).transform.position + delta, delta.magnitude, delta.magnitude);
    	}

    	private void FaceHer()
    	{
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		if (!((Object)(object)ari == (Object)null))
    		{
    			Vector3 val = ((Component)ari).transform.position - ((Component)this).transform.position;
    			val.y = 0f;
    			if (val.sqrMagnitude > 0.0001f)
    			{
    				((Component)this).transform.rotation = Quaternion.LookRotation(val.normalized, Vector3.up);
    			}
    		}
    	}

    	private void Face(Vector3 moved)
    	{
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		if (!(moved.sqrMagnitude < 1E-06f))
    		{
    			((Component)this).transform.rotation = Quaternion.LookRotation(moved.normalized, Vector3.up);
    		}
    	}

    	private void HoldStill()
    	{
    		SetCrawling(on: false);
    	}

    	private void Animate()
    	{
    		if (!((Object)(object)_anim == (Object)null))
    		{
    			float num = 1f - Mathf.Exp((0f - Time.deltaTime) / 0.05f);
    			float num2 = _anim.GetFloat(_speedHash);
    			_anim.SetFloat(_speedHash, num2 + (_moved - num2) * num);
    		}
    	}

    	private void SetCrawling(bool on)
    	{
    		if (!((Object)(object)_anim == (Object)null))
    		{
    			_anim.SetBool(_crawlHash, on);
    		}
    	}

    	private void Enter(State next)
    	{
    		_state = next;
    		if (next != State.Staggered)
    		{
    			LastHitWasBrush = false;
    		}
    	}

    	public void ResetCrawler()
    	{
    		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
    		Staggers = 0;
    		Lunges = 0;
    		HuntingSeconds = 0f;
    		EverNoticed = false;
    		Retired = false;
    		LastHitWasBrush = false;
    		LastPushMetres = 0f;
    		_state = State.Dormant;
    		_alertLeft = (_staggerLeft = (_lungeLeft = (_unseenSeconds = 0f)));
    		_hunting = false;
    		_moved = 0f;
    		((Component)this).transform.position = home;
    		Physics.SyncTransforms();
    	}

    	private void OnDrawGizmosSelected()
    	{
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
    		Gizmos.color = new Color(0.6f, 0.1f, 0.7f, 0.35f);
    		Gizmos.DrawWireSphere(((Component)this).transform.position, noticeRadius);
    		Gizmos.color = new Color(0.6f, 0.1f, 0.7f, 0.18f);
    		Gizmos.DrawWireSphere(((Component)this).transform.position, forgetRadius);
    		Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.5f);
    		Gizmos.DrawWireSphere(((Component)this).transform.position, lungeRange);
    		Gizmos.color = Color.yellow;
    		Gizmos.DrawWireSphere(home, 0.3f);
    	}
    }
}