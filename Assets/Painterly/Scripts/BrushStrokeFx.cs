using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Brush Stroke FX")]
    public sealed class BrushStrokeFx : MonoBehaviour
    {
    	private const float SurfaceOffset = 0.03f;

    	private const float OpenSeconds = 0.18f;

    	private const float HoldSeconds = 0.27f;

    	private const float PeakAlpha = 0.92f;

    	private const float StartScale = 0.28f;

    	private const int MaxLive = 12;

    	private static readonly Color MarkColor = new Color(1f, 0.93f, 0.78f);

    	private static readonly int ColorId = Shader.PropertyToID("_Color");

    	private static Material _shared;

    	private static Mesh _quad;

    	private static readonly List<BrushStrokeFx> _live = new List<BrushStrokeFx>();

    	private Renderer _renderer;

    	private MaterialPropertyBlock _block;

    	private Vector3 _normal;

    	private float _radius;

    	private float _age;

    	private bool _dying;

    	public static BrushStrokeFx Spawn(Vector3 point, Vector3 normal, float radius)
    	{
    		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Expected Obj, but got Unknown
    		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00da: Expected Obj, but got Unknown
    		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		Material val = SharedMaterial();
    		if ((Object)(object)val == (Object)null)
    		{
    			return null;
    		}
    		while (_live.Count >= 12)
    		{
    			BrushStrokeFx brushStrokeFx = _live[0];
    			if ((Object)(object)brushStrokeFx == (Object)null)
    			{
    				_live.RemoveAt(0);
    				continue;
    			}
    			brushStrokeFx.Expire();
    			break;
    		}
    		if (normal.sqrMagnitude < 1E-08f)
    		{
    			normal = Vector3.up;
    		}
    		normal.Normalize();
    		GameObject val2 = new GameObject("BrushMark");
    		BrushStrokeFx brushStrokeFx2 = val2.AddComponent<BrushStrokeFx>();
    		brushStrokeFx2._normal = normal;
    		brushStrokeFx2._radius = Mathf.Max(0.05f, radius);
    		brushStrokeFx2._renderer = (Renderer)(object)val2.AddComponent<MeshRenderer>();
    		val2.AddComponent<MeshFilter>().sharedMesh = Quad();
    		brushStrokeFx2._renderer.sharedMaterial = val;
    		brushStrokeFx2._renderer.shadowCastingMode = (ShadowCastingMode)0;
    		brushStrokeFx2._renderer.receiveShadows = false;
    		brushStrokeFx2._block = new MaterialPropertyBlock();
    		val2.transform.position = point + normal * 0.03f;
    		val2.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
    		val2.transform.localScale = Vector3.one * (brushStrokeFx2._radius * 2f * 0.28f);
    		_live.Add(brushStrokeFx2);
    		return brushStrokeFx2;
    	}

    	private void Update()
    	{
    		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Expected Obj, but got Unknown
    		_age += Time.deltaTime;
    		float num = Mathf.Clamp01(_age / 0.18f);
    		float num2 = Mathf.Clamp01((_age - 0.18f) / 0.27f);
    		float num3 = Mathf.Lerp(0.28f, 1f, 1f - (1f - num) * (1f - num));
    		((Component)this).transform.localScale = Vector3.one * (_radius * 2f * num3);
    		float num4 = 0.92f * (1f - num2) * (1f - num2);
    		if (_block == null)
    		{
    			_block = new MaterialPropertyBlock();
    		}
    		_block.SetColor(ColorId, new Color(MarkColor.r, MarkColor.g, MarkColor.b, num4));
    		_renderer.SetPropertyBlock(_block);
    		if (num2 >= 1f)
    		{
    			Expire();
    		}
    	}

    	private void Expire()
    	{
    		if (!_dying)
    		{
    			_dying = true;
    			_live.Remove(this);
    			if ((Object)(object)this != (Object)null && (Object)(object)((Component)this).gameObject != (Object)null)
    			{
    				Object.Destroy((Object)(object)((Component)this).gameObject);
    			}
    		}
    	}

    	private void OnDestroy()
    	{
    		_live.Remove(this);
    	}

    	private static Material SharedMaterial()
    	{
    		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005b: Expected Obj, but got Unknown
    		if ((Object)(object)_shared != (Object)null)
    		{
    			return _shared;
    		}
    		Shader val = Shader.Find("Sprites/Default");
    		if ((Object)(object)val == (Object)null)
    		{
    			val = Shader.Find("Universal Render Pipeline/Unlit");
    		}
    		if ((Object)(object)val == (Object)null)
    		{
    			return null;
    		}
    		_shared = new Material(val)
    		{
    			name = "BrushMark",
    			hideFlags = (HideFlags)61
    		};
    		_shared.mainTexture = (Texture)(object)Falloff();
    		return _shared;
    	}

    	private static Mesh Quad()
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Expected Obj, but got Unknown
    		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
    		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)_quad != (Object)null)
    		{
    			return _quad;
    		}
    		_quad = new Mesh
    		{
    			name = "BrushMarkQuad",
    			hideFlags = (HideFlags)61
    		};
    		_quad.vertices = new Vector3[4]
    		{
    			new Vector3(-0.5f, -0.5f, 0f),
    			new Vector3(0.5f, -0.5f, 0f),
    			new Vector3(0.5f, 0.5f, 0f),
    			new Vector3(-0.5f, 0.5f, 0f)
    		};
    		_quad.uv = new Vector2[4]
    		{
    			new Vector2(0f, 0f),
    			new Vector2(1f, 0f),
    			new Vector2(1f, 1f),
    			new Vector2(0f, 1f)
    		};
    		_quad.normals = new Vector3[4]
    		{
    			Vector3.forward,
    			Vector3.forward,
    			Vector3.forward,
    			Vector3.forward
    		};
    		_quad.triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
    		_quad.RecalculateBounds();
    		return _quad;
    	}

    	private static Texture2D Falloff()
    	{
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003a: Expected Obj, but got Unknown
    		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
    		Texture2D val = new Texture2D(128, 128, (TextureFormat)4, false)
    		{
    			name = "BrushMarkFalloff",
    			wrapMode = (TextureWrapMode)1,
    			filterMode = (FilterMode)1,
    			anisoLevel = 0,
    			hideFlags = (HideFlags)61
    		};
    		Color32[] array = new Color32[16384];
    		float num = 64f;
    		for (int i = 0; i < 128; i++)
    		{
    			for (int j = 0; j < 128; j++)
    			{
    				float num2 = ((float)j + 0.5f - num) / num;
    				float num3 = ((float)i + 0.5f - num) / num;
    				float num4 = Mathf.Sqrt(num2 * num2 + num3 * num3);
    				byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(1f - Mathf.Clamp01((num4 - 0.58000004f) / 0.42f)) * 255f);
    				array[i * 128 + j] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, b);
    			}
    		}
    		val.SetPixels32(array);
    		val.Apply(false, false);
    		return val;
    	}

    	static BrushStrokeFx()
    	{
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}