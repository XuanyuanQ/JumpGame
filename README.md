# JumpGamwe

JumpGamwe is a small Unity URP jumping game. The player holds Space to charge a jump, releases Space to leap toward the next platform, and tries to land safely without falling into the water.

video link:https://studio.youtube.com/video/ULrGP9Og9CQ/edit

## Unity Version

- Unity 6000.3.6f1
- Universal Render Pipeline 17.3.0
- Input System 1.18.0

Using the same Unity version, or a newer Unity 6 version, is recommended.

## Controls

- Hold `Space`: charge jump power.
- Release `Space`: jump.
- Press `Y`: restart after success or game over.
- Press `Esc`: stop Play Mode in the Unity Editor. In a built application, this quits the game.

## Gameplay

1. Hold `Space` to charge.
2. Release `Space` to jump toward the next platform.
3. Landing on the next platform adds 1 point.
4. Falling into the water causes Game Over.
5. Touching multiple platforms at the same time also counts as failure.
6. The game ends when the player reaches the success area or fails.

## Features

- Charge animation with character squash and particle feedback.
- Randomly generated platforms.
- Platforms can be either cubes or cylinders.
- Platform colors are randomized with high saturation and brightness.
- Water surface ripple effect when the player lands or falls.
- Platform surface ripple/brightness effect when the player lands successfully.
- Floating `+1` popup after a successful landing.
- Smooth camera transition when the next platform changes direction.
- Runtime-generated HUD showing score, rules, success, and game-over messages.

## Main Scripts

- `Assets/Script/PlayerMoveControl.cs`  
  Handles player charging, jumping, score updates, UI, `+1` popup, restart, and quit logic.

- `Assets/Script/playerLifeCyle.cs`  
  Handles player collisions, success/failure detection, water ripples, and platform landing feedback.

- `Assets/Script/BoxSpawner.cs`  
  Spawns the next platform and randomly chooses between cube and cylinder platforms.

- `Assets/Script/CameraUpdate.cs`  
  Smoothly moves and rotates the camera based on the player and next platform positions.

- `Assets/Script/PondWaterShader.shader`  
  Custom water shader with base waves and impact ripples.

- `Assets/Script/PlatformRippleMaterial.cs`  
  Applies and updates runtime material properties for platform ripple effects.

- `Assets/Script/Resources/PlatformRippleShader.shader`  
  Shader used for platform surface ripple/brightness feedback. It is stored under `Resources` so it is included in builds.

## How To Run

1. Open the project folder in Unity.
2. Open the scene:
   ```text
   Assets/Scenes/SampleScene.unity
   ```
3. Press Play.
4. Use `Space` to charge and jump.

## Build Notes

The platform ripple shader is loaded at runtime through:

```csharp
Resources.Load<Shader>("PlatformRippleShader")
```

For this reason, keep this file in the project:

```text
Assets/Script/Resources/PlatformRippleShader.shader
```

If this shader is moved or deleted, the effect may still work in the Editor but disappear in Build and Run.

## Git Notes

Recommended files and folders to commit:

```text
Assets/
Packages/
ProjectSettings/
.gitignore
README.md
```

Do not commit Unity-generated local folders and files:

```text
Library/
Temp/
Logs/
UserSettings/
*.csproj
*.sln
```
