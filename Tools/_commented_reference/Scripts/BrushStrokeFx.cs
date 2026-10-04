using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// The visible mark a brush stroke leaves where it lands.
    ///
    /// WHY THIS EXISTS, MEASURED
    ///
    /// Ari's brush already worked. The click fired, the ray landed, and the
    /// stroke reached four ColorRestoreTargets inside one frame. What it did not
    /// do was show the player anything, and that is what "the brush does not
    /// work" turned out to mean.
    ///
    /// The colour that comes back is genuinely small on a dull surface. Measured
    /// on the fountain, the biggest change a fully restored pixel makes is
    /// 58/765 of full range — about 7.6% — because the rock texture the
    /// fountain uses has a mean saturation of 14.1%, one of the lowest in the
    /// village, and desaturating and re-saturating a nearly grey texture is
    /// almost the same pixel. The same stroke on roof tiles, at 73% saturation,
    /// is dramatic. So the mechanic is legible or invisible depending entirely
    /// on what the player happened to be aiming at.
    ///
    /// Two things therefore happen on every stroke, and neither waits for the
    /// world to change:
    ///
    ///   1. Ari's arm swings (AriMover.PlaySwing), because a click that
    ///      repaints the world while she stands perfectly still reads as the
    ///      mouse doing something rather than as a brush stroke.
    ///   2. A mark appears on the surface she hit, on the first frame.
    ///
    /// The mark is not decoration and it is not a mechanic. It carries no
    /// colour-based meaning and nothing reads it back; it is the answer to
    /// "did my click land", which until now had no answer at all.
    /// </summary>
    [AddComponentMenu("Echoes/Brush Stroke FX")]
    public sealed class BrushStrokeFx : MonoBehaviour
    {
        /// <summary>How far off the surface the mark floats, in metres.</summary>
        const float SurfaceOffset = 0.03f;

        /// <summary>How long the mark takes to open out, in seconds.</summary>
        const float OpenSeconds = 0.18f;

        /// <summary>How long the mark lives after that before it is gone.</summary>
        const float HoldSeconds = 0.27f;

        /// <summary>Peak opacity. High enough to be unmistakable on a dark street.</summary>
        const float PeakAlpha = 0.92f;

        /// <summary>Fraction of the radius the mark starts at, so it opens outward.</summary>
        const float StartScale = 0.28f;

        /// <summary>
        /// Marks alive at once. A held click paints every cooldown, and the
        /// cooldown is 0.6s against a 0.45s life, so a player mashing the button
        /// would otherwise leave a small crowd of quads hanging in the street.
        /// </summary>
        const int MaxLive = 12;

        // Warm ivory rather than a saturated hue. The mark has to read as paint
        // arriving rather than as a status effect, and a strong colour here
        // would start competing with the colour the world is regaining — which
        // is the one thing in this game that is allowed to be bright.
        static readonly Color MarkColor = new Color(1f, 0.93f, 0.78f);

        static readonly int ColorId = Shader.PropertyToID("_Color");

        static Material _shared;
        static Mesh _quad;
        static readonly List<BrushStrokeFx> _live = new List<BrushStrokeFx>();

        Renderer _renderer;
        MaterialPropertyBlock _block;
        Vector3 _normal;
        float _radius;
        float _age;
        bool _dying;

        /// <summary>
        /// Put a mark on a surface. Returns null when nothing could be drawn,
        /// which is only ever a missing shader — a missing mark is invisible,
        /// so the caller carries on regardless.
        /// </summary>
        public static BrushStrokeFx Spawn(Vector3 point, Vector3 normal, float radius)
        {
            var mat = SharedMaterial();
            if (mat == null) return null;

            // The oldest mark gives way rather than the stroke being dropped.
            // A dropped stroke is exactly the "my click did nothing" this
            // component exists to prevent.
            while (_live.Count >= MaxLive)
            {
                var oldest = _live[0];
                if (oldest == null) { _live.RemoveAt(0); continue; }
                oldest.Expire();
                break;
            }

            if (normal.sqrMagnitude < 1e-8f) normal = Vector3.up;
            normal.Normalize();

            var go = new GameObject("BrushMark");
            var fx = go.AddComponent<BrushStrokeFx>();

            fx._normal = normal;
            fx._radius = Mathf.Max(0.05f, radius);
            fx._renderer = go.AddComponent<MeshRenderer>();

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = Quad();

            fx._renderer.sharedMaterial = mat;
            fx._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fx._renderer.receiveShadows = false;
            fx._block = new MaterialPropertyBlock();

            // A Unity primitive is a unit square facing +Z, so the mark's own
            // normal has to be turned to face out of the surface it sits on.
            go.transform.position = point + normal * SurfaceOffset;
            go.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
            go.transform.localScale = Vector3.one * (fx._radius * 2f * StartScale);

            _live.Add(fx);
            return fx;
        }

        void Update()
        {
            _age += Time.deltaTime;

            float open = Mathf.Clamp01(_age / OpenSeconds);
            float die = Mathf.Clamp01((_age - OpenSeconds) / HoldSeconds);

            // Ease out on the opening so the mark appears to be flicked on
            // rather than inflating like a balloon.
            float size = Mathf.Lerp(StartScale, 1f, 1f - (1f - open) * (1f - open));

            transform.localScale = Vector3.one * (_radius * 2f * size);

            // Fade on a curve, not linearly. A linear fade spends half its life
            // at an opacity nobody can see and then vanishes.
            float alpha = PeakAlpha * (1f - die) * (1f - die);

            _block ??= new MaterialPropertyBlock();
            _block.SetColor(ColorId, new Color(MarkColor.r, MarkColor.g, MarkColor.b, alpha));
            _renderer.SetPropertyBlock(_block);

            if (die >= 1f) Expire();
        }

        void Expire()
        {
            if (_dying) return;
            _dying = true;

            _live.Remove(this);
            if (this != null && gameObject != null) Destroy(gameObject);
        }

        void OnDestroy() => _live.Remove(this);

        // ---------------------------------------------------------------------
        // The soft round falloff.
        //
        // A plain quad with a flat colour is a rectangle, and a rectangle on a
        // village wall reads as a misplaced decal rather than as a brush stroke.
        // The falloff is generated rather than imported so there is no asset to
        // keep track of and no import settings that can be wrong.

        static Material SharedMaterial()
        {
            if (_shared != null) return _shared;

            // Sprites/Default is always present, always unlit and always
            // transparent, which is all this needs. URP/Unlit would work too and
            // would need its surface type set to Transparent by hand, which is
            // three properties and a keyword, each of which can be forgotten.
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return null;

            _shared = new Material(shader) { name = "BrushMark", hideFlags = HideFlags.HideAndDontSave };
            _shared.mainTexture = Falloff();

            return _shared;
        }

        /// <summary>
        /// A unit square facing +Z, built here rather than fetched.
        ///
        /// The built-in quad is looked up by file name, and that name has been
        /// "Quad.fbx" in one Unity release and something else in the next, so
        /// asking for it by name is a version lottery that fails silently — a
        /// null mesh and a mark that never draws. Four vertices is cheaper than
        /// being wrong.
        /// </summary>
        static Mesh Quad()
        {
            if (_quad != null) return _quad;

            _quad = new Mesh
            {
                name = "BrushMarkQuad",
                hideFlags = HideFlags.HideAndDontSave
            };

            _quad.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f)
            };

            _quad.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };

            _quad.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
            _quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quad.RecalculateBounds();

            return _quad;
        }

        static Texture2D Falloff()
        {
            const int size = 128;
            const float feather = 0.42f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "BrushMarkFalloff",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave
            };

            var px = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Distance from the centre, 0 at the middle and 1 at the corner.
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // Inside the feather the mark is solid. Beyond 1 it is gone.
                // The corner distance is 1.414, so the falloff is mapped to
                // reach zero at the circle rather than at the square's edge.
                float a = 1f - Mathf.Clamp01((d - (1f - feather)) / feather);

                byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, b);
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }
    }
}
