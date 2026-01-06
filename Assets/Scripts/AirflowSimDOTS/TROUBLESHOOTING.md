# Troubleshooting Guide - Particle Reload System

## ?? Debugging Steps

The system now has extensive debug logging. Follow these steps to identify the issue:

### Step 1: Check Console Messages

Run the game and look for these specific log messages:

#### On Game Start (Expected Messages):
```
[ParticleManager] Initialized with count: 400, min: 100, max: 2000, step: 50
[MainMenuController] Initialized slider - min: 100, max: 2000, value: 400
```

**If you DON'T see these messages:**
- ? ParticleManager component is not in the scene
- ? ParticleManager component is disabled
- ? MainMenuController references are not assigned

#### When Moving the Slider (Expected Messages):
```
[MainMenuController] Slider changed - raw value: 650, rounded: 650, step: 50
[ParticleManager] Particle count set to: 650
```

**If you DON'T see these messages:**
- ? Slider is not connected to MainMenuController
- ? ParticleManager reference is null
- ? Slider's OnValueChanged event is not hooked up

#### When Toggling "Reload on Play" (Expected Messages):
```
[MainMenuController] Reload on play toggled: true
```

**If you DON'T see this message:**
- ? Toggle is not connected to MainMenuController
- ? Toggle's OnValueChanged event is not hooked up

#### When Starting Simulation/Free Mode (Expected Messages):
```
[MainMenuController] OnModeStart called. ReloadOnPlay: true
[ParticleManager] Reload requested with 650 particles.
[ParticleReloadSystem] Processing reload request for 650 particles
[ParticleReloadSystem] Destroying 400 existing particles
[ParticleReloadSystem] Updated particle count to: 650
[ParticleReloadSystem] Triggered respawn with doSpawn = true
```

**If you DON'T see these messages:**
- ? ParticleReloadRequestAuthoring is not in the scene
- ? World is not created/initialized
- ? System is not running

#### When Clicking Reload Button In-Game (Expected Messages):
```
[InGameMenuController] Reload button clicked
[ParticleManager] Reload requested with 650 particles.
[ParticleReloadSystem] Processing reload request for 650 particles
...
```

**If you DON'T see these messages:**
- ? Reload button is not connected
- ? ParticleManager reference is null in InGameMenuController

---

## ??? Common Issues and Fixes

### Issue 1: "ParticleManager not found"

**Symptoms:**
- Console shows: `[MainMenuController] ParticleManager not found in scene!`
- OR: `[InGameMenuController] ParticleManager not found in scene!`

**Fix:**
1. Open your scene
2. Create an empty GameObject (Right-click Hierarchy ? Create Empty)
3. Name it "ParticleReloadManager"
4. Add the `ParticleManager` component
5. Add the `ParticleReloadRequestAuthoring` component
6. Save the scene

### Issue 2: "ParticleReloadRequest entity not found"

**Symptoms:**
- Console shows: `[ParticleManager] ParticleReloadRequest entity not found`

**Fix:**
1. Make sure `ParticleReloadRequestAuthoring` is on a GameObject in your scene
2. **IMPORTANT:** Save the scene
3. **IMPORTANT:** Reload the scene or restart Unity
4. The entity needs to be "baked" by Unity's entity conversion system
5. If still not working, select the GameObject with ParticleReloadRequestAuthoring and check Inspector to see if it has any errors

### Issue 3: Slider not responding

**Symptoms:**
- Slider moves but no console messages
- Text doesn't update

**Fix:**
1. Select your MainMenuController GameObject in the scene
2. In Inspector, check the "Main Menu Controller" component
3. Make sure these are assigned:
   - ? Particle Count Slider ? (Your slider GameObject)
   - ? Particle Count Text ? (Your text GameObject)
   - ? Reload On Play Toggle ? (Your toggle GameObject)
   - ? Particle Manager ? ParticleReloadManager (from scene)
4. If any are "None" or "Missing", drag the correct GameObject from the Hierarchy

### Issue 4: Slider moves in wrong increments

**Symptoms:**
- Slider doesn't snap to 50, 100, 150, etc.
- Values like 137, 243 appear

**Fix:**
1. Select the ParticleReloadManager GameObject
2. In the ParticleManager component, check:
   - Particle Count Step: should be **50** (or your desired step)
3. The slider will automatically round to the nearest step

### Issue 5: Reload button doesn't work

**Symptoms:**
- Clicking reload button does nothing
- No console messages when clicking

**Fix:**
1. Select your InGameMenuController GameObject
2. In Inspector, check:
   - ? Reload Button ? (Your reload button GameObject)
   - ? Particle Manager ? ParticleReloadManager (from scene)
3. Make sure the button has a Button component
4. Check that the button is active (enabled checkbox is checked)

### Issue 6: "Default world not found" or "World is not created"

**Symptoms:**
- Console shows: `[ParticleManager] Default world not found`
- OR: `[ParticleManager] Default world is not created`

**Fix:**
1. This means ECS world hasn't been initialized yet
2. Make sure you're in Play Mode
3. If it happens when starting simulation/free mode, the world should exist by then
4. Try the "Reload on Play" feature AFTER entering free mode or simulation, not during the initial spawn

### Issue 7: Particles don't actually respawn

**Symptoms:**
- Console shows reload messages
- But particle count doesn't change visually

**Fix:**
1. Check that `ParticleSpawnSettingsAuthoring` exists in your scene
2. Make sure it references a valid particle prefab
3. Check that the `ParticleSpawnSystem` is running
4. Look for any errors in the Console about spawning

### Issue 8: "ObjectDisposedException: ComponentTypeHandle has been invalidated"

**Symptoms:**
- Console shows error: `ObjectDisposedException: Attempted to access ComponentTypeHandle<ParticleReloadRequest> which has been invalidated by a structural change`
- Happens when pressing reload button

**Fix:**
This error has been fixed in the latest version of `ParticleReloadSystem.cs`. The system now:
1. Stores values locally before destroying entities
2. Gets fresh references after structural changes
3. If you still see this error, make sure you have the latest version of the file

### Issue 9: Toggle state not saving

**Symptoms:**
- Toggle is checked but reload doesn't happen
- OR: Toggle resets to unchecked

**Fix:**
1. The toggle state is only saved during that play session
2. If you stop and start play mode, it will reset
3. Set the default value in ParticleManager component:
   - Select ParticleReloadManager GameObject
   - Check "Reload On Play" checkbox in Inspector

---

## ?? Complete Checklist

Before asking for help, verify ALL of these:

### Scene Setup
- [ ] GameObject named "ParticleReloadManager" exists in scene
- [ ] It has `ParticleManager` component
- [ ] It has `ParticleReloadRequestAuthoring` component
- [ ] ParticleManager settings are configured (min: 100, max: 2000, etc.)

### Main Menu UI
- [ ] Slider exists with name "ParticleCountSlider"
- [ ] Text exists with name "ParticleCountText" (TextMeshPro)
- [ ] Toggle exists with name "ReloadOnPlayToggle"
- [ ] All three are children of MainMenuPanel (or visible in menu)

### Main Menu Controller References
- [ ] MainMenuController component has:
  - [ ] Particle Count Slider assigned
  - [ ] Particle Count Text assigned
  - [ ] Reload On Play Toggle assigned
  - [ ] Particle Manager assigned to ParticleReloadManager

### In-Game UI
- [ ] Button exists with name "ReloadButton"
- [ ] Button is in InGameMenuPanel or ButtonPanel
- [ ] Button has Button component

### In-Game Menu Controller References
- [ ] InGameMenuController component has:
  - [ ] Reload Button assigned
  - [ ] Particle Manager assigned to ParticleReloadManager

### Testing
- [ ] Run the game
- [ ] Open Console window (Window ? General ? Console)
- [ ] Filter console to show only "Log" messages
- [ ] Search for "[ParticleManager]" to see initialization
- [ ] Move slider and check for messages
- [ ] Start simulation/free mode and check for messages
- [ ] Click reload button and check for messages

---

## ?? Quick Test Procedure

1. **Clear Console** (Click "Clear" button in Console window)
2. **Start Play Mode**
3. **Look for**: `[ParticleManager] Initialized with count: 400...`
   - ? If you see it: ParticleManager is working
   - ? If not: ParticleManager is not in scene or disabled

4. **Move the Slider**
5. **Look for**: `[MainMenuController] Slider changed...`
   - ? If you see it: Slider is connected
   - ? If not: Slider reference not assigned or events not hooked

6. **Click Start Simulation or Free Mode**
7. **Look for**: `[ParticleManager] Reload requested...`
   - ? If you see it (and "Reload on Play" is checked): System is working
   - ? If not: Check toggle state and references

8. **Press R key or click Reload button**
9. **Look for**: `[InGameMenuController] Reload button clicked`
   - ? If you see it: Button is working
   - ? If not: Button not connected or InGameMenuController missing references

---

## ?? Still Not Working?

If you've checked everything above and it still doesn't work:

1. **Copy and paste ALL console messages** (especially those starting with [ParticleManager], [MainMenuController], [InGameMenuController], or [ParticleReloadSystem])

2. **Take screenshots of**:
   - ParticleReloadManager GameObject Inspector
   - MainMenuController component Inspector (showing all references)
   - InGameMenuController component Inspector (showing all references)
   - Your UI hierarchy showing slider, text, toggle, and button

3. **Verify the following**:
   - Unity version
   - Which scene you're testing in
   - Whether you saved the scene after adding components

---

## ?? Advanced Debugging

If the basic checks don't help, try these:

### Enable Deep Profiling
1. Window ? Analysis ? Profiler
2. Click CPU profiler
3. Enable "Deep Profile"
4. Look for ParticleReloadSystem in the profiler
5. Check if it's being called

### Check Entity Conversion
1. Window ? Entities ? Hierarchy
2. When in Play Mode, look for entities with ParticleReloadRequest component
3. If you don't see it, the baking failed

### Manual Test
Add this temporary script to test manually:

```csharp
using UnityEngine;

public class ParticleManagerTester : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (ParticleManager.Instance != null)
            {
                Debug.Log("F1: Testing reload with 1000 particles");
                ParticleManager.Instance.ReloadParticles(1000);
            }
            else
            {
                Debug.LogError("ParticleManager.Instance is NULL!");
            }
        }
    }
}
```

Add this to any GameObject, run the game, and press F1. Check console for results.
