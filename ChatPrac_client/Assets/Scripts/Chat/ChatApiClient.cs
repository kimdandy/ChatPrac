using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ChatSystem
{
    [Serializable]
    public class ChatMessage
    {
        public string id;         // messageId
        public string type;       // USER(일반) / SYSTEM(시스템 메시지: 입장·퇴장 알림, 공지) - 명세 추가 필드
        public string userId;     // 시스템 메시지는 빈 값
        public string nickname;
        public string content;    // message
        public string createdAt;  // 전송 응답에만 있음 (조회 응답에는 없음)

        public bool IsSystem
        {
            get { return string.Equals(type, "SYSTEM", StringComparison.OrdinalIgnoreCase); }
        }
    }

    /// <summary>
    /// 채팅 API 클라이언트 (API v1 명세)
    ///  - 조회: GET  {baseUrl}/api/v1/rooms/{roomId}/messages[?limit=N][&afterId=ID]
    ///          200 / 404 방 없음
    ///  - 전송: POST {baseUrl}/api/v1/rooms/{roomId}/messages  {"userId":1,"message":"..."}
    ///          201 / 403 채팅할 수 없는 플레이어 / 404 방 또는 유저 없음
    ///  - [개발용] 방 참가: POST {baseUrl}/api/v1/rooms/{roomId}/members  {"userId":1}
    /// </summary>
    public class ChatApiClient : MonoBehaviour
    {
        [Header("Server")]
        public string baseUrl = "http://localhost:8080";
        public string apiPrefix = "/api/v1";
        public string roomId = "1";
        [Tooltip("요청 타임아웃(초)")]
        public int timeoutSeconds = 5;

        [Header("JSON 필드명 (API 명세)")]
        public string messageIdField = "messageId";
        public string typeField = "type";
        public string userIdField = "userId";
        public string nicknameField = "nickname";
        public string messageField = "message";
        public string createdAtField = "createdAt";

        string RoomUrl
        {
            get { return baseUrl.TrimEnd('/') + apiPrefix + "/rooms/" + UnityWebRequest.EscapeURL(roomId); }
        }

        string MessagesUrl
        {
            get { return RoomUrl + "/messages"; }
        }

        public IEnumerator FetchMessages(Action<List<ChatMessage>> onSuccess, Action<string> onError)
        {
            return FetchMessages(-1, 0, onSuccess, onError);
        }

        /// <param name="afterId">0 이상이면 이 messageId 이후의 새 메시지만 조회 (폴링용), 음수면 최신 메시지 조회</param>
        /// <param name="limit">가져올 최대 개수 (0 이하면 서버 기본값)</param>
        public IEnumerator FetchMessages(long afterId, int limit, Action<List<ChatMessage>> onSuccess, Action<string> onError)
        {
            string url = MessagesUrl;
            var query = new List<string>();
            if (limit > 0) query.Add("limit=" + limit.ToString(CultureInfo.InvariantCulture));
            if (afterId >= 0) query.Add("afterId=" + afterId.ToString(CultureInfo.InvariantCulture));
            if (query.Count > 0) url += "?" + string.Join("&", query.ToArray());

            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = timeoutSeconds;
                req.SetRequestHeader("Accept", "application/json; charset=utf-8");
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    if (onError != null) onError(ErrorText("조회 실패", req));
                    yield break;
                }

                List<ChatMessage> list;
                // 한글이 깨지지 않도록 응답 바이트를 항상 UTF-8로 디코딩
                try { list = ParseMessages(Body(req)); }
                catch (Exception e)
                {
                    if (onError != null) onError("응답 파싱 실패: " + e.Message);
                    yield break;
                }
                if (onSuccess != null) onSuccess(list);
            }
        }

        /// <summary>채팅 전송: {"userId":1,"message":"..."}</summary>
        public IEnumerator PostMessage(long userId, string message, Action onSuccess, Action<string> onError)
        {
            string body = "{" + MiniJson.Quote(userIdField) + ":" + userId.ToString(CultureInfo.InvariantCulture) + ","
                              + MiniJson.Quote(messageField) + ":" + MiniJson.Quote(message) + "}";

            using (var req = PostJson(MessagesUrl, body))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    if (onError != null) onError(ErrorText("전송 실패", req));
                    yield break;
                }
                if (onSuccess != null) onSuccess();
            }
        }

        /// <summary>
        /// [개발용] 방 참가: {"userId":1}  (201 새로 참가 / 200 이미 참가자 - 둘 다 성공)
        /// 처음 참가하면 서버가 "OOO님이 입장했습니다." 시스템 메시지를 남깁니다.
        /// onError의 두 번째 값은 HTTP 상태 코드 (서버에 연결조차 못 했으면 0)
        /// </summary>
        public IEnumerator JoinRoom(long userId, Action onSuccess, Action<string, long> onError)
        {
            string body = "{" + MiniJson.Quote(userIdField) + ":" + userId.ToString(CultureInfo.InvariantCulture) + "}";

            using (var req = PostJson(RoomUrl + "/members", body))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    if (onError != null) onError(ErrorText("방 참가 실패", req), req.responseCode);
                    yield break;
                }
                if (onSuccess != null) onSuccess();
            }
        }

        UnityWebRequest PostJson(string url, string json)
        {
            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.timeout = timeoutSeconds;
            return req;
        }

        static string Body(UnityWebRequest req)
        {
            return req.downloadHandler == null || req.downloadHandler.data == null
                ? ""
                : Encoding.UTF8.GetString(req.downloadHandler.data);
        }

        /// <summary>
        /// 오류 문구. 서버가 {"status":403,"message":"현재 채팅할 수 없는 플레이어입니다."}처럼
        /// 이유를 보내면 그 message를 보여주고, 없으면 HTTP 오류를 그대로 보여줍니다.
        /// </summary>
        static string ErrorText(string prefix, UnityWebRequest req)
        {
            if (req.responseCode > 0)
            {
                try
                {
                    var obj = MiniJson.Parse(Body(req)) as Dictionary<string, object>;
                    object msg;
                    if (obj != null && obj.TryGetValue("message", out msg) && msg != null && msg.ToString().Length > 0)
                        return prefix + " (" + req.responseCode + "): " + msg;
                }
                catch (Exception) { /* 본문이 JSON이 아니면 아래 기본 문구 사용 */ }
            }
            return prefix + ": " + req.error;
        }

        /// <summary>응답: [{"messageId":100,"userId":1,"nickname":"철수","message":"...","type":"USER"}]</summary>
        List<ChatMessage> ParseMessages(string json)
        {
            var result = new List<ChatMessage>();
            var arr = MiniJson.Parse(json) as List<object>;
            if (arr == null) return result;

            foreach (var item in arr)
            {
                var d = item as Dictionary<string, object>;
                if (d == null) continue;
                result.Add(new ChatMessage
                {
                    id = Str(d, messageIdField),
                    type = Str(d, typeField),
                    userId = Str(d, userIdField),
                    nickname = Str(d, nicknameField),
                    content = Str(d, messageField),
                    createdAt = Str(d, createdAtField)
                });
            }
            return result;
        }

        static string Str(Dictionary<string, object> d, string key)
        {
            object v;
            if (string.IsNullOrEmpty(key) || !d.TryGetValue(key, out v) || v == null) return "";
            if (v is double) return ((double)v).ToString(CultureInfo.InvariantCulture);
            return v.ToString();
        }
    }
}
