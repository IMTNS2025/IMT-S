# Particle Reload Feature - Implementation Summary

## What Was Added

This implementation adds a complete particle count control and reload system to your airflow simulation project.

## New Files Created

1. **ParticleReloadSystem.cs** - ECS system for handling particle destruction and respawning
2. **ParticleReloadRequestAuthoring.cs** - Baking component for the reload request entity
3. **ParticleManager.cs** - MonoBehaviour bridge between UI and ECS
4. **ParticleManagerExample.cs** - Example usage script (optional)
5. **PARTICLE_RELOAD_SETUP.md** - Detailed setup instructions

## Modified Files

1. **MainMenuController.cs**
   - Added particle count slider control
   - Added reload on play checkbox
   - Integrated with ParticleManager
   - Triggers reload on mode start if enabled

2. **InGameMenuController.cs**
   - Added reload button functionality
   - Added keyboard shortcut (R key)
   - Integrated with ParticleManager

## Features Implemented

### Main Menu Features
? **Particle Count Slider**
   - Customizable min/max values (set in ParticleManager component)
   - Customizable step size for slider increments
   - Real-time text display showing current count
   - Values are clamped to valid ranges

? **Reload on Play Checkbox**
   - When enabled, automatically reloads particles when starting simulation or free mode
   - State is saved in ParticleManager component
   - Can be toggled on/off in the menu

### In-Game Features
? **Reload Button**
   - Destroys all current particles
   - Spawns new particles with the selected count
   - Works during simulation and free mode
   - Accessible via UI button or R keyboard shortcut

### Technical Features
? **ECS Integration**
   - Efficient particle destruction using EntityQuery
   - Reuses existing particle spawn system
   - Frame-perfect reload without noticeable lag
   - Works with DOTS architecture

? **Singleton Pattern**
   - ParticleManager uses singleton for easy access
   - DontDestroyOnLoad ensures persistence
   - Accessible from anywhere via ParticleManager.Instance

## Usage Instructions (Quick Start)

1. **Add to Scene**:
   - Create empty GameObject named "ParticleReloadManager"
   - Add `ParticleReloadRequestAuthoring` component
   - Add `ParticleManager` component
   - Configure min/max/default values in Inspector

2. **Setup UI**:
   - Add Slider to Main Menu ? name it "ParticleCountSlider"
   - Add TMP_Text to Main Menu ? name it "ParticleCountText"
   - Add Toggle to Main Menu ? name it "ReloadOnPlayToggle"
   - Add Button to In-Game Menu ? name it "ReloadButton"

3. **Connect References**:
   - Assign slider, text, and toggle to MainMenuController
   - Assign reload button to InGameMenuController
   - Assign ParticleManager to both controllers

4. **Test**:
   - Run the game
   - Adjust particle count slider
   - Enable/disable "Reload on Play"
   - Start simulation or free mode
   - Press reload button or R key in-game

## Keyboard Shortcuts

- **ESC**: Return to main menu (existing)
- **R**: Reload particles with current count (new)

## Customization

All settings can be customized in the Inspector:

### ParticleManager Component
- `Min Particle Count`: Minimum particles (default: 100)
- `Max Particle Count`: Maximum particles (default: 2000)
- `Default Particle Count`: Starting value (default: 400)
- `Particle Count Step`: Slider increment (default: 50)
- `Reload On Play`: Auto-reload flag (default: false)

## Integration Points

The system integrates with:
- **ParticleSpawnSystem**: Triggers respawn via doSpawn flag
- **ParticleSpawnSettings**: Updates particle count dynamically
- **MainMenuController**: UI controls for particle settings
- **InGameMenuController**: In-game reload functionality
- **SimulatedInputController**: Works in both simulation and free mode

## Performance Considerations

- Destroying all particles uses efficient EntityQuery
- Spawning uses the existing optimized ParticleSpawnSystem
- For counts under 2000 particles, reload is nearly instant
- No memory leaks - all entities are properly cleaned up
- System only runs when reload is requested (no overhead during normal gameplay)

## Next Steps

After setup:
1. Test the slider range and step size
2. Adjust min/max values based on your performance requirements
3. Customize button/slider styling to match your UI theme
4. Consider adding particle count presets (e.g., Low/Medium/High buttons)
5. Add visual feedback during reload (optional loading indicator)

## Support

If you encounter issues:
1. Check the Console for error messages
2. Verify all components are added to the scene
3. Ensure all UI references are assigned
4. See PARTICLE_RELOAD_SETUP.md for detailed troubleshooting

## Architecture Diagram

```
UI (MonoBehaviour)           ECS World
?????????????????           ?????????
MainMenuController    
    ?? Slider ?????????
    ?? Toggle ?????????
    ?? Buttons ????????
                    ???
InGameMenuController???
    ?? Reload Btn ?????
    ?? R Key ??????????
                  ????
ParticleManager   ????      ParticleReloadRequest (Entity)
  (Singleton)     ????              ?
    ?????????????????????????????????
                                    ?
                         ParticleReloadSystem
                                    ?
                         ???????????????????????
                         ?                     ?
                  EntityQuery            ParticleSpawnSettings
                  (Destroy)              (Update & Trigger)
                         ?                     ?
                    Particle Entities    ParticleSpawnSystem
                                              ?
                                         New Particles
```
