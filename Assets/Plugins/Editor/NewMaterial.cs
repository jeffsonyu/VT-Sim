using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class NewMaterial : Editor
{
    [MenuItem("Tools/NewMaterial")]
    static void Go()
    {
        GameObject[] prefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
        foreach (var item in prefabs)
        {
            Material mat = new Material(item.GetComponentInChildren<Renderer>().sharedMaterials[0]);
            mat.shader = Shader.Find("StandardDoubleSide");
            AssetDatabase.CreateAsset(mat, $"{Path.GetDirectoryName(AssetDatabase.GetAssetPath(item))}/NewMat.mat");
            item.GetComponentInChildren<Renderer>().sharedMaterial = mat;
            PrefabUtility.SavePrefabAsset(item);
        }
    }

}
