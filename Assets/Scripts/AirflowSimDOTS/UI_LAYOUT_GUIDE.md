# UI Layout Guide for Particle Reload System

## Main Menu UI Hierarchy

```
Canvas
??? MainMenuPanel
    ??? PatternDropdown (existing)
    ??? StartButton (existing)
    ??? FreeModeButton (existing)
    ??? QuitButton (existing)
    ?
    ??? ParticleSettingsPanel (NEW - recommended)
    ?   ??? ParticleCountSlider (NEW)
    ?   ?   ??? Background
    ?   ?   ??? Fill Area
    ?   ?   ?   ??? Fill
    ?   ?   ??? Handle Slide Area
    ?   ?       ??? Handle
    ?   ?
    ?   ??? ParticleCountText (NEW - TextMeshPro)
    ?   ?
    ?   ??? ReloadOnPlayToggle (NEW)
    ?       ??? Background
    ?       ??? Checkmark
    ?       ??? Label (TextMeshPro) - "Reload on Play"
    ?
    ??? (other existing UI elements)
```

## In-Game Menu UI Hierarchy

```
Canvas
??? InGameMenuPanel (or ButtonPanel)
    ??? MenuButton (existing)
    ??? ReloadButton (NEW)
        ??? Text (TextMeshPro) - "Reload Particles (R)"
```

## Recommended Layout Positions

### Main Menu - Particle Settings Panel
```
Position: Below pattern dropdown or above buttons
Recommended RectTransform:
  - Anchors: Middle-Center
  - Pos X: 0
  - Pos Y: -50 (adjust based on your layout)
  - Width: 400
  - Height: 150
```

### Particle Count Slider
```
Recommended RectTransform:
  - Anchors: Stretch-Top
  - Pos Y: -30
  - Height: 30
  - Left: 10
  - Right: -10
```

### Particle Count Text
```
Recommended RectTransform:
  - Anchors: Top-Center
  - Pos Y: -10
  - Width: 300
  - Height: 30
  
Font Size: 16-18
Alignment: Center
```

### Reload On Play Toggle
```
Recommended RectTransform:
  - Anchors: Bottom-Center
  - Pos Y: 10
  - Width: 200
  - Height: 30
```

### In-Game Reload Button
```
Recommended RectTransform:
  - Position: Next to Menu button
  - Anchors: Bottom-Right (or match Menu button)
  - Pos X: Menu button X - 120 (or other spacing)
  - Pos Y: Match Menu button
  - Width: 180
  - Height: 40

Button Colors (recommended):
  - Normal: Light Blue (matches action button)
  - Highlighted: Lighter Blue
  - Pressed: Darker Blue
  - Disabled: Gray
```

## Step-by-Step UI Creation

### Creating Particle Settings Panel (Optional but Recommended)

1. **Create Container Panel**:
   ```
   Right-click MainMenuPanel ? UI ? Panel
   Name: "ParticleSettingsPanel"
   Remove Image component (or set to transparent) if you want no background
   ```

2. **Add Slider**:
   ```
   Right-click ParticleSettingsPanel ? UI ? Slider
   Name: "ParticleCountSlider"
   
   In Slider Component:
     - Min Value: 100 (will be overridden by ParticleManager)
     - Max Value: 2000 (will be overridden by ParticleManager)
     - Whole Numbers: ? (checked)
     - Value: 400
   ```

3. **Add Text Label**:
   ```
   Right-click ParticleSettingsPanel ? UI ? Text - TextMeshPro
   Name: "ParticleCountText"
   Text: "Particle Count: 400"
   Font Size: 18
   Alignment: Center
   Color: White or match your theme
   ```

4. **Add Toggle**:
   ```
   Right-click ParticleSettingsPanel ? UI ? Toggle
   Name: "ReloadOnPlayToggle"
   
   Edit the Label child:
     - Change to TextMeshPro if needed
     - Text: "Reload on Play"
     - Font Size: 16
     - Alignment: Left
   ```

### Creating Reload Button

1. **Add Button**:
   ```
   Right-click InGameMenuPanel (or ButtonPanel) ? UI ? Button - TextMeshPro
   Name: "ReloadButton"
   
   Edit the Text child:
     - Text: "Reload Particles (R)"
     - Font Size: 14-16
     - Alignment: Center
   ```

2. **Position It**:
   ```
   Place it next to or below the Menu button
   Match the size and style of the Menu button for consistency
   ```

## Styling Tips

### Consistent Theme
- Use the same color scheme as your existing buttons
- Match font styles and sizes with other UI elements
- Keep spacing consistent with your current layout

### Color Suggestions
```csharp
// Particle Count Text
Normal: White or light gray
Highlight when changed: Yellow or cyan

// Reload Button
Normal: rgba(100, 150, 255, 255) // Light blue
Highlighted: rgba(120, 170, 255, 255)
Pressed: rgba(80, 130, 235, 255)
Disabled: rgba(100, 100, 100, 128)

// Toggle Checkmark
Active: Green (0, 255, 0)
Inactive: Gray (128, 128, 128)
```

### Accessibility
- Ensure text is large enough to read (minimum 14pt)
- Use high contrast colors
- Add tooltips if desired (using Unity's tooltip system)
- Consider adding icons to buttons for visual clarity

## Alternative Layouts

### Compact Layout (Less Space)
```
Place all controls in a vertical list:
??????????????????????
? Particle Count: 400?
? [====|==========] ?
? [?] Reload on Play ?
??????????????????????
```

### Expanded Layout (More Information)
```
???????????????????????????????
? Particle Count              ?
? [====|==========]           ?
? 400 particles (Min: 100, Max: 2000)?
?                             ?
? [?] Reload on Play          ?
?     Automatically reload    ?
?     particles when starting ?
???????????????????????????????
```

### Preset Buttons Layout
```
??????????????????????
? [Low] [Med] [High] ?
? Custom: [====|====]?
? [?] Reload on Play ?
??????????????????????
```

## Testing Checklist

- [ ] Slider moves smoothly
- [ ] Text updates when slider moves
- [ ] Values snap to step increments
- [ ] Toggle can be clicked
- [ ] Reload button is visible in-game
- [ ] Reload button text is readable
- [ ] All references are assigned in Inspector
- [ ] No console errors when clicking buttons
- [ ] Particle count changes take effect
- [ ] Reload actually respawns particles
- [ ] Keyboard shortcut (R) works

## Common Mistakes to Avoid

1. **Forgetting to assign references**: Always double-check Inspector
2. **Wrong text component type**: Use TextMeshPro, not legacy Text
3. **Slider not set to Whole Numbers**: Enable this in Slider component
4. **Missing ParticleManager in scene**: Must be on a GameObject
5. **Canvas Scaler issues**: Ensure UI scales properly on different resolutions
