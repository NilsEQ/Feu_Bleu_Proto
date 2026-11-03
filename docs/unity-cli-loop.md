# Unity CLI Loop

This project uses [Unity CLI Loop](https://github.com/hatayama/unity-cli-loop)
to operate the Unity Editor from Codex and the terminal.

The Unity package is pinned to `3.21.3` in `Packages/manifest.json`.
The verified dispatcher version is `3.13.2`, and the package selects project
runner `3.12.3` through `.uloop/project-runner-pin.json`. These components
have separate version numbers.

## Setup on another machine

Install the global `uloop` CLI using the upstream installation guide, then
run these commands from the Unity project root:

```sh
uloop launch
uloop skills install --agents
uloop status
```

Unity resolves the package through the existing OpenUPM scoped registry.
Codex skills are stored in `.agents/skills/uloop-*`.

## Common commands

```sh
uloop list --names
uloop compile
uloop get-logs --log-type Error --max-count 20
```

Run commands against this project from its root, or pass
`--project-path /path/to/Feu_Bleu_Proto`. Wait for `uloop status` to report
`Ready` before sending an Editor command. Compilation can end an active
Play Mode session.

Keep `.uloop/project-runner-pin.json` in version control. Generated command
metadata in `.uloop/tools.json` and runtime exports in `.uloop/outputs/`
are ignored.
