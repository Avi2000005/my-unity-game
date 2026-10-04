using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

// Deliberately in the global namespace and deliberately dependent on nothing
// but UnityEngine, so that run_script can compile and run it even while the
// project assembly is mid-repair.
public static class PhysicsApiProbe
{
    const string Report = "Temp/physics_api.txt";

    /// <summary>
    /// Prints the real signature of a Unity physics query, because the
    /// overload set is not what memory says it is.
    ///
    /// Physics.OverlapCapsule has been written here three different ways, each
    /// from a different and equally confident recollection of the signature,
    /// and each was refused by the compiler with a different complaint:
    /// six arguments is no such overload, four arguments wants an int fourth,
    /// five arguments wants a QueryTriggerInteraction fifth. Guessing a fourth
    /// time is not a plan. Every method whose name contains the query word is
    /// printed with its full parameter list, so the answer comes from the
    /// running engine rather than from anybody's memory.
    /// </summary>
    public static void Run()
    {
        var sb = new StringBuilder();

        try
        {
            Body(sb);
        }
        catch (Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] physics api\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        var type = typeof(Physics);

        sb.AppendLine("Physics.OverlapCapsule and its neighbours, as this engine " +
                      "actually declares them");
        sb.AppendLine("Unity " + Application.unityVersion);
        sb.AppendLine();

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name.StartsWith("Overlap", StringComparison.Ordinal))
            .Where(m => m.Name.IndexOf("Capsule", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        m.Name.IndexOf("NonAlloc", StringComparison.Ordinal) >= 0)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ThenBy(m => m.GetParameters().Length)
            .ToArray();

        if (methods.Length == 0)
        {
            sb.AppendLine("No matching static methods found on Physics. That is " +
                          "itself a finding, and it means the reflection below is " +
                          "looking somewhere unexpected.");
            return;
        }

        foreach (var m in methods)
        {
            var ps = m.GetParameters();

            sb.AppendLine(m.ReturnType.Name + " " + m.Name + "(" +
                          string.Join(", ", ps.Select(Describe)) + ")");
            sb.AppendLine("        // " + m.GetParameters().Length + " argument(s)");
        }

        sb.AppendLine();
        sb.AppendLine("--- the non-allocating family, which is often the better call ---");

        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(m => m.Name.StartsWith("Capsule", StringComparison.Ordinal))
                     .OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            sb.AppendLine("  " + m.ReturnType.Name + " " + m.Name + "(" +
                          string.Join(", ", m.GetParameters().Select(Describe)) + ")");
        }
    }

    /// <summary>
    /// One parameter, with its type spelled out. Default values are shown
    /// because an omitted optional argument and a wrong one compile the same
    /// way and behave differently.
    /// </summary>
    static string Describe(ParameterInfo p)
    {
        string s = p.ParameterType.Name + " " + p.Name;

        if (p.IsOptional)
            s += " = " + (p.RawDefaultValue == null
                             ? "null"
                             : p.RawDefaultValue.ToString());

        return s;
    }
}
