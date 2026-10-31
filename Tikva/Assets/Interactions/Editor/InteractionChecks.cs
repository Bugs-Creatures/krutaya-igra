using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class InteractionChecks
{
    public static object Run()
    {
        if(!Application.isPlaying)throw new System.InvalidOperationException("Play mode required.");
        var i=Object.FindAnyObjectByType<PlayerInteraction>();var p=i.player;var origin=p.transform.position;var rotation=p.transform.rotation;bool mode=p.firstPerson;
        var items=Object.FindObjectsByType<InteractionObject>();
        var crow=items.First(o=>o.kind==InteractionKind.Pickup&&o.suspicious);
        var screwdriver=items.First(o=>o.kind==InteractionKind.Pickup&&!o.suspicious);
        var door=items.First(o=>o.kind==InteractionKind.Door);
        var r=new Dictionary<string,bool>();
        void Position(Vector3 point,bool fps){p.Teleport(point);p.cameraRig.UpdateView(.016f,0);p.SetFirstPerson(fps);p.cameraRig.UpdateView(.016f,0);}
        Position(new Vector3(3.75f,3.05f,17.4f),false);
        r["successful pickup has no notice"]=i.ClickTarget(screwdriver,new Vector2(300,300))&&i.heldItem==screwdriver&&i.prompt.VisibleText=="";
        r["right hand attachment"]=screwdriver.transform.parent==i.rightHand;
        r["public held name"]=p.currentHeldItem=="Screwdriver";
        r["held Rigidbody interpolation disabled"]=screwdriver.GetComponent<Rigidbody>().interpolation==RigidbodyInterpolation.None;
        p.SetFirstPerson(true);p.cameraRig.UpdateView(.016f,0);i.RefreshHeldGrip();
        r["ordinary item moves to first person anchor"]=screwdriver.transform.parent==i.firstPersonAnchor;
        var previousFirstRotation=screwdriver.firstPersonHeldRotation;
        screwdriver.firstPersonHeldRotation+=new Vector3(0,15,0);i.RefreshHeldGrip();
        r["first person rotation editable"]=Quaternion.Angle(screwdriver.transform.localRotation,Quaternion.Euler(screwdriver.firstPersonHeldRotation))<.01f;
        screwdriver.firstPersonHeldRotation=previousFirstRotation;i.RefreshHeldGrip();
        p.SetFirstPerson(false);p.cameraRig.UpdateView(.016f,0);i.RefreshHeldGrip();
        r["ordinary item returns to hand"]=screwdriver.transform.parent==i.rightHand;
        var previousThirdRotation=screwdriver.thirdPersonHeldRotation;
        screwdriver.thirdPersonHeldRotation+=new Vector3(0,15,0);i.RefreshHeldGrip();
        r["camera mode rotation editable"]=Quaternion.Angle(screwdriver.transform.localRotation,Quaternion.Euler(screwdriver.thirdPersonHeldRotation))<.01f;
        screwdriver.thirdPersonHeldRotation=previousThirdRotation;i.RefreshHeldGrip();
        Position(new Vector3(30,3.05f,30),false);
        var farRay=new Ray(crow.transform.position+Vector3.up*.7f,Vector3.down);
        r["distant target still found"]=i.TargetAt(farRay)==crow&&!i.InReach(crow);
        crow.Outline.Show(true,Color.red);i.prompt.Present(crow,new Vector2(300,300),false);
        r["hover alone has no text"]=i.prompt.VisibleText=="";
        i.prompt.ShowClickFeedback(crow,new Vector2(300,300),false,false);
        r["distant suspicious item red with far text"]=crow.Outline.CurrentColor==Color.red&&i.prompt.VisibleText=="вы далеко";
        r["distant pickup denied with notice"]=!i.ClickTarget(crow,new Vector2(300,300))&&i.prompt.VisibleText=="вы далеко";
        Position(new Vector3(1.35f,3.05f,12.6f),false);
        r["suspicious third person denied with notice"]=!i.ClickTarget(crow,new Vector2(300,300))&&i.heldItem==screwdriver&&i.prompt.VisibleText=="подозрительное действие";
        crow.Outline.Show(true,Color.red);i.prompt.Present(crow,new Vector2(300,300),false);
        i.prompt.ShowClickFeedback(crow,new Vector2(300,300),false,true);
        r["red suspicious prompt"]=crow.Outline.CurrentColor==Color.red&&i.prompt.VisibleText=="подозрительное действие";
        var previousWarning=crow.suspiciousHoverText;var previousSize=crow.hoverTextSize;
        crow.suspiciousHoverText="нельзя брать при свидетелях";crow.hoverTextSize=31;
        i.prompt.ShowClickFeedback(crow,new Vector2(300,300),false,true);
        r["custom suspicious text and size"]=i.prompt.VisibleText=="нельзя брать при свидетелях"&&Mathf.Approximately(i.prompt.VisibleFontSize,31);
        crow.suspiciousHoverText=previousWarning;crow.hoverTextSize=previousSize;
        r["pointing hand cursor"]=i.prompt.HandShown&&i.prompt.linkCursor;
        p.SetFirstPerson(true);crow.Outline.Show(true,PlayerInteraction.AvailableOutlineColor);i.prompt.Present(crow,new Vector2(640,360),true);
        i.prompt.ShowClickFeedback(crow,new Vector2(640,360),true,true);
        r["FPS yellow pickup prompt"]=crow.Outline.CurrentColor==Color.yellow&&i.prompt.VisibleText=="подобрать"&&!i.prompt.HandShown;
        var previousAvailable=crow.availableHoverText;crow.availableHoverText="взять лом";
        i.prompt.ShowClickFeedback(crow,new Vector2(640,360),true,true);
        r["custom available text"]=i.prompt.VisibleText=="взять лом";
        crow.availableHoverText=previousAvailable;
        var aim=new Ray(crow.transform.position+Vector3.up*.7f,Vector3.down);
        r["aim ray resolves item"]=i.TargetAt(aim)==crow;
        r["FPS suspicious pickup has no notice"]=i.ClickTarget(crow,new Vector2(640,360))&&i.heldItem==crow&&p.currentHeldItem=="Crowbar"&&crow.transform.parent==i.firstPersonAnchor&&i.prompt.VisibleText=="";
        var rb=screwdriver.GetComponent<Rigidbody>();
        r["previous item physically dropped"]=!screwdriver.IsHeld&&screwdriver.transform.parent==null&&rb.useGravity&&!rb.isKinematic&&screwdriver.GetComponentsInChildren<Collider>().Any(c=>c.enabled);
        var temporary=new GameObject("Test pickup without Rigidbody");
        temporary.AddComponent<BoxCollider>();var simple=temporary.AddComponent<InteractionObject>();simple.kind=InteractionKind.Pickup;
        simple.Attach(i.rightHand);simple.Drop(p.transform.position+Vector3.up,new Collider[0]);
        r["new item without Rigidbody drops with collisions"]=simple.GetComponent<Rigidbody>().detectCollisions;
        Object.Destroy(temporary);
        p.SetFirstPerson(false);p.cameraRig.UpdateView(.016f,0);i.EnforceHeldRules();
        r["forced suspicious drop"]=i.heldItem==null&&!crow.IsHeld&&crow.GetComponent<Rigidbody>().useGravity&&p.currentHeldItem=="";
        Position(new Vector3(3.75f,3.05f,13.7f),false);
        r["door opens without notice"]=i.ClickTarget(door,new Vector2(400,400))&&door.IsOpen&&i.prompt.VisibleText=="";
        i.prompt.ShowClickFeedback(door,new Vector2(400,400),false,true);r["door close prompt"]=i.prompt.VisibleText=="закрыть";
        r["Cyrillic font"]="подобратьоткрытьподозрительноедействиезакрыть".All(c=>i.prompt.font.HasCharacter(c));
        crow.Outline.Show(false,PlayerInteraction.AvailableOutlineColor);i.prompt.Present(null,Vector2.zero,false);
        p.Teleport(origin);p.transform.rotation=rotation;p.SetFirstPerson(mode);p.cameraRig.UpdateView(.016f,0);return r;
    }
}
