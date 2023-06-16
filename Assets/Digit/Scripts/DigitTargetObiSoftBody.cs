using UnityEngine;
using Obi;
using System.Collections.Generic;

public class DigitTargetObiSoftBody : MonoBehaviour
{
    public static DigitTargetObiSoftBody Instance;

    public Material[] lightMaterial;
    public ObiSoftbody softbody;
    SkinnedMeshRenderer skinnedMeshRenderer;
    Mesh mesh;
    void Start()
    {
        Instance = this;
        mesh = new Mesh();
        softbody = GetComponent<ObiSoftbody>();
        softbody.solver.OnCollision += ParticleCollision;
        skinnedMeshRenderer = softbody.GetComponentInChildren<SkinnedMeshRenderer>();
    }
    private Dictionary<int, float> impulse = new Dictionary<int, float>();
    void ParticleCollision(ObiSolver solver, ObiSolver.ObiCollisionEventArgs contacts)
    {
        impulse.Clear();
        foreach (var item in contacts.contacts)
        {
            if (item.normalImpulse > 0)
                if (impulse.ContainsKey(item.bodyB))
                    impulse[item.bodyB] += item.normalImpulse;
                else
                    impulse.Add(item.bodyB, item.normalImpulse);
        }
        if (impulse.Count > 0)
        {
            skinnedMeshRenderer.BakeMesh(mesh);
            mesh.RecalculateNormals();
            foreach (var item in impulse)
            {
                ObiColliderWorld world = ObiColliderWorld.GetInstance();
                DigitCollider collider = world.colliderHandles[item.Key].owner.GetComponent<DigitCollider>();
                if (collider == null) continue;
                print(collider.name);
                GameObject copyRender = GetOrCreateRender(collider.digit);
                collider.digit.CollisionStay(transform, copyRender, item.Value);//将碰撞事件发送到Digit
            }
        }
    }

    //Render列表，key：Digit，value：复制出的渲染物体
    private Dictionary<Digit, GameObject> targets = new Dictionary<Digit, GameObject>();

    //获取或者生成渲染物体
    GameObject GetOrCreateRender(Digit target)
    {

        if (targets.TryGetValue(target, out GameObject copyRender))
        {
            copyRender.SetActive(true);
            return copyRender;
        }
        //Debug.Log(123);
        //复制一份Render
        copyRender = new GameObject("render", typeof(MeshFilter), typeof(MeshRenderer));
        copyRender.GetComponent<MeshFilter>().mesh = mesh;
        copyRender.GetComponent<MeshRenderer>().materials = lightMaterial;
        gameObject.transform.localScale = skinnedMeshRenderer.transform.lossyScale;
        //改变渲染层
        copyRender.layer = target.index + 22;
        //添加进Render列表
        targets.Add(target, copyRender);
        return copyRender;
    }
}
