using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FeuBleu.Hivebet
{
    public class HivebetApi : MonoBehaviour
    {
        public string endpoint = "https://script.google.com/macros/s/AKfycbz_7PvaMyKJHfmd0RYz1XiSd1fnfNEVUk6q0vTvkm5Y00kc6EhYhnLYAdjGlTsn4KvI/exec";
        [Min(5)] public float refreshSeconds = 10;
        [Min(1)] public int timeoutSeconds = 30;
        private string voteApiKey = "";
        private readonly HashSet<string> sending = new HashSet<string>();
        private readonly Dictionary<string, string> messages = new Dictionary<string, string>();
        public event Action<string, bool> VoteFinished;
        public bool CanVote { get { return !string.IsNullOrWhiteSpace(voteApiKey); } }

        private void Awake()
        {
            TextAsset bundled = Resources.Load<TextAsset>("HivebetLocalConfig");
            if (bundled != null) ReadConfig(bundled.text);
            string file = Path.Combine(Application.persistentDataPath, "HivebetLocalConfig.json");
            if (File.Exists(file))
            {
                try { ReadConfig(File.ReadAllText(file)); }
                catch (IOException) { Debug.LogWarning("Hivebet local configuration could not be read."); }
            }
        }

        private void ReadConfig(string json)
        {
            try
            {
                LocalConfig config = JsonUtility.FromJson<LocalConfig>(json);
                if (config != null) voteApiKey = config.voteApiKey;
            }
            catch (Exception) { Debug.LogWarning("Hivebet local configuration is invalid."); }
        }

        public bool IsSending(string pollId) { return sending.Contains(pollId); }
        public string Message(string pollId) { return messages.ContainsKey(pollId) ? messages[pollId] : ""; }
        public void ClearMessage(string pollId) { messages.Remove(pollId); }
        private string PendingKey(string pollId) { return "Hivebet.Pending." + endpoint + "." + pollId; }

        public VoteDraft Pending(string pollId)
        {
            string json = PlayerPrefs.GetString(PendingKey(pollId), "");
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<VoteDraft>(json);
        }

        public bool Submit(string pollId, string candidateId, long amountMinor)
        {
            if (IsSending(pollId) || !CanVote) return false;
            VoteDraft draft = Pending(pollId);
            if (draft == null)
            {
                draft = new VoteDraft { requestId = Guid.NewGuid().ToString("N"), pollId = pollId,
                    candidateId = candidateId, amountMinor = amountMinor };
                // Persist before sending; retries after close/restart reuse the exact same vote.
                PlayerPrefs.SetString(PendingKey(pollId), JsonUtility.ToJson(draft));
                PlayerPrefs.Save();
            }
            sending.Add(pollId);
            messages[pollId] = "Placing bet...";
            StartCoroutine(SendVote(draft));
            return true;
        }

        private IEnumerator SendVote(VoteDraft draft)
        {
            VoteRequest body = new VoteRequest { apiKey = voteApiKey, requestId = draft.requestId,
                pollId = draft.pollId, candidateId = draft.candidateId, amountMinor = draft.amountMinor };
            ApiResult result = null;
            yield return Request("votes", JsonUtility.ToJson(body), value => result = value);
            bool success = result != null && result.Success && result.Data.vote != null
                && !string.IsNullOrEmpty(result.Data.vote.voteId) && result.Data.vote.version > 0
                && result.Data.vote.pollId == draft.pollId && result.Data.vote.candidateId == draft.candidateId
                && result.Data.vote.amountMinor == draft.amountMinor;
            if (success || (result != null && !result.Success && !result.Retryable))
            {
                PlayerPrefs.DeleteKey(PendingKey(draft.pollId));
                PlayerPrefs.Save();
            }
            if (success)
            {
                PlayerPrefs.SetString("Hivebet.LastVote." + draft.pollId, JsonUtility.ToJson(result.Data.vote));
                PlayerPrefs.Save();
            }
            messages[draft.pollId] = success ? "Bet placed." :
                (result != null && !string.IsNullOrEmpty(result.Message) ? result.Message : "Could not confirm bet. Retry safely.");
            sending.Remove(draft.pollId);
            if (VoteFinished != null) VoteFinished(draft.pollId, success);
        }

        public virtual IEnumerator Request(string path, string json, Action<ApiResult> completed,
            Action<UnityWebRequest> trackRequest = null)
        {
            string url = endpoint + (endpoint.Contains("?") ? "&" : "?") + "path=" + UnityWebRequest.EscapeURL(path);
            using (UnityWebRequest request = new UnityWebRequest(url, json == null ? "GET" : "POST"))
            {
                if (trackRequest != null) trackRequest(request);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.redirectLimit = 8;
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                yield return request.SendWebRequest();
                ApiResult result;
                if (request.result != UnityWebRequest.Result.Success)
                    result = new ApiResult { Message = "Connection failed. Please retry.", Retryable = true };
                else result = HivebetData.Parse(request.downloadHandler.text);
                if (trackRequest != null) trackRequest(null);
                completed(result);
            }
        }
    }
}
