using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Highlighter : MonoBehaviour
{
    [SerializeField]
    LayerMask mask;

    public void Update()
    {
        DoCheck();
    }

    void DoCheck()
    {
        if ( // Raycast from Camera
            !Physics.Raycast(
            transform.position, transform.forward,
            out var hit, 9999f,
            mask, QueryTriggerInteraction.Ignore)
            || // Get Filter
            !hit.collider.TryGetComponent<Renderer>(out var render)
            )
        {
            BioShaderPass.Selection = null;
            return;
        }

        // ASIGN VALUES
        BioShaderPass.Selection = render;

    }
}
