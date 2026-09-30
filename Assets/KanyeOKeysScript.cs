using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KanyeOKeysScript : OKeysBaseScript {

	public KMSelectable KButton;
	public Texture[] kanyeAlbumImgs;
	public Texture kanyeImg;
	public string[] kanyeAlbumNames;

	bool hasToggled = false;
	static int modIDCnt;

    static readonly bool[][] modifyInitValTable = new[] {
		"--O-O-",
		"---OOO",
		"-OOO--",
		"O-O-OO",
		"OOOOO-",
		"O-O--O",
	}.Select(a => a.Select(b => b == 'O').ToArray()).ToArray();

	int[][] initialValueTable = new int[][] {
		new[] { 13, 16, 8, 4, 6, 14, 22, 3, 23, 19, 24, 5 },
		new[] { 8, 2, 12, 25, 18, 24, 16, 5, 20, 14, 23, 13 },
		new[] { 10, 7, 21, 17, 11, 5, 24, 19, 8, 12, 3, 15 },
		new[] { 19, 8, 21, 24, 9, 13, 23, 25, 20, 6, 16, 11 },
		new[] { 24, 10, 18, 14, 8, 21, 2, 4, 11, 10, 6, 20 },
		new[] { 15, 7, 10, 2, 24, 25, 22, 11, 5, 8, 13, 14 },
	};

	List<List<int>> allowedOrderIdxes = new List<List<int>>();
	List<int> idxPressed;
	static readonly int[] kanyeIdxes = new[] { 11, 1, 14, 25, 5 };
	int[] idxAlbums;
	const string alphabet = "ZABCDEFGHIJKLMNOPQRSTUVWXY";

    // Use this for initialization
    protected override void Start () {
		idxAlbums = new int[keyRenderers.Length];
		keyTypes = new string[keyRenderers.Length];
		idxPressed = new List<int>();
		moduleID = ++modIDCnt;
		buttonsPressed = new bool[keySelectables.Length];
		for (var x = 0; x < stageLights.Length; x++)
			stageLights[x].enabled = false;
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
		KButton.OnInteract += delegate {
			if (interactable)
				HandleToggle();
			return false;
		};
		GenerateStage();
		
	}
    protected override void HandleIdxPress(int idx)
    {
		buttonsPressed[idx] = true;
		keySelectables[idx].AddInteractionPunch();
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, keySelectables[idx].transform);
		idxPressed.Add(idx);
		keySelectables[idx].transform.localPosition = Vector3.back;
	}

    void HandleToggle()
    {
		KButton.AddInteractionPunch();
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, KButton.transform);
		hasToggled ^= true;
		interactable = false;
		if (hasToggled)
			StartCoroutine(HandleResetAnim());
		else if (!allowedOrderIdxes.Any() || allowedOrderIdxes.Any(a => a.SequenceEqual(idxPressed)))
		{
			mAudio.PlaySoundAtTransform("InputCorrect", transform);
			StartCoroutine(HandleSolveAnim());
		}
		else
		{
			QuickLog("{0} is not a valid sequence.", idxPressed.Select(a => a + 1).Join(","));
			modSelf.HandleStrike();
			resetCount++;
			GenerateStage();
		}
    }

	void GenerateStage()
    {
		idxPressed.Clear();
		var attemptCount = 0;
	retry:
		attemptCount++;
		for (var x = 0; x < idxAlbums.Length; x++)
			idxAlbums[x] = Random.Range(0, kanyeAlbumImgs.Length);
		var finalLetterIdx = new int[keyRenderers.Length];
        for (var x = 0; x < keyRenderers.Length; x++)
        {
			var pickedIdxColorBtn = Random.Range(0, possibleMatColors.Length);
			var pickedIdxColorLab = Random.Range(0, possibleColorTexts.Length);
			var pickedIdxLetter = Random.Range(0, possibleDisplayTexts.Length);

			var initialOffset = initialValueTable[x][idxAlbums[x]];
			if (modifyInitValTable[pickedIdxColorLab][pickedIdxColorBtn])
				initialOffset = 26 - initialOffset;

			finalLetterIdx[x] = (pickedIdxLetter + initialOffset) % 26;

			keyTypes[x] = string.Format("{0}{1}{2}", alphabet[pickedIdxColorBtn], alphabet[pickedIdxColorLab], alphabet[pickedIdxLetter]);
			/* Format this with the color of the button, the color of the label, and its letter.
			 * And use the alphabet to correspond this.
			 */
        }
		var finalLetterIdxAdjust = finalLetterIdx.ToArray();
		while (finalLetterIdxAdjust.Any(a => !kanyeIdxes.Contains(a)))
		{
			for (var x = 0; x < finalLetterIdx.Length; x++)
			{
				var curLetterIdx = finalLetterIdxAdjust[x];
				if (kanyeIdxes.Contains(curLetterIdx)) continue;
				curLetterIdx = (curLetterIdx + 1) % alphabet.Length;
				finalLetterIdxAdjust[x] = curLetterIdx;
			}
		}
		if (attemptCount < 15 && !kanyeIdxes.All(a => finalLetterIdxAdjust.Contains(a)))
			goto retry;
		if (kanyeIdxes.All(a => finalLetterIdxAdjust.Contains(a)))
			QuickLogDebug("{0} attempt{1} taken to focus 5 distinct letters.", attemptCount, attemptCount == 1 ? "" : "s");
		else
			QuickLogDebug("{1} adjusted distinct letter{2} after {0} attempts.", attemptCount, finalLetterIdxAdjust.Distinct().Count(), finalLetterIdxAdjust.Distinct().Count() == 1 ? "" : "s");

		if (resetCount == 0)
			QuickLog("Initial State:");
		else
			QuickLog("Reset #{0}:", resetCount);
		QuickLog("From left to right:");
		QuickLog("The albums from each key are \"{0}\"", idxAlbums.Select(a => kanyeAlbumNames[a]).Join("\",\""));
		QuickLog("The key colors from each key are {0}", keyTypes.Select(a => possibleColorNames[alphabet.IndexOf(a[0])]).Join(", "));
		QuickLog("The label colors from each key are {0}", keyTypes.Select(a => possibleColorNames[alphabet.IndexOf(a[1])]).Join(", "));
		QuickLog("The letters displayed from each key are {0}", keyTypes.Select(a => possibleDisplayTexts[alphabet.IndexOf(a[2])]).Join(", "));
		QuickLog("The resulting letters after accounting for the album covers and the key/label colors are {0}", finalLetterIdx.Select(a => alphabet[a]).Join(", "));
		QuickLog("Adjusting the letters results in the following: {0}", finalLetterIdxAdjust.Select(a => alphabet[a]).Join(", "));
		var idxesGrouped = kanyeIdxes.Select(a => Enumerable.Range(0, keySelectables.Length).Where(b => finalLetterIdxAdjust[b] == a).ToArray()).ToArray();
		QuickLogDebug("[{0}]", idxesGrouped.Select(a => a.Join(",")).Join("];["));
		var curIdxCombinationsAll = new List<IEnumerable<int>> { new int[0] };
        for (var x = 0; x < kanyeIdxes.Length; x++)
        {
			var curIdxBtns = idxesGrouped[x];
			if (curIdxBtns.Length == 0) continue;
			// Generate all possible buttons that can be pressed for the current letter.
			var possibleOrderIdxes = new List<IEnumerable<int>> { new int[0] };
            for (var y = 0; y < curIdxBtns.Count(); y++)
            {
				var nextOrderIdxes = new List<IEnumerable<int>>();
				foreach (var lastCombo in possibleOrderIdxes)
                {
					var allowedCandidates = curIdxBtns.Where(a => !lastCombo.Contains(a)).ToArray();
					foreach (var candidate in allowedCandidates)
						nextOrderIdxes.Add(lastCombo.Concat(new[] { candidate }).ToList());
                }
				possibleOrderIdxes = nextOrderIdxes;
            }
			// Append this to each of the entries in the list.
			var nextCombinedOrderIdxes = new List<IEnumerable<int>>();
			foreach (var lastCombo in curIdxCombinationsAll)
				foreach (var nextCombo in possibleOrderIdxes)
					nextCombinedOrderIdxes.Add(lastCombo.Concat(nextCombo).ToArray());
			curIdxCombinationsAll = nextCombinedOrderIdxes;
		}
		allowedOrderIdxes = curIdxCombinationsAll.Select(a => a.ToList()).ToList();
		if (allowedOrderIdxes.Count >= 6)
		{
			QuickLog("{0} allowed sequences. Example: [{1}]", allowedOrderIdxes.Count, allowedOrderIdxes.PickRandom().Select(b => b + 1).Join(","));
			QuickLogDebug("Allowed Sequences: [{0}]", allowedOrderIdxes.Select(a => a.Select(b => b + 1).Join(",")).Join("];["));
		}
		else
			QuickLog("Allowed Sequences: [{0}]", allowedOrderIdxes.Select(a => a.Select(b => b + 1).Join(",")).Join("];["));
		StartCoroutine(HandleResetAnim());
	}

    protected override IEnumerator HandleSolveAnim(float delay = 0.5F, int repeatCount = 5)
    {
		moduleSolved = true;
		stageLights.First().enabled = true;
		for (var x = 0; x < keyRenderers.Length; x++)
		{
			keyRenderers[x].material.color = Color.black;
			keyTexts[x].text = "";
		}
		for (var x = 0; x < keyRenderers.Length; x++)
		{
			yield return new WaitForSeconds(delay);
			keyRenderers[x].material.color = Color.white;
			keyRenderers[x].material.mainTexture = kanyeImg;
			keyTexts[x].color = Color.black;
		}
		modSelf.HandlePass();
	}

    protected override IEnumerator HandleResetAnim(float delay = 0.5F, int repeatCount = 5)
    {
		for (var x = 0; x < keyRenderers.Length; x++)
		{
			keyRenderers[x].material.color = Color.black;
			keyRenderers[x].material.mainTexture = null;
			keyTexts[x].text = "";
			keySelectables[x].transform.localPosition = Vector3.back;
			buttonsPressed[x] = true;
		}
		for (var x = 0; x < keyRenderers.Length; x++)
		{
			yield return new WaitForSeconds(delay);
			if (!hasToggled)
			{
				keyRenderers[x].material.color = Color.white;
				keyRenderers[x].material.mainTexture = kanyeAlbumImgs[idxAlbums[x]];
			}
			else
            {
				var keyRef = keyTypes[x];
				SetKeyVisuals(x,
					alphabet.IndexOf(keyRef[0]),
					alphabet.IndexOf(keyRef[1]),
					alphabet.IndexOf(keyRef[2]));
				// ... To be used to set the key's property here.
				keySelectables[x].transform.localPosition = Vector3.zero;
				buttonsPressed[x] = false;
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
			}
		}
		interactable = true;
	}
    protected override void SetKeyVisuals(int keyIdx, int idxColor, int idxTxtClr, int idxTxtDisp)
    {
		if (keyIdx < 0 || keyIdx >= Mathf.Min(keyRenderers.Length, keyTexts.Length)) return;

		var pickedText = possibleDisplayTexts[idxTxtDisp];
		keyRenderers[keyIdx].material.color = possibleMatColors[idxColor];
		keyTexts[keyIdx].color = possibleColorTexts[idxTxtClr];
		keyTexts[keyIdx].text = colorblindDetected ? string.Format("{0}\n{1}\n\n{2}", pickedText, possibleCBTexts[idxTxtClr], possibleCBTexts[idxColor]) : pickedText;
	}
	private readonly string TwitchHelpMessage = "!{0} press 0123 [position in reading order, \"press\" optional, 0 = display] | !{0} colorblind/colourblind/cb";

}
