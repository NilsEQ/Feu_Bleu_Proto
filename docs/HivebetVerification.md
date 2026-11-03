# Hivebet verification — 2026-11-03

## Automated and scene checks

- Unity compilation: zero errors and warnings for the changed scripts.
- PlayMode suite `FeuBleu.Hivebet.Tests`: 12 passed, zero failed/skipped.
- Scene binding validation: all six panels reference the shared API and their
  own controls; all three history graphs have CanvasRenderers; prediction
  buttons are active raycast targets; legacy voting callbacks are removed.
- MainScene has one EventSystem and the existing canvas raycaster.
- Comparing the original scene with the saved scene found no deleted object
  blocks. Every preexisting `UI_before`/`UI_after` destination is preserved.
  Existing changed objects are confined to BetsHist, NewBet and the disabled
  legacy BetManager, plus the scene root list for the new API service.

## Live Editor checks

The actual MainScene panels were temporarily moved into a screen-space canvas
in Play Mode for legible screenshots and EventSystem pointer tests. Those
preview changes were discarded on exiting Play Mode; the saved scene retains
the original world-space canvas and panel transforms.

- Public history retrieval showed the seeded Yes/No amounts and counts.
- A pointer click on Yes opened the inline controls. Minus changed 50 to 45
  dollars. Submit disabled the form and displayed SENDING while awaiting the
  deployed Apps Script API. Success incremented Yes by 45 dollars and one vote.
- A pointer click on No, then Plus and Submit, recorded 50 dollars and one vote.
- An independent HTTP read confirmed Yes = USD 195 / 21 votes and No = USD 100 /
  16 votes, with a total of USD 295 / 37 votes. These include the demo baseline.
- See comments and Go Back both worked through the EventSystem. Leaving history
  stopped its coroutine and cleared its read request; returning restarted it.
- NewBet was opened directly for testing. Its Miriam button and Plus updated
  that panel to 55 dollars. Submit left Final hidden while sending, then opened
  Final only after success. An independent API read confirmed Miriam = USD 265 /
  22 votes. Yes/No and character votes remain separate polls.
- No Unity Console errors occurred during these live checks.

The three successful test votes are intentionally retained as fictional demo
data in Events. No existing event history was deleted or overwritten.

## Device boundary

Android IL2CPP/ARM64 build succeeded in 178 seconds with zero errors and 12
warnings. APK: `Build/Hivebet-Android.apk` (approximately 171 MiB).
Package: `com.UnityTechnologies.Feu_Bleu_Proto`; minimum SDK 29, target SDK 36.
The manifest contains `android.permission.INTERNET`.

SHA-256: `2dbbcfcf8b1ab327180f802072172a755868b2b3d5a477359bb581b821a405ca`.

Warnings concern existing XR/package configuration, including missing ARSession,
PICO native libraries without 16 KB alignment, optional passthrough/URP settings,
package assets with missing scripts and diagnostics symbols. They were not
silently changed as part of the Hivebet work. Full messages are saved locally
in `Logs/Hivebet/android-build-warnings.txt`.

ADB reported no connected devices. PICO controller rays, physical readability,
network loss on the headset, and a two-headset session have not been accepted
on hardware. Desktop EventSystem checks and independent server reads do not
substitute for that validation. The generated Android APK is for that next
device check; it contains the locally configured prototype vote credential.
