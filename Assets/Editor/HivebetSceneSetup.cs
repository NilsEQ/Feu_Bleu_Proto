using System;
using System.Linq;
using FeuBleu.Hivebet;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Installs API bindings into the existing MainScene panels without adding navigation.</summary>
public static class HivebetSceneSetup
{
    private static TMP_FontAsset font;
    private static readonly Color Muted = new Color(0.7f, 0.74f, 0.76f);

    [MenuItem("Feu Bleu/Hivebet/Configure MainScene panels")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/MainScene.unity")
            throw new InvalidOperationException("Open MainScene in Edit Mode first.");
        if (UnityEngine.Object.FindObjectsByType<HivebetPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            throw new InvalidOperationException("Hivebet panels are already configured.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Oswald-VariableFont_wght Bitmap.asset");
        var api = new GameObject("HivebetApi").AddComponent<HivebetApi>();
        var legacy = UnityEngine.Object.FindFirstObjectByType<BetManager>(FindObjectsInactive.Include);
        var selected = legacy.Selected;
        var unselected = legacy.Unselected;
        legacy.enabled = false;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == "BetsHist") ConfigureHistory(transform, api);
                if (transform.name == "NewBet") ConfigureCharacters(transform, api, selected, unselected);
            }
        RefreshLabels();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Configured three history panels and three character panels. Existing navigation is unchanged.");
    }

    public static void RefreshLabels()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Oswald-VariableFont_wght Bitmap.asset");
        foreach (var panel in UnityEngine.Object.FindObjectsByType<HivebetPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var choice in panel.form.choices)
            {
                choice.totals.color = Muted;
                if (!panel.history) continue;
                choice.button.name = choice.candidateId == "yes" ? "YesButton" : "NoButton";
                foreach (var image in choice.button.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    if (image.sprite != null && (image.sprite.name == "Yes" || image.sprite.name == "No")) image.enabled = false;
                var label = choice.button.transform.Find("PredictionLabel");
                if (label == null)
                {
                    var text = Label(choice.button.transform, "PredictionLabel", choice.candidateId == "yes" ? "Yes" : "No",
                        Vector2.zero, new Vector2(200, 100), 60);
                    text.color = choice.candidateId == "yes" ? new Color(0.39f, 0.76f, 0.29f) : new Color(0.85f, 0.17f, 0.32f);
                }
            }
    }

    [MenuItem("Feu Bleu/Hivebet/Validate scene bindings")]
    public static void Validate()
    {
        var panels = UnityEngine.Object.FindObjectsByType<HivebetPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (panels.Length != 6 || panels.Count(p => p.history) != 3) throw new Exception("Expected six Hivebet panels.");
        foreach (var panel in panels)
        {
            var form = panel.form;
            if (form == null || panel.api == null || form.api != panel.api || form.submit == null || form.plus == null || form.minus == null)
                throw new Exception("Incomplete form binding: " + panel.name);
            if (form.choices.Any(c => c.button == null || !c.button.transform.IsChildOf(form.transform) || c.totals == null))
                throw new Exception("Cross-panel or missing choice binding: " + panel.name);
            if (form.submit.onClick.GetPersistentEventCount() != 0 || form.choices.Any(c => c.button.onClick.GetPersistentEventCount() != 0))
                throw new Exception("Legacy submit/choice handler remains.");
            if (panel.history && (panel.graph == null || panel.graph.GetComponent<CanvasRenderer>() == null
                || form.choices.Any(c => !c.button.gameObject.activeSelf || !c.button.image.raycastTarget)))
                throw new Exception("Graph or prediction button is not ready.");
            var canvas = panel.GetComponentInParent<Canvas>(true);
            if (canvas == null || canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                throw new Exception("Canvas/raycaster is missing.");
        }
        if (UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
            throw new Exception("Expected exactly one EventSystem.");
        Debug.Log("Hivebet scene bindings passed: 6 panels, local choices, no legacy submit callbacks, graph renderers and raycasters.");
    }

    private static void ConfigureCharacters(Transform root, HivebetApi api, Sprite selected, Sprite unselected)
    {
        var form = root.gameObject.AddComponent<HivebetVoteForm>();
        form.api = api; form.pollId = "school";
        form.selectedSprite = selected; form.unselectedSprite = unselected;
        var ids = new[] { "miriam", "beatrix", "betty", "sabrina", "you" };
        var choices = root.Find("Choices");
        form.choices = new VoteChoice[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            Transform choice = choices.GetChild(i);
            var button = choice.GetComponentInChildren<UnityEngine.UI.Button>(true);
            ClearEvents(button);
            var total = choice.GetComponentsInChildren<TMP_Text>(true).First(t => t.transform.parent == choice);
            total.text = "-- $\n-- Votes";
            total.fontSize = 38; total.rectTransform.sizeDelta = new Vector2(260, 100);
            total.rectTransform.anchoredPosition = new Vector2(0, -215);
            total.raycastTarget = false;
            form.choices[i] = new VoteChoice { candidateId = ids[i], button = button, totals = total };
        }
        form.amountLabel = root.Find("Money/Price").GetComponent<TMP_Text>();
        form.amountLabel.text = "50";
        form.plus = root.Find("Money/Plus").GetComponent<UnityEngine.UI.Button>();
        form.minus = root.Find("Money/Minus").GetComponent<UnityEngine.UI.Button>();
        form.submit = root.Find("PlaceBet").GetComponent<UnityEngine.UI.Button>();
        form.successScreen = form.submit.GetComponent<UITransition>().UI_after;
        ClearEvents(form.plus); ClearEvents(form.minus); ClearEvents(form.submit);
        // Replace the baked submit lettering so sending/retry states remain visible.
        form.submit.image.sprite = null; form.submit.image.color = new Color(0.8f, 0.08f, 0.1f);
        foreach (Transform child in form.submit.transform) child.gameObject.SetActive(false);
        form.submitLabel = Label(form.submit.transform, "SubmitLabel", "PLACE BET", Vector2.zero, new Vector2(560, 170), 68);
        form.statusLabel = Label(root, "VoteStatus", "", new Vector2(0, -490), new Vector2(1700, 40), 32);
        Panel(root, api, form, false);
    }

    private static void ConfigureHistory(Transform root, HivebetApi api)
    {
        var images = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        Func<string, UnityEngine.UI.Image> sprite = name => images.First(i => i.sprite != null && i.sprite.name == name);
        var background = images.First(i => i.rectTransform.sizeDelta.x > 1800);
        background.sprite = null; background.color = new Color(0.11f, 0.14f, 0.15f); background.raycastTarget = false;
        sprite("Hivebet").gameObject.SetActive(true);
        sprite("abeille-removebg-preview").gameObject.SetActive(true);
        sprite("Hivebet").raycastTarget = false;
        sprite("abeille-removebg-preview").raycastTarget = false;
        var oldTitle = sprite("Your bets"); oldTitle.enabled = false;
        Label(root, "AllBetsTitle", "All bets", oldTitle.rectTransform.anchoredPosition, new Vector2(500, 140), 90);
        var oldQuestion = sprite("The school is going to burn down at 1140"); oldQuestion.enabled = false;
        var question = Label(root, "PollTitle", "The school is going to burn down at 11:40",
            oldQuestion.rectTransform.anchoredPosition, new Vector2(850, 100), 50);
        question.enableAutoSizing = true; question.fontSizeMin = 32; question.fontSizeMax = 50;
        var form = root.gameObject.AddComponent<HivebetVoteForm>();
        form.api = api; form.pollId = "school_fire"; form.showCurrency = true;
        form.choices = new VoteChoice[2];
        for (int i = 0; i < 2; i++)
        {
            var label = sprite(i == 0 ? "Yes" : "No");
            var target = label.transform.parent.GetComponent<UnityEngine.UI.Image>();
            target.gameObject.SetActive(true);
            target.sprite = null; target.raycastTarget = true;
            target.color = new Color(0.14f, 0.18f, 0.2f);
            label.raycastTarget = false;
            var button = target.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = target;
            var votes = sprite(i == 0 ? "94 Votes" : "53 Votes"); votes.enabled = false;
            var totals = Label(target.transform, "VoteCount", "-- Votes", new Vector2(0, -98), new Vector2(230, 50), 36);
            totals.color = Muted;
            form.choices[i] = new VoteChoice { candidateId = i == 0 ? "yes" : "no", button = button, totals = totals };
        }
        var controls = Rect(root, "BetControls", new Vector2(0, 166), new Vector2(1700, 70));
        form.controls = controls.gameObject;
        form.selectionLabel = Label(controls, "SelectedPrediction", "Bet on Yes", new Vector2(-610, 0), new Vector2(440, 70), 44);
        form.minus = Button(controls, "Minus", "-", new Vector2(-265, 0), new Vector2(90, 70));
        form.amountLabel = Label(controls, "Amount", "50 $", new Vector2(-80, 0), new Vector2(240, 70), 48);
        form.plus = Button(controls, "Plus", "+", new Vector2(105, 0), new Vector2(90, 70));
        form.submit = Button(controls, "Submit", "PLACE BET", new Vector2(570, 0), new Vector2(430, 70));
        form.submitLabel = form.submit.GetComponentInChildren<TMP_Text>();
        form.submit.image.color = new Color(0.8f, 0.08f, 0.1f);
        controls.gameObject.SetActive(false);
        form.statusLabel = Label(root, "VoteStatus", "", new Vector2(-270, -87), new Vector2(1400, 45), 32);
        sprite("Groupe 34").enabled = false;
        var graphRect = Rect(root, "LiveGraph", new Vector2(40, -285), new Vector2(1500, 320));
        var graph = graphRect.gameObject.AddComponent<HivebetGraph>();
        graph.raycastTarget = false;
        graph.topLabel = Label(root, "UpperAmount", "-- $", new Vector2(-810, -125), new Vector2(170, 50), 35);
        graph.middleLabel = Label(root, "MiddleAmount", "-- $", new Vector2(-810, -285), new Vector2(170, 50), 35);
        Label(root, "ZeroAmount", "0 $", new Vector2(-810, -445), new Vector2(170, 50), 35);
        // Keep the original fictional story-clock labels; only the data is live.
        Label(root, "DummyTimeStart", "11:00", new Vector2(-710, -480), new Vector2(150, 50), 35);
        Label(root, "DummyTimeMiddle", "11:01", new Vector2(40, -480), new Vector2(150, 50), 35);
        Label(root, "DummyTimeEnd", "11:02", new Vector2(790, -480), new Vector2(150, 50), 35);
        var panel = Panel(root, api, form, true);
        panel.graph = graph; panel.title = question;
    }

    private static HivebetPanel Panel(Transform root, HivebetApi api, HivebetVoteForm form, bool history)
    {
        var panel = root.gameObject.AddComponent<HivebetPanel>();
        panel.api = api; panel.form = form; panel.history = history;
        panel.loadStatus = Label(root, "LoadStatus", "", new Vector2(-160, -526), new Vector2(1400, 38), 28);
        panel.retry = Button(root, "RetryLoad", "RELOAD", new Vector2(760, -520), new Vector2(200, 46));
        panel.retry.gameObject.SetActive(false);
        return panel;
    }

    private static void ClearEvents(UnityEngine.UI.Button button)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.fontSize = fontSize; label.text = text;
        label.color = Color.white; label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap; label.raycastTarget = false;
        return label;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size)
    {
        var rect = Rect(parent, name, position, size);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.05f, 0.5f, 0.76f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        Label(rect, "Label", text, Vector2.zero, size, 38);
        return button;
    }
}
