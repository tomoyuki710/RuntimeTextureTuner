# Runtime Texture Tuner

[日本語](README.md) | English

A development tool for tuning per-texture Mip Map Bias, Aniso Level and Filter Mode while your content is running on a VR device. You no longer need to rebuild and reinstall every time you change a value.

The values you settle on are brought back to the PC as JSON, reviewed in the Unity Editor, and then applied to `TextureImporter`. The tuning features are included only in builds made with a dedicated scripting define.

## Tested environment

| Item | Details |
|---|---|
| Unity | 6000.3.27f2 |
| Render pipeline | URP 17.3 |
| Device | PICO 4 Ultra (Android, Vulkan, OpenXR 1.15.0) |
| Shaders | URP Lit and Unlit |

`package.json` targets Unity 6000.0 and later, but the package has only been run on 6000.3.27f2. For 6000.0, only the existence of the APIs it uses has been checked.

## Installation

In the Unity Package Manager, choose "Install package from git URL..." and enter the following URL.

```
https://github.com/tomoyuki710/RuntimeTextureTuner.git?path=/Packages/com.tn.runtimetexturetuner
```

To pin a version, append a tag to the URL.

```
https://github.com/tomoyuki710/RuntimeTextureTuner.git?path=/Packages/com.tn.runtimetexturetuner#v0.1.0
```

The package does not depend on the Input System, the XR Interaction Toolkit, or any vendor XR SDK.

## Usage

### 1. Make a tuning build

Choose `Tools > Runtime Texture Tuner > Build Tuning Player...`. It builds with your current build settings (scene list and build target) and adds the scripting define `RUNTIME_TEXTURE_TUNER_ENABLED` for that build only. Your project settings are not modified.

If you have your own build process, you can make a tuning build in either of these ways.

- Add `RUNTIME_TEXTURE_TUNER_ENABLED` to the scripting defines of a Build Profile or of the Player Settings
- Call `TuningBuild.BuildPlayer(options)` from your build script

In a tuning build, a table that maps textures to their source assets is added to the build copy of each scene. The original scenes are not changed.

### 2. Tune on the device

| Action | Result |
|---|---|
| Press and hold both thumbsticks for 1 second | Open the panel. If it is open, hide it |
| Right-hand ray and right trigger | Press buttons and drag sliders on the panel |
| Point the ray at a wall or floor and pull the right trigger | List the renderers in that direction as candidates |
| Grab the bar at the top of the panel and move the ray | Move the panel |

1. Open the panel, point the ray at the object you want to tune, and pull the trigger.
2. Candidates are listed nearest first. Pick the target. The selected target is outlined.
3. The list switches to the textures used by that target. Pick the texture to tune.
4. Change Bias, Aniso and Filter.
5. Compare with `Show ORIGINAL` and `Show ADJUSTED`, then press `Keep` on the values you want to adopt.
6. Press `Hide panel` to check the result without the panel. The adjusted values stay in effect.
7. Press `Export kept` to save the kept values as JSON in the app's data folder.

Targets are selected by intersecting the ray with renderer bounds, so no colliders or layer setup are required. Bounds are box approximations, so occlusion and overlapping objects cannot be told apart. Confirm the target from the candidate list.

### 3. Apply in the Editor

1. Connect the device over USB and open `Tools > Runtime Texture Tuner > Sessions`.
2. Press `Pull from device (adb)` to fetch the JSON. To open a file directly, use `Load JSON...`.
3. Review the current values, the values adjusted on the device, and the status of each row.
4. Select the rows to apply and press `Apply selected`.
5. To undo, press `Restore last Apply`.

Only the fields you changed on the device are written, out of `mipMapBias`, `anisoLevel` and `filterMode`. Source images, max size, compression format and other settings are not touched. These three fields are import settings shared by all platforms, so the change also affects platforms other than Android.

The statuses in the list mean the following.

| Status | Meaning |
|---|---|
| Ready | Can be applied |
| Unchanged | Already has the same values |
| Stale | The source file changed after the tuning build. Not applied by default |
| Conflict | The settings to be written were changed after the tuning build. Not applied by default |
| Unsupported | The asset is not supported |
| Missing | The asset was not found |
| Invalid | The entry is malformed |

A record of each Apply is saved under `Library/RuntimeTextureTuner/`.

### 4. Make the product build

Build as you normally do. Builds without `RUNTIME_TEXTURE_TUNER_ENABLED` contain neither the tuning code nor the texture table.

## Which textures can be brought back

A texture can be brought back to the Editor when all of the following are true.

- It is under `Assets/`
- It is a standalone texture asset with a `TextureImporter`
- At the time of the tuning build, it is referenced by a material of a renderer in a scene

Other textures (those loaded through Addressables or AssetBundles, generated or swapped at runtime, located in `Packages/`, embedded in another asset, and so on) can still be tried on the device, but cannot be brought back. The panel shows the reason.

## Changing the settings

Button usage differs from one project to another, so the bindings can be changed with a settings asset. Create one with `Assets > Create > Runtime Texture Tuner > Settings` and place it in a `Resources` folder with the name `RuntimeTextureTunerSettings`.

| Item | Default |
|---|---|
| Opening the panel | Press and hold both thumbsticks for 1 second |
| Select button | Right trigger |
| Hand that casts the ray | Right hand |
| Bias range | -1.0 to +2.0 (step 0.05) |
| Panel distance | 1.2 m in front of the head |

A settings asset placed in `Resources` is also included in product builds. It is not used there, because the code that reads it is absent, but the asset itself remains in the build.

### Replacing the input

If your project has its own input system, implement `ITunerInputAdapter` and set it with `RuntimeTuner.Current.SetInputAdapter(...)`. The default implementation reads `InputDevices` from `UnityEngine.XR`.

### Suspending existing input while tuning

The tuner does not interfere with existing input, so your content still receives the same button input while the panel is being operated. If that is a problem, subscribe to `RuntimeTuner.OnTuningModeChanged` and suspend your own input while the panel is open.

## Notes and limitations

- Unity's documentation does not recommend a negative Mip Map Bias stronger than -0.5, because of the performance cost and shimmering. The panel shows a warning below -0.5.
- For textures without mipmaps, changing the bias has no visible effect.
- When Anisotropic Textures in the Quality Settings is set to anything other than Per Texture, the per-texture Aniso Level is overridden.
- Textures swapped in through a `MaterialPropertyBlock` are not listed. The panel does warn that a property block is present.
- Changing the values of a shared texture affects every material and renderer that uses it.
- Per-texture settings may have no effect in the following cases. This is an expectation from reading the shader source and has not been verified on a device.
  - A Shader Graph with a Sampler State node connected
  - A Shader Graph whose Mip Sampling Mode is LOD
  - Layers 1 to 3 of URP Terrain Lit
- Textures referenced only by something other than renderers, such as UI, are not supported.
- The Sessions window can load one session at a time.
- On-device behavior has only been verified on PICO 4 Ultra.

## License

MIT License. See [LICENSE](LICENSE).
