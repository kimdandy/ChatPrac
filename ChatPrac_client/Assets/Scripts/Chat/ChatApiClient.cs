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
        public string id;
        public string sender;
        public string content;
        public string createdAt;
    }

    /// <summary>
    /// Spring 채팅 API 클라이언트
    ///  - 조회: GET  {baseUrl}/api/rooms/{roomId}/messages[?limit=N][&afterId=ID]
    ///  - 전송: POST {baseUrl}/api/rooms/{roomId}/messages
    /// </summary>
    public class ChatApiClient : MonoBehaviour
    {
        [Header("Server")]
        public string baseUrl = "http://localhost:8080";
        public string roomId = "1";
        [Tooltip("요청 타임아웃(초)")]
        public int timeoutSeconds = 5;

        [Header("JSON 필드명 (Spring DTO에 맞게 수정)")]
        public string idField = "id";
        public string senderField = "sender";
        public string contentField = "content";
        public string createdAtField = "createdAt";

        string MessagesUrl
        {
            get { return baseUrl.TrimEnd('/') + "/api/rooms/" + UnityWebRequest.EscapeURL(roomId) + "/messages"; }
        }

        public IEnumerator FetchMessages(Action<List<ChatMessage>> onSuccess, Action<string> onError)
        {
            return FetchMessages(-1, 0, onSuccess, onError);
        }

        /// <param name="afterId">0 이상이면 이 id 이후의 새 메시지만 조회 (폴링용), 음수면 최신 메시지 조회</param>
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
                    if (onError != null) onError("조회 실패: " + req.error);
                    yield break;
                }

                List<ChatMessage> list;
                // 한글이 깨지지 않도록 응답 바이트를 항상 UTF-8로 디코딩
                try { list = ParseMessages(Encoding.UTF8.GetString(req.downloadHandler.data ?? new byte[0])); }
                catch (Exception e)
                {
                    if (onError != null) onError("응답 파싱 실패: " + e.Message);
                    yield break;
                }
                if (onSuccess != null) onSuccess(list);
            }
        }

        public IEnumerator PostMessage(string sender, string content, Action onSuccess, Action<string> onError)
        {
            string body = "{" + MiniJson.Quote(senderField) + ":" + MiniJson.Quote(sender) + ","
                              + MiniJson.Quote(contentField) + ":" + MiniJson.Quote(content) + "}";

            using (var req = new UnityWebRequest(MessagesUrl, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                req.timeout = timeoutSeconds;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    if (onError != null) onError("전송 실패: " + req.error);
                    yield break;
                }
                if (onSuccess != null) onSuccess();
            }
        }

        /// <summary>
        /// 응답이 배열이거나, {content:[...]}(Spring Page) / {data:[...]} / {messages:[...]} 형태여도 처리합니다.
        /// </summary>
        List<ChatMessage> ParseMessages(string json)
        {
            var result = new List<ChatMessage>();
            object root = MiniJson.Parse(json);

            var arr = root as List<object>;
            var obj = root as Dictionary<string, object>;
            if (arr == null && obj != null)
            {
                foreach (var key in new[] { "content", "data", "messages", "items", "result" })
                {
                    object v;
                    if (obj.TryGetValue(key, out v) && v is List<object>) { arr = (List<object>)v; break; }
                }
            }
            if (arr == null) return result;

            foreach (var item in arr)
            {
                var d = item as Dictionary<string, object>;
                if (d == null) continue;
                result.Add(new ChatMessage
                {
                    id = Str(d, idField),
                    sender = Str(d, senderField),
                    content = Str(d, contentField),
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
            var list = v as List<object>; // LocalDateTime이 배열([2026,9,28,...])로 직렬화된 경우
            if (list != null)
            {
                var parts = new List<string>();
                foreach (var p in list) parts.Add(p == null ? "0" : Convert.ToString(p, CultureInfo.InvariantCulture));
                return string.Join(",", parts.ToArray());
            }
            return v.ToString();
        }
    }
}
