using RFUniverse.Manager;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RFUniverse.Attributes;
using UnityEngine.Experimental.Rendering;
using System;
using System.Linq;
using Robotflow.RFUniverse.SideChannels;

public class DigitMain : MonoBehaviour
{
    public List<Camera> screenshotCamera;
    public Transform center1;
    public Transform center2;
    public Camera sceneCamera;

    public Shader idShader;
    public Shader depthShader;
    public float zeroDis;
    public float oneDis;
    public float zeroDisCam;
    public float oneDisCam;

    string processPath;
    RenderTexture screenshot;
    public static DigitMain Instance;
    Texture2D tex;

    private void Awake()
    {

        Instance = this;
        tex = new Texture2D(1024, 1024);

        processPath = $"/path/to/dataset/{System.DateTime.Now.ToString("yyyyMMddHHmmss")}";
        // processPath = $"{Application.streamingAssetsPath}/dataset/{System.DateTime.Now.ToString("yyyyMMddHHmmss")}";
        AssetManager.Instance.AddListener("SaveData", SaveData);
        screenshot = new RenderTexture(1024, 1024, 24, GraphicsFormat.R8G8B8A8_UNorm);
        foreach (var item in screenshotCamera)
        {
            item.targetTexture = screenshot;
        }
        Shader.SetGlobalFloat("_CameraZeroDis", zeroDisCam);
        Shader.SetGlobalFloat("_CameraOneDis", oneDisCam);
        AssetManager.Instance.AddListener("SaveScreenshot", SaveScreenshot);

        //for (int i = 1; i <= screenshotCamera.Count; i++)
        //{
        //    Camera item = screenshotCamera[i - 1];

        //    item.backgroundColor = 0.3f > 0.6f ? Color.black : Color.white;
        //    item.SetReplacementShader(depthShader, "");
        //    item.Render();
        //    RenderTexture.active = screenshot;
        //    //tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        //    //tex.Apply();
        //    //File.WriteAllBytes("C:/Users/87251/Desktop/cvpr2023/Fig5_VTSim/white_" + i + "_Depth.png", tex.EncodeToPNG());
        //}
    }

    private void GetObjImage(IncomingMessage msg)
    {

        float[] intrinsicMatrix = new float[] { 500, 0, 0, 0, 500, 0, 512, 512, 1 };
        int width = (int)intrinsicMatrix[6] * 2;
        int height = (int)intrinsicMatrix[7] * 2;
        Digit.SetCameraIntrinsicMatrix(sceneCamera, intrinsicMatrix.ToList());
        Texture2D objTex = new Texture2D(width, height);
        RenderTexture objRenderTexture = RenderTexture.GetTemporary(objTex.width, objTex.height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, QualitySettings.antiAliasing);
        sceneCamera.targetTexture = objRenderTexture;


        string objPath = Path.Combine(processPath, "ObjImage");
        if (!Directory.Exists(objPath))
            Directory.CreateDirectory(objPath);

        string posPath = Path.Combine(objPath, "pos_rot.txt");
        using (StreamWriter sw = File.CreateText(posPath))
        {
            int img_idx = 0;
            for (int i = 0; i < 360; i += 15)
            {
                center1.localEulerAngles = new Vector3(0, i, 0);
                for (int j = -45; j <= 45; j += 45)
                {
                    center2.localEulerAngles = new Vector3(j, 0, 0);
                    sceneCamera.backgroundColor = Color.white;
                    sceneCamera.ResetReplacementShader();
                    sceneCamera.Render();
                    RenderTexture.active = objRenderTexture;
                    objTex.ReadPixels(new Rect(0, 0, objTex.width, objTex.height), 0, 0);
                    objTex.Apply();
                    string img_idx_s = img_idx.ToString().PadLeft(3, '0');
                    File.WriteAllBytes($"{objPath}/{img_idx_s}.png", objTex.EncodeToPNG());

                    sceneCamera.backgroundColor = zeroDisCam > oneDisCam ? Color.black : Color.white;
                    sceneCamera.SetReplacementShader(depthShader, "");
                    sceneCamera.Render();
                    RenderTexture.active = objRenderTexture;
                    objTex.ReadPixels(new Rect(0, 0, objTex.width, objTex.height), 0, 0);
                    objTex.Apply();
                    File.WriteAllBytes($"{objPath}/{img_idx_s}_depth.png", objTex.EncodeToPNG());


                    img_idx++;

                    Vector3 position = sceneCamera.transform.position;
                    Quaternion quaternion = sceneCamera.transform.rotation;
                    Vector3 rpy = sceneCamera.transform.eulerAngles;

                    sw.WriteLine($"{position.x} {position.y} {position.z} {quaternion.x} {quaternion.y} {quaternion.z} {quaternion.w} {rpy.x} {rpy.y} {rpy.z}");
                }

            }
        }

    }

    public int saveIndex = 1;

    void SaveScreenshot(IncomingMessage msg)
    {
        string saveIndex_str = saveIndex.ToString().PadLeft(4, '0');
        Screenshot($"{processPath}/touch{saveIndex_str}/Unoccluded");
    }

    void SaveData(IncomingMessage msg)
    {
        string saveIndex_str = saveIndex.ToString().PadLeft(4, '0');
        Digit.SaveData($"{processPath}/touch{saveIndex_str}");
        //SaveParticleData($"{processPath}/touch{saveIndex_str}");
        Screenshot($"{processPath}/touch{saveIndex_str}/Occluded");
        saveIndex++;
    }


    public void SaveParticleData(string path)
    {
        if (DigitTargetObiSoftBody.Instance.softbody == null) return;
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        using (StreamWriter sw = File.CreateText($"{path}/Particles.txt"))
        {
            for (int i = 0; i < DigitTargetObiSoftBody.Instance.softbody.particleCount; i++)
            {
                Vector3 pos = DigitTargetObiSoftBody.Instance.softbody.GetParticlePosition(i);
                Vector3 rot = DigitTargetObiSoftBody.Instance.softbody.GetParticleOrientation(i).eulerAngles;
                sw.WriteLine($"{pos.x},{pos.y},{pos.z},{rot.x},{rot.y},{rot.z}");
            }
        }
    }
    public void Screenshot(string path)
    {
        Shader.SetGlobalFloat("_CameraZeroDis", zeroDis);
        Shader.SetGlobalFloat("_CameraOneDis", oneDis);
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        for (int i = 1; i <= screenshotCamera.Count; i++)
        {
            string png_index = i.ToString().PadLeft(3, '0');
            Camera item = screenshotCamera[i - 1];
            item.backgroundColor = Color.white;
            item.ResetReplacementShader();
            item.Render();
            RenderTexture.active = screenshot;
            //tex.(screenshot.width, screenshot.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path + png_index + ".png", tex.EncodeToPNG());

            item.backgroundColor = zeroDis > oneDis ? Color.black : Color.white;
            item.SetReplacementShader(depthShader, "");
            item.Render();
            RenderTexture.active = screenshot;
            //tex = new Texture2D(screenshot.width, screenshot.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path + png_index + "_Depth.png", tex.EncodeToPNG());

            item.RenderWithShader(idShader, "");
            RenderTexture.active = screenshot;
            //tex = new Texture2D(screenshot.width, screenshot.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path + png_index + "_ID.png", tex.EncodeToPNG());
        }
    }
}
