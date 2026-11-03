# Hivebet Unity integration

`MainScene` contains three copies of each panel (onboarding, normal and warning).
All six now share one `HivebetApi` component and have independent form bindings.
No new navigation to `NewBet` has been added. Existing comments, back and close
transitions remain in place.

## Player behavior

- **All bets** reads `school_fire/history` when opened, every 10 seconds while
  visible, and after a confirmed vote. It shows all participants' active bet
  amounts and vote counts from the same server snapshot.
- Press **Yes** or **No** to reveal an inline amount selector and **PLACE BET**.
  Amounts use integer cents, start at the poll default (currently USD 50), and
  follow the existing UI range of USD 5–100 in USD 5 steps, restricted further
  by the server's poll limits.
- The chart uses the last two minutes of data, sampled every five seconds. Its
  vertical scale adjusts to the totals. The **11:00 / 11:01 / 11:02 labels are
  intentionally fictional placeholders**, not the server clock.
- **NewBet** submits character selections to the separate `school` poll. Each
  candidate shows its aggregate dollars and vote count. Only a confirmed
  submission opens the existing Final screen.
- Closing a panel cancels its read request and polling. A submission already
  in flight continues on the API service. A network failure retains the last
  displayed totals and offers Reload; it never replaces failed reads with zero.
- Sending disables the form. An unconfirmed submission is stored locally and
  retried with the same request ID, candidate and amount, including after
  closing/reopening the screen or restarting the app. Until resolved, another
  candidate or amount cannot replace that uncertain submission. Definitive
  server rejections unlock editing. The latest successful vote ID/version is
  stored locally; there is no client admin update/delete UI.

## Configuration

The `HivebetApi` scene component contains the public endpoint, refresh interval
and timeout. Routing uses `?path=`, and both HTTP errors and the Apps Script JSON
`ok/status/error` envelope are checked.

The voting credential is not serialized into the scene or committed to Git.
Create this ignored file before running/building on another machine:

`Assets/Resources/HivebetLocalConfig.json`

```json
{"voteApiKey":"COPY_THE_EXISTING_VOTE_API_KEY_HERE"}
```

Only use the existing `VOTE_API_KEY` from Apps Script project properties. Never
place `ADMIN_API_KEY` in Unity. The local development machine is configured.
The Resources file is included in local builds; it is a shared prototype
credential and can be extracted from a client build. Keep those builds within
the intended prototype audience. An optional `HivebetLocalConfig.json` in
`Application.persistentDataPath` overrides the bundled credential. Without a
credential the public chart still works and submission is disabled.

## Seed data

The existing Candidates sheet contains these explicit demo baselines:

| Poll | Candidate | Dollars | Votes |
| --- | --- | ---: | ---: |
| school | Miriam | 210 | 21 |
| school | Beatrix | 420 | 42 |
| school | Betty | 340 | 34 |
| school | Sabrina | 410 | 41 |
| school | You | 30 | 3 |
| school_fire | Yes | 150 | 20 |
| school_fire | No | 50 | 15 |

Amounts are stored as cents in column F, counts in column G. Notes identify the
new demo values. Baselines are undated and appear across the entire graph, so
the initial curves are flat until votes arrive. Subsequent demo/test votes in
Events add to these values. No existing event rows were overwritten. Before a
live exhibition, plan a clean workbook/session with the server operator;
changing baseline cells alone does not clear existing vote events.

## Source responsibilities

- `HivebetApi`: UnityWebRequest, credentials, persisted idempotent submission.
- `HivebetModels`: wire format, cents formatting and graph scale calculations.
- `HivebetPanel`: screen-scoped reads, refresh and totals.
- `HivebetVoteForm`: candidate selection, amounts, pending and success states.
- `HivebetGraph`: uGUI line mesh and amount-axis labels.
- `HivebetSceneSetup` (Editor): reproducible one-time setup for the original
  MainScene. It refuses to run again on an already configured scene. Existing
  scenes other than MainScene retain the legacy `BetManager` behavior.

## Verification

```sh
uloop compile
uloop run-tests --test-mode PlayMode --filter-type assembly --filter-value FeuBleu.Hivebet.Tests --unsaved-changes fail
```

Tests cover envelope errors, malformed responses, integer cents, chart bounds,
duplicate suppression, uncertain retries, rejection recovery, absent keys,
form limits and reopening a pending form. Live Editor checks use the deployed
Apps Script API. See `Docs/HivebetVerification.md` for observed results and
remaining device verification.

For a headset check: open All bets through the existing story flow, select
Yes/No with the controller ray, change the amount and submit. Confirm totals
on another device, comments/back/close behavior, and recovery after losing
connectivity. Inspect NewBet directly in the Editor until a future story task
adds its entrance. All records are fictional game bets, with no payment flow.
