# FPS Log and Menu Event Tracking - iOS Guide

## Overview
The `FPSLog` system has been enhanced to track all menu events and store logs in a location accessible on your iPhone. You can view and share these logs directly from your device.

## What's New

### 1. **Menu Event Tracking**
The FPSLog now automatically tracks the following menu events:
- Start button clicks
- Free Mode button clicks
- Quit button clicks
- Pattern selection changes
- Return to menu button clicks

All events are logged with timestamps and descriptions.

### 2. **iOS-Accessible Storage**
Logs are now stored in `Application.persistentDataPath`, which is accessible on iOS devices:
- **Path**: `/var/mobile/Containers/Data/Application/[APP_ID]/Documents/TestResults/`
- This location is backed up by iTunes/Finder and accessible through file sharing

### 3. **In-App Log Viewer**
You can view logs directly within the app without needing a computer.

### 4. **Native iOS Sharing**
Share log files via:
- AirDrop
- Email
- Messages
- Files app
- Any other iOS share extension

---

## Setup Instructions

### Step 1: Add UI Components
You need to add the following UI components to your scene and assign them in the FPSLog inspector:

1. **View Log Button** - Button to open the log viewer
2. **Log View Panel** - Panel containing the log content display
3. **Log Content Text** - TextMeshProUGUI to display log content
4. **Close Log Button** - Button to close the log viewer
5. **Share Log Button** - Button to share the log file via iOS native share

### Step 2: Configure the FPSLog Component
In the Unity Inspector for the `FPSLog` component:
1. Assign the `MainMenuController` reference
2. Assign the `InGameMenuController` reference
3. Assign all the log viewing UI components listed above

### Step 3: Build Settings (Xcode)
When building for iOS, ensure the following:

1. **File Sharing Enabled** (Optional - for iTunes/Finder access):
   - In Xcode, select your project
   - Go to the "Info" tab
   - Add the following keys:
     - `UIFileSharingEnabled` = `YES`
     - `LSSupportsOpeningDocumentsInPlace` = `YES`

2. **Build and Run** on your iPhone

---

## How to Use on iPhone

### Method 1: In-App Viewing (Recommended)
1. Launch the app on your iPhone
2. Tap the **"View Log"** button (you'll need to add this to your UI)
3. The log content will be displayed in a scrollable text view
4. Tap **"Share"** to open the iOS share sheet
5. Choose where to send the log:
   - AirDrop to your Mac
   - Email to yourself
   - Save to Files app
   - Send via Messages

### Method 2: Keyboard Shortcut (Testing)
- Press **Shift + L** to toggle the log viewer (works with external keyboard or in Editor)

### Method 3: iTunes/Finder File Sharing (if enabled)
1. Connect your iPhone to your Mac via cable
2. Open **Finder** (macOS Catalina+) or **iTunes** (older macOS/Windows)
3. Select your iPhone
4. Go to the **Files** section
5. Find your app in the list
6. Navigate to **Documents/TestResults/**
7. Drag and drop the log files to your computer

### Method 4: Files App (if LSSupportsOpeningDocumentsInPlace is enabled)
1. Open the **Files** app on your iPhone
2. Go to **On My iPhone**
3. Find your app name
4. Navigate to **Documents/TestResults/**
5. Tap and hold on a log file
6. Choose **Share** to send it anywhere

---

## Log File Format

The log file contains:
```
Timestamp;Event Type;Description
2024-01-15 14:23:45.123;Event;Application Started
2024-01-15 14:23:50.456;Event;Menu: Start Button Clicked
2024-01-15 14:23:50.789;Event;Menu: Pattern Changed to 'Circle Pattern' (index: 0)
2024-01-15 14:24:00.123;FPS;30.5;1000
```

---

## Programmatic Access

### Get Log File Path
```csharp
FPSLog fpsLog = FindObjectOfType<FPSLog>();
string logPath = fpsLog.GetLogFilePath();
Debug.Log($"Log file is at: {logPath}");
```

### Log Custom Events
```csharp
FPSLog fpsLog = FindObjectOfType<FPSLog>();
fpsLog.LogCustomEvent("Custom Event: User performed special action");
```

---

## Troubleshooting

### "No log file found" message
- Make sure the app has been running for at least a few seconds
- Check that `doLog` is enabled in the FPSLog inspector
- The log file is created on first Start()

### Share button doesn't work
- Make sure you're running on an actual iOS device, not the simulator
- The native share functionality only works on physical devices

### Can't find logs in Files app
- You need to enable `LSSupportsOpeningDocumentsInPlace` in your Info.plist
- Add this in Xcode after building from Unity

### Logs not showing in iTunes/Finder
- You need to enable `UIFileSharingEnabled` in your Info.plist
- Add this in Xcode after building from Unity

---

## File Locations

### Development (Unity Editor)
- macOS: `/Users/[Username]/Library/Application Support/[CompanyName]/[ProductName]/TestResults/`
- Windows: `C:\Users\[Username]\AppData\LocalLow\[CompanyName]\[ProductName]\TestResults\`

### iOS Device
- `/var/mobile/Containers/Data/Application/[APP_ID]/Documents/TestResults/`

---

## Security Notes

- Log files contain gameplay data and timestamps
- They are stored locally on the device
- They are included in device backups
- When shared, the recipient will have access to all logged data
- Consider adding data privacy notices if collecting sensitive information

---

## Additional Tips

1. **AirDrop is the fastest method** - Share directly to your Mac for immediate analysis
2. **Email works everywhere** - Send logs to yourself for later review
3. **Files app integration** - Save to cloud storage like iCloud Drive
4. **Regular cleanup** - Old log files will accumulate, consider adding a cleanup function

---

## Next Steps

Consider adding:
- A log file browser to select from multiple sessions
- Automatic log cleanup for old files
- Log compression for large files
- CSV export functionality
- Graph visualization of FPS data
