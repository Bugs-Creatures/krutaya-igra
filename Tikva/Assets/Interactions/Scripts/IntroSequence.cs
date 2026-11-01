using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public sealed class IntroSequence : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [TextArea(4, 10)] public string story = "Это был обычный день. Я просто смотрел телевизор. 6-ой канал никогда не работал, но в тот день на нём появилась странная трансляция. Там был я. Они снимают меня везде. Вся моя жизнь ненастоящая. Надо сбежать отсюда.";
    public Texture2D defaultCursor;
    public Texture2D handCursor;
    [Min(0.001f)] public float secondsPerCharacter = 0.04f;

    TextMeshProUGUI storyLabel, continueLabel;
    TMP_FontAsset font;
    bool ready, hovered;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetCursor(false);
        font = TMP_FontAsset.CreateFontAsset("Arial", "Regular", 48);

        var canvasObject = new GameObject("Intro Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var background = new GameObject("Black background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        background.transform.SetParent(canvasObject.transform, false);
        var backgroundRect = (RectTransform)background.transform;
        backgroundRect.anchorMin = Vector2.zero; backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;
        var image = background.GetComponent<UnityEngine.UI.Image>();
        image.color = Color.black; image.raycastTarget = false;

        storyLabel = MakeLabel("Story", canvasObject.transform, 30);
        storyLabel.alignment = TextAlignmentOptions.Center;
        storyLabel.textWrappingMode = TextWrappingModes.Normal;
        storyLabel.raycastTarget = false;
        var storyRect = storyLabel.rectTransform;
        storyRect.anchorMin = new Vector2(0.12f, 0.25f); storyRect.anchorMax = new Vector2(0.88f, 0.8f);
        storyRect.offsetMin = storyRect.offsetMax = Vector2.zero;
        storyLabel.text = story;
        storyLabel.maxVisibleCharacters = 0;

        continueLabel = MakeLabel("Continue", canvasObject.transform, 25);
        continueLabel.alignment = TextAlignmentOptions.Center;
        continueLabel.text = "Нажмите чтобы продолжить";
        var continueRect = continueLabel.rectTransform;
        continueRect.anchorMin = new Vector2(0.2f, 0.06f); continueRect.anchorMax = new Vector2(0.8f, 0.16f);
        continueRect.offsetMin = continueRect.offsetMax = Vector2.zero;
        // Pointer events are handled by this component on the label itself.
        var target = continueLabel.gameObject.AddComponent<IntroContinueTarget>();
        target.owner = this;
        continueLabel.gameObject.SetActive(false);

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(transform, false);
        StartCoroutine(TypeStory());
    }

    TextMeshProUGUI MakeLabel(string name, Transform parent, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = font; label.fontSize = size; label.color = Color.white;
        return label;
    }

    IEnumerator TypeStory()
    {
        storyLabel.ForceMeshUpdate();
        int count = storyLabel.textInfo.characterCount;
        for (int i = 1; i <= count; i++)
        {
            yield return new WaitForSecondsRealtime(secondsPerCharacter);
            storyLabel.maxVisibleCharacters = i;
        }
        ready = true;
        continueLabel.gameObject.SetActive(true);
    }

    void SetCursor(bool hand) => Cursor.SetCursor(hand ? handCursor : defaultCursor, hand ? new Vector2(8, 4) : new Vector2(4, 5), CursorMode.Auto);
    public void OnPointerEnter(PointerEventData eventData) { if (!ready) return; hovered = true; continueLabel.color = Color.yellow; SetCursor(true); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; continueLabel.color = Color.white; SetCursor(false); }
    public void OnPointerClick(PointerEventData eventData) { if (ready && eventData.button == PointerEventData.InputButton.Left) SceneManager.LoadScene("GameScene"); }
    void OnApplicationFocus(bool focus) { if (focus) SetCursor(hovered); }
    void OnDestroy()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (!font) return;
        foreach (var atlas in font.atlasTextures) if (atlas) Destroy(atlas);
        if (font.material) Destroy(font.material);
        Destroy(font);
    }
}

public sealed class IntroContinueTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public IntroSequence owner;
    public void OnPointerEnter(PointerEventData data) => owner.OnPointerEnter(data);
    public void OnPointerExit(PointerEventData data) => owner.OnPointerExit(data);
    public void OnPointerClick(PointerEventData data) => owner.OnPointerClick(data);
}
