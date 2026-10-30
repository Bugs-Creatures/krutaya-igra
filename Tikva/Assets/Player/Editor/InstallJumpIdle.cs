using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;
public static class InstallJumpIdle
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode");
        var player=Object.FindAnyObjectByType<SurvivalPlayerController>();
        var controller=(AnimatorController)player.animator.runtimeAnimatorController;
        var machine=controller.layers[0].stateMachine;
        var idle=machine.states.Select(s=>s.state).First(s=>s.name=="Idle");
        string path="Assets/Player/jump_idle.anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(!clip){clip=Object.Instantiate((AnimationClip)idle.motion);clip.name="jump_idle";AssetDatabase.CreateAsset(clip,path);}
        AnimationUtility.SetAnimationEvents(clip,new AnimationEvent[0]);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
        if(!controller.parameters.Any(p=>p.name=="isJumping"))controller.AddParameter("isJumping",AnimatorControllerParameterType.Bool);
        var jump=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Jump");if(!jump)jump=machine.AddState("Jump",new Vector3(450,250));jump.motion=clip;
        foreach(var transition in machine.anyStateTransitions)
        {
            if(transition.destinationState==jump)continue;
            if(!transition.conditions.Any(c=>c.parameter=="isJumping"))transition.AddCondition(AnimatorConditionMode.IfNot,0,"isJumping");
        }
        foreach(var state in machine.states.Select(s=>s.state))foreach(var transition in state.transitions)
            if(transition.destinationState!=jump&&!transition.conditions.Any(c=>c.parameter=="isJumping"))transition.AddCondition(AnimatorConditionMode.IfNot,0,"isJumping");
        var enter=machine.anyStateTransitions.FirstOrDefault(t=>t.destinationState==jump);
        if(!enter){enter=machine.AddAnyStateTransition(jump);enter.AddCondition(AnimatorConditionMode.If,0,"isJumping");}
        enter.duration=0;enter.hasExitTime=false;enter.canTransitionToSelf=false;
        EditorUtility.SetDirty(clip);EditorUtility.SetDirty(jump);EditorUtility.SetDirty(machine);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
    }
}
