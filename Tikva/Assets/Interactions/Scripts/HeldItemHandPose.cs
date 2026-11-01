using System.Collections.Generic;
using UnityEngine;

// A small additive grip over the existing locomotion animation; never edits the animation clips.
[DefaultExecutionOrder(1100)]
public sealed class HeldItemHandPose : MonoBehaviour
{
    public PlayerInteraction interaction;
    [Tooltip("Optional NPC hand bone, instead of the player's hand.")]
    public Transform handBone;
    [Tooltip("NPC grip is active only while this item belongs to the specified hand.")]
    public InteractionObject externalHeldItem;
    [Min(.01f)] public float blendTime=.12f;
    [Range(0,1)] public float gripStrength=1;
    readonly List<Transform> fingers=new();
    readonly List<Quaternion> baseRotations=new();
    readonly List<float> angles=new();
    readonly List<bool> thumbs=new();
    Transform wrist;
    float weight;
    bool applied;
    void Awake()
    {
        if(!interaction)interaction=GetComponent<PlayerInteraction>();
        wrist=handBone?handBone:interaction&&interaction.rightHand?interaction.rightHand.parent:null;
        if(!wrist)return;
        foreach(var bone in wrist.GetComponentsInChildren<Transform>())
        {
            bool thumb=bone.name.Contains("RightHandThumb");
            bool finger=thumb||bone.name.Contains("RightHandIndex")||bone.name.Contains("RightHandMiddle")||bone.name.Contains("RightHandRing")||bone.name.Contains("RightHandPinky");
            if(!finger)continue;
            char joint=bone.name[bone.name.Length-1];if(joint<'1'||joint>'3')continue;
            fingers.Add(bone);thumbs.Add(thumb);baseRotations.Add(bone.localRotation);
            angles.Add(thumb?(joint=='1'?30:joint=='2'?25:15):(joint=='1'?55:joint=='2'?75:40));
        }
    }
    void Restore()
    {
        if(!applied)return;
        for(int n=0;n<fingers.Count;n++)if(fingers[n])fingers[n].localRotation=baseRotations[n];
        applied=false;
    }
    void Update()=>Restore(); // Restore before Animator evaluates the next frame, avoiding cumulative curls.
    void LateUpdate()
    {
        if(!wrist)return;
        Restore();
        bool playerHolding=interaction&&interaction.player&&interaction.heldItem&&!interaction.player.EffectiveFirstPerson;
        bool npcHolding=externalHeldItem&&externalHeldItem.IsHeld&&externalHeldItem.transform.IsChildOf(wrist);
        float target=playerHolding||npcHolding?gripStrength:0;
        weight=Mathf.MoveTowards(weight,target,Time.deltaTime/Mathf.Max(.01f,blendTime));
        if(weight<=0)return;
        for(int n=0;n<fingers.Count;n++)
        {
            var bone=fingers[n];if(!bone)continue;
            baseRotations[n]=bone.localRotation;
            var axis=bone.InverseTransformDirection(wrist.TransformDirection(thumbs[n]?Vector3.up:Vector3.right));
            bone.localRotation=baseRotations[n]*Quaternion.AngleAxis(angles[n]*weight,axis);
        }
        applied=true;
    }
    void OnDisable(){Restore();weight=0;}
}
