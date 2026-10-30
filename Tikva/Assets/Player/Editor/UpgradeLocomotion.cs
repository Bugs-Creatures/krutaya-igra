using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UpgradeLocomotion
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
        var player=Object.FindAnyObjectByType<SurvivalPlayerController>();
        var controller=(AnimatorController)player.animator.runtimeAnimatorController;
        Undo.RegisterCompleteObjectUndo(controller,"Connect locomotion animations");
        foreach(string p in new[]{"isWalking","isRunning","isWalkingBackward"})
            if(!controller.parameters.Any(v=>v.name==p))controller.AddParameter(p,AnimatorControllerParameterType.Bool);
        var machine=controller.layers[0].stateMachine;
        foreach(var t in machine.anyStateTransitions)machine.RemoveAnyStateTransition(t);
        var existing=machine.states.Select(s=>s.state).ToArray();
        foreach(var s in existing)foreach(var t in s.transitions)s.RemoveTransition(t);
        AnimatorState State(string name,string clip,int index)
        {
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name);
            if(!state)state=index<existing.Length?existing[index]:machine.AddState(name,new Vector3(index*220,100));
            state.name=name;state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Player/"+clip+".anim");
            if(!state.motion)throw new System.InvalidOperationException("Missing animation: "+clip);
            var settings=AnimationUtility.GetAnimationClipSettings((AnimationClip)state.motion);settings.loopTime=true;
            AnimationUtility.SetAnimationClipSettings((AnimationClip)state.motion,settings);
            state.writeDefaultValues=true;EditorUtility.SetDirty(state);return state;
        }
        var idle=State("Idle","idle",0);var walk=State("Walk","walking",1);
        var backward=State("Walk Backward","walking_backward",2);var run=State("Run","running",3);
        machine.defaultState=idle;
        void Transition(AnimatorState state,bool moving,bool running,bool backing)
        {
            var t=machine.AddAnyStateTransition(state);t.hasExitTime=false;t.duration=.12f;t.hasFixedDuration=true;t.canTransitionToSelf=false;
            t.AddCondition(moving?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"isWalking");
            if(moving){t.AddCondition(running?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"isRunning");t.AddCondition(backing?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"isWalkingBackward");}
        }
        Transition(idle,false,false,false);Transition(walk,true,false,false);Transition(backward,true,false,true);Transition(run,true,true,false);
        player.cameraRig.droneOffset=new Vector3(7,9,-7);player.cameraRig.orthographicDrone=false;player.cameraRig.orthographicSize=6;
        player.cameraRig.avoidObstacles=true;player.cameraRig.collisionRadius=.35f;
        EditorUtility.SetDirty(controller);EditorUtility.SetDirty(machine);EditorUtility.SetDirty(player.cameraRig);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
