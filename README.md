# Echo Architect

Echo Architect is a sound-driven horror survival game built in Unity. The player explores a dead town, manages noise, avoids a pursuing monster, and searches for the escape gate before being caught.

## Features

- First-person horror chase gameplay
- Noise-based monster awareness
- Microphone and movement noise detection
- Sprint, crouch, jump, and mouse-look controls
- Genies avatar creator integration
- Enclosed town arena with physical perimeter walls
- Main menu, game over flow, best survival time, and replay loop

## Controls

- `WASD` - Move
- `Mouse` - Look around
- `Shift` - Sprint
- `Ctrl` - Crouch
- `Space` - Jump
- `R` - Return to main menu after game over
- `Esc` - Return from the Genies creator screen

## Requirements

- Unity `2022.3.62f3`
- Git LFS for large Unity assets

## Setup

1. Clone the repository.
2. Make sure Git LFS is installed:

   ```powershell
   git lfs install
   git lfs pull
   ```

3. Open the project folder in Unity Hub.
4. Use Unity `2022.3.62f3`.
5. Open `Assets/Main.unity`.
6. Press Play.

## Notes

This repository intentionally ignores Unity-generated folders such as `Library`, `Temp`, `Logs`, `UserSettings`, IDE project files, and build outputs. Unity will regenerate them locally when the project is opened.
