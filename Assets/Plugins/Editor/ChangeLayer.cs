using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class ChangeLayer : Editor
{
    [MenuItem("Tools/ChangeLayer")]
    static void Go()
    {
        GameObject[] prefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
        foreach (var item in prefabs)
        {
            Transform[] trans = item.GetComponentsInChildren<Transform>();
            foreach (var t in trans)
            {
                t.gameObject.layer = 20;
            }
            PrefabUtility.SavePrefabAsset(item);
        }
    }

}
