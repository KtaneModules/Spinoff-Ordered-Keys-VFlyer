using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class ShorderedKeysScript : OKeysBaseScript {

    public TextMesh counterDisplay;
    public KMBombInfo bombInfo;
    int lastSolveCount = 0, curSetShown = 6, lastIdxKeyPressed = -1, idxValHidden;
    Dictionary<int, List<int>> storedKeyGroupIdxes = new Dictionary<int, List<int>>();
    static int modIDCnt;
    const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int[] baseKeyPosSub, storedIdxKeysSub;
    List<int> sequenceExpected, sequencePressed;
    bool inSubmission = false;

    protected override void Start()
    {
        moduleID = ++modIDCnt;
        sequenceExpected = new List<int>();
        sequenceExpected.AddRange(Enumerable.Range(0, 6));
        sequencePressed = new List<int>();
        storedIdxKeysSub = new int[6];
        var serialNo = bombInfo.GetSerialNumber();
        baseKeyPosSub = serialNo.Select(a => base36.IndexOf(a) % 6).ToArray();
        QuickLogDebug("Base idx pos with 0 solved Shordered Keys: {0}", baseKeyPosSub.Join());
        buttonsPressed = new bool[6];
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
        for (var x = 0; x < stageLights.Length; x++)
            stageLights[x].enabled = false;
        ResetModule();
    }

    protected override void HandleIdxPress(int idx)
    {
        keySelectables[idx].AddInteractionPunch();
        mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, keySelectables[idx].transform);
        // Handle submission presses here.
        if (inSubmission)
        {
            buttonsPressed[idx] = true;
            keySelectables[idx].transform.localPosition = Vector3.back;
            sequencePressed.Add(idx);
            if (buttonsPressed.All(a => a))
                if (sequenceExpected.SequenceEqual(sequencePressed))
                {
                    moduleSolved = true;
                    mAudio.PlaySoundAtTransform("InputCorrect", transform);
                    StartCoroutine(HandleSolveAnim());
                }
                else
                {
                    QuickLog("The keys were pressed in this order: {0}", sequencePressed.Select(a => a + 1).Join(""));
                    modSelf.HandleStrike();
                    ResetModule();
                }
            return;
        }
        // Assign the keys to set the counters to such.
        var counterVals = Enumerable.Range(0, 7).ToList();
        counterVals.Remove(curSetShown); // Don't repeat the same set of keys.
        counterVals.Remove(idxValHidden);
        var curKeyPosSubMod6 = baseKeyPosSub.Select(a => (a + bombInfo.GetSolvedModuleIDs().Count(b => b == modSelf.ModuleType)) % 6).ToArray();
        var curKeyPosAssign = curKeyPosSubMod6[curSetShown - 1];
        var curAction = new int[6];
        while (counterVals.Any())
        {
            if (!buttonsPressed[curKeyPosAssign])
            {
                curAction[curKeyPosAssign] = counterVals[0];
                counterVals.RemoveAt(0);
            }
            curKeyPosAssign = (curKeyPosAssign + 1) % 6;
        }
        // Then set the key to the new value based on what's pressed;
        curSetShown = curAction[idx];
        
        buttonsPressed[lastIdxKeyPressed] = false;
        keySelectables[lastIdxKeyPressed].transform.localPosition = Vector3.zero;
        lastIdxKeyPressed = idx;
        buttonsPressed[idx] = true;
        keySelectables[idx].transform.localPosition = Vector3.back;
        interactable = false;
        inSubmission = curSetShown == 0;
        counterDisplay.text = curSetShown.ToString();
        StartCoroutine(inSubmission ? HandleSubmitAnim() : HandleResetAnim());
    }

    void ResetModule()
    {
        inSubmission = false;
        storedKeyGroupIdxes.Clear();
        sequencePressed.Clear();
        // Generate keys corresponding to values 1-6 respectively.
        var allPossibleKeyIdxes = Enumerable.Range(0, 216).ToList();
        allPossibleKeyIdxes.Shuffle();
        var pickedKeyIdxes = allPossibleKeyIdxes.Take(36);
        for (var x = 0; x < 6; x++)
        {
            var y = x + 1;
            var newStoredIdxes = new List<int>();
            newStoredIdxes.AddRange(pickedKeyIdxes.Skip(6 * x).Take(6));
            storedKeyGroupIdxes.Add(y, newStoredIdxes);
        }
        QuickLogDebug("{0}", storedKeyGroupIdxes.Select(a => string.Format("[{0}: {1}]", a.Key, a.Value.Join(",")) ).Join(";"));
        QuickLog("After {1} reset{0}:", resetCount == 1 ? "" : "s", resetCount++);
        foreach (var keysAssigned in storedKeyGroupIdxes)
            QuickLog("These keys are assigned to a value of {0}: {1}", keysAssigned.Key,
                keysAssigned.Value.Select(a => string.Format("{0}{1}{2}", possibleCBTexts[a % 6], possibleDisplayTexts[a / 36 % 6], possibleCBTexts[a / 6 % 6])).Join(", "));
        // Generate valid keys for submission.
        //var remainingKeyIdxes = allPossibleKeyIdxes.Skip(36);
        sequenceExpected.Shuffle();
        idxValHidden = Random.Range(1, 7);
        QuickLog("Keys with a value of {0} are not initially shown on the module.", idxValHidden);
        for (var x = 0; x < 6; x++)
        {
            var idxExp = sequenceExpected.IndexOf(x);
            storedIdxKeysSub[x] = storedKeyGroupIdxes[idxExp + 1].PickRandom();
        }
        QuickLogDebug("{0}", storedIdxKeysSub.Join(", "));
        QuickLog("The keys displayed when the counter reaches 0 are {0}", storedIdxKeysSub.Select(a => string.Format("{0}{1}{2}", possibleCBTexts[a % 6], possibleDisplayTexts[a / 36 % 6], possibleCBTexts[a / 6 % 6])).Join(", "));
        QuickLog("The keys should be pressed in this order by position: {0}", sequenceExpected.Select(a => a + 1).Join(""));
        curSetShown = Enumerable.Range(1, 6).Where(a => a != idxValHidden).PickRandom();
        lastIdxKeyPressed = Random.Range(0, 6);
        for (var x = 0; x < buttonsPressed.Length; x++)
            buttonsPressed[x] = x == lastIdxKeyPressed;
        counterDisplay.text = curSetShown.ToString();
        interactable = false;
        StartCoroutine(HandleResetAnim());
    }

    private IEnumerator HandleSubmitAnim(float delay = 0.1F, int repeatCount = 5)
    {
        for (var x = 0; x < keyRenderers.Length; x++)
        {
            for (var cnt = 0; cnt < repeatCount; cnt++)
            {
                for (var y = x; y < keyRenderers.Length; y++)
                    RandomizeKeyVisuals(y);
                yield return new WaitForSeconds(delay);
            }
            var keyIdxCur = storedIdxKeysSub[x];
            SetKeyVisuals(x, keyIdxCur % 6, keyIdxCur / 6 % 6, keyIdxCur / 36 % 6);
            if (buttonsPressed[x])
            {
                keySelectables[x].transform.localPosition = Vector3.zero;
                buttonsPressed[x] = false;
                mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
            }
        }
        interactable = true;
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
            var keyIdxesCur = storedKeyGroupIdxes[curSetShown];
            SetKeyVisuals(x, keyIdxesCur[x] % 6, keyIdxesCur[x] / 6 % 6, keyIdxesCur[x] / 36 % 6);
            if (!buttonsPressed[x] && keySelectables[x].transform.localPosition != Vector3.zero)
            {
                keySelectables[x].transform.localPosition = Vector3.zero;
                mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
            }
            else if (buttonsPressed[x] && keySelectables[x].transform.localPosition != Vector3.back)
            {
                keySelectables[x].transform.localPosition = Vector3.back;
                mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            }
        }
        interactable = true;
    }
    protected override void SetKeyVisuals(int keyIdx, int idxMat, int idxTxtClr, int idxTxtDisp)
    {
        if (keyIdx < 0 || keyIdx >= Mathf.Min(keyRenderers.Length, keyTexts.Length)) return;

        var pickedText = possibleDisplayTexts[idxTxtDisp];
        keyRenderers[keyIdx].material.color = possibleMatColors[idxMat];
        keyTexts[keyIdx].color = possibleColorTexts[idxTxtClr];
        keyTexts[keyIdx].text = colorblindDetected ? string.Format("{0}\n{1}\n\n{2}", pickedText, possibleCBTexts[idxTxtClr], possibleCBTexts[idxMat]) : pickedText;
    }
    protected override void RandomizeKeyVisuals(int keyIdx)
    {
        if (keyIdx < 0 || keyIdx >= Mathf.Min(keyRenderers.Length, keyTexts.Length)) return;

        var pickedIDxMat = Random.Range(0, possibleMatColors.Length);
        var pickedIDxClr = Random.Range(0, possibleColorTexts.Length);
        var pickedText = possibleDisplayTexts.PickRandom();

        keyRenderers[keyIdx].material.color = possibleMatColors[pickedIDxMat];
        keyTexts[keyIdx].color = possibleColorTexts[pickedIDxClr];
        keyTexts[keyIdx].text = colorblindDetected ? string.Format("{0}\n{1}\n\n{2}", pickedText, possibleCBTexts[pickedIDxClr], possibleCBTexts[pickedIDxMat]) : pickedText;
    }

    protected override IEnumerator HandleSolveAnim(float delay = 0.1F, int repeatCount = 3)
    {
        for (var x = 0; x < keyRenderers.Length; x++)
        {
            for (var cnt = 0; cnt < repeatCount; cnt++)
            {
                for (var y = x; y < keyRenderers.Length; y++)
                    RandomizeKeyVisuals(y);
                yield return new WaitForSeconds(delay);
            }
            stageLights[x].enabled = true;
            keyRenderers[x].material.color = Color.white;
            keyTexts[x].text = "0";
            keyTexts[x].color = Color.black;
            counterDisplay.text = "";
        }
        mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        modSelf.HandlePass();
    }
    private readonly string TwitchHelpMessage = "!{0} press 123456 [position in reading order, \"press\" optional] | !{0} colorblind/colourblind/cb";

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
            var validPressCmd = rgxPress.Value.ToLowerInvariant().Trim();
            if (validPressCmd.StartsWith("start"))
                validPressCmd = validPressCmd.Substring(5).Trim();
            var validPressCmdParts = validPressCmd.Split();
            var validDigits = "123";
            var allIdxes = new List<int>();
            foreach (var valPart in validPressCmdParts)
            {
                foreach (var chr in valPart)
                {
                    if (!validDigits.Contains(chr))
                    {
                        yield return string.Format("sendtochaterror The corresponding character \"{0}\" is not a valid digit!", chr);
                        yield break;
                    }
                    allIdxes.Add(validDigits.IndexOf(chr));
                }
            }
            if (allIdxes.Any())
            {
                yield return null;
                foreach (var idx in allIdxes)
                {
                    keySelectables[idx].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                if (moduleSolved)
                    yield return "solve";
            }
        }
    }

    protected override IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            while (!interactable)
                yield return true;
            if (!inSubmission)
            {
                var curKeyPosSubMod6 = baseKeyPosSub.Select(a => (a + bombInfo.GetSolvedModuleIDs().Count(b => b == modSelf.ModuleType)) % 6).ToArray();
                var curPosEnterSub = curKeyPosSubMod6[curSetShown - 1];
                keySelectables[(buttonsPressed[curPosEnterSub] ? (curPosEnterSub + 1) : curPosEnterSub) % keySelectables.Length].OnInteract();
                while (!interactable)
                    yield return true;
            }
            if (sequencePressed.Any() && !sequenceExpected.Take(sequencePressed.Count).SequenceEqual(sequencePressed))
            {
                yield return HandleSolveAnim(delay: 0.1f, repeatCount: 10);
                yield break;
            }
            while (sequencePressed.Count < sequenceExpected.Count)
            {
                keySelectables[sequenceExpected[sequencePressed.Count]].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        while (moduleSolved)
            yield return true;
    }

    void Update()
    {
        var curSolves = bombInfo.GetSolvedModuleIDs().Count;
        if (curSolves != lastSolveCount && interactable && !moduleSolved)
        {
            lastSolveCount = curSolves;
            QuickLog("Solve count changed, resetting module...");
            ResetModule();
        }
    }
}
