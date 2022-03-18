using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Vuforia;
using UnityEngine.UI;
/*
 * 1) Call Init()
 * 1b) CameraStarted(true)
 * 2) TakePhoto
 */
public class TakePhotogrammetryPhoto : MonoBehaviour
{
    bool inSquid;
    static string basePath;
    public static string GenBasePath(string ledger)
    {
        return Path.Combine(Application.persistentDataPath, ledger);
    }

    private PixelFormat mPixelFormat = PixelFormat.UNKNOWN_FORMAT;

    public bool requireInSqiud;

    bool cameraStarted = false;

    public static string pf_sessions = "Sessions";
    public static string pf_sessionLedger;//= "SessionLedger";

    public UnityEngine.UI.Image imageGlowInSquid;

    public CamSquidCreator csc;

    private void Start()
    {
#if UNITY_EDITOR
        mPixelFormat = PixelFormat.GRAYSCALE; // Need Grayscale for Editor
#else
        mPixelFormat = PixelFormat.RGB888; // Use RGB888 for mobile
#endif

        Input.gyro.enabled = true;
        Init();

        VuforiaApplication.Instance.OnVuforiaStarted += RegisterFormat;
    }

    public void Init()
    {

        pf_sessionLedger = EpochStart().ToString(); 
        basePath = GenBasePath(pf_sessionLedger);
        Directory.CreateDirectory(basePath);
        print(basePath);
        WriteToPf(pf_sessions, basePath);

    }

    public static string[] Pf2String(string sessionledger)
    {
        return Parse.SSV(PlayerPrefs.GetString(sessionledger));
    }

    void WriteToPf(string pfname, string append)
    {
        string sess = PlayerPrefs.GetString(pfname);
        sess += append + ";";
        PlayerPrefs.SetString(pfname, sess);
    }

    public void CameraStarted(bool f)
    {
        cameraStarted = f;
    }
    GameObject goOther;
    private void OnTriggerEnter(Collider other)
    {
        if (!cameraStarted) return;

        inSquid = true;
        goOther = other.gameObject;

        imageGlowInSquid.enabled = true;
    }
    private void OnTriggerExit(Collider other)
    {
        inSquid = false;
        goOther = null;

        imageGlowInSquid.enabled = false;
    }

    public void DeactivateSquid(GameObject g)
    {
        // TODO visual effect

        g.SetActive(false);
    }

    void RegisterFormat()
    {
        // Vuforia has started, now register camera image format
        bool success = VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(mPixelFormat, true);
        if (success)

        {
            Debug.Log("Successfully registered pixel format " + mPixelFormat.ToString());
            // mFormatRegistered = true;
        }
        else
        {
            Debug.LogError(
                "Failed to register pixel format " + mPixelFormat.ToString() +
                "\n the format may be unsupported by your device;" +
                "\n consider using a different pixel format.");
            // mFormatRegistered = false;
        }
    }

    public void TakePhoto()
    {
        if ((inSquid && requireInSqiud) || !requireInSqiud)
        {
            // photo name
            string name = "PhCa_" + Time.time.ToString();
            if (goOther != null)
            {
                name += "_" + goOther.name;
            }

            // path
            string path = Path.Combine(basePath, name);


            // create image file
            Vuforia.Image image = CameraDevice.Instance.GetCameraImage(mPixelFormat);
            Texture2D texture = new Texture2D(1, 1);
            image.CopyBufferToTexture(texture);
            File.WriteAllBytes(GetImagePath(path), texture.EncodeToPNG());
            File.WriteAllBytes(GetThumbPath(path), ScaleImage.ScaleTexture(texture, 256).EncodeToPNG());
            //ScreenCapture.CaptureScreenshot(Path.Combine(path, ".png"));

            // create gravity file
            Vector3 g = Input.gyro.gravity;
            WriteString(GetGravityPath(path), g.ToString("F6"));

            // create attitude file
            WriteString(GetAttitudePath(path), Input.gyro.attitude.ToString("F6"));


            // record to session ledger
            WriteToPf(pf_sessionLedger, path);

            csc.CreateFloatingPhoto(texture, Camera.main.transform.position, Camera.main.transform.localRotation);

            // deactivate squid
            if (goOther != null)
                DeactivateSquid(goOther.gameObject);
        }
    }

    public static string[] GetAllFiles(string path)
    {
        string[] s = new string[5];

        s[0] = path;
        s[1]= GetImagePath(path);
        s[2] = GetThumbPath(path);
        s[3] = GetGravityPath(path);
        s[4] = GetAttitudePath(path);
        
        return s;
    }
    public static string RetrieveBaseFilepath(string pathmod)
    {
        pathmod = pathmod.Replace(".png", "");
        pathmod = pathmod.Replace(".txt", "");
        pathmod = pathmod.Replace("_thumb", "");
        pathmod = pathmod.Replace("_gravity", "");
        pathmod = pathmod.Replace("_attitude", "");
        return pathmod;
    }
  
    public static string GetImagePath(string path)
    {
        return path + ".png";
    }
    public static string GetThumbPath(string path)
    {
        return path + "_thumb.png";
    }
    public static string GetThumbPathNameOnly(string name)
    {
        return GetThumbPath(Path.Combine(basePath, name));
    }
    public static string GetGravityPath(string path)
    {
        return path + "_gravity.txt";
    }
    public static string GetAttitudePath(string path)
    {
        return path + "_attitude.txt";
    }

    static int EpochStart()
    {
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        return  (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
    }

    static void WriteString(string path, string writeme)
    {
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(writeme);
        writer.Close();
    }
}