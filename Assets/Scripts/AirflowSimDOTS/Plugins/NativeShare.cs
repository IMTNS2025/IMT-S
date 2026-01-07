using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cross-platform native sharing plugin for iOS and Android
/// </summary>
public class NativeShare
{
    private string subject = "";
    private string text = "";
    private readonly List<string> files = new ();

    public NativeShare SetSubject(string subject)
    {
        this.subject = subject;
        return this;
    }

    public NativeShare SetText(string text)
    {
        this.text = text;
        return this;
    }

    public NativeShare AddFile(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
        {
            files.Add(filePath);
        }
        else
        {
            Debug.LogWarning($"[NativeShare] File not found: {filePath}");
        }
        return this;
    }

    public IEnumerator Share()
    {
#if UNITY_IOS && !UNITY_EDITOR
        ShareIOS();
#elif UNITY_ANDROID && !UNITY_EDITOR
        ShareAndroid();
#else
        Debug.Log("[NativeShare] Sharing not available in Editor or unsupported platform.");
        Debug.Log($"Subject: {subject}");
        Debug.Log($"Text: {text}");
        Debug.Log($"Files: {string.Join(", ", files)}");
#endif
        yield return null;
    }

#if UNITY_IOS && !UNITY_EDITOR
    private void ShareIOS()
    {
        if (files.Count > 0)
        {
            // Share files using iOS native share sheet
            _NativeShare_ShareFiles(files.ToArray(), files.Count, subject, text);
        }
        else
        {
            // Share text only
            _NativeShare_ShareText(subject, text);
        }
    }

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _NativeShare_ShareFiles(string[] files, int filesCount, string subject, string text);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _NativeShare_ShareText(string subject, string text);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    private void ShareAndroid()
    {
        using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
        using (AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent"))
        {
            intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            
            if (files.Count > 0)
            {
                using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
                using (AndroidJavaObject unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject fileProviderClass = new AndroidJavaClass("androidx.core.content.FileProvider"))
                {
                    string authority = Application.identifier + ".fileprovider";
                    using (AndroidJavaObject fileObject = new AndroidJavaObject("java.io.File", files[0]))
                    {
                        AndroidJavaObject uri = fileProviderClass.CallStatic<AndroidJavaObject>(
                            "getUriForFile",
                            currentActivity,
                            authority,
                            fileObject
                        );
                        
                        intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uri);
                        intentObject.Call<AndroidJavaObject>("setType", "text/plain");
                        intentObject.Call<AndroidJavaObject>("addFlags", 1); // FLAG_GRANT_READ_URI_PERMISSION
                    }
                }
            }
            
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
            
            using (AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Share via"))
            using (AndroidJavaObject unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                currentActivity.Call("startActivity", chooser);
            }
        }
    }
#endif
}
