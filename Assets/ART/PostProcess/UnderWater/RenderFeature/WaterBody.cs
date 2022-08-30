using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WaterBody : MonoBehaviour
{

    [SerializeField]
    Vector2 size = new Vector2(5, 5);
    [SerializeField]
    float depth = 20;
    [SerializeField, Range(.01f, 1)]
    float blendDist = .1f;

    Vector3 camLocalPos;

    MeshRenderer render;

    public float Depth
    {
        get => depth;
        set => depth = value;
    }
    public Vector2 Size
    {
        get => size;
        set => size = value;
    }

    public void OnEnable()
    {
        UnderWaterPass.Drivers += GetI;
        if (!render)
        {
            render = GetComponent<MeshRenderer>();
        }
    }

    //private void OnGUI()
    //{
    //    //GUILayout.Label("LenX: " + lenX.ToString());
    //    //GUILayout.Label("LenY: " + lenY.ToString());
    //    //GUILayout.Label("LenZ: " + lenZ.ToString());
    //    GUILayout.Label("Len: " + len.ToString());
    //    GUILayout.Label("CamLPos: " + camLocalPos.ToString());
    //}

    private float GetI(Vector3 camPos, out WaterShaderParams shaderParams)
    {
        // Signed Distance function
        GetBounds(out Vector3 localPos, out Vector3 totalSize);
        camLocalPos = transform.InverseTransformPoint(camPos) - localPos;

        totalSize /= 2;
        
        float distX = Mathf.Abs(camLocalPos.x) - totalSize.x;
        float distY = Mathf.Abs(camLocalPos.y) - totalSize.y;
        float distZ = Mathf.Abs(camLocalPos.z) - totalSize.z;

        float slen = Mathf.Max(distX, Mathf.Max(distY, distZ));

        // Desmos: \left(-x\ \cdot\frac{1}{a}\right)+1

        shaderParams = new WaterShaderParams
        {
            baseColor = render.sharedMaterial.GetColor("_BaseColor"),
            blendColor = render.sharedMaterial.GetColor("_BlendColor"),
            depth = render.sharedMaterial.GetFloat("_Depth"),
            strenght = render.sharedMaterial.GetFloat("_Strenght"),
            blend = render.sharedMaterial.GetFloat("_Blend"),
            minOpacity = render.sharedMaterial.GetFloat("_MinOpacity"),
            UpDir = transform.up,
            waterLevel = transform.position.y + blendDist,
        };

        return Mathf.Clamp01((-slen * (1 / blendDist)) + 1);
    }

    public void OnDisable()
    {
        UnderWaterPass.Drivers -= GetI;
    }

    public void OnDrawGizmosSelected()
    {
        GetBounds(out Vector3 localPos, out Vector3 totalSize);

        Vector3 worldPos = transform.TransformPoint(localPos);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(worldPos, totalSize);

        Gizmos.DrawLine(transform.position + camLocalPos, transform.position);
    }

    private void GetBounds(out Vector3 localPos, out Vector3 totalSize)
    {
        localPos = Vector3.down * depth;
        totalSize = new Vector3()
        {
            y = depth * 2,
            x = size.x * 2,
            z = size.y * 2,
        };
    }
}
