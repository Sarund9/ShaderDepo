using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PaperCellDriver : MonoBehaviour
{

    [SerializeField]
    MeshRenderer m_Render;

    [SerializeField]
    Transform lightDriver;

    MaterialPropertyBlock block;

    Light[] lights;

    public void OnEnable()
    {
        if (m_Render == null)
        {
            m_Render = GetComponent<MeshRenderer>();
        }

        lights = FindObjectsOfType<Light>();
    }

    public void Update()
    {

        // Find Closest Light
        var curr = Vector3.one * 20; // Default
        if (lightDriver)
        {
            curr = lightDriver.position - transform.position;
        }
        else for (int i = 0; i < lights.Length; i++)
        {
            var newVec = lights[i].transform.position - transform.position;
            if (curr.sqrMagnitude > newVec.sqrMagnitude)
            {
                curr = newVec;
            }
        }
        
        
        // Set Light Value
        SetLight(curr, 0);
        
        
    }

    private void SetLight(Vector3 pos, float magnitude)
    {
        //if (block == null)

        var block = new MaterialPropertyBlock();
        print("BLOCK CREATED");

        block.Clear();

        block.SetVector("_LightSource", new Vector4(pos.x, pos.y, pos.z, magnitude));
        m_Render.SetPropertyBlock(block);
        //print("SET DRIVER");

        
    }
}
