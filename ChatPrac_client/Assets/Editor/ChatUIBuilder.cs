using ChatSystem;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tools > Chat > Build Chat UI : 현재 씬에 채팅 UI를 생성합니다. (기존 ChatCanvas는 교체)
/// </summary>
public static class ChatUIBuilder
{
    const float FontSize = 15f;
    const float InputHeight = FontSize * 1.5f;      // 22.5
    const float ViewHeight = InputHeight * 10f;     // 225
    const float BoxWidth = 400f;
    const float SendButtonWidth = 48f;

    [MenuItem("Tools/Chat/Build Chat UI")]
    public static void Build()
    {
        var old = GameObject.Find("ChatCanvas");
        if (old != null) Undo.DestroyObjectImmediate(old);

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        // Canvas
        var canvasGo = new GameObject("ChatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Chat UI");
        canvasGo.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Panel (두 박스를 세로로 배치, 가로 폭 동일)
        var panel = CreateUI("ChatPanel", canvasGo.transform);
        panel.anchorMin = panel.anchorMax = Vector2.zero;
        panel.pivot = Vector2.zero;
        panel.anchoredPosition = new Vector2(20, 20);
        panel.sizeDelta = new Vector2(BoxWidth, ViewHeight + InputHeight + 4);
        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ---------- 조회 박스 ----------
        var view = CreateUI("MessageView", panel);
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        var viewLe = view.gameObject.AddComponent<LayoutElement>();
        viewLe.preferredHeight = ViewHeight;
        viewLe.minHeight = ViewHeight;
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;

        var viewport = CreateUI("Viewport", view);
        Stretch(viewport, 6, 4, 6, 4);
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = CreateUI("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var msgText = content.gameObject.AddComponent<TextMeshProUGUI>();
        msgText.font = font;
        msgText.fontSize = FontSize;
        msgText.textWrappingMode = TextWrappingModes.Normal;   // 가로 초과 시 자동 줄바꿈
        msgText.overflowMode = TextOverflowModes.Overflow;
        msgText.alignment = TextAlignmentOptions.TopLeft;
        msgText.color = Color.white;
        msgText.richText = true;
        msgText.raycastTarget = false;
        msgText.text = "";
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;

        // ---------- 입력 박스 ----------
        var inputBox = CreateUI("InputBox", panel);
        var inputBg = inputBox.gameObject.AddComponent<Image>();
        inputBg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        var inputLe = inputBox.gameObject.AddComponent<LayoutElement>();
        inputLe.preferredHeight = InputHeight;
        inputLe.minHeight = InputHeight;

        var textArea = CreateUI("TextArea", inputBox);
        Stretch(textArea, 6, 0, SendButtonWidth + 4, 0);
        textArea.gameObject.AddComponent<RectMask2D>();

        var placeholder = CreateUI("Placeholder", textArea);
        Stretch(placeholder, 0, 0, 0, 0);
        var ph = placeholder.gameObject.AddComponent<TextMeshProUGUI>();
        ph.font = font;
        ph.fontSize = FontSize;
        ph.fontStyle = FontStyles.Italic;
        ph.color = new Color(1, 1, 1, 0.35f);
        ph.text = "메시지 입력 (Enter 전송 / Shift+Enter 줄바꿈)";
        ph.textWrappingMode = TextWrappingModes.Normal;
        ph.alignment = TextAlignmentOptions.MidlineLeft;
        ph.raycastTarget = false;

        var inputTextRt = CreateUI("Text", textArea);
        Stretch(inputTextRt, 0, 0, 0, 0);
        var inputText = inputTextRt.gameObject.AddComponent<TextMeshProUGUI>();
        inputText.font = font;
        inputText.fontSize = FontSize;
        inputText.color = Color.white;
        inputText.textWrappingMode = TextWrappingModes.Normal;  // 가로 초과 시 자동 줄바꿈
        inputText.alignment = TextAlignmentOptions.MidlineLeft;
        inputText.richText = false;

        var input = inputBox.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = inputBg;
        input.textViewport = textArea;
        input.textComponent = inputText;
        input.placeholder = ph;
        input.fontAsset = font;
        input.pointSize = FontSize;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.richText = false;
        input.caretColor = Color.white;
        input.customCaretColor = true;
        input.onFocusSelectAll = false;
        input.restoreOriginalTextOnEscape = false;
        input.scrollSensitivity = 20f;

        // 전송 버튼 (입력 박스 우측 끝단)
        var btnRt = CreateUI("SendButton", inputBox);
        btnRt.anchorMin = new Vector2(1, 0);
        btnRt.anchorMax = new Vector2(1, 1);
        btnRt.pivot = new Vector2(1, 0.5f);
        btnRt.anchoredPosition = Vector2.zero;
        btnRt.sizeDelta = new Vector2(SendButtonWidth, 0);
        var btnImg = btnRt.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.5f, 0.9f, 1f);
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var btnLabelRt = CreateUI("Label", btnRt);
        Stretch(btnLabelRt, 0, 0, 0, 0);
        var btnLabel = btnLabelRt.gameObject.AddComponent<TextMeshProUGUI>();
        btnLabel.font = font;
        btnLabel.fontSize = FontSize;
        btnLabel.text = "전송";
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.color = Color.white;
        btnLabel.raycastTarget = false;

        // ---------- 컨트롤러 ----------
        var api = panel.gameObject.AddComponent<ChatApiClient>();
        var ctrl = panel.gameObject.AddComponent<ChatUIController>();
        ctrl.api = api;
        ctrl.inputField = input;
        ctrl.sendButton = btn;
        ctrl.messagesText = msgText;
        ctrl.messagesScroll = scroll;
        ctrl.inputBoxLayout = inputLe;
        ctrl.baseInputHeight = InputHeight;

        EnsureEventSystem();

        Selection.activeGameObject = panel.gameObject;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGo.scene);
    }

    static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
