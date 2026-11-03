using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FeuBleu.Hivebet;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class FakeHivebetApi : HivebetApi
{
    public readonly List<VoteRequest> sent = new List<VoteRequest>();
    public ApiResult reply;
    public override IEnumerator Request(string path, string json, Action<ApiResult> completed,
        Action<UnityEngine.Networking.UnityWebRequest> trackRequest = null)
    {
        sent.Add(JsonUtility.FromJson<VoteRequest>(json));
        yield return null;
        completed(reply);
    }
}

public class HivebetTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private FakeHivebetApi api;
    private string pollId;

    [SetUp] public void Setup()
    {
        pollId = "unit_" + Guid.NewGuid().ToString("N");
        api = Object("Api").AddComponent<FakeHivebetApi>();
        api.endpoint = "https://test.invalid/" + pollId;
        typeof(HivebetApi).GetField("voteApiKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(api, "test-key");
    }
    [TearDown] public void Cleanup()
    {
        PlayerPrefs.DeleteKey("Hivebet.Pending." + api.endpoint + "." + pollId);
        PlayerPrefs.DeleteKey("Hivebet.LastVote." + pollId);
        foreach (GameObject obj in objects) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        objects.Clear();
    }
    private GameObject Object(string name)
    {
        var obj = new GameObject(name); objects.Add(obj); return obj;
    }

    [Test] public void ApiWrapperErrorOverridesHttpSuccess()
    {
        var result = HivebetData.Parse("{\"ok\":false,\"status\":409,\"error\":{\"code\":\"POLL_CLOSED\",\"message\":\"Voting is closed.\"}}");
        Assert.IsFalse(result.Success); Assert.IsFalse(result.Retryable);
        Assert.AreEqual("Voting is closed.", result.Message);
    }
    [TestCase("<html>Sign in</html>")]
    [TestCase("{}")]
    [TestCase("{\"ok\":true}")]
    public void MalformedResponseRetainsUncertainVote(string json)
    {
        Assert.IsTrue(HivebetData.Parse(json).Retryable);
    }
    [Test] public void IntegerCentsAndLargeTotalsStayExact()
    {
        var result = HivebetData.Parse("{\"ok\":true,\"status\":200,\"data\":{\"candidates\":[{\"candidateId\":\"yes\",\"amountMinor\":1234567890123,\"votes\":20}]}}");
        Assert.AreEqual(1234567890123L, result.Data.candidates[0].amountMinor);
        Assert.AreEqual("0.01", HivebetData.Money(1));
        Assert.AreEqual("12345678901.23", HivebetData.Money(result.Data.candidates[0].amountMinor));
    }
    [Test] public void ChartUsesBothSeriesAndHandlesEmptyData()
    {
        Assert.AreEqual(10, HivebetData.ChartMaximum(new HistoryPoint[0]));
        var points = new[] { new HistoryPoint { candidates = new[] {
            new Candidate { candidateId = "yes", amountMinor = 5000 },
            new Candidate { candidateId = "no", amountMinor = 123456 } } } };
        Assert.Greater(HivebetData.ChartMaximum(points), 1234.56);
    }
    [UnityTest] public IEnumerator RetryKeepsOriginalRequestAndBlocksDoubleSubmit()
    {
        api.reply = new ApiResult { Message = "Timeout", Retryable = true };
        Assert.IsTrue(api.Submit(pollId, "yes", 5000));
        Assert.IsFalse(api.Submit(pollId, "no", 1000));
        yield return WaitForVote();
        var pending = api.Pending(pollId);
        Assert.IsNotNull(pending);
        api.reply = new ApiResult { Data = new ApiData { vote = new Vote {
            voteId = "vote_1", pollId = pollId, candidateId = "yes", amountMinor = 5000, version = 1 } } };
        Assert.IsTrue(api.Submit(pollId, "no", 1000));
        yield return WaitForVote();
        Assert.AreEqual(2, api.sent.Count);
        Assert.AreEqual(api.sent[0].requestId, api.sent[1].requestId);
        Assert.AreEqual("yes", api.sent[1].candidateId); Assert.AreEqual(5000, api.sent[1].amountMinor);
        Assert.IsNull(api.Pending(pollId));
        Assert.AreEqual(1, JsonUtility.FromJson<Vote>(PlayerPrefs.GetString("Hivebet.LastVote." + pollId)).version);
    }
    [UnityTest] public IEnumerator ExplicitRejectionUnlocksDraft()
    {
        api.reply = new ApiResult { Message = "Voting is closed.", Retryable = false };
        api.Submit(pollId, "yes", 5000);
        yield return WaitForVote();
        Assert.IsNull(api.Pending(pollId)); Assert.AreEqual("Voting is closed.", api.Message(pollId));
    }
    [Test] public void MissingCredentialCannotSubmit()
    {
        typeof(HivebetApi).GetField("voteApiKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(api, "");
        Assert.IsFalse(api.Submit(pollId, "yes", 5000)); Assert.IsNull(api.Pending(pollId));
    }
    [UnityTest] public IEnumerator PendingVoteSurvivesServiceRecreation()
    {
        api.reply = new ApiResult { Retryable = true, Message = "Timeout" };
        api.Submit(pollId, "no", 500);
        yield return WaitForVote();
        var original = api.Pending(pollId);
        string endpoint = api.endpoint;
        UnityEngine.Object.DestroyImmediate(api.gameObject);
        api = Object("RecreatedApi").AddComponent<FakeHivebetApi>(); api.endpoint = endpoint;
        var restored = api.Pending(pollId);
        Assert.AreEqual(original.requestId, restored.requestId);
        Assert.AreEqual("no", restored.candidateId); Assert.AreEqual(500, restored.amountMinor);
    }
    [Test] public void FormClampsAmountsAndRejectsInactiveCandidates()
    {
        var form = Form();
        form.SetPoll(new Poll { pollId = pollId, status = "open", minAmountMinor = 700, maxAmountMinor = 2200, defaultAmountMinor = 5000 },
            new[] { new Candidate { candidateId = "yes", name = "Yes", active = true }, new Candidate { candidateId = "no", active = false } });
        Assert.AreEqual("22", form.amountLabel.text);
        form.Select("no"); Assert.IsFalse(form.submit.interactable);
        form.Select("yes"); Assert.IsTrue(form.submit.interactable);
        form.ChangeAmount(-100000); Assert.AreEqual("7", form.amountLabel.text); Assert.IsFalse(form.minus.interactable);
    }
    [UnityTest] public IEnumerator ClosedPanelDoesNotLosePendingSubmission()
    {
        var form = Form();
        form.SetPoll(new Poll { pollId = pollId, status = "open", minAmountMinor = 1, maxAmountMinor = 100000, defaultAmountMinor = 5000 },
            new[] { new Candidate { candidateId = "yes", name = "Yes", active = true } });
        form.Select("yes"); api.reply = new ApiResult { Retryable = true, Message = "Timeout" };
        form.Submit(); form.gameObject.SetActive(false);
        yield return WaitForVote();
        form.gameObject.SetActive(true);
        Assert.IsNotNull(api.Pending(pollId)); Assert.IsTrue(form.submit.interactable);
        Assert.IsFalse(form.plus.interactable);
    }
    private IEnumerator WaitForVote()
    {
        int frames = 0;
        while (api.IsSending(pollId) && frames++ < 30) yield return null;
        Assert.IsFalse(api.IsSending(pollId));
    }
    private HivebetVoteForm Form()
    {
        var obj = Object("Form"); obj.SetActive(false);
        var form = obj.AddComponent<HivebetVoteForm>(); form.api = api; form.pollId = pollId;
        form.amountLabel = Object("Amount").AddComponent<TextMeshProUGUI>();
        form.statusLabel = Object("Status").AddComponent<TextMeshProUGUI>();
        form.plus = NewButton(); form.minus = NewButton(); form.submit = NewButton();
        form.choices = new[] { new VoteChoice { candidateId = "yes", button = NewButton() },
            new VoteChoice { candidateId = "no", button = NewButton() } };
        obj.SetActive(true);
        return form;
    }
    private Button NewButton()
    {
        var obj = Object("Button"); obj.AddComponent<Image>(); var button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>(); return button;
    }
}
