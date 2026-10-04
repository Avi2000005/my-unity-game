using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;

/// <summary>
/// Puts the game view on the monitor's real resolution.
///
/// WHY THIS IS THE ACTUAL BLUR
///
/// The pipeline assets were nearly innocent. Render scale was 1.0, textures were
/// full size, nothing in post-processing blurs. What did the damage was this:
///
///     camera pixelWidth x pixelHeight: 876 x 453
///     monitor:                           1920x1080
///
/// Unity draws the game into the Game view panel and then scales that image to
/// fit the panel. At 876x453 shown on a 1920x1080 display, every pixel is
/// stretched about 2.2 times. Nothing in the project settings can repair it,
/// because the detail was never drawn — which is why changing MSAA or
/// anisotropy alone would not have fixed the complaint.
///
/// The Game view's resolution is a property of the panel, not of the project,
/// so it has to be set through the editor's internal API. Everything here is
/// wrapped and verified: the reflection is on types that have been renamed
/// across Unity versions, and a silent failure would leave the game at 453p
/// looking exactly as it did before.
///
/// The reason it was small in the first place is that the panel is docked
/// beside the Scene view at its default size. Maximising it is the other half
/// of the fix, so both are done: a fixed resolution that matches the monitor,
/// and a full-width panel to show it in.
/// </summary>
public static class GameViewFix
{
    const string Report = "Temp/gameviewfix.txt";

    public static void Run()
    {
        var sb = new StringBuilder();
        try { Body(sb); }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] game view fix\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    static Assembly EditorAsm { get { return typeof(EditorWindow).Assembly; } }

    // The Standalone group is the one that applies when running on this machine.
    const int StandaloneGroup = 0;

    /// <summary>FixedResolution in Unity's own enum. Named by value because the
    /// enum type is internal and its members move between versions.</summary>
    const int FixedResolutionKind = 1;

    static void Body(StringBuilder sb)
    {
        sb.AppendLine("SETTING THE GAME VIEW TO THE MONITOR'S RESOLUTION");
        sb.AppendLine();

        int monW = Screen.currentResolution.width;
        int monH = Screen.currentResolution.height;
        sb.AppendLine("  monitor: " + monW + "x" + monH);

        var main = Camera.main;
        if (main != null)
            sb.AppendLine("  camera before: " + main.pixelWidth + "x" + main.pixelHeight);

        string label = "Echoes " + monW + "x" + monH;
        int index = SetFixedResolution(sb, monW, monH, label);
        var win = Maximise(sb);

        sb.AppendLine();
        if (win != null)
        {
            win.Repaint();
            sb.AppendLine("  panel size after: " +
                          Mathf.RoundToInt(win.position.width) + " x " +
                          Mathf.RoundToInt(win.position.height) +
                          "   maximized=" + win.maximized);
        }

        sb.AppendLine("  selection verified from the game's own size list: " +
                      DescribeSelection(sb, index, monW, monH));

        if (main != null)
        {
            sb.AppendLine("  camera after: " + main.pixelWidth + "x" + main.pixelHeight +
                          "   (updates on the next repaint, so this can lag by one frame)");
        }

        sb.AppendLine();
        sb.AppendLine("=== VERDICT ===");
        if (index < 0)
        {
            sb.AppendLine("  The internal API did not respond, so the resolution could not");
            sb.AppendLine("  be set from here. The panel was still widened, which helps on");
            sb.AppendLine("  its own. To do it by hand: open the Game view, click the");
            sb.AppendLine("  'Free Aspect' dropdown at the top right of it, and choose");
            sb.AppendLine("  'Add' or '1920 x 1080'. Or press Ctrl+Shift+M to maximize the");
            sb.AppendLine("  game view panel.");
            sb.AppendLine();
            sb.AppendLine("  What matters is that the number next to the dropdown reads the");
            sb.AppendLine("  monitor's resolution, not 876 x 453.");
        }
        else
        {
            sb.AppendLine("  The game view is now set to " + monW + "x" + monH +
                          ", which is the monitor's own resolution, so the image is drawn");
            sb.AppendLine("  once at final size instead of being stretched to fit a small");
            sb.AppendLine("  panel. Combined with the 4x MSAA and anisotropy 8 already saved,");
            sb.AppendLine("  nothing in the pipeline is softening the frame any more.");
        }
    }

    // ---------------------------------------------------------------------
    // the fixed resolution itself
    // ---------------------------------------------------------------------

    /// <summary>
    /// Adds the monitor's resolution to the Standalone size group if it is not
    /// already there, then selects it.
    ///
    /// The type name is looked up by string rather than referenced directly
    /// because UnityEditor.GameView and its size list are internal, and naming
    /// them directly would not compile. Every step is checked, and the index it
    /// returns is verified afterwards by reading the size list back — a
    /// reflection call that silently does nothing looks exactly like one that
    /// worked.
    /// </summary>
    static int SetFixedResolution(StringBuilder sb, int w, int h, string label)
    {
        var sizesType = EditorAsm.GetType("UnityEditor.GameViewSizes");
        if (sizesType == null)
        {
            sb.AppendLine("  GameViewSizes type not found by name; skipping the fixed resolution");
            return -1;
        }

        var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singletonType.GetProperty("instance",
            BindingFlags.Public | BindingFlags.Static);
        if (instanceProp == null)
        {
            sb.AppendLine("  size-list singleton property not found; skipping");
            return -1;
        }

        object sizes = instanceProp.GetValue(null, null);
        if (sizes == null)
        {
            sb.AppendLine("  size-list singleton is null; skipping");
            return -1;
        }

        var groupEnum = EditorAsm.GetType("UnityEditor.GameViewSizeGroupType");
        var getGroup = sizesType.GetMethod("GetGroup");
        if (getGroup == null)
        {
            sb.AppendLine("  GetGroup not found on the size list; skipping");
            return -1;
        }

        var group = getGroup.Invoke(sizes,
            new object[] { System.Enum.ToObject(groupEnum, StandaloneGroup) });
        if (group == null)
        {
            sb.AppendLine("  standalone size group is null; skipping");
            return -1;
        }

        // The counting, reading and adding methods live on the GROUP that
        // GetGroup returned, not on the size list that returned it. Reflecting
        // them off the size list looks for a method that was never there, and
        // calling a null MethodInfo throws a NullReferenceException that says
        // nothing about which method was missing.
        var groupType = group.GetType();

        var getTotal = FindMethod(groupType, "GetTotalCount");
        if (getTotal == null)
        {
            sb.AppendLine("  GetTotalCount not found on '" + groupType.Name + "'; skipping");
            sb.AppendLine("  the methods it does have: " + MethodNames(groupType));
            return -1;
        }

        var addSize = FindMethod(groupType, "AddCustomSize");

        // Look for the resolution that is already in the list before adding a
        // duplicate. Adding one every run would fill the list with copies of the
        // same entry, which is exactly the kind of mess that outlasts a fix.
        int count = (int)getTotal.Invoke(group, null);
        var getSize = FindMethod(groupType, "GetGameViewSize");
        if (getSize == null)
        {
            sb.AppendLine("  GetGameViewSize not found on '" + groupType.Name + "'; skipping");
            sb.AppendLine("  the methods it does have: " + MethodNames(groupType));
            return -1;
        }

        sb.AppendLine("  sizes already in the standalone group: " + count);
        int found = -1;
        for (int i = 0; i < count; i++)
        {
            var size = getSize.Invoke(group, new object[] { i });
            if (size == null) continue;

            // Read defensively: these are properties on an internal class, and a
            // missing one has to skip the entry rather than end the whole run.
            var widthProp = size.GetType().GetProperty("width");
            var heightProp = size.GetType().GetProperty("height");
            if (widthProp == null || heightProp == null) continue;

            int sw = (int)widthProp.GetValue(size, null);
            int sh = (int)heightProp.GetValue(size, null);
            var st = size.GetType().GetProperty("sizeType");
            int kind = st == null ? 0 : (int)st.GetValue(size, null);

            if (sw == w && sh == h && kind == FixedResolutionKind)
            {
                found = i;
                sb.AppendLine("    [" + i + "] " + sw + "x" + sh + "  already present, reusing");
            }
        }

        if (found < 0)
        {
            var sizeType = EditorAsm.GetType("UnityEditor.GameViewSize");
            var kindEnum = EditorAsm.GetType("UnityEditor.GameViewSizeType");
            if (sizeType == null || kindEnum == null)
            {
                sb.AppendLine("  GameViewSize type or its enum not found; skipping");
                return -1;
            }

            var ctor = sizeType.GetConstructor(new[] { kindEnum, typeof(int), typeof(int), typeof(string) });
            if (ctor == null)
            {
                sb.AppendLine("  GameViewSize constructor not found; skipping");
                return -1;
            }

            var size = ctor.Invoke(new object[]
            {
                System.Enum.ToObject(kindEnum, FixedResolutionKind), w, h, label
            });

            if (addSize == null)
            {
                sb.AppendLine("  AddCustomSize not found on '" + groupType.Name + "'; skipping");
                sb.AppendLine("  the methods it does have: " + MethodNames(groupType));
                return -1;
            }

            addSize.Invoke(group, new[] { size });

            count = (int)getTotal.Invoke(group, null);

            found = count - 1;
            sb.AppendLine("    added '" + label + "' at index " + found);
        }

        var gameViewType = EditorAsm.GetType("UnityEditor.GameView");
        if (gameViewType == null)
        {
            sb.AppendLine("  game view type not found; the resolution is in the list but");
            sb.AppendLine("  could not be selected automatically");
            return found;
        }

        var gameView = EditorWindow.GetWindow(gameViewType);
        if (gameView == null)
        {
            sb.AppendLine("  game view window not found; resolution added but not selected");
            return found;
        }

        // selectedSizeIndex is a property on this internal window. If it cannot
        // be found the list still has the entry, which the user can pick by hand
        // from the resolution dropdown.
        var indexProp = gameView.GetType().GetProperty("selectedSizeIndex",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (indexProp == null)
        {
            sb.AppendLine("  selectedSizeIndex not found on the game view; the resolution");
            sb.AppendLine("  is in the dropdown but was not selected automatically");
            return found;
        }

        indexProp.SetValue(gameView, found, null);
        sb.AppendLine("  selectedSizeIndex set to " + found);

        // SizeSelectionCallback is what actually applies a selection, so setting
        // the property alone would change the number without changing anything
        // that gets drawn.
        //
        // Its signature is not stable: in older Unity it took the index as an
        // argument, and in this version it takes none at all and re-applies
        // whatever the property now says. Invoking it with a hardcoded single
        // argument throws TargetParameterCountException on the second version.
        // The arguments are therefore built from the parameters the method
        // actually declares, rather than assumed.
        var callback = gameView.GetType().GetMethod("SizeSelectionCallback",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (callback == null)
        {
            gameView.Repaint();
            sb.AppendLine("  SizeSelectionCallback not found; repainted instead, which may");
            sb.AppendLine("  need a click on the resolution dropdown to take effect");
            return found;
        }

        var ps = callback.GetParameters();
        var args = new object[ps.Length];
        var described = new List<string>();

        for (int i = 0; i < ps.Length; i++)
        {
            // An int parameter is the index being selected. A bool on this
            // callback means "add to the list first", which is only wanted when
            // the size was just created, and false is the safe answer here
            // because the entry was already in the list.
            if (ps[i].ParameterType == typeof(int)) args[i] = found;
            else if (ps[i].ParameterType == typeof(bool)) args[i] = false;
            else args[i] = ps[i].ParameterType.IsValueType
                ? Activator.CreateInstance(ps[i].ParameterType)
                : null;

            described.Add(ps[i].ParameterType.Name + " " + ps[i].Name +
                          " = " + (args[i] ?? "null"));
        }

        sb.AppendLine("  SizeSelectionCallback takes " + ps.Length + " parameter(s): " +
                      string.Join(", ", described.ToArray()));

        try
        {
            callback.Invoke(gameView, args);
            sb.AppendLine("  selection applied through SizeSelectionCallback");
        }
        catch (System.Exception e)
        {
            // The selection is already stored on the window, so a repaint is
            // still worth trying rather than giving up here.
            sb.AppendLine("  SizeSelectionCallback threw: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine("    the methods it does have: " + MethodNames(gameView.GetType()));
            gameView.Repaint();
            sb.AppendLine("  repainted instead; the selection may need a click on the");
            sb.AppendLine("  resolution dropdown to take effect");
        }

        return found;
    }

    /// <summary>
    /// Reads the selection back out of the game's own size list. Without this
    /// the report would be describing the call that was made rather than the
    /// state that resulted, and the two are not the same thing when the
    /// reflection misses.
    /// </summary>
    static string DescribeSelection(StringBuilder sb, int wanted, int w, int h)
    {
        if (wanted < 0) return "none";

        var sizesType = EditorAsm.GetType("UnityEditor.GameViewSizes");
        if (sizesType == null) return "cannot read back";

        var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        object sizes = singletonType.GetProperty("instance",
            BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
        if (sizes == null) return "cannot read back";

        var groupEnum = EditorAsm.GetType("UnityEditor.GameViewSizeGroupType");
        var getGroup = sizesType.GetMethod("GetGroup");
        if (getGroup == null) return "GetGroup not found";

        var group = getGroup.Invoke(sizes,
            new object[] { System.Enum.ToObject(groupEnum, StandaloneGroup) });
        if (group == null) return "standalone group is null";

        // Again on the group, not the size list. Getting this wrong makes the
        // read-back fail for the same reason the write did, which would leave
        // the verification reporting "cannot read back" for a fix that in fact
        // worked.
        var groupType = group.GetType();
        var getTotal = FindMethod(groupType, "GetTotalCount");
        var getSize = FindMethod(groupType, "GetGameViewSize");
        if (getTotal == null || getSize == null) return "group methods not found";

        int count = (int)getTotal.Invoke(group, null);
        if (wanted >= count) return "index " + wanted + " is out of range (" + count + " sizes)";

        var size = getSize.Invoke(group, new object[] { wanted });
        if (size == null) return "index " + wanted + " is empty";

        int sw = (int)size.GetType().GetProperty("width").GetValue(size, null);
        int sh = (int)size.GetType().GetProperty("height").GetValue(size, null);

        if (sw == w && sh == h)
            return "index " + wanted + " is " + sw + "x" + sh + ", which is the monitor's. OK.";

        return "index " + wanted + " is " + sw + "x" + sh + ", NOT " + w + "x" + h + ". FAILED.";
    }
    // ---------------------------------------------------------------------
    // reflection helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Looks a method up by name and reports when it is absent, instead of
    /// handing back a null MethodInfo for the caller to dereference. Every
    /// method this tool calls is on an internal Unity class, and the failure
    /// mode of guessing a name wrong is a NullReferenceException on the next
    /// line that says nothing about what was actually missing.
    /// </summary>
    static MethodInfo FindMethod(Type t, string name)
    {
        if (t == null) return null;

        var m = t.GetMethod(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (m != null) return m;

        // Overloaded or differently-declared variants still count.
        foreach (var c in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (c.Name == name) return c;
        }

        return null;
    }

    /// <summary>
    /// Lists what a type actually has. A report that names the methods it
    /// looked for and did not find is worth ten that throw, because the next
    /// attempt can use the right one.
    /// </summary>
    static string MethodNames(Type t)
    {
        if (t == null) return "(type is null)";

        var names = new List<string>();
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (names.Contains(m.Name)) continue;
            names.Add(m.Name);
        }

        names.Sort();
        return string.Join(", ", names.ToArray());
    }

    // ---------------------------------------------------------------------
    // the panel itself
    // ---------------------------------------------------------------------

    /// <summary>
    /// Widens the panel. The docked panel at its default size is why the game
    /// was being drawn at 876x453 in the first place, so the resolution being
    /// right is only half the fix — it also has to be shown at that size.
    /// </summary>
    static EditorWindow Maximise(StringBuilder sb)
    {
        var type = EditorAsm.GetType("UnityEditor.GameView");
        if (type == null)
        {
            sb.AppendLine("  game view type not found; panel not maximised");
            return null;
        }

        var win = EditorWindow.GetWindow(type);
        if (win == null)
        {
            sb.AppendLine("  no game view window open");
            return null;
        }

        sb.AppendLine("  panel before: " + Mathf.RoundToInt(win.position.width) + " x " +
                      Mathf.RoundToInt(win.position.height) + "   maximized=" + win.maximized);

        if (!win.maximized)
        {
            win.maximized = true;
            sb.AppendLine("  maximised the panel, which is what it was docked away from");
        }

        return win;
    }
}
