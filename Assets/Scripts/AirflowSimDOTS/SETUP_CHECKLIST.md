# Particle Reload System - Quick Setup Checklist

## ? Step 1: Scene Setup (5 minutes)

### Create ParticleManager GameObject
- [ ] Create empty GameObject in scene
- [ ] Name it "ParticleReloadManager"
- [ ] Add `ParticleReloadRequestAuthoring` component
- [ ] Add `ParticleManager` component
- [ ] Configure ParticleManager settings:
  - [ ] Min Particle Count: **100**
  - [ ] Max Particle Count: **2000**
  - [ ] Default Particle Count: **400**
  - [ ] Particle Count Step: **50**

## ? Step 2: Main Menu UI (10 minutes)

### Option A: Create Container Panel (Recommended)
- [ ] Right-click MainMenuPanel ? UI ? Panel
- [ ] Name: "ParticleSettingsPanel"
- [ ] Position below pattern dropdown

### Add UI Elements
- [ ] **Particle Count Slider**
  - Right-click ParticleSettingsPanel ? UI ? Slider
  - Name: "ParticleCountSlider"
  - Set Whole Numbers: ?
  - Value: 400, Min: 100, Max: 2000

- [ ] **Particle Count Text**
  - Right-click ParticleSettingsPanel ? UI ? Text - TextMeshPro
  - Name: "ParticleCountText"
  - Text: "Particle Count: 400"
  - Font Size: 18
  - Alignment: Center

- [ ] **Reload on Play Toggle**
  - Right-click ParticleSettingsPanel ? UI ? Toggle
  - Name: "ReloadOnPlayToggle"
  - Edit Label ? Text: "Reload on Play"

### Connect References
- [ ] Select MainMenuController GameObject
- [ ] Assign in Inspector:
  - [ ] Particle Count Slider ? ParticleCountSlider
  - [ ] Particle Count Text ? ParticleCountText
  - [ ] Reload On Play Toggle ? ReloadOnPlayToggle
  - [ ] Particle Manager ? ParticleReloadManager

## ? Step 3: In-Game UI (3 minutes)

### Add Reload Button
- [ ] Right-click InGameMenuPanel/ButtonPanel ? UI ? Button - TextMeshPro
- [ ] Name: "ReloadButton"
- [ ] Button Text: "Reload Particles (R)"
- [ ] Position next to Menu button

### Connect References
- [ ] Select InGameMenuController GameObject
- [ ] Assign in Inspector:
  - [ ] Reload Button ? ReloadButton
  - [ ] Particle Manager ? ParticleReloadManager

## ? Step 4: Test (2 minutes)

### Quick Debug Setup (Optional but Recommended)
- [ ] Add `ParticleManagerDebugger` component to any GameObject
- [ ] Run the game
- [ ] Press **F3** to check system status
- [ ] Look at bottom-left corner for debug overlay

### Test in Editor
- [ ] Run the game
- [ ] **Check Console for**: `[ParticleManager] Initialized with count: 400...`
- [ ] Check slider moves smoothly
- [ ] **Check Console for**: `[MainMenuController] Slider changed...`
- [ ] Verify text updates when slider moves
- [ ] Verify values snap to step increments (50, 100, 150, etc.)
- [ ] Toggle "Reload on Play" checkbox
- [ ] **Check Console for**: `[MainMenuController] Reload on play toggled: true`
- [ ] Start simulation or free mode
- [ ] If "Reload on Play" was checked, particles should reload
- [ ] **Check Console for**: `[ParticleReloadSystem] Processing reload request...`
- [ ] Press R key in-game
- [ ] **Check Console for**: `[InGameMenuController] Reload button clicked`
- [ ] Verify particles reload
- [ ] Press Reload button
- [ ] **Check Console for**: Same reload messages
- [ ] Verify particles reload again

### Console Check
- [ ] No errors in Console
- [ ] Look for "[ParticleManager] Reload requested..." message
- [ ] Look for "[ParticleReloadSystem] Processing reload..." message

### If Something Doesn't Work
- [ ] Press **F3** (if using debugger) to see detailed status
- [ ] Read the console messages carefully
- [ ] See **TROUBLESHOOTING.md** for detailed solutions
- [ ] All issues are covered in the troubleshooting guide

## ?? Quick Visual Layout Reference

```
Main Menu Panel
???????????????????????????????
?  Pattern Dropdown           ?
?  ???????????????????????    ?
?  ???????????????????????    ?
?                             ?
?  Particle Settings          ?
?  ???????????????????????    ?
?  ? Particle Count: 400 ?    ?
?  ? [====|==========]   ?    ?
?  ? [?] Reload on Play  ?    ?
?  ???????????????????????    ?
?                             ?
?  [Start] [Free Mode] [Quit] ?
???????????????????????????????

In-Game UI
???????????????????????????????
?                             ?
?                             ?
?                [Menu]       ?
?      [Reload Particles (R)] ?
???????????????????????????????
```

## ?? Troubleshooting

### "ParticleManager not found"
- ? Check ParticleManager component is on a GameObject in scene
- ? Check references assigned in MainMenuController and InGameMenuController

### "ParticleReloadRequest entity not found"
- ? Check ParticleReloadRequestAuthoring is on a GameObject in scene
- ? Save scene and reload to ensure entity is baked

### Slider not snapping to steps
- ? Check ParticleCountStep value in ParticleManager (should be > 0)
- ? Ensure Whole Numbers is checked on Slider component

### Particles not reloading
- ? Check Console for errors
- ? Ensure ParticleSpawnSettingsAuthoring exists in scene
- ? Verify particle prefab is assigned in ParticleSpawnSettingsSO

### Text not updating
- ? Check Particle Count Text reference is assigned
- ? Ensure it's a TextMeshPro component, not legacy Text

## ?? Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **R** | Reload particles (in-game) |
| **ESC** | Return to main menu |

## ?? Optional Enhancements

After basic setup works, consider:
- [ ] Add preset buttons (Low/Medium/High)
- [ ] Add visual feedback during reload (loading indicator)
- [ ] Add particle count display in-game
- [ ] Add confirmation dialog for very high particle counts
- [ ] Add animation to slider
- [ ] Add tooltips to UI elements

## ? Feature Summary

What you just added:
? Dynamic particle count control (100-2000 particles)
? Slider with customizable step size
? Auto-reload on play feature
? In-game reload button
? Keyboard shortcut (R key)
? Real-time particle count display
? ECS-integrated particle management
? No performance impact when not reloading

## ?? Next Steps

1. Test with different particle counts
2. Find optimal range for your hardware
3. Adjust min/max/step values as needed
4. Customize UI styling to match your theme
5. Consider adding tooltips for better UX

---

**Estimated Total Setup Time: ~20 minutes**

**Need Help?** Check `PARTICLE_RELOAD_SETUP.md` for detailed instructions or `IMPLEMENTATION_SUMMARY.md` for technical details.
