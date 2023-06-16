﻿using Robotflow.RFUniverse.SideChannels;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace RFUniverse.Attributes
{
    [Serializable]
    public class SceneData
    {
        public bool ground = false;
        public float[] cameraPosition = { 0, 1, -3 };
        public float[] cameraRotation = { 0, 0, 0 };
        public float[] groundPosition = { 0, 0, 0 };

        public List<BaseAttrData> assetsData = new List<BaseAttrData>();
    }
    [Serializable]
    public class BaseAttrData
    {
        public string name;
        public int id = -1;
        public string type;
        public int parentID = -1;
        public string parentName;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public BaseAttrData()
        {
            type = "Base";
        }
        public BaseAttrData(BaseAttrData b)
        {
            name = b.name;
            id = b.id;
            type = "Base";
            parentID = b.parentID;
            parentName = b.parentName;
            position = b.position;
            rotation = b.rotation;
            scale = b.scale;
        }

        public virtual void SetAttrData(BaseAttr attr)
        {
            attr.Name = name;
            attr.ID = id;
            attr.SetParent(parentID, parentName);

            Vector3 position = new Vector3(this.position[0], this.position[1], this.position[2]);
            Vector3 rotation = new Vector3(this.rotation[0], this.rotation[1], this.rotation[2]);
            Vector3 scale = new Vector3(this.scale[0], this.scale[1], this.scale[2]);

            attr.SetTransform(true, true, true, position, rotation, scale);
        }
    }
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class BaseAttr : MonoBehaviour
    {
        private static Dictionary<int, BaseAttr> attrs = new Dictionary<int, BaseAttr>();
        public static Dictionary<int, BaseAttr> Attrs => new Dictionary<int, BaseAttr>(attrs);
        public static Dictionary<int, BaseAttr> ActiveAttrs => attrs.Where((s) => s.Value.gameObject.activeInHierarchy).ToDictionary((s) => s.Key, (s) => s.Value);

        public static Action OnAttrChange;
        public static void AddAttr(BaseAttr attr)
        {
            if (!attrs.ContainsKey(attr.ID))
            {
                attrs.Add(attr.ID, attr);
                OnAttrChange?.Invoke();
            }
        }
        public static void RemoveAttr(BaseAttr attr)
        {
            if (attrs.ContainsKey(attr.ID))
            {
                attrs.Remove(attr.ID);
                OnAttrChange?.Invoke();
            }
        }
        public static void ClearAttr()
        {
            attrs.Clear();
            OnAttrChange?.Invoke();
        }

        [SerializeField]
        private int id = -1;
        public int ID
        {
            get
            {
                if (id < 0)
                    id = UnityEngine.Random.Range(100000, 1000000);
                return id;
            }
            set
            {
                id = value;
            }
        }
        [HideInInspector]
        [SerializeField]
        private string attrName = string.Empty;
        public virtual string Name
        {
            get
            {
                if (string.IsNullOrEmpty(attrName))
                    attrName = gameObject.name;
                return attrName;
            }
            set
            {
                attrName = value;
            }
        }

        public List<BaseAttr> childs = new List<BaseAttr>();

        private bool isRFMoveCollider = true;
        public bool IsRFMoveCollider
        {
            get
            {
                return isRFMoveCollider;
            }
            set
            {
                isRFMoveCollider = value;
            }
        }
        public void Instance()
        {
            Init();
            Rigister();
        }
        public virtual void Init()
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_IDColor", RFUniverseUtility.EncodeIDAsColor(ID));
            foreach (var item in this.GetChildComponentFilter<Renderer>())
            {
                item.SetPropertyBlock(mpb);
            }
            for (int i = 0; i < childs.Count; i++)
            {
                childs[i].ID = ID * 10 + i;
                childs[i].Instance();
            }
        }

        protected virtual void Rigister()
        {
            if (Attrs.ContainsKey(ID))
                Debug.LogError($"ID:{ID} Name:{Name} exist");
            else
            {
                Debug.Log($"Rigister ID:{ID} Name:{Name}");
                BaseAttr.AddAttr(this);
            }
        }
        public virtual BaseAttrData GetAttrData()
        {
            BaseAttrData data = new BaseAttrData();

            data.name = Name;
            data.id = ID;
            BaseAttr parentAttr = null;
            List<BaseAttr> parents = GetComponentsInParent<BaseAttr>().ToList();
            parents.RemoveAt(0);
            if (parents.Count > 1)
            {
                List<BaseAttr> parentsTmp = new List<BaseAttr>(parents);
                foreach (var item in parentsTmp)
                {
                    foreach (var child in item.childs)
                    {
                        parents.Remove(child);
                    }
                }
                parentAttr = parents[0];
            }

            data.parentID = parentAttr == null ? -1 : parentAttr.ID;

            Transform parent = transform.parent;
            data.parentName = parent == null ? "" : parent.name;

            data.position = new float[3] { transform.position.x, transform.position.y, transform.position.z };
            data.rotation = new float[3] { transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z };
            data.scale = new float[3] { transform.localScale.x, transform.localScale.y, transform.localScale.z };

            return data;
        }

        private void OnDestroy()
        {
            RemoveAttr(this);
        }

        public virtual void CollectData(OutgoingMessage msg)
        {
            // Name
            msg.WriteString(Name);
            // Position
            msg.WriteFloat32(transform.position.x);
            msg.WriteFloat32(transform.position.y);
            msg.WriteFloat32(transform.position.z);
            // Rotation
            msg.WriteFloat32(transform.eulerAngles.x);
            msg.WriteFloat32(transform.eulerAngles.y);
            msg.WriteFloat32(transform.eulerAngles.z);
            // Quaternion
            msg.WriteFloat32(transform.rotation.x);
            msg.WriteFloat32(transform.rotation.y);
            msg.WriteFloat32(transform.rotation.z);
            msg.WriteFloat32(transform.rotation.w);
            // LocalPosition
            msg.WriteFloat32(transform.localPosition.x);
            msg.WriteFloat32(transform.localPosition.y);
            msg.WriteFloat32(transform.localPosition.z);
            // LocalRotation
            msg.WriteFloat32(transform.localEulerAngles.x);
            msg.WriteFloat32(transform.localEulerAngles.y);
            msg.WriteFloat32(transform.localEulerAngles.z);
            // LocalQuaternion
            msg.WriteFloat32(transform.localRotation.x);
            msg.WriteFloat32(transform.localRotation.y);
            msg.WriteFloat32(transform.localRotation.z);
            msg.WriteFloat32(transform.localRotation.w);

            List<float> matrix = new List<float>();
            for (int i = 0; i < 16; i++)
            {
                matrix.Add(transform.localToWorldMatrix[i]);
            }
            msg.WriteFloatList(matrix);

            if (resultLocalPoint != null)
            {
                msg.WriteBoolean(true);
                msg.WriteFloatList(resultLocalPoint);
                resultLocalPoint = null;
            }
            else
                msg.WriteBoolean(false);
            if (resultWorldPoint != null)
            {
                msg.WriteBoolean(true);
                msg.WriteFloatList(resultWorldPoint);
                resultWorldPoint = null;
            }
            else
                msg.WriteBoolean(false);
        }

        public void ReceiveData(IncomingMessage msg)
        {
            string type = msg.ReadString();
            AnalysisMsg(msg, type);
        }

        public virtual void AnalysisMsg(IncomingMessage msg, string type)
        {
            switch (type)
            {
                case "SetTransform":
                    SetTransform(msg);
                    return;
                case "SetRotationQuaternion":
                    SetRotationQuaternion(msg);
                    return;
                case "Translate":
                    Translate(msg);
                    return;
                case "Rotate":
                    Rotate(msg);
                    return;
                case "SetParent":
                    SetParent(msg);
                    return;
                case "SetActive":
                    SetActive(msg);
                    return;
                case "SetLayer":
                    SetLayer(msg);
                    return;
                case "Copy":
                    Copy(msg);
                    return;
                case "Destroy":
                    Destroy();
                    return;
                case "SetRFMoveColliderActive":
                    SetRFMoveColliderActive(msg);
                    return;
                case "GetLoaclPointFromWorld":
                    GetLoaclPointFromWorld(msg);
                    return;
                case "GetWorldPointFromLocal":
                    GetWorldPointFromLocal(msg);
                    return;
                default:
                    Debug.Log($"ID:{ID} Dont have mehond:{type}");
                    return;
            }
        }

        protected virtual void SetTransform(IncomingMessage msg)
        {
            Debug.Log("SetTransform");
            bool set_position = msg.ReadBoolean();
            bool set_rotation = msg.ReadBoolean();
            bool set_scale = msg.ReadBoolean();

            Vector3 position = Vector3.zero;
            if (set_position)
            {
                float x = msg.ReadFloat32();
                float y = msg.ReadFloat32();
                float z = msg.ReadFloat32();
                position = new Vector3(x, y, z);
            }
            Vector3 rotation = Vector3.zero;
            if (set_rotation)
            {
                float rx = msg.ReadFloat32();
                float ry = msg.ReadFloat32();
                float rz = msg.ReadFloat32();
                rotation = new Vector3(rx, ry, rz);
            }
            Vector3 scale = Vector3.one;
            if (set_scale)
            {
                float sx = msg.ReadFloat32();
                float sy = msg.ReadFloat32();
                float sz = msg.ReadFloat32();
                scale = new Vector3(sx, sy, sz);
            }
            bool isWorld = msg.ReadBoolean();
            SetTransform(set_position, set_rotation, set_scale, position, rotation, scale, isWorld);
        }
        protected virtual void SetRotationQuaternion(IncomingMessage msg)
        {
            Debug.Log("SetRotationQuaternion");
            float x = msg.ReadFloat32();
            float y = msg.ReadFloat32();
            float z = msg.ReadFloat32();
            float w = msg.ReadFloat32();
            bool isWorld = msg.ReadBoolean();
            Quaternion quaternion = new Quaternion(x, y, z, w);
            SetTransform(false, true, false, Vector3.zero, quaternion.eulerAngles, Vector3.one, isWorld);
        }
        public virtual void SetTransform(bool set_position, bool set_rotation, bool set_scale, Vector3 position, Vector3 rotation, Vector3 scale, bool worldSpace = true)
        {
            if (set_position)
            {
                if (worldSpace)
                    transform.position = position;
                else
                    transform.localPosition = position;
            }
            if (set_rotation)
            {
                if (worldSpace)
                    transform.eulerAngles = rotation;
                else
                    transform.localEulerAngles = rotation;
            }
            if (set_scale)
            {
                transform.localScale = scale;
            }
        }

        private void Translate(IncomingMessage msg)
        {
            float x = msg.ReadFloat32();
            float y = msg.ReadFloat32();
            float z = msg.ReadFloat32();
            Space space = msg.ReadBoolean() ? Space.World : Space.Self;
            transform.Translate(new Vector3(x, y, z), space);
        }

        private void Rotate(IncomingMessage msg)
        {
            float rx = msg.ReadFloat32();
            float ry = msg.ReadFloat32();
            float rz = msg.ReadFloat32();
            Space space = msg.ReadBoolean() ? Space.World : Space.Self;
            transform.Rotate(new Vector3(0, 0, 1), rz, space);
            transform.Rotate(new Vector3(1, 0, 0), rx, space);
            transform.Rotate(new Vector3(0, 1, 0), ry, space);
        }
        protected virtual void SetParent(IncomingMessage msg)
        {
            Debug.Log("SetParent");
            int parentID = msg.ReadInt32();
            string parentName = msg.ReadString();
            SetParent(parentID, parentName);
        }
        public virtual void SetParent(int parentID, string parentName)
        {
            Transform parent = null;
            if (Attrs.TryGetValue(parentID, out BaseAttr attr))
            {
                parent = attr.transform.FindChlid(parentName, true);
                if (parent == null)
                    parent = attr.transform;
            }
            transform.SetParent(parent);
        }

        protected void SetActive(IncomingMessage msg)
        {
            bool active = msg.ReadBoolean();
            SetActive(active);
        }
        protected virtual void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        protected void SetLayer(IncomingMessage msg)
        {
            Debug.Log("SetLayer");
            int layer = msg.ReadInt32();
            SetLayer(layer);
        }
        public void SetLayer(int layer)
        {
            foreach (var item in this.GetChildComponentFilter<Transform>())
            {
                item.gameObject.layer = layer;
            }
        }
        protected void Copy(IncomingMessage msg)
        {
            Debug.Log("Copy");
            int copyID = msg.ReadInt32();
            GameObject copy = GameObject.Instantiate(gameObject);
            BaseAttr attr = copy.GetComponent<BaseAttr>();
            attr.ID = copyID;
            attr.Instance();
        }

        public virtual void Destroy()
        {
            if (Application.isEditor)
                DestroyImmediate(gameObject);
            else
                Destroy(gameObject);
        }

        protected void SetRFMoveColliderActive(IncomingMessage msg)
        {
            IsRFMoveCollider = msg.ReadBoolean();
        }

        float[] resultLocalPoint = null;
        void GetLoaclPointFromWorld(IncomingMessage msg)
        {
            Debug.Log("GetLoaclPointFromWorld");
            float x = msg.ReadFloat32();
            float y = msg.ReadFloat32();
            float z = msg.ReadFloat32();
            Vector3 local = transform.InverseTransformPoint(new Vector3(x, y, z));
            resultLocalPoint = new float[] { local.x, local.y, local.z };
        }

        float[] resultWorldPoint = null;
        void GetWorldPointFromLocal(IncomingMessage msg)
        {
            Debug.Log("GetWorldPointFromLocal");
            float x = msg.ReadFloat32();
            float y = msg.ReadFloat32();
            float z = msg.ReadFloat32();
            Vector3 world = transform.TransformPoint(new Vector3(x, y, z));
            resultWorldPoint = new float[] { world.x, world.y, world.z };
        }
        private static List<Tuple<int, int>> collisionPairs = new List<Tuple<int, int>>();
        public static List<Tuple<int, int>> CollisionPairs => new List<Tuple<int, int>>(collisionPairs);
        public static Action OnCollisionPairsChange;
        private void OnCollisionEnter(Collision other)
        {
            BaseAttr otherAttr = other.gameObject.GetComponentInParent<BaseAttr>();
            if (otherAttr == null) return;
            if (collisionPairs.Exists(s => (s.Item1 == this.ID && s.Item2 == otherAttr.ID) || (s.Item1 == otherAttr.ID && s.Item2 == this.ID))) return;
            collisionPairs.Add(new Tuple<int, int>(this.ID, otherAttr.ID));
            OnCollisionPairsChange?.Invoke();
        }
        private void OnCollisionExit(Collision other)
        {
            BaseAttr otherAttr = other.gameObject.GetComponentInParent<BaseAttr>();
            if (otherAttr == null) return;
            Tuple<int, int> pair = collisionPairs.Find(s => (s.Item1 == this.ID && s.Item2 == otherAttr.ID) || (s.Item1 == otherAttr.ID && s.Item2 == this.ID));
            collisionPairs.Remove(pair);
            OnCollisionPairsChange?.Invoke();
        }
    }


}
