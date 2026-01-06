# Quick Start: FPS Log with Menu Events on iOS

## What Was Changed

? **FPSLog now tracks all menu events automatically**
? **Logs are stored in iPhone-accessible location**
? **Added in-app log viewer with iOS native sharing**
? **Created native iOS plugin for sharing files**

---

## Quick Setup (3 Steps)

### Step 1: Create the UI (Automated)
1. In Unity, go to the menu: **Tools ? Setup FPSLog UI**
2. This will automatically create all necessary UI components

### Step 2: Verify References
1. Select the `FPSLog` GameObject in your scene
2. In the Inspector, make sure these are assigned:
   - Main Menu Controller
   - In Game Menu Controller
   - View Log Button
   - Log View Panel
   - Log Content Text
   - Close Log Button
   - Share Log Button

### Step 3: Build and Test
1. Build for iOS
2. Open in Xcode
3. Run on your iPhone

---

## How to Use on iPhone

### View Logs
1. Tap the **"View Log"** button (top-left corner)
2. See all FPS data and menu events with timestamps

### Share Logs
1. In the log viewer, tap **"Share"**
2. Choose how to share:
   - **AirDrop** to your Mac (fastest!)
   - **Email** to yourself
   - **Save to Files** app
   - **Messages**
   - Any other iOS share extension

---

## What Gets Logged

### Menu Events (Automatic)
- ? Start button clicks
- ? Free Mode button clicks  
- ? Return to Menu button clicks
- ? Pattern dropdown changes
- ? Quit button clicks

### Performance Data
- ? FPS measurements
- ? Timestamps for every event
- ? Number of particles (if configured)

---

## File Location on iPhone

```
/var/mobile/Containers/Data/Application/[YOUR_APP_ID]/Documents/TestResults/
```

Files are named: `[SceneName]_[DateTime].txt`

Example: `AirflowScene_2024-01-15_14-23-45.txt`

---

## Accessing Logs (Multiple Methods)

### Method 1: In-App Share (Easiest) ?
Tap "View Log" ? Tap "Share" ? Choose destination

### Method 2: Files App
1. Open Files app on iPhone
2. Browse ? On My iPhone ? [Your App Name]
3. Documents/TestResults/
4. Tap and hold file ? Share

### Method 3: Mac/PC via Cable
1. Connect iPhone to computer
2. Open Finder (Mac) or iTunes (Windows)
3. Select your iPhone
4. Go to Files section
5. Find your app ? Documents/TestResults/
6. Drag files to your computer

---

## Log File Format

```csv
Timestamp;Event Type;Description
2024-01-15 14:23:45.123;Event;Application Started
2024-01-15 14:23:50.456;Event;Menu: Start Button Clicked
2024-01-15 14:23:50.789;Event;Menu: Pattern Changed to 'Circle Pattern' (index: 0)
Time;Number of Particles;FPS
0.5;0;60
1.0;100;58
1.5;200;55
```

---

## Xcode Configuration (Optional)

To enable iTunes/Finder file sharing, add to Info.plist in Xcode:

```xml
<key>UIFileSharingEnabled</key>
<true/>
<key>LSSupportsOpeningDocumentsInPlace</key>
<true/>
```

This makes the Documents folder visible in Finder/iTunes.

---

## Troubleshooting

### "No log file found"
- Wait a few seconds after app launch
- Make sure `doLog` is enabled in Inspector

### Share button does nothing
- Must run on physical iPhone (not simulator)
- Check iOS version is 13.0+

### UI is missing
- Run **Tools ? Setup FPSLog UI** again
- Check Canvas exists in scene

---

## Code Examples

### Log Custom Events
```csharp
FPSLog fpsLog = FindObjectOfType<FPSLog>();
fpsLog.LogCustomEvent("User completed level 1");
```

### Get Log Path
```csharp
FPSLog fpsLog = FindObjectOfType<FPSLog>();
string path = fpsLog.GetLogFilePath();
Debug.Log($"Logs at: {path}");
```

---

## Tips

?? **AirDrop is fastest** - Share directly to your Mac in seconds  
?? **Email for later** - Send to yourself for analysis later  
?? **Regular cleanup** - Delete old log files to save space  
?? **CSV format** - Open in Excel or Google Sheets for analysis  

---

## Next Steps

- [ ] Run the UI setup tool
- [ ] Build to iPhone
- [ ] Test the log viewer
- [ ] Try sharing via AirDrop
- [ ] Analyze your performance data!

---

## Support

For issues or questions:
1. Check the full guide: `FPS_LOG_IOS_GUIDE.md`
2. Verify all Inspector references are assigned
3. Check Unity Console for error messages
4. Ensure iOS deployment target is 13.0+
