using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class BabyOKeysScript : OKeysBaseScript {
	public KMBombInfo bombInfo;
	static int modIDCnt;
	static int[][,] valueTable = new int[][,] {
		new int[,]
        {
			{ 1, 2, 3 },
			{ 2, 3, 1 },
			{ 3, 1, 2 },
		},
		new int[,]
        {
			{ 2, 3, 1 },
			{ 3, 1, 2 },
			{ 1, 2, 3 },
		},
		new int[,]
        {
			{ 3, 1, 2 },
			{ 1, 2, 3 },
			{ 2, 3, 1 },
		},
	};
	int[][] orderSequences = new int[][] {
		new[] { 1, 2, 3 }, new[] { 2, 3, 1 }, new[] { 3, 1, 2 },
		new[] { 2, 1, 3 }, new[] { 1, 3, 2 }, new[] { 3, 2, 1 },
	},
		sequencesLookup = new int[][] {
		new[] { 3, 2, 1 }, new[] { 3, 1, 2 }, new[] { 2, 1, 3 },
		new[] { 1, 3, 2 }, new[] { 2, 3, 1 }, new[] { 1, 2, 3 },
	}; // Arranged by ordering B, then A.
	List<int> sequenceExpected, sequencePressed;
	int stagesCompleted = 0;
	// Use this for initialization
	protected override void Start()
	{
		colorblindDetected = colorblindMode.ColorblindModeActive;
		moduleID = ++modIDCnt;
		buttonsPressed = new bool[keySelectables.Length];
		for (var x = 0; x < keySelectables.Length; x++)
		{
			keySelectables[x].transform.localPosition = Vector3.back;
			var y = x;
			keySelectables[x].OnInteract += delegate {
				if (!buttonsPressed[y] && interactable)
					HandleIdxPress(y);
				return false;
			};
		}
		sequenceExpected = new List<int>();
		sequencePressed = new List<int>();
		keyTypes = new string[keySelectables.Length];
		GenerateStage();
		
	}
	void GenerateStage()
    {
		for (var x = 0; x < stageLights.Length; x++)
			stageLights[x].enabled = x < stagesCompleted;
		sequenceExpected.Clear();
		sequencePressed.Clear();
		for (var x = 0; x < buttonsPressed.Length; x++)
			buttonsPressed[x] = false;
		// Start by creating the button values since that plays an important role.
		var buttonValues = Enumerable.Range(1, 3).ToArray().Shuffle();
		// Create the button visuals here based on the buttons' values.
		var displayedValues = new List<int>();
		var idxLabelClr = new List<int>();
		var idxKeyClr = new List<int>();
		for (var x = 0; x < keySelectables.Length; x++)
		{
			displayedValues.Add(Random.Range(1, 4));
			var possibleLabKeyClrIdxes = Enumerable.Range(0, 9).Where(a => valueTable[x][a % 3, a / 3] == buttonValues[x]);
			var pickedPossible = possibleLabKeyClrIdxes.PickRandom();
			idxKeyClr.Add(pickedPossible % 3);
			idxLabelClr.Add(pickedPossible / 3);
		}
		// Obtain the order of the buttons to press. It is relevant after all.
		var obtainedOrder = new List<int>();
		if (displayedValues.Distinct().Count() == displayedValues.Count)
		{ // 3 distinct labels = one of the sequences.
			var idxLookup = Enumerable.Range(0, 6).First(a => sequencesLookup[a].SequenceEqual(displayedValues));
			QuickLogDebug("{1}: {0}", idxLookup, displayedValues.Join(","));
			obtainedOrder.AddRange(orderSequences[idxLookup]);
        }
		else
		{ // 1-2 distinct values case.
			QuickLogDebug(displayedValues.Join());
			var countsOccurance = Enumerable.Range(1, 3).Select(a => displayedValues.Count(b => b == a));
			QuickLogDebug(countsOccurance.Join());
			var xthDigitMod2 = bombInfo.GetSerialNumberNumbers().ElementAt(stagesCompleted) % 2; // Technically value B
			var idxMax = Enumerable.Range(0, 3).Single(a => countsOccurance.ElementAt(a) >= countsOccurance.Max()); // Technically value A.
			obtainedOrder.AddRange(orderSequences[3 * xthDigitMod2 + idxMax]);
		}
		sequenceExpected.AddRange(Enumerable.Range(0, 3).OrderBy(a => obtainedOrder.IndexOf(buttonValues[a])));
		// Encode it for later.
		for (var x = 0; x < keyTypes.Length; x++)
			keyTypes[x] = string.Format("{0}{1}{2}", displayedValues[x], possibleCBTexts[idxKeyClr[x]], possibleCBTexts[idxLabelClr[x]]);
		// Now log all this.
		QuickLog(resetCount == 0 ? "Initial State:" : "Reset #{0}:", resetCount);
		QuickLog("The buttons have the colors: {0}", idxKeyClr.Select(a => possibleColorNames[a]).Join(", "));
		QuickLog("The buttons have the labels: {0}", displayedValues.Join(", "));
		QuickLog("The labels have the colors: {0}", idxLabelClr.Select(a => possibleColorNames[a]).Join(", "));
		QuickLog("The values of the buttons are {0}", buttonValues.Join(", "));
		QuickLog("The order of values obtained from the displayed labels are {0}", obtainedOrder.Join(", "));
		QuickLog("Press these keys in the following order: {0}", sequenceExpected.Select(a => a + 1).Join(", "));
		interactable = false;
		resetCount++;
		StartCoroutine(HandleResetAnim(0.1f, 10));
	}
    protected override void HandleIdxPress(int idx)
    {
		buttonsPressed[idx] = true;
		keySelectables[idx].transform.localPosition = Vector3.back;
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, keySelectables[idx].transform);
		sequencePressed.Add(idx);
		if (sequencePressed.Count >= 3)
        {
			QuickLog("The following keys were pressed: {0}", sequencePressed.Select(a => a + 1).Join(", "));
			if (sequencePressed.SequenceEqual(sequenceExpected))
			{
				mAudio.PlaySoundAtTransform("InputCorrect", transform);
				stagesCompleted++;
				if (stagesCompleted >= 2)
				{
					moduleSolved = true;
					StartCoroutine(HandleSolveAnim(0.1f, 10));
					interactable = false;
				}
				else
					GenerateStage();
			}
			else
            {
				modSelf.HandleStrike();
				GenerateStage();
            }
        }
    }

    protected override IEnumerator HandleSolveAnim(float delay = 0.1f, int repeatCount = 5)
	{
		for (var x = 0; x < stageLights.Length; x++)
			stageLights[x].enabled = true;
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
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
	}
    protected override IEnumerator HandleResetAnim(float delay = 0.1F, int repeatCount = 5)
	{
		for (var x = 0; x < keyRenderers.Length; x++)
		{
			for (var cnt = 0; cnt < repeatCount; cnt++)
			{
				for (var y = x; y < keyRenderers.Length; y++)
					RandomizeKeyVisuals(y);
				yield return new WaitForSeconds(delay);
			}
			DecryptAndSetSpecificKey(x);
			keySelectables[x].transform.localPosition = Vector3.zero;
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
		}
		interactable = true;
	}
	void DecryptAndSetSpecificKey(int idx)
    {
		var listedCBTexts = possibleCBTexts.ToList();
		var obtainedEncoding = keyTypes[idx];
		SetKeyVisuals(idx,
			listedCBTexts.IndexOf(obtainedEncoding[1].ToString()), // Key Color
			listedCBTexts.IndexOf(obtainedEncoding[2].ToString()), // Label Color
			digits.IndexOf(obtainedEncoding[0]) - 1 // Label Text
			);
    }

    protected override void HandleColorblindModeToggle()
    {
		colorblindDetected ^= true;
		for (var x = 0; x < 3 && interactable; x++)
			DecryptAndSetSpecificKey(x);
    }

    private readonly string TwitchHelpMessage = "!{0} press 123 [position in reading order, \"press\" optional] | !{0} colorblind/colourblind/cb";

    protected override IEnumerator ProcessTwitchCommand(string cmd)
    {
		if (!interactable)
        {
			yield return "sendtochaterror The module is not ready to accept commands right now. Wait a bit until the module is ready.";
			yield break;
        }
		var rgxCB = Regex.Match(cmd, @"^(colou?rblind|cb)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		var rgxPress = Regex.Match(cmd, @"^(press\s)?(\d+\s?)+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		if (rgxCB.Success)
        {
			yield return null;
			HandleColorblindModeToggle();
			yield break;
        }
		else if (rgxPress.Success)
        {

        }
    }

    protected override IEnumerator TwitchHandleForcedSolve()
    {
		yield return HandleSolveAnim(delay: 0.1f, repeatCount: 10);
	}
}
