# Ghost Avatar

A faint blue-white silhouette based on `WaterAvatar_Remote`, for noticing other participants without obscuring the experience. The lower body dissolves naturally. The material stays visible at a distance and becomes clearer nearby; it has no water reflections, texture animation, particles, shadows, or physics colliders.

## Choose the correct NetSync slot

| Prefab | NetSyncManager field | Behaviour |
| --- | --- | --- |
| `Prefabs/GhostAvatar_Presence.prefab` | **Human Presence Prefab** | Displays the other person's physical HMD position and yaw. Head-centred body, no inferred physical hands. Visible through virtual scenery. Use this for co-located collision awareness. |
| `Prefabs/GhostAvatar_Remote.prefab` | **Remote Avatar Prefab** | Follows the networked head and hands, including virtual movement. Respects scene depth. Use this as the normal remote avatar when physical and virtual positions coincide, or alongside physical presence when they differ. |

The presence root must remain at unit scale: NetSync places it at **head height**, not on the floor. Its mesh is offset accordingly. It intentionally has no `NetSyncAvatar` component, so it cannot overwrite NetSync's physical-position driver. Presence creation, local-user exclusion, and disconnect cleanup are handled by NetSync 0.17.5.

Each participating client needs a non-null **Local Avatar Prefab** (for example, the existing `WaterAvatar_Local`). A null local prefab enables NetSync stealth mode, and stealth users do not produce human-presence markers. The existing `MainScene` currently has a null local prefab; this task creates reusable assets without changing that scene's multiplayer configuration. Configure the avatar fields and the common server/room on all participating clients before a multiplayer test. Co-located physical poses also require a shared, calibrated coordinate system.

Do not put the Remote prefab into the Human Presence slot: their origins and pose drivers differ. Using both at coincident positions can layer two silhouettes; choose the slot appropriate to the experience.

## Preview and tune

Open `GhostAvatar_Preview.unity` and press Play for an offline visual preview. This scene contains a camera and the Remote ghost; it opens no network connection. Move its head and hand tracker children during Play to inspect the form.

Both materials expose these controls:

- **Distant core opacity:** 0.07 at 2.5 metres and farther.
- **Nearby core opacity:** 0.22 at 0.75 metres and nearer, blended smoothly between those distances.
- **Silhouette opacity:** an additional 0.16 at grazing angles.
- **Ghost tint:** pale blue-white. Opacity and tint can be adjusted independently.

Distances are horizontal world-space distances from the rendering camera. The ghost never becomes invisible solely because it is far away. `GhostPresence.mat` uses depth test `Always` and render queue 3100 so virtual walls do not hide it. `GhostSilhouette.mat` uses `LessEqual` and queue 3000 for normal remote rendering.

The Remote prefab reuses the existing water meshes, `WaterAvatarRig`, and `WaterAvatarRemoteForm`. Preserve the `WaterAvatar` folder when moving these assets. It inherits the source's tracking loss/recovery fades and position validation without editing shared code or materials. NetSync retains the last valid head pose for packets without `HeadValid`; this asset does not add packet-age detection or change transport timeouts.

The URP unlit shader uses premultiplied colour/coverage and stereo-instancing macros. It needs no opaque/depth texture, bloom, renderer feature, or scene-colour sampling. Preserve camera alpha when using passthrough; post-processing in a host scene can affect compositor alpha.

## Rebuild and verification

**Tools > Feu Bleu > Rebuild Ghost Avatars** regenerates only the two ghost prefabs, their materials, and the presence mesh. It resets generated material values, so duplicate a material/prefab for a custom variant. It does not modify WaterAvatar assets or production scenes.

The project includes the Unity Pipeline package for Editor-driven creation and validation. From the project root, with this project open:

```sh
unity status --until-ready --project-path "$PWD" --format json
unity command script run --project-path "$PWD" --caller plugin --skill unity-cli -- --file Tools/GhostAvatar/ValidateGhostAvatar.cs --entry ValidateGhostAvatar.Assets
```

For runtime checks, open the ghost preview scene, enter Play mode, confirm that frames are advancing, then run:

```sh
unity command script run --project-path "$PWD" --caller plugin --skill unity-cli -- --file Tools/GhostAvatar/ValidateGhostAvatar.cs --entry ValidateGhostAvatar.Runtime
```

`ValidateGhostAvatar.CreatePreview` recreates the preview scene without replacing the open scene. Reports and actual Unity renders are in `Documentation/GhostAvatar/`. Validation passed 19 asset checks and 15 runtime/render checks in Unity 6000.3.19f1 on Metal. Runtime checks inject packets into NetSync's receive path and exercise its human-presence lifecycle; they do not establish two-device transport, headset stereo/passthrough quality, real-world alignment, or device performance. This is a visual awareness aid, not a physical collision-prevention system.
