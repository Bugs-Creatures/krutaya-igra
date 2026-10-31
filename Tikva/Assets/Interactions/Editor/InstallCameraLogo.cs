using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class InstallCameraLogo
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var root=GameObject.Find("Camera Broadcast Canvas");
        var player=Object.FindAnyObjectByType<SurvivalPlayerController>();var font=Object.FindAnyObjectByType<InteractionPrompt>().font;
        if(!root)
        {
            var canvas=GameHudUI.Canvas("Camera Broadcast Canvas",180);root=canvas.gameObject;
            var logo=GameHudUI.Rect("Russia 67 logo",root.transform,new Vector2(250,70),new Vector2(-26,-26));logo.anchorMin=logo.anchorMax=Vector2.one;logo.pivot=Vector2.one;
            var blue=new Color(.035f,.25f,.56f);var red=new Color(.72f,.035f,.09f);
            var bluePanel=GameHudUI.Rect("Blue",logo,new Vector2(174,40),new Vector2(-33,10));GameHudUI.Image(bluePanel,blue);bluePanel.localRotation=Quaternion.Euler(0,0,3);
            var number=GameHudUI.Rect("Red",logo,new Vector2(64,40),new Vector2(87,10));GameHudUI.Image(number,red);number.localRotation=Quaternion.Euler(0,0,3);
            GameHudUI.Text("Channel",logo,font,"РОССИЯ",25,Color.white,new Vector2(170,40),new Vector2(-33,10)).fontStyle=FontStyles.Bold|FontStyles.Italic;
            GameHudUI.Text("Number",logo,font,"67",29,Color.white,new Vector2(64,40),new Vector2(87,10)).fontStyle=FontStyles.Bold|FontStyles.Italic;
            var live=GameHudUI.Rect("Live",logo,new Vector2(236,21),new Vector2(0,-23));GameHudUI.Image(live,red);GameHudUI.Text("Live label",live,font,"ПРЯМОЙ ЭФИР",13,Color.white,new Vector2(230,20),Vector2.zero).characterSpacing=3;
            var group=root.AddComponent<CanvasGroup>();group.alpha=1;group.interactable=false;group.blocksRaycasts=false;
        }
        var driver=root.GetComponent<CameraOnlyCanvas>();if(!driver)driver=root.AddComponent<CameraOnlyCanvas>();driver.player=player;
        EditorUtility.SetDirty(driver);EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
    }
}
