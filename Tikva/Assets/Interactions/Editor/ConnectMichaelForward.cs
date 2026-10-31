using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;
public static class ConnectMichaelForward
{
    public static void Apply()
    {
        var npc=Object.FindAnyObjectByType<MichaelBehaviour>();var controller=(AnimatorController)npc.animator.runtimeAnimatorController;
        if(!controller.parameters.Any(p=>p.name=="walkForward"))controller.AddParameter("walkForward",AnimatorControllerParameterType.Bool);
        var machine=controller.layers[0].stateMachine;var forward=machine.states.First(s=>s.state.name=="walking").state;var backward=machine.states.First(s=>s.state.name=="Walk Backward").state;
        foreach(var transition in machine.anyStateTransitions.Concat(machine.states.SelectMany(s=>s.state.transitions)))
        {
            if(transition.destinationState!=forward&&transition.destinationState!=backward)continue;
            foreach(var condition in transition.conditions.Where(c=>c.parameter=="walkForward").ToArray())transition.RemoveCondition(condition);
            transition.AddCondition(transition.destinationState==forward?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"walkForward");EditorUtility.SetDirty(transition);
        }
        foreach(var state in new[]{forward,backward})
        {
            if(machine.anyStateTransitions.Any(t=>t.destinationState==state))continue;
            var transition=machine.AddAnyStateTransition(state);transition.hasExitTime=false;transition.duration=.1f;transition.canTransitionToSelf=false;
            transition.AddCondition(AnimatorConditionMode.If,0,"isWalking");transition.AddCondition(state==forward?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"walkForward");
        }
        var idle=machine.states.First(s=>s.state.name=="Idle").state;
        if(!machine.anyStateTransitions.Any(t=>t.destinationState==idle))
        {
            var transition=machine.AddAnyStateTransition(idle);transition.hasExitTime=false;transition.duration=.1f;transition.canTransitionToSelf=false;transition.AddCondition(AnimatorConditionMode.IfNot,0,"isWalking");
        }
        EditorUtility.SetDirty(machine);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
    }
}
