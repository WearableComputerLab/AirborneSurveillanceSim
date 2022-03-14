using UnityEngine;
using System;

/// <summary>
/// Component that should be attached to a camera together with a DOFLogic component
/// and applies the depth of field post processing.
///
/// Unity already has a DOF post-process but it didn't work properly in VR and was basically just a cheap blur
/// for background objects.
/// 
/// This script has been stolen from https://catlikecoding.com/unity/tutorials/advanced-rendering/depth-of-field/.
/// The only things I added were "infinity", which basically fades out the blur for large distances when focusing
/// on the far plane, and also the "debugDepth" thing which allows you to see the depth buffer.
///
/// <strong>NOTE:</strong> This requires multi-pass VR rendering mode!
/// </summary>
[RequireComponent(typeof(Camera))]
public class DepthOfFieldEffect : MonoBehaviour {

	const int circleOfConfusionPass = 0;
	const int preFilterPass = 1;
	const int bokehPass = 2;
	const int postFilterPass = 3;
	const int combinePass = 4;
	const int debugPass = 5;

	[Range(0.1f, 100f)]
	public float focusDistance = 10f;

	[Range(0.1f, 10f)]
	public float focusRange = 3f;

	[Range(1f, 10f)]
	public float bokehRadius = 4f;

	public float infinityThresholdLow = 5.5f;
	public float infinityThresholdHigh = 6.5f;
	public bool debugDepth = false;
	public float debugDepthScale = 1.0f;

	[HideInInspector]
	public Shader dofShader;

	[NonSerialized]
	Material dofMaterial;

	void OnRenderImage (RenderTexture source, RenderTexture destination) {
		if (dofMaterial == null) {
			dofMaterial = new Material(dofShader);
			dofMaterial.hideFlags = HideFlags.HideAndDontSave;
		}

		if(debugDepth) {
			dofMaterial.SetFloat("_DepthScale", debugDepthScale);
			Graphics.Blit(source, destination, dofMaterial, debugPass);
			return;
		}

		dofMaterial.SetFloat("_BokehRadius", bokehRadius);
		dofMaterial.SetFloat("_FocusDistance", focusDistance);
		dofMaterial.SetFloat("_FocusRange", focusRange);
		dofMaterial.SetFloat("_InfinityLow", infinityThresholdLow);
		dofMaterial.SetFloat("_InfinityHigh", infinityThresholdHigh);

		RenderTexture coc = RenderTexture.GetTemporary(
			source.width, source.height, 0,
			RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear
		);

		int width = source.width / 2;
		int height = source.height / 2;
		RenderTextureFormat format = source.format;
		RenderTexture dof0 = RenderTexture.GetTemporary(width, height, 0, format);
		RenderTexture dof1 = RenderTexture.GetTemporary(width, height, 0, format);

		dofMaterial.SetTexture("_CoCTex", coc);
		dofMaterial.SetTexture("_DoFTex", dof0);

		Graphics.Blit(source, coc, dofMaterial, circleOfConfusionPass);
		Graphics.Blit(source, dof0, dofMaterial, preFilterPass);
		Graphics.Blit(dof0, dof1, dofMaterial, bokehPass);
		Graphics.Blit(dof1, dof0, dofMaterial, postFilterPass);
		Graphics.Blit(source, destination, dofMaterial, combinePass);

		RenderTexture.ReleaseTemporary(coc);
		RenderTexture.ReleaseTemporary(dof0);
		RenderTexture.ReleaseTemporary(dof1);
	}
}