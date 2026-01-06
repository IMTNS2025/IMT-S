# Particle Reload System - Setup Guide

## Overview
This system adds the ability to dynamically control particle count and reload particles during runtime through the main menu and in-game UI.

## New Components Added

### 1. **ParticleReloadSystem.cs**
- ECS system that handles destroying and respawning particles
- Listens for reload requests and updates particle count

### 2. **ParticleReloadRequestAuthoring.cs**
- Authoring component to create the singleton entity for reload requests
- **IMPORTANT**: Add this component to a GameObject in your scene (e.g., create an empty GameObject called "ParticleReloadManager")

### 3. **ParticleManager.cs**
- MonoBehaviour bridge between UI and ECS
- Manages particle count settings and reload requests
- **IMPORTANT**: Add this component to a GameObject in your scene (can be the same as above or separate)

### 4. **Updated MainMenuController.cs**
- Added particle count slider
- Added "Reload on Play" checkbox
- Connects to ParticleManager

### 5. **Updated InGameMenuController.cs**
- Added reload button functionality
- Added keyboard shortcut (R key) for quick reload

## Setup Instructions

### Scene Setup
1. **Create ParticleManager GameObject**:
   - In your scene hierarchy, create an empty GameObject
   - Name it "ParticleReloadManager"
   - Add the `ParticleReloadRequestAuthoring` component
   - Add the `ParticleManager` component

2. **Configure ParticleManager Settings**:
   - Select the ParticleReloadManager GameObject
   - In the Inspector, find the ParticleManager component
   - Set the following values (customize as needed):
     - Min Particle Count: 100
     - Max Particle Count: 2000
     - Default Particle Count: 400
     - Particle Count Step: 50
     - Reload On Play: false (can be toggled in UI)

### UI Setup - Main Menu

1. **Add Particle Count Slider**:
   - In your Main Menu Canvas, create a new Slider:
     - Right-click Canvas ? UI ? Slider
     - Name it "ParticleCountSlider"
     - Position it in your menu layout

2. **Add Particle Count Text**:
   - Create a TextMeshPro Text element:
     - Right-click Canvas ? UI ? Text - TextMeshPro
     - Name it "ParticleCountText"
     - Position it near the slider
     - Set default text to "Particle Count: 400"

3. **Add Reload on Play Toggle**:
   - Create a Toggle:
     - Right-click Canvas ? UI ? Toggle
     - Name it "ReloadOnPlayToggle"
     - Add a label child TextMeshPro text: "Reload on Play"
     - Position it below the slider

4. **Connect to MainMenuController**:
   - Select your MainMenuController GameObject
   - In the Inspector, find the MainMenuController component
   - Assign references:
     - Particle Count Slider ? ParticleCountSlider
     - Particle Count Text ? ParticleCountText
     - Reload On Play Toggle ? ReloadOnPlayToggle
     - Particle Manager ? ParticleReloadManager (from hierarchy)

### UI Setup - In-Game Menu

1. **Add Reload Button**:
   - In your In-Game Button Panel, create a new Button:
     - Right-click ButtonPanel ? UI ? Button - TextMeshPro
     - Name it "ReloadButton"
     - Set button text to "Reload Particles (R)"
     - Position it next to the Menu button

2. **Connect to InGameMenuController**:
   - Select your InGameMenuController GameObject
   - In the Inspector, find the InGameMenuController component
   - Assign references:
     - Reload Button ? ReloadButton
     - Particle Manager ? ParticleReloadManager (from hierarchy)

## How to Use

### Main Menu
1. Use the slider to select the desired particle count (it will snap to increments defined by ParticleCountStep)
2. Check "Reload on Play" if you want particles to automatically reload when starting simulation or free mode
3. Start your simulation or free mode as normal

### In-Game
1. Press the "Reload Particles" button to destroy all current particles and spawn new ones with the selected count
2. Or use the **R** keyboard shortcut for quick reload
3. Press ESC or the Menu button to return to the main menu

## Features

- **Dynamic Particle Count**: Change the number of particles without restarting the application
- **Reload on Play**: Automatically reload particles when starting simulation or free mode
- **In-Game Reload**: Reload particles during gameplay with a button or keyboard shortcut
- **Customizable Settings**: Editor-exposed min/max values and step size
- **ECS Integration**: Efficient particle destruction and spawning using DOTS

## Technical Notes

- The system uses a singleton ParticleManager to bridge MonoBehaviour and ECS
- Particle reload is handled by the ParticleReloadSystem which runs in the InitializationSystemGroup
- The system destroys all existing Particle entities and triggers a respawn with the new count
- The reload is frame-perfect and doesn't cause noticeable lag for reasonable particle counts

## Troubleshooting

**"ParticleManager not found"**:
- Make sure you've added the ParticleManager component to a GameObject in the scene
- Check that the reference is assigned in MainMenuController and InGameMenuController

**"ParticleReloadRequest entity not found"**:
- Make sure you've added the ParticleReloadRequestAuthoring component to a GameObject in the scene
- Ensure the scene has been saved and the entity was baked

**Particles not reloading**:
- Check the Console for error messages
- Ensure the ParticleSpawnSettings entity exists in the scene
- Verify that the particle prefab is properly assigned in ParticleSpawnSettingsSO

**Slider not working correctly**:
- Ensure the slider's Min Value, Max Value are set correctly
- Check that the ParticleManager's min/max/step values are reasonable
- The slider automatically rounds to the nearest step value
