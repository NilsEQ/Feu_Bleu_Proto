using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace FeuBleu.Hivebet
{
    public class HivebetPanel : MonoBehaviour
    {
        public HivebetApi api;
        public HivebetVoteForm form;
        public bool history;
        public HivebetGraph graph;
        public TMP_Text title, loadStatus;
        public Button retry;
        private Coroutine polling;
        private bool refreshRequested;
        private bool hasData;
        private UnityWebRequest activeRead;

        private void Awake() { retry.onClick.AddListener(() => refreshRequested = true); }
        private void OnEnable()
        {
            api.VoteFinished += OnVoteFinished;
            polling = StartCoroutine(Poll());
        }
        private void OnDisable()
        {
            if (api != null) api.VoteFinished -= OnVoteFinished;
            if (polling != null) StopCoroutine(polling);
            polling = null;
            if (activeRead != null)
            {
                activeRead.Abort();
                activeRead.Dispose();
                activeRead = null;
            }
        }
        private void OnVoteFinished(string id, bool success)
        {
            if (id == form.pollId && success) refreshRequested = true;
        }

        private IEnumerator Poll()
        {
            while (true)
            {
                refreshRequested = false;
                if (!hasData) loadStatus.text = "Loading bets...";
                retry.gameObject.SetActive(false);
                ApiResult result = null;
                yield return api.Request("polls/" + form.pollId + (history ? "/history" : ""), null,
                    value => result = value, request => activeRead = request);
                bool valid = result != null && result.Success && result.Data.poll != null
                    && result.Data.poll.pollId == form.pollId && result.Data.candidates != null
                    && (!history || (result.Data.points != null && result.Data.points.Length > 0 && result.Data.summary != null));
                if (valid)
                {
                    ApiData data = result.Data;
                    if (title != null) title.text = data.poll.title;
                    form.SetPoll(data.poll, data.candidates);
                    foreach (VoteChoice choice in form.choices)
                    {
                        Candidate candidate = HivebetData.Find(history ? data.summary.candidates : data.candidates, choice.candidateId);
                        if (choice.totals != null)
                            choice.totals.text = candidate == null ? "--" :
                                (history ? "" : HivebetData.Money(candidate.amountMinor) + " $\n") + candidate.votes + " Votes";
                    }
                    if (history) graph.SetPoints(data.points);
                    hasData = true;
                    loadStatus.text = "";
                }
                else
                {
                    loadStatus.text = hasData ? "Update failed. Showing last totals." : "Could not load bets.";
                    retry.gameObject.SetActive(true);
                }
                float next = Time.realtimeSinceStartup + api.refreshSeconds;
                while (!refreshRequested && Time.realtimeSinceStartup < next) yield return null;
            }
        }
    }
}
