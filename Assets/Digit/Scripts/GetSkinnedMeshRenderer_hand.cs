using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using RFUniverse.Manager;
using Robotflow.RFUniverse.SideChannels;

public class GetSkinnedMeshRenderer_hand : MonoBehaviour
{
    public SkinnedMeshRenderer Obj;

    // Start is called before the first frame update
    void Start()
    {
        AssetManager.Instance.AddListener("SaveMesh", SaveMesh);
    }

    // Update is called once per frame
    void SaveMesh(IncomingMessage msg)
    {

        Mesh mesh = new Mesh();
        List<Vector3> objVertices = new List<Vector3>();

        Obj.BakeMesh(mesh, true);

        foreach (var item in mesh.vertices)
        {
            objVertices.Add(Obj.transform.TransformPoint(item));
        }

        string path = $"E:/dataset/ycb_touch/pc_hand_{System.DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";
        using (StreamWriter sw = File.CreateText(path))
        {
            foreach (Vector3 point in objVertices)
            {
                sw.WriteLine($"{point.z} {-point.x} {point.y}");
            }
        }
    }
}
