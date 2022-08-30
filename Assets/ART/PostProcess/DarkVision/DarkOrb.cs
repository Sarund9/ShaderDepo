using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DarkOrb : MonoBehaviour
{
    [SerializeField]
    Transform track;
    [SerializeField]
    MeshRenderer render;

    [SerializeField, Range(.001f, 1)]
    float blendDistance;

    [SerializeField]
    [ColorUsage(true, true), FormerlySerializedAs("highlightColor")]
    Color bioColor;

    [SerializeField, ColorUsage(true, true)]
    Color selectionColor, selectionBlendColor;

    public void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == 10)
        {
            DarkVisionPass.Color = render.material.GetColor("_Color");

            var localCamPos = transform.InverseTransformPoint(track.position);

            var slen = localCamPos.magnitude - .5f;

            var I = (-slen * (1 / blendDistance)) + 1;

            DarkVisionPass.Intensity = I;

            BioShaderPass.Active = DarkVisionPass.Intensity > 0;

            BioShaderPass.Color = bioColor;
            BioShaderPass.SelectionColor = selectionColor;
            BioShaderPass.SelectionBlendColor = selectionBlendColor;
        }
    }
}
