using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class InstallPauseMenu
{
    [MenuItem("Tools/Interactions/Install pause menu")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        var interaction=Object.FindAnyObjectByType<PlayerInteraction>();
        var menu=interaction.GetComponent<PauseMenu>();if(!menu)menu=interaction.gameObject.AddComponent<PauseMenu>();
        menu.interaction=interaction;
        if(menu.window)return;
        var canvas=GameHudUI.Canvas("Pause Canvas",500);
        canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();menu.window=canvas.gameObject;
        var backdrop=GameHudUI.Rect("Backdrop",canvas.transform,Vector2.zero,Vector2.zero);
        backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
        GameHudUI.Image(backdrop,new Color(0,0,0,.65f)).raycastTarget=true;
        var panel=GameHudUI.Rect("Pause window",canvas.transform,new Vector2(700,300),Vector2.zero);
        GameHudUI.Image(panel,new Color(.065f,.065f,.065f,1)).raycastTarget=true;
        var border=panel.gameObject.AddComponent<UnityEngine.UI.Outline>();border.effectColor=new Color(.4f,.4f,.38f,1);border.effectDistance=Vector2.one;
        var title=GameHudUI.Text("Title",panel,interaction.prompt.font,"Пауза",28,new Color(.94f,.94f,.91f),new Vector2(500,42),new Vector2(-60,100));
        title.alignment=TextAlignmentOptions.Left;
        var line=GameHudUI.Rect("Divider",panel,new Vector2(624,1),new Vector2(0,66));GameHudUI.Image(line,new Color(.3f,.3f,.29f,1));
        var close=GameHudUI.Rect("Close",panel,new Vector2(38,38),new Vector2(313,105));
        var closeImage=GameHudUI.Image(close,Color.clear);closeImage.raycastTarget=true;
        menu.closeButton=close.gameObject.AddComponent<UnityEngine.UI.Button>();menu.closeButton.targetGraphic=closeImage;menu.closeButton.transition=UnityEngine.UI.Selectable.Transition.None;
        GameHudUI.Text("Cross",close,interaction.prompt.font,"×",32,Color.white,new Vector2(38,38),Vector2.zero);
        UnityEngine.UI.Slider Row(string name,string label,float y,float min,float max,out TextMeshProUGUI value)
        {
            var row=GameHudUI.Rect(name,panel,new Vector2(624,48),new Vector2(0,y));
            var caption=GameHudUI.Text("Label",row,interaction.prompt.font,label,20,new Color(.9f,.9f,.87f),new Vector2(200,42),new Vector2(-212,0));caption.alignment=TextAlignmentOptions.Left;
            var sliderRect=GameHudUI.Rect("Slider",row,new Vector2(300,36),new Vector2(45,0));
            var hit=GameHudUI.Image(sliderRect,Color.clear);hit.raycastTarget=true;
            var slider=sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.minValue=min;slider.maxValue=max;slider.wholeNumbers=false;
            var track=GameHudUI.Rect("Track",sliderRect,new Vector2(280,4),Vector2.zero);GameHudUI.Image(track,new Color(.25f,.25f,.24f,1));
            var fillArea=GameHudUI.Rect("Fill area",sliderRect,new Vector2(280,4),Vector2.zero);
            var fill=GameHudUI.Rect("Fill",fillArea,Vector2.zero,Vector2.zero);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;
            GameHudUI.Image(fill,new Color(.76f,.76f,.72f,1));slider.fillRect=fill;
            var handleArea=GameHudUI.Rect("Handle area",sliderRect,new Vector2(280,24),Vector2.zero);
            var handle=GameHudUI.Rect("Handle",handleArea,new Vector2(12,0),Vector2.zero);var handleImage=GameHudUI.Image(handle,new Color(.94f,.94f,.9f,1));handleImage.raycastTarget=true;
            slider.handleRect=handle;slider.targetGraphic=handleImage;slider.transition=UnityEngine.UI.Selectable.Transition.None;
            slider.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            value=GameHudUI.Text("Value",row,interaction.prompt.font,"",20,new Color(.94f,.94f,.91f),new Vector2(90,42),new Vector2(265,0));value.alignment=TextAlignmentOptions.Right;
            return slider;
        }
        menu.sensitivitySlider=Row("Sensitivity","Чувствительность",15,.01f,.6f,out var sensitivity);menu.sensitivityValue=sensitivity;
        menu.volumeSlider=Row("Volume","Звук",-60,0,1,out var volume);menu.volumeValue=volume;
        menu.sensitivitySlider.SetValueWithoutNotify(interaction.player.mouseSensitivity);menu.volumeSlider.SetValueWithoutNotify(1);
        sensitivity.text=interaction.player.mouseSensitivity.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);volume.text="100%";
        var events=Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        if(!events)
        {
            var go=new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem));
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        }
        canvas.gameObject.SetActive(false);EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);EditorSceneManager.SaveScene(interaction.gameObject.scene);
    }
}
