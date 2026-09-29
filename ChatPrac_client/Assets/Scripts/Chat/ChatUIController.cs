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
    ///  - 조회 시점: 씬 시작 시, 내 메시지 전송 직후, 그리고 pollInterval초마다 자동(폴링)
    ///  - 폴링은 afterId(마지막으로 받은 id) 이후의 새 메시지만 받아 목록 뒤에 붙입니다
    ///  - 시작 시 방 참가(개발용) → 서버가 만든 시스템 메시지(입장·퇴장 알림, 공지)는 녹색으로 표시
    ///  - 내 메시지 구분은 userId로 (닉네임은 서버가 userId로 찾아서 붙여줌)
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
        [Tooltip("내 userId (서버의 POST /api/v1/users 로 만든 유저)")]
        public long userId = 1;
        [Tooltip("[개발용] 시작할 때 방에 자동으로 참가 (POST /api/v1/rooms/{roomId}/members)")]
        public bool autoJoinRoom = true;
        public int maxMessages = 10;

        [Header("Polling (새 메시지 자동 수신)")]
        [Tooltip("새 메시지를 확인하는 간격(초). 0 이하면 자동 수신 끔")]
        public float pollInterval = 2f;
        [Tooltip("폴링 N회마다 한 번은 전체 목록을 다시 받아 동기화 (서버/Redis 초기화 대비)")]
        public int fullSyncEveryPolls = 15;

        [Header("Input Box")]
        [Tooltip("입력 박스 기본 높이 (54pt x 1.5)")]
        public float baseInputHeight = 81f;
        [Tooltip("Shift+Enter로 줄이 늘어날 때 입력 박스가 커지는 최대 줄 수 (넘으면 박스 안에서 스크롤)")]
        public int maxVisibleInputLines = 4;

        [Header("Style")]
        public Color myNameColor = new Color(0.45f, 0.8f, 1f);
        public Color otherNameColor = new Color(1f, 0.85f, 0.4f);
        public Color errorColor = new Color(1f, 0.45f, 0.45f);
        [Tooltip("시스템 메시지(입장 알림, 공지) 색")]
        public Color systemColor = new Color(0.4f, 0.9f, 0.45f);
        [Tooltip("시스템 메시지 앞에 붙는 말머리")]
        public string systemPrefix = "[시스템] ";

        readonly List<ChatMessage> _messages = new List<ChatMessage>();
        string _status = "";
        int _sendQueuedFrame = -1;
        bool _sending;
        bool _fetching;
        bool _fetchAgain;
        long _lastId = -1;      // 마지막으로 받은 메시지 id (-1: 아직 없음 → 전체 조회)
        float _nextPollTime;
        int _pollCount;
        bool _joined;
        bool _joining;
        bool _joinErrorLogged;
        string _joinError = "";   // 유저/방이 없는 등 재시도해도 안 되는 참가 오류 (화면에 계속 표시)

        void Awake()
        {
            if (api == null) api = GetComponent<ChatApiClient>();
            if (!autoJoinRoom) _joined = true;

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
            TryJoin();
            RequestFetch();
            inputField.ActivateInputField();
        }

        void Update()
        {
            UpdateInputHeight();

            if (pollInterval > 0f && Time.unscaledTime >= _nextPollTime)
            {
                _nextPollTime = Time.unscaledTime + pollInterval;
                if (!_joined) TryJoin(); // 시작할 때 서버가 꺼져 있었다면 연결되면 다시 입장 시도

                if (!_fetching) // 이미 조회 중이면 이번 폴링은 건너뜀
                {
                    _pollCount++;
                    bool fullSync = fullSyncEveryPolls > 0 && _pollCount % fullSyncEveryPolls == 0;
                    RequestFetch(fullSync);
                }
            }
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
            // 입력 박스가 활성화되면 한글 IME 조합 입력을 켭니다
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
            if (string.IsNullOrEmpty(text) || _sending) { FocusInput(); return; }

            _sending = true;
            sendButton.interactable = false;
            inputField.text = "";
            FocusInput();

            StartCoroutine(api.PostMessage(userId, text,
                () =>
                {
                    _sending = false;
                    sendButton.interactable = true;
                    _status = "";
                    _stickToBottom = true; // 내가 보낸 메시지는 항상 보이도록
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

        // ---------------- 입장 ----------------

        void TryJoin()
        {
            if (_joined || _joining) return;
            _joining = true;
            StartCoroutine(api.JoinRoom(userId,
                () =>
                {
                    _joining = false;
                    _joined = true;
                    if (!string.IsNullOrEmpty(_joinError)) { _joinError = ""; Render(); }
                    RequestFetch(); // 서버가 만든 입장 알림을 바로 받아오기
                },
                (err, code) =>
                {
                    _joining = false;
                    if (code >= 400 && code < 500)
                    {
                        // 404(유저/방 없음) 등은 다시 시도해도 같으므로 멈추고 화면에 계속 표시
                        _joined = true;
                        _joinError = err;
                        Debug.LogWarning("[Chat] " + err);
                        Render();
                        return;
                    }
                    // 서버 연결 오류는 조회 쪽에서 화면에 표시하므로 여기서는 로그만 한 번 남기고 다음 폴링 때 재시도
                    if (!_joinErrorLogged) { Debug.LogWarning("[Chat] " + err); _joinErrorLogged = true; }
                }));
        }

        // ---------------- 조회/표시 ----------------

        bool _fullSyncRequested;
        bool _stickToBottom = true;

        /// <param name="fullSync">true면 최신 목록 전체를 다시 받고, false면 마지막 id 이후 새 메시지만 받습니다</param>
        public void RequestFetch(bool fullSync = false)
        {
            if (fullSync) _fullSyncRequested = true;
            if (_fetching) { _fetchAgain = true; return; }
            StartCoroutine(FetchRoutine());
        }

        IEnumerator FetchRoutine()
        {
            _fetching = true;
            do
            {
                _fetchAgain = false;
                bool full = _fullSyncRequested || _lastId < 0;
                _fullSyncRequested = false;

                if (full)
                    yield return api.FetchMessages(-1, maxMessages, OnFullListReceived, SetError);
                else
                    yield return api.FetchMessages(_lastId, 0, OnNewMessagesReceived, SetError);
            } while (_fetchAgain);
            _fetching = false;
        }

        /// 전체 목록으로 교체 (시작 시 / 주기적 동기화)
        void OnFullListReceived(List<ChatMessage> list)
        {
            bool statusChanged = ClearStatus();
            var sorted = SortOldestFirst(list).ToList();
            if (!statusChanged && SameIds(sorted, _messages)) return; // 바뀐 게 없으면 다시 그리지 않음

            _messages.Clear();
            _messages.AddRange(sorted);
            TrimToMax();
            _lastId = MaxId(_messages);
            Render();
        }

        /// afterId 이후 새 메시지만 목록 뒤에 추가 (폴링 / 전송 직후)
        void OnNewMessagesReceived(List<ChatMessage> list)
        {
            bool statusChanged = ClearStatus();
            bool added = false;
            foreach (var m in SortOldestFirst(list))
            {
                long id;
                if (long.TryParse(m.id, out id) && id <= _lastId) continue; // 이미 받은 메시지
                _messages.Add(m);
                if (id > _lastId) _lastId = id;
                added = true;
            }
            if (!added && !statusChanged) return;
            TrimToMax();
            Render();
        }

        bool ClearStatus()
        {
            if (string.IsNullOrEmpty(_status)) return false;
            _status = "";
            return true;
        }

        void TrimToMax()
        {
            if (_messages.Count > maxMessages)
                _messages.RemoveRange(0, _messages.Count - maxMessages); // 최신 N개만 유지
        }

        static long MaxId(List<ChatMessage> list)
        {
            long max = -1, id;
            foreach (var m in list)
                if (long.TryParse(m.id, out id) && id > max) max = id;
            return max;
        }

        static bool SameIds(List<ChatMessage> a, List<ChatMessage> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i].id != b[i].id) return false;
            return true;
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
            if (err == _status) return; // 폴링 중 같은 오류가 반복되면 로그/화면 갱신 생략
            Debug.LogWarning("[Chat] " + err);
            _status = err;
            Render();
        }

        void Render()
        {
            // 사용자가 위로 스크롤해서 이전 메시지를 보고 있으면 자동으로 끌어내리지 않음
            bool atBottom = messagesScroll == null
                || messagesScroll.content == null || messagesScroll.viewport == null
                || messagesScroll.content.rect.height <= messagesScroll.viewport.rect.height + 1f // 아직 스크롤할 만큼 길지 않음
                || messagesScroll.verticalNormalizedPosition <= 0.01f;
            bool scrollDown = atBottom || _stickToBottom;
            _stickToBottom = false;

            var sb = new StringBuilder();
            string myHex = ColorUtility.ToHtmlStringRGB(myNameColor);
            string otherHex = ColorUtility.ToHtmlStringRGB(otherNameColor);
            string systemHex = ColorUtility.ToHtmlStringRGB(systemColor);

            foreach (var m in _messages)
            {
                if (sb.Length > 0) sb.Append('\n');

                if (m.IsSystem)
                {
                    // 시스템 메시지: 말머리 + 내용 전체를 녹색으로
                    sb.Append("<color=#").Append(systemHex).Append(">")
                      .Append(NoParse(systemPrefix + m.content))
                      .Append("</color>");
                    continue;
                }

                bool mine = m.userId == userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                sb.Append("<color=#").Append(mine ? myHex : otherHex).Append("><b>")
                  .Append(NoParse(string.IsNullOrEmpty(m.nickname) ? "?" : m.nickname))
                  .Append("</b></color>: ")
                  .Append(NoParse(m.content));
            }

            if (!string.IsNullOrEmpty(_joinError))
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(errorColor)).Append(">")
                  .Append(NoParse(_joinError)).Append("</color>");
            }

            if (!string.IsNullOrEmpty(_status) && _status != _joinError)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(errorColor)).Append(">")
                  .Append(NoParse(_status)).Append("</color>");
            }

            messagesText.text = sb.ToString();

            // 최신 메시지가 보이도록 아래로 스크롤
            if (messagesScroll != null && scrollDown)
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
