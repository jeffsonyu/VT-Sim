using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using RFUniverse.Manager;
using Robotflow.RFUniverse.SideChannels;

public class GetSkinnedMeshRenderer_obj : MonoBehaviour
{
    public SkinnedMeshRenderer Obj;

    // Start is called before the first frame update
    void Start()
    {
        AssetManager.Instance.AddListener("SaveObjMesh", SaveMesh);
    }

    // Update is called once per frame
    void SaveMesh(IncomingMessage msg)
    {
        Debug.Log("!!!!!!!!!!!!");
        Mesh mesh = new Mesh();
        List<Vector3> objVertices = new List<Vector3>();
        List<int> objTriangles = new List<int>();

        Obj.BakeMesh(mesh, true);
        Debug.Log("!!!!!!!!!!!!");

        foreach (var item in mesh.vertices)
        {
            objVertices.Add(Obj.transform.TransformPoint(item));
        }
        foreach (var item in mesh.triangles)
        {
            // var item_i = new Vector3(item[0], item[1], item[2]);
            objTriangles.Add(item);

        }
        Debug.Log("!!!!!!!!!!!!");

        string path_pc = $"E:/dataset/ycb_touch/pc_obj_{System.DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";
        using (StreamWriter sw = File.CreateText(path_pc))
        {
            foreach (Vector3 point in objVertices)
            {
                sw.WriteLine($"{point.z} {-point.x} {point.y}");
            }
        }
        Debug.Log("!!!!!!!!!!!!");

        string path_t = $"E:/dataset/ycb_touch/pc_obj_t_{System.DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";
        using (StreamWriter sw = File.CreateText(path_t))
        {
            foreach (int point in objTriangles)
            {
                sw.WriteLine($"{point}");
            }
        }
        Debug.Log("!!!!!!!!!!!!");
    }
}
