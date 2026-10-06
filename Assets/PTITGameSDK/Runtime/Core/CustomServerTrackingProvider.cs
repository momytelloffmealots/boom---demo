using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Firebase.Analytics;
using System.Globalization;
using PTITGameSDK.Modules;

namespace PTITGameSDK.Core
{
    [Serializable]
    public class CachedLogData
    {
        public List<string> logs = new List<string>();
    }

    public class CustomServerTrackingProvider : ITrackingProvider
    {
        private string _serverUrl;
        private string _gameId;
        private const string CACHE_KEY = "PTITGameSDK_CachedLogs";
        private List<string> _cachedLogs = new List<string>();
        private bool _isProcessingCache = false;

        private bool _useBatchMode = true;
        private string _batchUrl;
        private Coroutine _saveCacheCoroutine;

        [Serializable]
        private class BatchResponse
        {
            public string[] accepted_ids;
            public string[] rejected_ids;
        }

        [Serializable]
        private class LogEntry
        {
            public string event_id;
            public string event_name;
            public Dictionary<string, object> parameters;
        }

        public CustomServerTrackingProvider(string serverUrl, string gameId, bool useBatchMode = true)
        {
            _serverUrl = serverUrl;
            _batchUrl = serverUrl.EndsWith("/") ? serverUrl + "batch" : serverUrl + "/batch";
            _gameId = gameId;
            _useBatchMode = useBatchMode;
        }

        public void Initialize()
        {
            LoadCache();
            
            // Cố gắng gửi ngay nếu có mạng lúc khởi tạo
            if (_cachedLogs.Count > 0)
            {
                if (_useBatchMode)
                {
                    PTITGameTracker.Instance.StartCoroutine(ProcessBatchLogsCoroutine());
                }
                else
                {
                    PTITGameTracker.Instance.StartCoroutine(ProcessCachedLogsCoroutine());
                }
            }

            // Bắt đầu vòng lặp kiểm tra định kỳ để tự động flush cache khi có mạng trở lại
            PTITGameTracker.Instance.StartCoroutine(PeriodicCacheFlush());
        }

        private IEnumerator PeriodicCacheFlush()
        {
            while (true)
            {
                // Kiểm tra mỗi 5 giây, nếu có log bị kẹt và có mạng thì thử gửi lại
                yield return new WaitForSeconds(5f);
                if (_cachedLogs.Count > 0 && !_isProcessingCache && Application.internetReachability != NetworkReachability.NotReachable)
                {
                    if (_useBatchMode)
                    {
                        yield return PTITGameTracker.Instance.StartCoroutine(ProcessBatchLogsCoroutine());
                    }
                    else
                    {
                        yield return PTITGameTracker.Instance.StartCoroutine(ProcessCachedLogsCoroutine());
                    }
                }
            }
        }

        private void LoadCache()
        {
            string cacheString = PlayerPrefs.GetString(CACHE_KEY, "");
            if (!string.IsNullOrEmpty(cacheString))
            {
                try
                {
                    var data = JsonUtility.FromJson<CachedLogData>(cacheString);
                    if (data != null && data.logs != null)
                    {
                        _cachedLogs = data.logs;
                    }
                }
                catch
                {
                    _cachedLogs = new List<string>();
                }
            }
        }

        public void ForceSyncCache()
        {
            SaveCache();
        }

        private void SaveCache()
        {
            CachedLogData data = new CachedLogData { logs = _cachedLogs };
            string cacheString = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(CACHE_KEY, cacheString);
            PlayerPrefs.Save();
        }

        private void AddToCache(string jsonPayload)
        {
            _cachedLogs.Add(jsonPayload);
            if (_cachedLogs.Count > 1000)
            {
                _cachedLogs.RemoveAt(0);
            }
            SaveCache();
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(_serverUrl)) return;

            try
            {
                string eventId;
                string jsonPayload = SerializeToJson(eventName, parameters, out eventId);
                
                AddToCache(jsonPayload);
                
                if (!_useBatchMode || eventName == "ad_impression")
                {
                    // Run in coroutine to avoid blocking the main thread (Send immediately)
                    PTITGameTracker.Instance.StartCoroutine(SendPostRequest(_serverUrl, jsonPayload, eventName, false, eventId));
                }
            }
            catch (Exception ex)
            {
                // Catch any serialization errors to avoid crashing
                SendErrorToFirebase("serialization_error", ex.Message, eventName);
            }
        }

        public void SetUserProperty(string propertyName, string value)
        {
            // Custom server implementation for user properties if needed
            // Currently ignored or you could append it to a persistent local storage
            // and send with every log.
        }

        public void Flush()
        {
            if (_cachedLogs.Count > 0 && !_isProcessingCache && Application.internetReachability != NetworkReachability.NotReachable)
            {
                if (_useBatchMode)
                {
                    PTITGameTracker.Instance.StartCoroutine(ProcessBatchLogsCoroutine());
                }
                else
                {
                    PTITGameTracker.Instance.StartCoroutine(ProcessCachedLogsCoroutine());
                }
            }
        }

        private IEnumerator SendPostRequest(string url, string json, string eventName, bool isRetry, string eventId)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Timeout is critical so it doesn't hang the coroutine pipeline forever
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // Nếu lỗi kết nối (mất mạng/timeout), thì giữ nguyên trong cache (nó đã được AddToCache trước đó)
                    if (request.result == UnityWebRequest.Result.ConnectionError || (request.error != null && request.error.ToLower().Contains("timeout")))
                    {
                        // Không làm gì, để batch xử lý sau
                    }
                    else if (request.responseCode >= 500 || request.responseCode == 429)
                    {
                        // Lỗi server (5xx) hoặc quá tải (429), giữ nguyên trong cache để retry
                        SendErrorToFirebase("http_server_error_retry", $"Code: {request.responseCode}, Error: {request.error}", eventName);
                    }
                    else
                    {
                        // Lỗi cấu trúc request (4xx), bỏ qua và log lỗi
                        SendErrorToFirebase("http_client_error_dropped", $"Code: {request.responseCode}, Error: {request.error}", eventName);
                        RemoveFromCache(eventId);
                    }
                }
                else
                {
                    // Gửi thành công, xóa khỏi cache bằng eventId
                    RemoveFromCache(eventId);

                    // Chứng tỏ có mạng -> kích hoạt gửi cache (nếu chưa chạy)
                    if (!isRetry && _cachedLogs.Count > 0 && !_isProcessingCache)
                    {
                        PTITGameTracker.Instance.StartCoroutine(ProcessCachedLogsCoroutine());
                    }
                }
            }
        }

        private void RemoveFromCache(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;
            for (int i = _cachedLogs.Count - 1; i >= 0; i--)
            {
                if (_cachedLogs[i].Contains($"\"event_id\":\"{eventId}\""))
                {
                    _cachedLogs.RemoveAt(i);
                    SaveCache();
                    break;
                }
            }
        }

        private IEnumerator ProcessCachedLogsCoroutine()
        {
            if (_isProcessingCache) yield break;
            _isProcessingCache = true;

            // Lặp cho đến khi hết cache
            while (_cachedLogs.Count > 0)
            {
                string jsonPayload = _cachedLogs[0];
                
                using (UnityWebRequest request = new UnityWebRequest(_serverUrl, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = 10;

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Thành công -> xóa khỏi danh sách và lưu lại
                        _cachedLogs.RemoveAt(0);
                        SaveCache();
                    }
                    else if (request.result == UnityWebRequest.Result.ConnectionError || (request.error != null && request.error.ToLower().Contains("timeout")))
                    {
                        // Vẫn mất mạng -> dừng việc retry, chờ lần sau có mạng kích hoạt lại
                        break;
                    }
                    else if (request.responseCode >= 500 || request.responseCode == 429)
                    {
                        // Lỗi server (5xx) hoặc quá tải (429) -> tạm dừng và chờ gửi lại
                        SendErrorToFirebase("http_server_error_retry", $"Code: {request.responseCode}, Error: {request.error}", "single_cached_event");
                        break;
                    }
                    else 
                    {
                        // Lỗi cấu trúc request (4xx) -> log lại Firebase và xóa khỏi queue để tránh kẹt mãi mãi
                        SendErrorToFirebase("http_client_error_dropped", $"Code: {request.responseCode}, Error: {request.error}", "single_cached_event");
                        _cachedLogs.RemoveAt(0);
                        SaveCache();
                    }
                }
                
                // Đợi 1 chút giữa các request để tránh spam server quá nhanh
                yield return new WaitForSeconds(0.5f);
            }

            _isProcessingCache = false;
        }

        private IEnumerator ProcessBatchLogsCoroutine()
        {
            if (_isProcessingCache) yield break;
            _isProcessingCache = true;

            while (_cachedLogs.Count > 0)
            {
                // Lấy số lượng log gửi 1 lần từ RemoteConfig, mặc định là 10, để vừa phải còn khi thoát game/ pause game còn gửi kịp
                int batchSizeLimit = (int)PTITRemoteConfig.Instance.GetLong("batch_log_count", 10);
                if (batchSizeLimit <= 0) batchSizeLimit = 10;
                int count = Mathf.Min(batchSizeLimit, _cachedLogs.Count);
                List<string> batch = _cachedLogs.GetRange(0, count);

                string jsonArray = "[" + string.Join(",", batch) + "]";
                
                using (UnityWebRequest request = new UnityWebRequest(_batchUrl, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonArray);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = 15;

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = request.downloadHandler.text;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"[PTIT Batch] HTTP {request.responseCode}: {responseText}");
#endif
                        BatchResponse response = null;
                        try
                        {
                            response = JsonUtility.FromJson<BatchResponse>(responseText);
                        }
                        catch { }

                        int initialCount = _cachedLogs.Count;

                        if (response != null && (response.accepted_ids != null || response.rejected_ids != null))
                        {
                            List<string> processedIds = new List<string>();
                            if (response.accepted_ids != null) processedIds.AddRange(response.accepted_ids);
                            if (response.rejected_ids != null) processedIds.AddRange(response.rejected_ids);

                            if (response.rejected_ids != null && response.rejected_ids.Length > 0)
                            {
                                SendErrorToFirebase("batch_partial_rejected", $"Rejected {response.rejected_ids.Length} logs", "batch_event");
                            }

                            // Chỉ xóa những event đã được xử lý (thành công hoặc bị loại bỏ do lỗi payload)
                            for (int i = count - 1; i >= 0; i--)
                            {
                                string logStr = _cachedLogs[i];
                                bool isProcessed = false;
                                foreach (string id in processedIds)
                                {
                                    if (logStr.Contains($"\"event_id\":\"{id}\""))
                                    {
                                        isProcessed = true;
                                        break;
                                    }
                                }
                                if (isProcessed)
                                {
                                    _cachedLogs.RemoveAt(i);
                                }
                            }
                            
                            if (_cachedLogs.Count != initialCount)
                            {
                                SaveCache();
                            }
                        }

                        // Chống lặp vô tận: Nếu sau khi xử lý mà không có event nào bị xóa (VD server không trả ID nào dù HTTP 200)
                        if (_cachedLogs.Count == initialCount)
                        {
                            SendErrorToFirebase("batch_ack_failed", "Batch returned 200 but no IDs acknowledged", "batch_event");
                            break;
                        }
                    }
                    else if (request.result == UnityWebRequest.Result.ConnectionError || (request.error != null && request.error.ToLower().Contains("timeout")))
                    {
                        break;
                    }
                    else if (request.responseCode >= 500 || request.responseCode == 429)
                    {
                        // Lỗi server (5xx) hoặc quá tải (429) -> tạm dừng và chờ gửi lại batch này
                        SendErrorToFirebase("http_server_error_retry", $"Code: {request.responseCode}, Error: {request.error}", "batch_event");
                        break;
                    }
                    else 
                    {
                        // Lỗi cấu trúc payload (4xx) -> Có một event bị sai khiến cả lô bị reject.
                        // Chuyển sang gửi lẻ từng event để cách ly event lỗi thay vì xóa cả lô.
                        SendErrorToFirebase("http_client_error_split", $"Code: {request.responseCode}, Error: {request.error}", "batch_event");
                        yield return PTITGameTracker.Instance.StartCoroutine(ProcessCachedLogsCoroutine());
                        break; // Dừng vòng lặp batch, ProcessCachedLogsCoroutine sẽ tiếp quản gửi lẻ
                    }
                }
                
                yield return new WaitForSeconds(0.5f);
            }

            _isProcessingCache = false;
        }

        private void SendErrorToFirebase(string errorType, string errorMessage, string originalEventName)
        {
            try
            {
                // We bypass PTITGameTracker.Instance.LogEvent to prevent an infinite loop 
                // in case the error handling itself triggers another LogEvent
                Parameter[] errorParams = new Parameter[] {
                    new Parameter("error_type", errorType),
                    new Parameter("error_message", errorMessage ?? "Unknown"),
                    new Parameter("original_event", originalEventName)
                };
                FirebaseAnalytics.LogEvent("server_log_error", errorParams);
            }
            catch
            {
                // Completely silent if Firebase also fails
            }
        }

        /// <summary>
        /// Simple JSON serializer for flat dictionaries to avoid dependencies like Newtonsoft.Json
        /// </summary>
        private string SerializeToJson(string eventName, Dictionary<string, object> parameters, out string eventId)
        {
            if (parameters != null && parameters.TryGetValue("event_id", out object idObj))
            {
                eventId = idObj.ToString();
                parameters.Remove("event_id"); // Không đưa vào block "parameters" nữa vì đã đưa lên root
            }
            else
            {
                eventId = Guid.NewGuid().ToString();
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"event_id\":\"{eventId}\",");
            sb.Append($"\"event_name\":\"{EscapeJsonString(eventName)}\",");
            sb.Append($"\"game_id\":\"{EscapeJsonString(_gameId)}\",");
            sb.Append($"\"collection\":\"event\",");
            sb.Append($"\"parameters\":{{");
            
            bool first = true;
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    if (!first) sb.Append(",");
                    sb.Append($"\"{EscapeJsonString(kvp.Key)}\":");

                    if (kvp.Value == null)
                    {
                        sb.Append("null");
                    }
                    else if (kvp.Value is string s)
                    {
                        sb.Append($"\"{EscapeJsonString(s)}\"");
                    }
                    else if (kvp.Value is bool b)
                    {
                        sb.Append(b ? "true" : "false");
                    }
                    else
                    {
                        if (kvp.Value is int || kvp.Value is float || kvp.Value is double || kvp.Value is long)
                        {
                            if (kvp.Value is IFormattable formattable)
                                sb.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                            else
                                sb.Append(kvp.Value.ToString());
                        }
                        else
                        {
                            // Đối với các kiểu phức tạp khác, bọc trong ngoặc kép hoặc dùng JsonUtility
                            try
                            {
                                sb.Append($"\"{EscapeJsonString(kvp.Value.ToString())}\"");
                            }
                            catch
                            {
                                sb.Append("\"\"");
                            }
                        }
                    }
                    first = false;
                }
            }
            
            sb.Append("}}");
            return sb.ToString();
        }

        private string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length + 4);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
