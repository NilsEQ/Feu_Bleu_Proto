using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeuBleu.Hivebet
{
    [Serializable] public class VoteChoice
    {
        public string candidateId;
        public Button button;
        public TMP_Text totals;
    }

    public class HivebetVoteForm : MonoBehaviour
    {
        public HivebetApi api;
        public string pollId;
        public VoteChoice[] choices;
        public TMP_Text amountLabel, selectionLabel, statusLabel, submitLabel;
        public Button plus, minus, submit;
        public GameObject controls;
        public GameObject successScreen;
        public bool showCurrency;
        public Sprite selectedSprite, unselectedSprite;
        private Poll poll;
        private Candidate[] candidates;
        private string selectedId;
        private long amountMinor = 5000;
        private const long Step = 500;
        private long Minimum { get { return Math.Max(500, poll.minAmountMinor); } }
        private long Maximum { get { return Math.Min(10000, poll.maxAmountMinor); } }

        private void Awake()
        {
            foreach (VoteChoice choice in choices)
            {
                string id = choice.candidateId;
                choice.button.onClick.AddListener(() => Select(id));
            }
            plus.onClick.AddListener(() => ChangeAmount(Step));
            minus.onClick.AddListener(() => ChangeAmount(-Step));
            submit.onClick.AddListener(Submit);
        }

        private void OnEnable()
        {
            api.VoteFinished += OnVoteFinished;
            poll = null;
            RestorePending();
            Refresh();
        }

        private void OnDisable() { if (api != null) api.VoteFinished -= OnVoteFinished; }

        public void SetPoll(Poll value, Candidate[] values)
        {
            bool first = poll == null;
            poll = value;
            candidates = values;
            if (api.Pending(pollId) == null && !api.IsSending(pollId))
                amountMinor = Math.Max(Minimum, Math.Min(Maximum, first ? poll.defaultAmountMinor : amountMinor));
            Refresh();
        }

        private void RestorePending()
        {
            VoteDraft pending = api.Pending(pollId);
            if (pending == null) return;
            selectedId = pending.candidateId;
            amountMinor = pending.amountMinor;
        }

        public void Select(string candidateId)
        {
            if (poll == null || api.IsSending(pollId) || api.Pending(pollId) != null) return;
            Candidate candidate = HivebetData.Find(candidates, candidateId);
            if (candidate == null || !candidate.active || poll.status != "open") return;
            selectedId = candidateId;
            api.ClearMessage(pollId);
            Refresh();
        }

        public void ChangeAmount(long delta)
        {
            if (poll == null || api.IsSending(pollId) || api.Pending(pollId) != null) return;
            amountMinor = Math.Max(Minimum, Math.Min(Maximum, amountMinor + delta));
            Refresh();
        }

        public void Submit()
        {
            if (!submit.interactable) return;
            api.Submit(pollId, selectedId, amountMinor);
            Refresh();
        }

        private void OnVoteFinished(string id, bool success)
        {
            if (id != pollId) return;
            if (success) selectedId = null;
            Refresh();
            if (success && successScreen != null)
            {
                successScreen.SetActive(true);
                gameObject.SetActive(false);
            }
        }

        private void Refresh()
        {
            bool pending = api.Pending(pollId) != null;
            bool busy = api.IsSending(pollId);
            bool open = poll != null && poll.status == "open" && Minimum <= Maximum;
            Candidate selected = HivebetData.Find(candidates, selectedId);
            foreach (VoteChoice choice in choices)
            {
                Candidate candidate = HivebetData.Find(candidates, choice.candidateId);
                choice.button.interactable = open && !pending && !busy && candidate != null && candidate.active;
                if (selectedSprite != null && unselectedSprite != null)
                    choice.button.image.sprite = choice.candidateId == selectedId ? selectedSprite : unselectedSprite;
                else
                    choice.button.image.color = choice.candidateId == selectedId ? new Color(0.27f, 0.36f, 0.43f) : new Color(0.14f, 0.18f, 0.2f);
            }
            if (controls != null) controls.SetActive(!string.IsNullOrEmpty(selectedId));
            amountLabel.text = HivebetData.Money(amountMinor) + (showCurrency ? " $" : "");
            if (selectionLabel != null) selectionLabel.text = selected != null ? "Bet on " + selected.name : "Choose a prediction";
            plus.interactable = open && !pending && !busy && amountMinor < Maximum;
            minus.interactable = open && !pending && !busy && amountMinor > Minimum;
            // An uncertain submission may be retried even after the poll closes.
            submit.interactable = api.CanVote && !busy && (pending || (open && selected != null && selected.active));
            if (submitLabel != null) submitLabel.text = busy ? "SENDING..." : pending ? "RETRY BET" : "PLACE BET";
            statusLabel.text = !api.CanVote ? "Voting is not configured on this device." :
                busy ? "Placing bet..." : pending ? "Confirmation unavailable. Retry the same bet." :
                poll != null && !open ? "Voting is closed." : api.Message(pollId);
        }
    }
}
