# Water Avatar

A floating, translucent water avatar for STYLY NetSync 0.17.5 and Unity 6000.3 / URP 17.3. Silver-blue reflections and moving surface currents retain the original water-person identity. The local avatar keeps its original articulated hands and hidden upper body. The remote avatar has a continuous head/neck/chest that tapers into transparent water, two fingerless hand droplets, and short detached currents in place of anatomical arms. There are no legs or waist cut-off.

## Try it

Open `Assets/Scenes/Avatar_Test.unity` and press Play. The animated avatar in front of the camera is a standalone visual preview. NetSync also spawns the local avatar so the wearer's water hands can be tested.

The scene automatically alternates **VR for 10 seconds, then MR for 10 seconds**, repeating continuously. The interval uses unscaled time, so changing the animation time scale does not change the switching interval. On `Water Avatar Test`, disable `Auto Switch Modes` to stay in a mode or change `Mode Switch Interval` to adjust the duration. Manual switches below restart the 10-second interval.

| Control | Action |
| --- | --- |
| Desktop `1` | Underwater VR |
| Desktop `2` | Passthrough MR |
| Desktop `3` | Show or hide the animated preview |
| Right controller primary button (A on PICO controllers) | Toggle VR / MR |
| Left controller primary button (X on PICO controllers) | Show or hide the animated preview |

On the `Water Avatar Test` object, `Mixed Reality` selects the starting mode and can also be changed during Play mode. Its component context menu exposes both mode switches. A desktop cannot show the headset's passthrough; MR clears to transparent black there. The existing STYLY XR Rig performs the headset transition.

For an isolated preview, enable **NetSyncManager > Offline Mode**. To test multiple clients, disable Offline Mode, configure the server address/discovery and room settings, run the same scene on each client, and hide the animated preview. The remote-only rebuild preserves these scene settings. The reference video is not copied into the project or build.

## Use the avatar in another scene

Assign these prefab assets to that scene's NetSyncManager:

- Local Avatar Prefab: `Prefabs/WaterAvatar_Local.prefab`
- Remote Avatar Prefab: `Prefabs/WaterAvatar_Remote.prefab`

Both prefabs have separate head, left-hand, and right-hand tracking anchors wired to `NetSyncAvatar`. The runtime component reads these anchors after NetSync interpolation; it never overwrites them. The local wearer sees the original water hands; local tracking-loss behavior is unchanged. `WaterAvatarRemoteForm`, attached only to the remote prefab, replaces arm IK with detached forms driven by hand positions. Wrist rotation does not affect these forms, so controller/hand orientation differences do not expose reversed fingers. Keep avatar roots at unit scale.

For remote hands, tracking-loss events, inactive/missing anchors, non-finite poses, default-origin poses, and implausible reaches hold the last valid pose and fade that side out over 0.22 seconds. Recovery fades in over 0.32 seconds with position smoothing. A correction over 0.45 metres fades out before relocating. Stationary hands remain visible: stillness is not treated as tracking loss. Currents shorten near the torso instead of stretching into it.

NetSync 0.17.5 disables both hand anchors when a received packet lacks `HeadValid`, while retaining its last valid head pose; the remote form follows that behavior. A missing, inactive, or non-finite head anchor dissolves the whole form. There is no public head-validity or packet-age API on `NetSyncAvatar`, so this component does not infer network disconnection from motion or change the package's transport timeout behavior.

## Material and rendering

`Materials/LivingWater.mat` is retained for the local avatar. The remote avatar uses the independent `Materials/RemoteLivingWater.mat` and `Shaders/RemoteLivingWater.shader`; per-renderer visibility fades do not modify shared materials. Both materials expose water tint, core opacity, reflection strength, flow detail/speed/brightness, and ripple displacement. `Textures/SeamlessWaterFlow.asset` supplies the tiled flow field. It is generated locally from deterministic noise.

The shader uses premultiplied color and alpha (`One, OneMinusSrcAlpha`) and Unity stereo-instancing macros. It does not sample scene color or passthrough video, so it works with the mobile pipeline's opaque texture disabled. Reflections are stylized analytic lighting, not real-world camera reflections or physical refraction. No bloom, depth texture, renderer feature, or additional package is required. Post-processing is disabled on the test camera to preserve alpha; preserve alpha explicitly if adding post-processing in another scene.

The local geometry remains 32,364 triangles, excluding up to 48 small droplets. The remote geometry uses 27,864 triangles and up to 24 droplets, with two skinning bones and four detached mesh renderers. Geometry is baked into separate assets; no mesh generation runs at runtime. All Unity API calls run on the main thread. Shader complexity and transparent overdraw still need profiling on the intended headset and participant count.

Unity's [custom shader stereo-instancing documentation](https://docs.unity.com/en-us/engine/6000.3/manual/xr/graphics/stereo-rendering/single-pass-instancing) describes the macros used here.

## Rebuild and verify

`Tools > Ancient Future > Rebuild Remote Water Avatar Only` updates only the remote prefab and remote geometry/material; it preserves the local prefab, local meshes, shared flow texture, and the test scene. Use this for remote-only design changes.

`Tools > Ancient Future > Build Water Avatar Test` regenerates the owned meshes, material, texture, prefabs, and `Water Avatar Test` hierarchy in the open `Avatar_Test` scene. It also reassigns the scene's NetSync avatar references and enables offline testing. Rebuilding replaces edits to these generated assets. Custom production variants should be separate prefab/material assets.

From the project root, with the Editor connected:

```sh
unity status --until-ready --format json
unity command script run --caller plugin --skill unity-cli --format json -- --file Tools/WaterAvatar/ValidateWaterAvatar.cs --entry ValidateWaterAvatar.Assets
unity command editor.playmode start --caller plugin --skill unity-cli --format json
unity status --until-ready --format json
unity command script run --caller plugin --skill unity-cli --format json -- --file Tools/WaterAvatar/ValidateWaterAvatar.cs --entry ValidateWaterAvatar.Runtime
unity command editor.playmode stop --caller plugin --skill unity-cli --format json
```

Validation output and actual Unity renders are saved under `Documentation/WaterAvatar/` at the project root. Runtime validation injects received poses through NetSync's real remote-avatar update path and checks tracking loss/recovery, extreme wrist positions, local visibility, and rendered MR alpha. It does not establish two-device transport, headset stereo presentation, passthrough quality, or device frame rate.

The previous five-second `Documentation/WaterAvatar/water-avatar-preview.mp4` shows the original avatar. `Documentation/WaterAvatar/remote-water-avatar.png` shows the current remote design rendered by Unity. `Tools/WaterAvatar/RecordWaterAvatarPreview.cs` can reproduce the 120 frames at 24 fps through `script run` with entry `RecordWaterAvatarPreview.Start` while the scene is playing.

The existing `_Build/WaterAvatarTest.apk` predates the remote-form redesign. Rebuild it to test the current source on hardware. The current project's Android setup enables PICO, PICO 4 / Ultra controller profiles, hand tracking, and passthrough. To rebuild only the test scene after stopping Play mode:

```sh
unity command build run --caller plugin --skill unity-cli --format json -- --target Android --outputPath _Build/WaterAvatarTest.apk --scenes '["Assets/Scenes/Avatar_Test.unity"]' --confirm true
unity command build status --caller plugin --skill unity-cli --format json
```

The original test scene is preserved in `Documentation/WaterAvatar/Backups/Avatar_Test.before-water.unity.txt`. With Unity closed, copying that file back to `Assets/Scenes/Avatar_Test.unity` restores the original scene while retaining its existing `.meta` file. The water assets are isolated under this directory; package sources were not modified.
