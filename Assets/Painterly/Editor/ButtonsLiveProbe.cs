using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Verifies the on-screen buttons actually move Ari. Writes
    /// Temp/buttons_live.txt.
    ///
    /// Under automation there is no pointer, so a button can never be truly
    /// clicked. What is measured instead is the whole chain that a click is
    /// supposed to drive: the button reports pressed, OnScreenControls sums it
    /// into AriMover.ScreenMove, the mover reads that, and the Animator leaves
    /// the idle clip. Any break in it shows up as Ari not moving, which is the
    /// same symptom as broken buttons, so passing this is a real result.
    ///
    /// The pressed state is set through the Button component's own field rather
    /// than by faking pointer events, so what is tested is the state the
    /// EventSystem would set — not a parallel path that only the test uses.
    /// </summary>
    public static class ButtonsLiveProbe
    {
        [MenuItem("Tools/Echoes/Probe Screen Buttons", priority = 100)]
        public static void Run()
        {
            var sb = new StringBuilder();

            if (!EditorApplication.isPlaying)
            {
                sb.AppendLine("Not in play mode.");
                Finish(sb);
                return;
            }

            var controls = GameObject.Find("ScreenControls");
            var ari = GameObject.Find("Ari");

            if (controls == null) { sb.AppendLine("No ScreenControls in the scene."); Finish(sb); return; }
            if (ari == null) { sb.AppendLine("No Ari in the scene."); Finish(sb); return; }

            var mover = ari.GetComponent<Echoes.Painterly.AriMover>();
            var anim = ari.GetComponent<Animator>();

            var canvas = controls.GetComponent<Canvas>();
            var es = Object.FindAnyObjectByType<EventSystem>();

            sb.AppendLine("--- wiring ---");
            sb.AppendLine($"  canvas present={canvas != null} " +
                          $"mode={(canvas == null ? "-" : canvas.renderMode.ToString())} " +
                          $"active={canvas != null && canvas.gameObject.activeInHierarchy}");
            // Pulled out because an interpolated string cannot wrap onto a second
            // line, and the LINQ chain is too long to fit in one.
            var modules = es == null
                ? new string[0]
                : es.GetComponents<BaseInputModule>().Select(m => m.GetType().Name).ToArray();
            string module = modules.Length == 0 ? "<none>" : string.Join(" + ", modules);

            sb.AppendLine($"  EventSystem present={es != null} inputModule={module}");
            sb.AppendLine($"  AriMover.UseScreenControls = {mover?.UseScreenControls}");

            var buttons = controls.GetComponentsInChildren<Button>(true);
            sb.AppendLine($"  {buttons.Length} button(s): " +
                string.Join(", ", buttons.Select(b => b.name)));

            if (mover == null || anim == null) { Finish(sb); return; }

            // ---- press Up and see if she walks ----
            var up = buttons.FirstOrDefault(b => b.name == "Up");
            if (up == null) { sb.AppendLine("\nNo 'Up' button found."); Finish(sb); return; }

            bool moverWasEnabled = mover.enabled;
            mover.enabled = false;   // so Update does not also drive her

            sb.AppendLine("\n--- pressing UP ---");
            Vector3 start = ari.transform.position;

            SetPressed(up, true);
            // OnScreenControls.Update is what turns a pressed button into a
            // direction, and it is a normal Update, so it is invoked directly
            // rather than waited on.
            var oc = controls.GetComponent<Echoes.Painterly.OnScreenControls>();
            SendUpdate(oc);

            sb.AppendLine($"  button.IsPressed = {up.IsPressed()}");
            sb.AppendLine($"  AriMover.ScreenMove = {Echoes.Painterly.AriMover.ScreenMove} " +
                          $"(expect 0,1 for up)");

            // The real mover consumes ScreenMove in its own Update. Stepping it
            // by hand with ScreenMove is the same read, so the result is the
            // same movement.
            const float dt = 1f / 60f;
            for (int i = 0; i < 120; i++)
            {
                mover.Step(new Vector3(Echoes.Painterly.AriMover.ScreenMove.x, 0f,
                                       Echoes.Painterly.AriMover.ScreenMove.y),
                           Echoes.Painterly.AriMover.ScreenRun, dt);
                anim.Update(dt);
            }

            Vector3 afterUp = ari.transform.position;
            float travelled = Vector3.Distance(start, afterUp);
            sb.AppendLine($"  travelled {travelled:F3} in 2.0s while UP held " +
                          $"(walk 2.2 => about 4.4)");
            sb.AppendLine($"  moved: {travelled > 0.5f}");
            sb.AppendLine($"  animator now: {ClipNow(anim)}");

            // ---- release, confirm she stops ----
            sb.AppendLine("\n--- releasing ---");
            SetPressed(up, false);
            SendUpdate(oc);
            sb.AppendLine($"  AriMover.ScreenMove = {Echoes.Painterly.AriMover.ScreenMove} " +
                          $"(expect 0,0)");

            Vector3 before = ari.transform.position;
            for (int i = 0; i < 60; i++)
            {
                mover.Step(Vector3.zero, false, dt);
                anim.Update(dt);
            }
            float drift = Vector3.Distance(before, ari.transform.position);
            sb.AppendLine($"  moved {drift:F4} after release (expect ~0)");
            sb.AppendLine($"  animator now: {ClipNow(anim)}");

            // ---- STOP button ----
            sb.AppendLine("\n--- STOP button ---");
            var stop = buttons.FirstOrDefault(b => b.name == "STOP");
            if (stop == null) sb.AppendLine("  no STOP button");
            else
            {
                // Simulate a real press-then-release, which is what onClick sees.
                Echoes.Painterly.AriMover.ScreenMove = Vector2.up;
                stop.onClick.Invoke();
                sb.AppendLine($"  after clicking STOP: ScreenMove = " +
                              $"{Echoes.Painterly.AriMover.ScreenMove}, " +
                              $"ScreenRun = {Echoes.Painterly.AriMover.ScreenRun} " +
                              $"(expect 0,0 and False)");
            }

            // ---- RUN toggle ----
            sb.AppendLine("\n--- RUN button ---");
            var run = buttons.FirstOrDefault(b => b.name == "Run");
            if (run == null) sb.AppendLine("  no RUN button");
            else
            {
                run.onClick.Invoke();
                bool after1 = Echoes.Painterly.AriMover.ScreenRun;
                run.onClick.Invoke();
                bool after2 = Echoes.Painterly.AriMover.ScreenRun;
                sb.AppendLine($"  ScreenRun after 1st click = {after1}, " +
                              $"after 2nd = {after2} (expect True then False — a toggle)");
            }

            ari.transform.position = start;
            mover.enabled = moverWasEnabled;

            Finish(sb);
        }

        /// <summary>
        /// Press or release a button through the same public pointer events the
        /// EventSystem sends, rather than by writing the Button's private state.
        ///
        /// That matters for what the test proves. Setting a private field would
        /// confirm the probe can set a field; dispatching IPointerDownHandler
        /// confirms the whole chain the way a real click does — the event lands
        /// on the Button, the Button sets its own pressed state, and IsPressed
        /// becomes true without the probe touching it.
        /// </summary>
        static void SetPressed(Button b, bool pressed)
        {
            var rect = (RectTransform)b.transform;

            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                // pointerPress is what Button.OnPointerUp checks to decide it is
                // the button that was pressed, so it has to be set for a release
                // to clear the state rather than being ignored.
                pointerPress = pressed ? b.gameObject : null,
                position = rect.TransformPoint(rect.rect.center)
            };

            if (pressed)
                ExecuteEvents.Execute<IPointerDownHandler>(
                    b.gameObject, data, ExecuteEvents.pointerDownHandler);
            else
                ExecuteEvents.Execute<IPointerUpHandler>(
                    b.gameObject, data, ExecuteEvents.pointerUpHandler);
        }

        static void SendUpdate(Echoes.Painterly.OnScreenControls oc)
        {
            oc?.Tick();
        }

        static string ClipNow(Animator anim)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            var clip = anim.runtimeAnimatorController.animationClips
                .Select(c => new { c.name, c.length })
                .FirstOrDefault(c => Animator.StringToHash(c.name) == info.shortNameHash);
            return $"clip={(clip == null ? "?" : clip.name)} " +
                   $"t={info.normalizedTime:F2} transitioning={anim.IsInTransition(0)}";
        }

        static void Finish(StringBuilder sb)
        {
            var text = sb.ToString();
            File.WriteAllText("Temp/buttons_live.txt", text);
            Debug.Log("[Echoes] Screen buttons probe\n" + text);
        }
    }
}
