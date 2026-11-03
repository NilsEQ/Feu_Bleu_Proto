using System;
using System.Globalization;
using UnityEngine;

namespace FeuBleu.Hivebet
{
    [Serializable] public class ApiError { public string code; public string message; }
    [Serializable] public class ApiResponse { public bool ok; public int status; public ApiData data; public ApiError error; }
    [Serializable] public class ApiData
    {
        public Poll poll;
        public Candidate[] candidates;
        public HistoryPoint[] points;
        public HistoryPoint summary;
        public Vote vote;
    }
    [Serializable] public class Poll
    {
        public string pollId, title, status, currency;
        public long minAmountMinor, maxAmountMinor, defaultAmountMinor;
    }
    [Serializable] public class Candidate
    {
        public string candidateId, name;
        public bool active;
        public long amountMinor, votes;
    }
    [Serializable] public class HistoryPoint { public string timestamp; public Candidate[] candidates; }
    [Serializable] public class Vote
    {
        public string voteId, pollId, candidateId;
        public long amountMinor;
        public int version;
    }
    [Serializable] public class VoteDraft
    {
        public string requestId, pollId, candidateId;
        public long amountMinor;
    }
    [Serializable] public class VoteRequest : VoteDraft { public string apiKey; }
    [Serializable] public class LocalConfig { public string voteApiKey; }

    public sealed class ApiResult
    {
        public ApiData Data;
        public string Message;
        public bool Retryable;
        public bool Success { get { return Data != null; } }
    }

    public static class HivebetData
    {
        public static string Money(long cents) { return (cents / 100m).ToString("0.##", CultureInfo.InvariantCulture); }

        public static Candidate Find(Candidate[] candidates, string id)
        {
            if (candidates != null)
                foreach (Candidate candidate in candidates)
                    if (candidate != null && candidate.candidateId == id) return candidate;
            return null;
        }

        public static ApiResult Parse(string json)
        {
            try
            {
                ApiResponse response = JsonUtility.FromJson<ApiResponse>(json);
                if (response != null && response.ok && response.status == 200 && response.data != null
                    && (response.data.candidates != null || (response.data.vote != null && !string.IsNullOrEmpty(response.data.vote.voteId))))
                    return new ApiResult { Data = response.data };
                if (response != null && response.status >= 400 && response.error != null && !string.IsNullOrEmpty(response.error.code))
                    return new ApiResult { Message = response.error.message, Retryable = response.status >= 500 };
            }
            catch (Exception) { /* A proxy may return HTML instead of JSON. */ }
            return new ApiResult { Message = "Invalid server response. Please retry.", Retryable = true };
        }

        public static double ChartMaximum(HistoryPoint[] points)
        {
            double maximum = 0;
            if (points != null)
                foreach (HistoryPoint point in points)
                    foreach (string id in new[] { "yes", "no" })
                    {
                        Candidate candidate = Find(point.candidates, id);
                        if (candidate != null) maximum = Math.Max(maximum, candidate.amountMinor / 100d);
                    }
            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(1, maximum))));
            return Math.Max(10, Math.Ceiling(maximum / scale * 1.1) * scale);
        }
    }
}
