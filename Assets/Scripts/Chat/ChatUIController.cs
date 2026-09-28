using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChatSystem
{
    /// <summary>
    /// 채팅 UI 컨트롤러
    ///  - 최신 N개(기본 10개) 메시지 표시, 가로 초과 시 자동 줄바꿈
    ///  - Enter: 전송 / Shift+Enter: 줄바꿈
    ///  - 조회 시점: 씬 시작 시, 내 메시지 전송 직후
    /// </summary>
    public class ChatUIController : MonoBehaviour
    {
        [Header("References")]
        public ChatApiClient api;
        public TMP_InputField inputField;
        public Button sendButton;
        public TMP_Text messagesText;
        public ScrollRect messagesScroll;
        public LayoutElement inputBoxLayout;

        [Header("Chat")]
        [Tooltip("비워두면 실행 시 User#### 형태로 자동 생성")]
        public string userName = "";
        public int maxMessages = 10;

        [Header("Input Box")]
        [Tooltip("입력 박스 기본 높이 (15pt x 1.5)")]
        public float baseInputHeight = 22.5f;
        [Tooltip("Shift+Enter로 줄이 늘어날 때 입력 박스가 커지는 최대 줄 수 (넘으면 박스 안에서 스크롤)")]
        public int maxVisibleInputLines = 4;

        [Header("Style")]
        public Color myNameColor = new Color(0.45f, 0.8f, 1f);
        public Color otherNameColor = new Color(1f, 0.85f, 0.4f);
        public Color errorColor = new Color(1f, 0.45f, 0.45f);

        [Header("Font")]
        [Tooltip("기본 TMP 폰트(LiberationSans)에 없는 한글을 표시하기 위한 OS 폰트 폴백")]
        public string osFallbackFontFamily = "Malgun Gothic";

        readonly List<ChatMessage> _messages = new List<ChatMessage>();
        string _status = "";
        bool _sendQueued;
        bool _sending;
        bool _fetching;
        bool _fetchAgain;

        void Awake()
        {
            if (string.IsNullOrEmpty(userName)) userName = "User" + Random.Range(1000, 10000);
            if (api == null) api = GetComponent<ChatApiClient>();

            SetupFontFallback();

            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.textComponent.textWrappingMode = TextWrappingModes.Normal;
            inputField.onValidateInput = ValidateInput;
            sendButton.onClick.AddListener(Send);

            messagesText.textWrappingMode = TextWrappingModes.Normal;
            messagesText.richText = true;
        }

        void Start()
        {
            Render();
            RequestFetch();
            inputField.ActivateInputField();
        }

        void Update()
        {
            UpdateInputHeight();
        }

        void LateUpdate()
        {
            if (_sendQueued)
            {
                _sendQueued = false;
                Send();
            }
        }

        // ---------------- 입력 처리 ----------------

        char ValidateInput(string text, int charIndex, char addedChar)
        {
            if (addedChar == '\n' || addedChar == '\r')
            {
                if (IsShiftHeld()) return '\n'; // Shift+Enter → 줄바꿈
                _sendQueued = true;            // Enter → 전송 (IME 조합 확정 후 처리하도록 한 프레임 늦춤)
                return '\0';
            }
            return addedChar;
        }

        static bool IsShiftHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.shiftKey.isPressed) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return true;
#endif
            return false;
        }

        public void Send()
        {
            string text = inputField.text.Trim();
            if (string.IsNullOrEmpty(text) || _sending) { FocusInput(); return; }

            _sending = true;
            sendButton.interactable = false;
            inputField.text = "";
            FocusInput();

            StartCoroutine(api.PostMessage(userName, text,
                () =>
                {
                    _sending = false;
                    sendButton.interactable = true;
                    _status = "";
                    RequestFetch();
                },
                err =>
                {
                    _sending = false;
                    sendButton.interactable = true;
                    inputField.text = text; // 실패 시 입력 내용 복구
                    inputField.caretPosition = text.Length;
                    SetError(err);
                }));
        }

        void FocusInput()
        {
            inputField.ActivateInputField();
            inputField.Select();
        }

        void UpdateInputHeight()
        {
            if (inputBoxLayout == null) return;
            var tc = inputField.textComponent;
            int lines = Mathf.Max(1, tc.textInfo != null ? tc.textInfo.lineCount : 1);
            float lineH = tc.font != null && tc.font.faceInfo.pointSize > 0
                ? tc.font.faceInfo.lineHeight / tc.font.faceInfo.pointSize * tc.fontSize
                : tc.fontSize * 1.2f;
            float target = baseInputHeight + (Mathf.Min(lines, maxVisibleInputLines) - 1) * lineH;
            if (!Mathf.Approximately(inputBoxLayout.preferredHeight, target))
                inputBoxLayout.preferredHeight = target;
        }

        // ---------------- 조회/표시 ----------------

        public void RequestFetch()
        {
            if (_fetching) { _fetchAgain = true; return; }
            StartCoroutine(FetchRoutine());
        }

        IEnumerator FetchRoutine()
        {
            _fetching = true;
            do
            {
                _fetchAgain = false;
                yield return api.FetchMessages(OnMessagesReceived, SetError);
            } while (_fetchAgain);
            _fetching = false;
        }

        void OnMessagesReceived(List<ChatMessage> list)
        {
            _status = "";
            _messages.Clear();
            _messages.AddRange(SortOldestFirst(list));
            if (_messages.Count > maxMessages)
                _messages.RemoveRange(0, _messages.Count - maxMessages); // 최신 N개만 유지
            Render();
        }

        static IEnumerable<ChatMessage> SortOldestFirst(List<ChatMessage> list)
        {
            long dummy;
            if (list.Count > 0 && list.All(m => long.TryParse(m.id, out dummy)))
                return list.OrderBy(m => long.Parse(m.id));
            if (list.Count > 0 && list.All(m => !string.IsNullOrEmpty(m.createdAt)))
                return list.OrderBy(m => m.createdAt, System.StringComparer.Ordinal);
            return list; // 정렬 기준이 없으면 서버 순서(오래된 → 최신) 그대로
        }

        void SetError(string err)
        {
            Debug.LogWarning("[Chat] " + err);
            _status = err;
            Render();
        }

        void Render()
        {
            var sb = new StringBuilder();
            string myHex = ColorUtility.ToHtmlStringRGB(myNameColor);
            string otherHex = ColorUtility.ToHtmlStringRGB(otherNameColor);

            foreach (var m in _messages)
            {
                if (sb.Length > 0) sb.Append('\n');
                bool mine = m.sender == userName;
                sb.Append("<color=#").Append(mine ? myHex : otherHex).Append("><b>")
                  .Append(NoParse(string.IsNullOrEmpty(m.sender) ? "?" : m.sender))
                  .Append("</b></color>: ")
                  .Append(NoParse(m.content));
            }

            if (!string.IsNullOrEmpty(_status))
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(errorColor)).Append(">")
                  .Append(NoParse(_status)).Append("</color>");
            }

            messagesText.text = sb.ToString();

            // 최신 메시지가 보이도록 아래로 스크롤
            if (messagesScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                messagesScroll.verticalNormalizedPosition = 0f;
            }
        }

        static string NoParse(string s)
        {
            // 사용자가 입력한 <, > 등이 리치 텍스트 태그로 해석되지 않도록 처리
            return "<noparse>" + (s ?? "").Replace("</noparse>", "</no parse>") + "</noparse>";
        }

        // ---------------- 폰트 ----------------

        TMP_FontAsset _patchedFont;
        TMP_FontAsset _osFont;

        void SetupFontFallback()
        {
            TMP_FontAsset baseFont = messagesText.font;
            if (baseFont == null || string.IsNullOrEmpty(osFallbackFontFamily)) return;

            try { _osFont = TMP_FontAsset.CreateFontAsset(osFallbackFontFamily, "Regular"); }
            catch (System.Exception ex) { Debug.LogWarning("[Chat] OS 폰트 폴백 생성 실패: " + ex.Message); }
            if (_osFont == null) { Debug.LogWarning("[Chat] OS 폰트를 찾지 못했습니다: " + osFallbackFontFamily); return; }

            // 기본 폰트의 폴백 목록에 런타임으로만 추가하고, OnDestroy에서 되돌립니다 (에셋 파일은 변경되지 않음)
            if (baseFont.fallbackFontAssetTable == null) baseFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            baseFont.fallbackFontAssetTable.Insert(0, _osFont);
            _patchedFont = baseFont;
        }

        void OnDestroy()
        {
            if (_patchedFont != null && _patchedFont.fallbackFontAssetTable != null)
                _patchedFont.fallbackFontAssetTable.Remove(_osFont);
            if (_osFont != null) Destroy(_osFont);
        }
    }
}
