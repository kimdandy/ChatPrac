using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LocalChat
{
    public class LocalChatMessage
    {
        public string sender;
        public string content;
        public string createdAt;
    }

    /// <summary>
    /// 로컬 채팅 UI 컨트롤러 (서버 통신 없음)
    ///  - 보낸 메시지를 메모리에만 저장하고 최신 N개(기본 10개)를 표시
    ///  - Enter: 전송 / Shift+Enter: 줄바꿈
    /// </summary>
    public class LocalChatUIController : MonoBehaviour
    {
        [Header("References")]
        public TMP_InputField inputField;
        public Button sendButton;
        public TMP_Text messagesText;
        public ScrollRect messagesScroll;
        public LayoutElement inputBoxLayout;

        [Header("Chat")]
        [Tooltip("내 유저 이름")]
        public string userName = "User";
        public int maxMessages = 10;

        [Header("Test Reply")]
        [Tooltip("상대 유저 이름")]
        public string partnerName = "Test";
        [Tooltip("이 문구를 입력했을 때만 상대가 응답 (대소문자 구분, 앞뒤 공백 무시)")]
        public string triggerText = "test";
        [Tooltip("내 메시지 전송 후 상대 응답까지의 지연(초)")]
        public float replyDelay = 2f;
        public string replyText = "업로드 정상";

        [Header("Input Box")]
        [Tooltip("입력 박스 기본 높이 (15pt x 1.5)")]
        public float baseInputHeight = 22.5f;
        [Tooltip("Shift+Enter로 줄이 늘어날 때 입력 박스가 커지는 최대 줄 수 (넘으면 박스 안에서 스크롤)")]
        public int maxVisibleInputLines = 4;

        [Header("Style")]
        public Color myNameColor = new Color(0.45f, 0.8f, 1f);
        public Color otherNameColor = new Color(1f, 0.85f, 0.4f);

        readonly List<LocalChatMessage> _messages = new List<LocalChatMessage>();
        int _sendQueuedFrame = -1;

        void Awake()
        {
            if (string.IsNullOrEmpty(userName)) userName = "User";
            if (string.IsNullOrEmpty(partnerName)) partnerName = "Test";

            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.textComponent.textWrappingMode = TextWrappingModes.Normal;
            inputField.onValidateInput = ValidateInput;
            inputField.onSelect.AddListener(_ => EnableIme(true));
            inputField.onDeselect.AddListener(_ => EnableIme(false));
            sendButton.onClick.AddListener(Send);

            messagesText.textWrappingMode = TextWrappingModes.Normal;
            messagesText.richText = true;
        }

        void Start()
        {
            Render();
            inputField.ActivateInputField();
        }

        void Update()
        {
            UpdateInputHeight();
        }

        void LateUpdate()
        {
            // Enter 입력 후 최소 한 프레임 뒤, 한글 IME 조합이 확정된 뒤에 전송
            if (_sendQueuedFrame >= 0 && Time.frameCount > _sendQueuedFrame && !IsImeComposing())
            {
                _sendQueuedFrame = -1;
                Send();
            }
        }

        // ---------------- 입력 처리 ----------------

        char ValidateInput(string text, int charIndex, char addedChar)
        {
            if (addedChar == '\n' || addedChar == '\r')
            {
                if (IsShiftHeld()) return '\n'; // Shift+Enter → 줄바꿈
                _sendQueuedFrame = Time.frameCount; // Enter → 전송 (IME 조합 확정 후 LateUpdate에서 처리)
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

        static bool IsImeComposing()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return !string.IsNullOrEmpty(Input.compositionString);
#else
            return false;
#endif
        }

        static void EnableIme(bool on)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Input.imeCompositionMode = on ? IMECompositionMode.On : IMECompositionMode.Auto;
#endif
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null) kb.SetIMEEnabled(on);
#endif
        }

        public void Send()
        {
            string text = inputField.text.Trim();
            inputField.text = "";
            FocusInput();
            if (string.IsNullOrEmpty(text)) return;

            AddMessage(userName, text);
            if (text == triggerText) StartCoroutine(ReplyAfterDelay());
        }

        IEnumerator ReplyAfterDelay()
        {
            // triggerText("test")를 보냈을 때만 replyDelay초 뒤 상대(Test)의 확인 응답 출력
            yield return new WaitForSeconds(replyDelay);
            AddMessage(partnerName, replyText);
        }

        /// <summary>외부(테스트용 봇 등)에서 메시지를 추가할 때 사용</summary>
        public void AddMessage(string sender, string content)
        {
            _messages.Add(new LocalChatMessage
            {
                sender = sender,
                content = content,
                createdAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
            if (_messages.Count > maxMessages)
                _messages.RemoveRange(0, _messages.Count - maxMessages); // 최신 N개만 유지
            Render();
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

        // ---------------- 표시 ----------------

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
                  .Append("</b></color> : ")
                  .Append(NoParse(m.content));
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
    }
}
