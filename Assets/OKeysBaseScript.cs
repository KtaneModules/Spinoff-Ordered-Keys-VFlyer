using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class OKeysBaseScript : MonoBehaviour {

	public KMAudio mAudio;
	public KMBombModule modSelf;
	public KMColorblindMode colorblindMode;
	public KMSelectable[] keySelectables;
	public MeshRenderer[] keyRenderers, stageLights;
	public TextMesh[] keyTexts;
	public Material[] possibleMatKeys;
	public Color[] possibleColorTexts, possibleMatColors;
	public string[] possibleDisplayTexts, possibleCBTexts, possibleColorNames;

	protected const string digits = "0123456789";

	protected bool moduleSolved, colorblindDetected, interactable, bypassStrike;
	protected bool[] buttonsPressed;
	protected string[] keyTypes;

	protected int moduleID, resetCount;

	protected virtual void QuickLog(string toLog = "", params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	protected virtual void QuickLogDebug(string toLog = "", params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	protected virtual void Start()
    {
		buttonsPressed = new bool[keySelectables.Length];
		for (var x = 0; x < keySelectables.Length; x++)
		{
			keySelectables[x].transform.localPosition = Vector3.back;
			var y = x;
			keySelectables[x].OnInteract += delegate {
				if (!buttonsPressed[x] && interactable)
					HandleIdxPress(y);
				return false;
			};
		}
		StartCoroutine(HandleResetAnim());
    }
	protected virtual void HandleIdxPress(int idx)
    {

    }
	protected virtual IEnumerator HandleSolveAnim(float delay = 0.1f, int repeatCount = 5)
    {
		moduleSolved = true;
		for (var x = 0; x < keyRenderers.Length; x++)
        {
			for (var cnt = 0; cnt < repeatCount; cnt++)
			{
				for (var y = x; y < keyRenderers.Length; y++)
					RandomizeKeyVisuals(y);
				yield return new WaitForSeconds(delay);
			}
			keyRenderers[x].material.color = Color.white;
			keyTexts[x].text = "0";
			keyTexts[x].color = Color.black;
        }
		modSelf.HandlePass();
    }
	protected virtual IEnumerator HandleResetAnim(float delay = 0.1f, int repeatCount = 5)
    {
		for (var x = 0; x < keyRenderers.Length; x++)
		{
			for (var cnt = 0; cnt < repeatCount; cnt++)
			{
				for (var y = x; y < keyRenderers.Length; y++)
					RandomizeKeyVisuals(y);
				yield return new WaitForSeconds(delay);
			}
		}
	}

	protected virtual void SetKeyVisuals(int keyIdx, int idxMat, int idxTxtClr, int idxTxtDisp)
    {
		if (keyIdx < 0 || keyIdx >= Mathf.Min(keyRenderers.Length, keyTexts.Length)) return;

		var pickedText = possibleDisplayTexts[idxTxtDisp];
		keyRenderers[keyIdx].material = possibleMatKeys[idxMat];
		keyTexts[keyIdx].color = possibleColorTexts[idxTxtClr];
		keyTexts[keyIdx].text = colorblindDetected ? string.Format("{0}\n{1}\n\n{2}", pickedText, possibleCBTexts[idxTxtClr], possibleCBTexts[idxMat]) : pickedText;
	}
	protected virtual void RandomizeKeyVisuals(int keyIdx)
    {
		if (keyIdx < 0 || keyIdx >= Mathf.Min(keyRenderers.Length, keyTexts.Length)) return;

		var pickedIDxMat = Random.Range(0, possibleMatKeys.Length);
		var pickedIDxClr = Random.Range(0, possibleColorTexts.Length);
		var pickedText = possibleDisplayTexts.PickRandom();

		keyRenderers[keyIdx].material = possibleMatKeys[pickedIDxMat];
		keyTexts[keyIdx].color = possibleColorTexts[pickedIDxClr];
		keyTexts[keyIdx].text = colorblindDetected ? string.Format("{0}\n{1}\n\n{2}", pickedText, possibleCBTexts[pickedIDxClr], possibleCBTexts[pickedIDxMat]) : pickedText;
	}

	protected virtual void HandleColorblindModeToggle()
    {

    }

	protected virtual IEnumerator ProcessTwitchCommand(string cmd)
    {
		yield break;
    }

	protected virtual IEnumerator TwitchHandleForcedSolve()
    {
		bypassStrike = true;
		yield return HandleSolveAnim();
    }
}
