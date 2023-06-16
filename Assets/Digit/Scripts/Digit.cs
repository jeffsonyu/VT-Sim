using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using RFUniverse.Attributes;
using Robotflow.RFUniverse.SideChannels;
using UnityEngine.Experimental.Rendering;


//Config参数结构
public class DigitConfig
{
    public CameraConfig cameraConfig = new CameraConfig();
    public GelConfig gelConfig = new GelConfig();
    public LightConfig lightConfig = new LightConfig();
    public ForceConfig forceConfig = new ForceConfig();
}
//相机参数结构
public class CameraConfig
{
    public Vector3 position = new Vector3(0, 0, 0.015f);//位置
    public Vector3 rotation = new Vector3(-90, 0, 0);//旋转
    public float fov = 60;//FOV
    public float nearPlane = 0.01f;//近裁减平面

}
//Gel参数结构
public class GelConfig
{
    public Vector3 position = new Vector3(0, 0.022f, 0.015f);//位置
    public float width = 0.02f;//宽
    public float height = 0.03f;//高
    public bool curvature = true;//应用球面
    public float curvatureMax = 0.005f;//最大偏移
    public float r = 0.1f;//球面半径
    public int meshVerticesCountW = 100;//生成网格宽度上的顶点数
    public int meshVerticesCountH => (int)(meshVerticesCountW * height / width);
    public int resolutionW = 100;//输出图像宽度上的像素
    public int resolutionH => (int)(resolutionW * height / width);
}
//灯光参数结构
public class LightConfig
{
    public Vector3 centerPosition = new Vector3(0, 0.005f, 0.015f);//中心位置
    public bool spot = true;//方向光
    public float[] spotAngle = new float[3] { 60f, 60f, 60f };//spotAngle
    public bool polar = true;//应用极坐标
    public Vector3[] cartesianPosition = new Vector3[3] { new Vector3(-0.01732f, 0, -0.01f), new Vector3(0.01732f, 0, -0.01f), new Vector3(0, 0, 0.02f) };//三维坐标位置
    public Vector3[] cartesianRotaiton = new Vector3[3] { new Vector3(-0.01732f, 0, -0.01f), new Vector3(0.01732f, 0, -0.01f), new Vector3(0, 0, 0.02f) };//三维坐标旋转
    public float[] polarDistance = new float[3] { 0.02f, 0.02f, 0.02f };//极坐标距离
    public float[] polarAngle = new float[3] { 210, 330, 90 };//极坐标角度
    public Color32[] color = new Color32[3] { Color.red, Color.green, Color.blue };//灯光颜色
    public float[] intensity = new float[3] { 1, 1, 1 };//灯光强度
}
//力参数结构
public class ForceConfig
{
    public bool enable = true;//应用受力变形
    public float minForce = 0;//最小变形受力
    public float maxForce = 0.01f;//最大变形受力
    public float maxDeformation = 0.005f;//最大变形

}
//Digit脚本
public class Digit : MonoBehaviour
{
    public Camera cameraLight;//光照相机
    public Camera cameraDepth;//深度相机
    public Shader depthShader;//深度Shdaer
    public MeshRenderer gel;//Gel
    public Transform lightCenter;//灯光中心
    public Light light0;//灯光1
    public Light light1;//灯光2
    public Light light2;//灯光3

    public RawImage lightImage;//灯光UI
    public RawImage depthImage;//深度UI

    public Transform proxy;//光照相机
    public Camera cameraLightProxy;//光照相机
    public MeshRenderer gelProxy;//Gel
    public Transform lightCenterProxy;//灯光中心
    public Light light0Proxy;//灯光1
    public Light light1Proxy;//灯光2
    public Light light2Proxy;//灯光3 
    public static List<Color> defultDepth = null;

    Texture2D tex;

    private static List<Digit> Digits = new List<Digit>();//所有Digit
    public int index;//digit编号
    private void Awake()
    {
        //读取config
        ReadConfig();
        //设置深度相机材质
        cameraDepth.SetReplacementShader(depthShader, "");
        //将RT赋予相应UI
        lightImage.texture = cameraLight.targetTexture;
        depthImage.texture = cameraDepth.targetTexture;
        //根据config宽高改变UI大小位置
        lightImage.rectTransform.sizeDelta = new Vector2(cameraLight.targetTexture.width, cameraLight.targetTexture.height);
        depthImage.rectTransform.sizeDelta = new Vector2(cameraDepth.targetTexture.width, cameraDepth.targetTexture.height);
        lightImage.rectTransform.anchoredPosition = new Vector2(cameraLight.targetTexture.width * index, cameraLight.targetTexture.height);
        depthImage.rectTransform.anchoredPosition = new Vector2(cameraDepth.targetTexture.width * index, cameraDepth.targetTexture.height);
        //添加自身
        Digits.Add(this);

        //根据index设置gel的层，灯光照射等层，相机渲染的层
        gel.gameObject.layer = index + 22;
        light0.cullingMask = 1 << (index + 22);
        light1.cullingMask = 1 << (index + 22);
        light2.cullingMask = 1 << (index + 22);
        cameraLight.cullingMask = 1 << (index + 22);
        cameraDepth.cullingMask = 1 << (index + 22);

        SetProxy();

        if (defultDepth == null)
        {
            defultDepth = new List<Color>();
            cameraDepth.Render();
            RenderTexture.active = cameraDepth.targetTexture;
            tex = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.R8, false);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            foreach (var item in tex.GetPixels())
            {
                defultDepth.Add(item);
            }
        }
    }
    
    static DigitConfig config = null;//全局config实例
    //读取Config
    void ReadConfig()
    {
        //已读取后不再读取
        if (config == null)
        {
            string path = $"{Application.streamingAssetsPath}/DigitConfig.xml";
            string xml = File.ReadAllText(path);
            config = XMLHelper.XMLToObject<DigitConfig>(xml);
            //File.WriteAllText(path, XMLHelper.ObjectToXML(config));
        }
        //应用相应参数
        cameraLight.transform.localPosition = config.cameraConfig.position;
        cameraLight.transform.localEulerAngles = config.cameraConfig.rotation;
        cameraLight.fieldOfView = config.cameraConfig.fov;
        cameraLight.nearClipPlane = config.cameraConfig.nearPlane;

        cameraDepth.transform.localPosition = config.cameraConfig.position;
        cameraDepth.transform.localEulerAngles = config.cameraConfig.rotation;
        cameraDepth.fieldOfView = config.cameraConfig.fov;
        cameraDepth.nearClipPlane = config.cameraConfig.nearPlane;

        //将RT赋予相应相机
        cameraLight.targetTexture = RenderTexture.GetTemporary(config.gelConfig.resolutionW, config.gelConfig.resolutionH, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, QualitySettings.antiAliasing);
        cameraDepth.targetTexture = RenderTexture.GetTemporary(config.gelConfig.resolutionW, config.gelConfig.resolutionH, 24, RenderTextureFormat.R8, RenderTextureReadWrite.Linear, 1);

        gel.transform.localPosition = config.gelConfig.position;

        Shader.SetGlobalFloat("_ZeroDis", gel.transform.localPosition.y);
        Shader.SetGlobalFloat("_OneDis", gel.transform.localPosition.y - 0.005f);

        gel.GetComponent<MeshFilter>().mesh = GenerateGelMesh(config.gelConfig.width, config.gelConfig.height, config.gelConfig.meshVerticesCountW, config.gelConfig.meshVerticesCountH, config.gelConfig.r, config.gelConfig.curvatureMax, config.gelConfig.curvature, gel.transform, cameraDepth);
        

        lightCenter.localPosition = config.lightConfig.centerPosition;
        if (config.lightConfig.spot)
        {
            light0.type = LightType.Spot;
            light1.type = LightType.Spot;
            light2.type = LightType.Spot;
            light0.spotAngle = config.lightConfig.spotAngle[0];
            light1.spotAngle = config.lightConfig.spotAngle[1];
            light2.spotAngle = config.lightConfig.spotAngle[2];
        }
        else
        {
            light0.type = LightType.Point;
            light1.type = LightType.Point;
            light2.type = LightType.Point;
            light0.cookie = null;
            light1.cookie = null;
            light2.cookie = null;
        }
        light0.color = config.lightConfig.color[0];
        light1.color = config.lightConfig.color[1];
        light2.color = config.lightConfig.color[2];
        light0.intensity = config.lightConfig.intensity[0];
        light1.intensity = config.lightConfig.intensity[1];
        light2.intensity = config.lightConfig.intensity[2];
        if (config.lightConfig.polar)
        {
            float angel;
            float ydx;
            angel = config.lightConfig.polarAngle[0];
            ydx = Mathf.Tan(Mathf.Deg2Rad * angel);
            light0.transform.localPosition = (new Vector3(Mathf.Cos(Mathf.Deg2Rad * angel), 0, Mathf.Sin(Mathf.Deg2Rad * angel))) * config.lightConfig.polarDistance[0];

            angel = config.lightConfig.polarAngle[1];
            ydx = Mathf.Tan(Mathf.Deg2Rad * angel);
            light1.transform.localPosition = (new Vector3(Mathf.Cos(Mathf.Deg2Rad * angel), 0, Mathf.Sin(Mathf.Deg2Rad * angel))) * config.lightConfig.polarDistance[1];

            angel = config.lightConfig.polarAngle[2];
            ydx = Mathf.Tan(Mathf.Deg2Rad * angel);
            light2.transform.localPosition = (new Vector3(Mathf.Cos(Mathf.Deg2Rad * angel), 0, Mathf.Sin(Mathf.Deg2Rad * angel))) * config.lightConfig.polarDistance[2];
        }
        else
        {
            light0.transform.localPosition = config.lightConfig.cartesianPosition[0];
            light1.transform.localPosition = config.lightConfig.cartesianPosition[1];
            light2.transform.localPosition = config.lightConfig.cartesianPosition[2];
            light0.transform.localEulerAngles = config.lightConfig.cartesianRotaiton[0];
            light1.transform.localEulerAngles = config.lightConfig.cartesianRotaiton[1];
            light2.transform.localEulerAngles = config.lightConfig.cartesianRotaiton[2];
        }

    }
    void SetProxy()
    {
        proxy.SetParent(null);
        proxy.position = Vector3.up * 100 + Vector3.right * index;
        proxy.rotation = Quaternion.identity;
        //应用相应参数
        cameraLightProxy.transform.localPosition = cameraLight.transform.localPosition;
        cameraLightProxy.transform.localEulerAngles = cameraLight.transform.localEulerAngles;
        cameraLightProxy.fieldOfView = cameraLight.fieldOfView;
        cameraLightProxy.nearClipPlane = cameraLight.nearClipPlane;

        //将RT赋予相应相机
        cameraLightProxy.targetTexture = RenderTexture.GetTemporary(cameraLight.targetTexture.width, cameraLight.targetTexture.height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, QualitySettings.antiAliasing);

        gelProxy.transform.localPosition = gel.transform.localPosition;
        gelProxy.GetComponent<MeshFilter>().mesh = gel.GetComponent<MeshFilter>().mesh;

        lightCenterProxy.localPosition = lightCenter.localPosition;
        light0Proxy.type = light0.type;
        light1Proxy.type = light1.type;
        light2Proxy.type = light2.type;
        light0Proxy.spotAngle = light0.spotAngle;
        light1Proxy.spotAngle = light1.spotAngle;
        light2Proxy.spotAngle = light2.spotAngle;
        light0Proxy.cookie = light0.cookie;
        light1Proxy.cookie = light1.cookie;
        light2Proxy.cookie = light2.cookie;
        light0Proxy.color = light0.color;
        light1Proxy.color = light1.color;
        light2Proxy.color = light2.color;
        light0Proxy.intensity = light0.intensity;
        light1Proxy.intensity = light1.intensity;
        light2Proxy.intensity = light2.intensity;
        light0Proxy.transform.localPosition = light0.transform.localPosition;
        light1Proxy.transform.localPosition = light1.transform.localPosition;
        light2Proxy.transform.localPosition = light2.transform.localPosition;
        light0Proxy.transform.localEulerAngles = light0.transform.localEulerAngles;
        light1Proxy.transform.localEulerAngles = light1.transform.localEulerAngles;
        light2Proxy.transform.localEulerAngles = light2.transform.localEulerAngles;

        gelProxy.gameObject.layer = gel.gameObject.layer;
        light0Proxy.cullingMask = light0.cullingMask;
        light1Proxy.cullingMask = light1.cullingMask;
        light2Proxy.cullingMask = light2.cullingMask;
        cameraLightProxy.cullingMask = cameraLight.cullingMask;
    }

    static Mesh m = null;
    //生成Gel网格
    public Mesh GenerateGelMesh(float width, float height, int countW, int countH, float r, float max, bool curvature, Transform gel, Camera camera)
    {
        if (Digit.m != null) return Digit.m;
        if (!curvature)
        {
            countW = 2;
            countH = 2;
            max = 0;
        }
        Mesh m = new Mesh();
        List<Vector3> v = new List<Vector3>();
        List<Vector2> u = new List<Vector2>();
        float w = -width / 2;
        float dMax = r - Mathf.Sqrt(r * r - width * width / 4 - height * height / 4);
        for (int i = 0; i < countW; i++)
        {
            float h = -height / 2;
            for (int j = 0; j < countH; j++)
            {
                float d = r - Mathf.Sqrt(r * r - w * w - h * h);
                d = d / dMax * max;
                Vector3 point = new Vector3(w, h, d);
                v.Add(point);
                point = camera.WorldToViewportPoint(gel.TransformPoint(point));
                u.Add(point);
                h += height / (countH - 1);
            }
            w += width / (countW - 1);
        }
        m.vertices = v.ToArray();
        m.uv = u.ToArray();
        List<int> t = new List<int>();
        for (int i = 0; i < m.vertices.Length; i++)
        {
            if (i % countH < countH - 1 && i + countH < m.vertices.Length)
            {
                t.Add(i);
                t.Add(i + countH);
                t.Add(i + countH + 1);
                t.Add(i);
                t.Add(i + countH + 1);
                t.Add(i + 1);
            }
        }
        m.triangles = t.ToArray();
        return m;
    }
    //保存图像
    public static int saveIndex = 1;
    public static Vector2Int SetCameraIntrinsicMatrix(Camera set_camera, List<float> intrinsicMatrix)
    {
        set_camera.usePhysicalProperties = true;
        float focal = 35;
        float ax, ay, sizeX, sizeY;
        float x0, y0, shiftX, shiftY;
        ax = intrinsicMatrix[0];
        ay = intrinsicMatrix[4];
        x0 = intrinsicMatrix[6];
        y0 = intrinsicMatrix[7];
        int width = (int)x0 * 2;
        int height = (int)y0 * 2;
        sizeX = focal * width / ax;
        sizeY = focal * height / ay;
        shiftX = -(x0 - width / 2.0f) / width;
        shiftY = (y0 - height / 2.0f) / height;
        set_camera.sensorSize = new Vector2(sizeX, sizeY);
        set_camera.focalLength = focal;
        set_camera.lensShift = new Vector2(shiftX, shiftY);
        return new Vector2Int(width, height);
    }
    public static void SaveData(string path)
    {
        saveIndex++;
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        
        using (StreamWriter sw = File.CreateText($"{path}/Cam_rot.txt"))
        {
            foreach (var digit in Digits)
            {
                // Vector3 cam_rot = digit.gel.transform.eulerAngles;
                Vector3 cam_rot = digit.cameraDepth.transform.eulerAngles;
                // Vector3 cam_rot = digit.transform.eulerAngles;
                sw.WriteLine($"{digit.index}");
                sw.WriteLine($"{cam_rot.x},{cam_rot.y},{cam_rot.z}");
            }
        }

        using (StreamWriter sw = File.CreateText($"{path}/Cam_pos.txt"))
        {
            foreach (var digit in Digits)
            {
                // Vector3 cam_pos = digit.gel.transform.position;
                Vector3 cam_pos = digit.cameraDepth.transform.position;
                // Vector3 cam_pos = digit.transform.position;
                sw.WriteLine($"{digit.index}");
                sw.WriteLine($"{cam_pos.x},{cam_pos.y},{cam_pos.z}");
            }
        }

        using (StreamWriter sw = File.CreateText($"{path}/Cam_quat.txt"))
        {
            foreach (var digit in Digits)
            {
                // Vector3 cam_pos = digit.gel.transform.position;
                Quaternion cam_quat = digit.cameraDepth.transform.rotation;
                // Vector3 cam_pos = digit.transform.position;
                sw.WriteLine($"{digit.index}");
                sw.WriteLine($"{cam_quat.x},{cam_quat.y},{cam_quat.z},{cam_quat.w}");
            }
        }


        foreach (var digit in Digits)
        {


            RenderTexture.active = digit.cameraLight.targetTexture;
            digit.tex = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, false);
            digit.tex.ReadPixels(new Rect(0, 0, digit.tex.width, digit.tex.height), 0, 0);
            digit.tex.Apply();
            File.WriteAllBytes($"{path}/Light{digit.index}.png", digit.tex.EncodeToPNG());

            //int tempMask = digit.cameraLight.cullingMask;
            //digit.cameraLight.cullingMask = 1 << 20;
            //digit.cameraLight.Render();
            //RenderTexture.active = digit.cameraLight.targetTexture;
            //digit.tex = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, false);
            //digit.tex.ReadPixels(new Rect(0, 0, digit.tex.width, digit.tex.height), 0, 0);
            //digit.tex.Apply();
            //File.WriteAllBytes($"{path}/RGB{digit.index}.png", digit.tex.EncodeToPNG());
            //digit.cameraLight.cullingMask = tempMask;


            //////
            ///


            //float[] intrinsicMatrix = new float[] { 240, 0, 0, 0, 240, 0, 240, 320, 1 };
            float[] intrinsicMatrix = new float[] { 500, 0, 0, 0, 500, 0, 512, 512, 1 };
            int width = (int)intrinsicMatrix[6] * 2;
            int height = (int)intrinsicMatrix[7] * 2;

            Texture2D intrinsicMatrixTex = new Texture2D(width, height, TextureFormat.RGB24, false);

            float tempFov = digit.cameraLight.fieldOfView;
            digit.cameraLight.usePhysicalProperties = true;
            SetCameraIntrinsicMatrix(digit.cameraLight, intrinsicMatrix.ToList());

            int tempMask = digit.cameraLight.cullingMask;
            digit.cameraLight.cullingMask = 1 << 20;
            digit.cameraLight.Render();
            RenderTexture tempRenderTexture = digit.cameraLight.targetTexture;
            digit.cameraLight.targetTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, QualitySettings.antiAliasing);
            digit.cameraLight.Render();
            RenderTexture.active = digit.cameraLight.targetTexture;
            intrinsicMatrixTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            intrinsicMatrixTex.Apply();
            File.WriteAllBytes($"{path}/RGB{digit.index}.png", intrinsicMatrixTex.EncodeToPNG());
            digit.cameraLight.cullingMask = tempMask;
            digit.cameraLight.targetTexture.Release();
            digit.cameraLight.targetTexture = tempRenderTexture;

            digit.cameraLight.usePhysicalProperties = false;
            digit.cameraLight.fieldOfView = tempFov;

            ////////
            ///
            RenderTexture.active = digit.cameraDepth.targetTexture;
            digit.tex = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.R8, false);
            digit.tex.ReadPixels(new Rect(0, 0, digit.tex.width, digit.tex.height), 0, 0);
            digit.tex.Apply();
            File.WriteAllBytes($"{path}/Depth{digit.index}.png", digit.tex.EncodeToPNG());

            // using (StreamWriter sw = File.CreateText($"{path}/Point{digit.index}.txt"))
            // {
            //     // sw.WriteLine($"row:{config.gelConfig.meshVerticesCountW}，column:{config.gelConfig.meshVerticesCountH}");
            //     for (int i = 0; i < m.vertices.Length; i++)
            //     {
            //         // if (i % config.gelConfig.meshVerticesCountH == 0)
            //             // sw.WriteLine($"---row:{i / config.gelConfig.meshVerticesCountH}---");
            //         Vector3 pos = digit.gel.transform.TransformPoint(m.vertices[i]);
            //         // sw.WriteLine($"column{i % config.gelConfig.meshVerticesCountH}:{pos.x},{pos.y},{pos.z}");
            //         sw.WriteLine($"{pos.x},{pos.y},{pos.z}");
            //     }
            // }

            //RenderTexture.active = digit.cameraDepth.targetTexture;
            //digit.tex = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.R8, false);
            //digit.tex.ReadPixels(new Rect(0, 0, digit.tex.width, digit.tex.height), 0, 0);
            //digit.tex.Apply();
            List<Color> depth = digit.tex.GetPixels().ToList();

            for (int i = 0; i < depth.Count; i++)
            {
                depth[i] -= Digit.defultDepth[i];
            }
            float sourceMax = depth.Max(s => s.grayscale);
            GaussianBlur gauss = new GaussianBlur(3);
            gauss.SetSourceImage(depth, digit.tex.width, digit.tex.height);
            Color[] blurDepth = gauss.GetBlurImage();
            float blurMax = blurDepth.Max(s => s.grayscale);
            float scale = sourceMax / blurMax;

            float zero = Shader.GetGlobalFloat("_ZeroDis");
            float one = Shader.GetGlobalFloat("_OneDis");

            for (int i = 0; i < depth.Count; i++)
            {
                blurDepth[i] *= scale;
            }
            digit.tex.SetPixels(blurDepth.ToArray());
            //tex.filterMode = FilterMode.Bilinear;
            digit.tex.Apply();
            digit.gelProxy.material.SetTexture("_Offset", digit.tex);
            digit.gelProxy.material.SetFloat("_Scale", Mathf.Abs(one - zero));

            digit.cameraLightProxy.Render();
            RenderTexture.active = digit.cameraLightProxy.targetTexture;
            Texture2D tex_new = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, false);
            tex_new.ReadPixels(new Rect(0, 0, tex_new.width, tex_new.height), 0, 0);
            tex_new.Apply();
            File.WriteAllBytes($"{path}/Light{digit.index}_g.png", tex_new.EncodeToPNG());

            for (int i = 0; i < depth.Count; i++)
            {
                blurDepth[i] += defultDepth[i];
            }
            tex_new = new Texture2D(digit.cameraDepth.targetTexture.width, digit.cameraDepth.targetTexture.height, TextureFormat.R8, false);
            tex_new.SetPixels(blurDepth.ToArray());
            tex_new.Apply();
            File.WriteAllBytes($"{path}/Depth{digit.index}_g.png", tex_new.EncodeToPNG());

            digit.gel.gameObject.SetActive(false);
            digit.lightCenter.gameObject.SetActive(false);
            Material material = null;
            Color tempColor = Color.white;
            if (digit.currentRender != null)
            {
                material = digit.currentRender.GetComponent<Renderer>().material;
                tempColor = material.GetColor("_Color");
                material.SetColor("_Color", Color.white);
            }
            digit.cameraLight.Render();
            RenderTexture.active = digit.cameraLight.targetTexture;
            digit.tex = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, false);
            digit.tex.ReadPixels(new Rect(0, 0, digit.tex.width, digit.tex.height), 0, 0);
            digit.tex.Apply();
            File.WriteAllBytes($"{path}/Light{digit.index}_rgb.png", digit.tex.EncodeToPNG());
            if (material != null)
            {
                material.SetColor("_Color", tempColor);
            }
            digit.gel.gameObject.SetActive(true);
            digit.lightCenter.gameObject.SetActive(true);
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            SaveData("D:/dataset/ycb_touch");
    }

    //碰撞时接收到CollisionEvent发送来的消息
    GameObject currentRender;
    public void CollisionStay(Transform target, GameObject render, float force)
    {
        currentRender = render;
        //变形
        float deformation = 0;
        //如果启用变形，则根据压力改变Render位置
        if (config.forceConfig.enable)
        {
            force = force < config.forceConfig.minForce ? 0 : force;
            deformation = Mathf.Min(force / config.forceConfig.maxForce, 1) * config.forceConfig.maxDeformation;
        }
        //应用Render位置和旋转
        // render.transform.position = target.transform.position - transform.up * deformation;
        render.transform.position = target.transform.position;
        render.transform.rotation = target.transform.rotation;
    }

}
