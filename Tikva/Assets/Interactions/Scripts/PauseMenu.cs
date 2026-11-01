using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[DefaultExecutionOrder(-1000)]
public sealed class PauseMenu : MonoBehaviour
{
    public PlayerInteraction interaction;
    public GameObject window;
    public UnityEngine.UI.Button closeButton;
    public UnityEngine.UI.Slider sensitivitySlider,volumeSlider;
    public TextMeshProUGUI sensitivityValue,volumeValue;
    static PauseMenu instance;
    static int toggledFrame=-1;
    static float pausedSeconds;
    bool paused,previousModal,previousAudioPause,previousCursorVisible;
    float previousTimeScale,pauseStarted;
    CursorLockMode previousCursorLock;
    public static bool IsPaused=>instance&&instance.paused;
    public static bool BlocksGameplayInput=>IsPaused||Time.frameCount==toggledFrame;
    public static float GameplayUnscaledTime=>Time.unscaledTime-pausedSeconds-(IsPaused?Time.unscaledTime-instance.pauseStarted:0);
    void Awake()
    {
        instance=this;pausedSeconds=0;toggledFrame=-1;
        if(!interaction)interaction=GetComponent<PlayerInteraction>();
        if(window)window.SetActive(false);
        if(closeButton)closeButton.onClick.AddListener(Resume);
        if(sensitivitySlider)sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        if(volumeSlider)volumeSlider.onValueChanged.AddListener(SetVolume);
    }
    void OnEnable()=>instance=this;
    void Update()
    {
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){Toggle();return;}
        if(!paused||!interaction||!interaction.prompt)return;
        var mouse=Mouse.current;bool hand=false;
        if(mouse!=null)
        {
            var point=mouse.position.ReadValue();
            hand=GameHudUI.Hit((RectTransform)closeButton.transform,point)||GameHudUI.Hit((RectTransform)sensitivitySlider.transform,point)||GameHudUI.Hit((RectTransform)volumeSlider.transform,point);
        }
        interaction.prompt.SetInteractionCursor(hand);
    }
    public void Toggle(){if(paused)Resume();else Open();}
    public void Open()
    {
        if(paused||!window||!interaction||!interaction.player)return;
        var player=interaction.player;
        previousTimeScale=Time.timeScale;previousAudioPause=AudioListener.pause;previousModal=player.modalInputLocked;
        previousCursorLock=Cursor.lockState;previousCursorVisible=Cursor.visible;
        pauseStarted=Time.unscaledTime;paused=true;toggledFrame=Time.frameCount;
        sensitivitySlider.SetValueWithoutNotify(player.mouseSensitivity);volumeSlider.SetValueWithoutNotify(AudioListener.volume);RefreshValues();
        player.modalInputLocked=true;Time.timeScale=0;AudioListener.pause=true;
        interaction.prompt.ClearFeedback();interaction.prompt.Present(null,Vector2.zero,false);interaction.prompt.UseDefaultCursor();
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;window.SetActive(true);
    }
    public void Resume()
    {
        if(!paused)return;
        pausedSeconds+=Time.unscaledTime-pauseStarted;paused=false;toggledFrame=Time.frameCount;
        Time.timeScale=previousTimeScale;AudioListener.pause=previousAudioPause;if(window)window.SetActive(false);
        if(interaction)
        {
            if(interaction.player){interaction.player.modalInputLocked=previousModal;interaction.player.dialogueClickFrame=Time.frameCount;}
            if(interaction.prompt)interaction.prompt.UseDefaultCursor();
        }
        Cursor.lockState=previousCursorLock;Cursor.visible=previousCursorVisible;
    }
    public void SetSensitivity(float value)
    {
        float sensitivity=Mathf.Round(Mathf.Clamp(value,sensitivitySlider.minValue,sensitivitySlider.maxValue)*100)/100;
        if(interaction&&interaction.player)interaction.player.mouseSensitivity=sensitivity;
        sensitivitySlider.SetValueWithoutNotify(sensitivity);RefreshValues();
    }
    public void SetVolume(float value)
    {
        AudioListener.volume=Mathf.Round(Mathf.Clamp01(value)*100)/100;
        volumeSlider.SetValueWithoutNotify(AudioListener.volume);RefreshValues();
    }
    void RefreshValues()
    {
        if(sensitivityValue&&interaction&&interaction.player)sensitivityValue.text=interaction.player.mouseSensitivity.ToString("0.00",CultureInfo.InvariantCulture);
        if(volumeValue)volumeValue.text=Mathf.RoundToInt(AudioListener.volume*100)+"%";
    }
    void OnDisable(){if(paused)Resume();if(instance==this)instance=null;}
}
