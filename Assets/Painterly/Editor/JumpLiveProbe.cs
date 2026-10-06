using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class JumpLiveProbe
    {
    	private const string Report = "Temp/jump_live.txt";

    	private const string ControllerPath = "Assets/Art/Ari/Ari.controller";

    	[MenuItem("Tools/Echoes/Probe Jump", priority = 98)]
    	public static void Run()
    	{
    		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
    		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder sb = new StringBuilder();
    		sb.AppendLine("[Echoes] does the jump work?");
    		sb.AppendLine("mode: " + (EditorApplication.isPlaying ? "play" : "edit") + "  (play is better; edit still drives the real code path)");
    		AuditController(sb);
    		GameObject ari = GameObject.Find("Ari");
    		if ((Object)(object)ari == (Object)null)
    		{
    			sb.AppendLine("\nNo Ari in the scene. Nothing to jump.");
    			Finish(sb);
    			return;
    		}
    		Animator anim = ari.GetComponent<Animator>();
    		AriMover mover = ari.GetComponent<AriMover>();
    		sb.AppendLine("\n--- on Ari ---");
    		sb.AppendLine("  Animator: " + (((Object)(object)anim == (Object)null) ? "MISSING" : ("enabled=" + ((Behaviour)anim).enabled + " controller=" + (((Object)(object)anim.runtimeAnimatorController == (Object)null) ? "<NONE>" : ((Object)anim.runtimeAnimatorController).name) + " avatar=" + (((Object)(object)anim.avatar == (Object)null) ? "<NONE>" : ((Object)anim.avatar).name))));
    		sb.AppendLine("  AriMover: " + (((Object)(object)mover == (Object)null) ? "MISSING" : "present"));
    		if ((Object)(object)anim == (Object)null || (Object)(object)mover == (Object)null)
    		{
    			Finish(sb);
    			return;
    		}
    		bool enabled = ((Behaviour)anim).enabled;
    		if (!((Behaviour)anim).enabled)
    		{
    			((Behaviour)anim).enabled = true;
    		}
    		bool enabled2 = ((Behaviour)mover).enabled;
    		if (EditorApplication.isPlaying)
    		{
    			((Behaviour)mover).enabled = false;
    		}
    		Vector3 position = ari.transform.position;
    		Transform foot = ari.GetComponentsInChildren<Transform>(true).FirstOrDefault((Transform t) => ((Object)t).name == "LeftFoot");
    		Vector3 footAtRest = (((Object)(object)foot != (Object)null) ? foot.position : Vector3.zero);
    		try
    		{
    			Guard(sb, "standing jump", () =>
    			{
    				//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    				StandJump(ari, anim, mover, foot, footAtRest, 1f / 60f, sb);
    			});
    			Guard(sb, "walking jump", () =>
    			{
    				WalkingJump(ari, anim, mover, 1f / 60f, sb);
    			});
    			Guard(sb, "held jump", () =>
    			{
    				NoDoubleJump(ari, anim, mover, 1f / 60f, sb);
    			});
    		}
    		finally
    		{
    			ari.transform.position = position;
    			((Behaviour)mover).enabled = enabled2;
    			((Behaviour)anim).enabled = enabled;
    		}
    		Finish(sb);
    	}

    	private static void Guard(StringBuilder sb, string what, Action test)
    	{
    		try
    		{
    			test();
    		}
    		catch (Exception ex)
    		{
    			sb.AppendLine("  " + what + " THREW: " + ex.GetType().Name + " — " + (ex.Message ?? "").Split('\n', StringSplitOptions.None)[0]);
    		}
    	}

    	private static void AuditController(StringBuilder sb)
    	{
    		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- controller ---");
    		AnimatorController val = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/Ari/Ari.controller");
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("  Assets/Art/Ari/Ari.controller NOT FOUND. Run Tools/Echoes/Build Ari Controller.");
    			return;
    		}
    		string[] array = new string[3] { "Speed", "Jump", "Grounded" };
    		foreach (string name in array)
    		{
    			AnimatorControllerParameter val2 = val.parameters.FirstOrDefault((AnimatorControllerParameter x) => x.name == name);
    			sb.AppendLine("  parameter " + name + ": " + ((val2 == null) ? "MISSING" : (((object)val2.type/*cast due to constrained. prefix*/).ToString() + " default=" + val2.defaultBool + "/" + val2.defaultFloat)));
    		}
    		AnimatorStateMachine stateMachine = val.layers[0].stateMachine;
    		sb.AppendLine("  default state: " + (((Object)(object)stateMachine.defaultState == (Object)null) ? "<none>" : ((Object)stateMachine.defaultState).name));
    		ChildAnimatorState[] states = stateMachine.states;
    		for (int i = 0; i < states.Length; i++)
    		{
    			ChildAnimatorState val3 = states[i];
    			AnimatorState state = val3.state;
    			Motion motion = state.motion;
    			AnimationClip val4 = (AnimationClip)(object)((motion is AnimationClip) ? motion : null);
    			sb.AppendLine("  state '" + ((Object)state).name + "' motion=" + (((Object)(object)state.motion == (Object)null) ? "<none>" : ("'" + ((Object)state.motion).name + "'")) + (((Object)(object)val4 == (Object)null) ? "" : (" len=" + val4.length.ToString("F2") + "s loop=" + ((Motion)val4).isLooping)) + (state.writeDefaultValues ? " [writeDefaults ON]" : ""));
    			AnimatorStateTransition[] transitions = state.transitions;
    			foreach (AnimatorStateTransition val5 in transitions)
    			{
    				string text = string.Join(" AND ", ((AnimatorTransitionBase)val5).conditions.Select((AnimatorCondition c) =>
    				{
    					//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    					//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    					return c.parameter + " " + ((object)c.mode/*cast due to constrained. prefix*/).ToString();
    				}));
    				sb.AppendLine("      -> '" + (((Object)(object)((AnimatorTransitionBase)val5).destinationState == (Object)null) ? "<exit>" : ((Object)((AnimatorTransitionBase)val5).destinationState).name) + "'  [" + text + "]  exitTime=" + val5.exitTime.ToString("F2") + " duration=" + val5.duration.ToString("F2"));
    			}
    		}
    	}

    	private static void StandJump(GameObject ari, Animator anim, AriMover mover, Transform foot, Vector3 footAtRest, float dt, StringBuilder sb)
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- one jump from standing ---");
    		for (int i = 0; i < 30; i++)
    		{
    			mover.Step(Vector3.zero, running: false, dt);
    			anim.Update(dt);
    		}
    		float y = ari.transform.position.y;
    		sb.AppendLine("  standing at y=" + y.ToString("F4") + "  grounded=" + mover.IsGrounded + "  state=" + ClipNow(anim));
    		mover.Step(Vector3.zero, running: false, dt, jump: true);
    		anim.Update(dt);
    		float num = y;
    		float num2 = 0f;
    		int num3 = 0;
    		bool flag = false;
    		string text = "-";
    		int num4 = 0;
    		bool isGrounded = mover.IsGrounded;
    		int num5 = Mathf.RoundToInt(2f / dt);
    		for (int j = 0; j < num5; j++)
    		{
    			mover.Step(Vector3.zero, running: false, dt);
    			anim.Update(dt);
    			float y2 = ari.transform.position.y;
    			if (y2 > num)
    			{
    				num = y2;
    				num2 = (float)j * dt;
    			}
    			if (mover.IsAirborne)
    			{
    				num3++;
    			}
    			if (mover.IsAirborne && text == "-")
    			{
    				text = ClipNow(anim);
    			}
    			AnimatorStateInfo currentAnimatorStateInfo = anim.GetCurrentAnimatorStateInfo(0);
    			if (currentAnimatorStateInfo.shortNameHash == Animator.StringToHash("Ari_Jump"))
    			{
    				flag = true;
    			}
    			if (mover.IsGrounded && !isGrounded)
    			{
    				num4++;
    			}
    			isGrounded = mover.IsGrounded;
    			if (num4 > 0 && j > 10)
    			{
    				break;
    			}
    		}
    		float y3 = ari.transform.position.y;
    		sb.AppendLine("  left the ground: " + ((num3 > 0) ? ("yes, for " + ((float)num3 * dt).ToString("F2") + "s") : "NO — HE NEVER LEFT THE PAVING"));
    		sb.AppendLine("  peak y=" + num.ToString("F4") + " at " + num2.ToString("F2") + "s  =>  " + (num - y).ToString("F3") + " m up  (jumpHeight is set to 1.2)");
    		sb.AppendLine("  landed back at y=" + y3.ToString("F4") + ", " + (y3 - y).ToString("F4") + " m from where he took off" + ((num4 == 1) ? "  (one landing, no bounce)" : ("  LANDINGS=" + num4)));
    		sb.AppendLine("  mover reported LastJumpHeight = " + mover.LastJumpHeight.ToString("F3") + " m");
    		sb.AppendLine("  grounded now=" + mover.IsGrounded);
    		sb.AppendLine("  graph entered Ari_Jump: " + flag);
    		sb.AppendLine("  state on the frame after the jump key went down: " + text);
    		for (int k = 0; k < 20; k++)
    		{
    			mover.Step(Vector3.zero, running: false, dt);
    			anim.Update(dt);
    		}
    		string text2 = ClipNow(anim);
    		sb.AppendLine("  state once settled: " + text2 + ((text2.Contains("Ari_Idle") && !text2.Contains("transitioning=True")) ? "  — he is back to standing" : "  — CHECK: did not come back to rest"));
    		if ((Object)(object)foot != (Object)null)
    		{
    			float num6 = Vector3.Distance(foot.position, footAtRest);
    			sb.AppendLine("  LeftFoot ended " + num6.ToString("F3") + " m from where it started — " + ((num6 > 0.02f) ? "the clip is posing his" : "THE BONES DID NOT MOVE"));
    		}
    		else
    		{
    			sb.AppendLine("  no LeftFoot bone found on the rig");
    		}
    	}

    	private static void WalkingJump(GameObject ari, Animator anim, AriMover mover, float dt, StringBuilder sb)
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
    		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- jump while walking ---");
    		Vector3 position = ari.transform.position;
    		for (int i = 0; i < 40; i++)
    		{
    			mover.Step(Vector3.forward, running: false, dt);
    			anim.Update(dt);
    		}
    		string text = ClipNow(anim);
    		sb.AppendLine("  after 40 frames of forward input: " + text);
    		sb.AppendLine("  CurrentSpeed=" + mover.CurrentSpeed.ToString("F2"));
    		mover.Step(Vector3.forward, running: false, dt, jump: true);
    		anim.Update(dt);
    		bool flag = false;
    		for (int j = 0; j < 8; j++)
    		{
    			mover.Step(Vector3.forward, running: false, dt);
    			anim.Update(dt);
    			AnimatorStateInfo currentAnimatorStateInfo = anim.GetCurrentAnimatorStateInfo(0);
    			if (currentAnimatorStateInfo.shortNameHash == Animator.StringToHash("Ari_Jump"))
    			{
    				flag = true;
    			}
    		}
    		sb.AppendLine("  jumped out of the walk: " + flag + (flag ? "" : "  — THE WALK-TO-JUMP EDGE IS MISSING"));
    		ari.transform.position = position;
    		for (int k = 0; k < 30; k++)
    		{
    			mover.Step(Vector3.zero, running: false, dt);
    			anim.Update(dt);
    		}
    	}

    	private static void NoDoubleJump(GameObject ari, Animator anim, AriMover mover, float dt, StringBuilder sb)
    	{
    		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- one press, then a press held down mid-air ---");
    		int num = Mathf.RoundToInt(0.5f / dt);
    		Settle(mover, anim, dt);
    		float y = ari.transform.position.y;
    		mover.Step(Vector3.zero, running: false, dt, jump: true);
    		anim.Update(dt);
    		float num2 = y;
    		for (int i = 0; i < num; i++)
    		{
    			mover.Step(Vector3.zero, running: false, dt);
    			anim.Update(dt);
    			num2 = Mathf.Max(num2, ari.transform.position.y);
    		}
    		float num3 = num2 - y;
    		bool isGrounded = mover.IsGrounded;
    		Settle(mover, anim, dt);
    		float y2 = ari.transform.position.y;
    		float num4 = y2;
    		bool flag = false;
    		for (int j = 0; j < num; j++)
    		{
    			mover.Step(Vector3.zero, running: false, dt, jump: true);
    			anim.Update(dt);
    			num4 = Mathf.Max(num4, ari.transform.position.y);
    			if (mover.IsGrounded)
    			{
    				flag = true;
    			}
    		}
    		float num5 = num4 - y2;
    		sb.AppendLine("  single press        : peak " + num3.ToString("F3") + " m" + (isGrounded ? "" : "  (still in the air at the end of the window)"));
    		sb.AppendLine("  key held all through: peak " + num5.ToString("F3") + " m");
    		sb.AppendLine("  difference          : " + (num5 - num3).ToString("F3") + " m — " + ((num5 - num3 < 0.05f) ? "the mid-air presses were ignored, there is no double jump" : "HE GETS A SECOND HOP, which the design does not give him"));
    		sb.AppendLine("  touched down inside half a second: " + flag + (flag ? "  WRONG" : "  (no, as expected)"));
    		for (int k = 0; k < Mathf.RoundToInt(2f / dt); k++)
    		{
    			mover.Step(Vector3.zero, running: false, dt);
    			anim.Update(dt);
    			if (mover.IsGrounded && k > 5)
    			{
    				break;
    			}
    		}
    		float y3 = ari.transform.position.y;
    		sb.AppendLine("  after the flight: y=" + y3.ToString("F4") + " from " + y2.ToString("F4") + ", " + (y3 - y2).ToString("F4") + " m from take-off" + ((Mathf.Abs(y3 - y2) < 0.3f) ? "  — back on the paving, not through it" : "  — HE HAS SANK THROUGH IT OR DRIFTED"));
    		sb.AppendLine("  grounded=" + mover.IsGrounded + ", mover measured LastJumpHeight=" + mover.LastJumpHeight.ToString("F3") + " m");
    	}

    	private static void Settle(AriMover mover, Animator anim, float dt)
    	{
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		for (int i = 0; i < 30; i++)
    		{
    			mover.Step(Vector3.zero, running: false, dt);
    			anim.Update(dt);
    		}
    	}

    	private static string ClipNow(Animator anim)
    	{
    		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    		AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
    		RuntimeAnimatorController runtimeAnimatorController = anim.runtimeAnimatorController;
    		if ((Object)(object)runtimeAnimatorController == (Object)null)
    		{
    			return "clip=<NO CONTROLLER ASSIGNED>";
    		}
    		AnimationClip val = runtimeAnimatorController.animationClips.FirstOrDefault((AnimationClip c) => Animator.StringToHash(((Object)c).name) == info.shortNameHash);
    		return "clip=" + (((Object)(object)val == (Object)null) ? "?" : ((Object)val).name) + " t=" + info.normalizedTime.ToString("F2") + " transitioning=" + anim.IsInTransition(0);
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp/jump_live.txt");
    		Directory.CreateDirectory(Path.GetDirectoryName(path));
    		File.WriteAllText(path, sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}