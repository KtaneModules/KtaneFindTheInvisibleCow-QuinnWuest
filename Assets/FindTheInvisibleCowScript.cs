using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class FindTheInvisibleCowScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] CowSels;
    public GameObject CowQuad;
    public Material[] CowMats;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private bool[] _highlightedCells = new bool[324];
    private bool _foundCow;
    private Coroutine _cowCowCow;
    private int[] _cowSoundCells = new int[324];
    private int _goalCow;

    private bool TwitchPlaysActive;
    public GameObject CursorObj;
    private int _cPos;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += delegate ()
        {
            if (TwitchPlaysActive)
            {
                CursorObj.SetActive(true);
                while (_cPos % 18 == 0 || _cPos % 18 == 17 || _cPos / 18 == 0 || _cPos / 18 == 17)
                    _cPos = Rnd.Range(0, 324);
                CursorObj.transform.localPosition = new Vector3(0.01f * (_cPos % 18) - 0.085f, 0.0153f, -0.01f * (_cPos / 18) + 0.085f);
            }
        };
        _cowCowCow = StartCoroutine(CowCowCow());
        while (_goalCow % 18 < 2 || _goalCow % 18 > 16 || _goalCow / 18< 2 || _goalCow / 18 > 16)
            _goalCow = Rnd.Range(0, 324);
        Debug.LogFormat("[Find The Invisible Cow #{0}] Goal: {1}", _moduleId, _goalCow);
        GetAdjacents();
        CowQuad.transform.localPosition = new Vector3(0.01f * (_goalCow % 18) - 0.085f, 0.0151f, -0.01f * (_goalCow / 18) + 0.085f);
        CowQuad.transform.localScale = new Vector3(0, 0, 0);
        for (int i = 0; i < CowSels.Length; i++)
        {
            CowSels[i].OnInteract += CowPress(i);
            CowSels[i].OnHighlight += CowHighlight(i);
            CowSels[i].OnHighlightEnded += CowHighlightEnded(i);
        }
    }

    private KMSelectable.OnInteractHandler CowPress(int i)
    {
        return delegate ()
        {
            if (_foundCow || _moduleSolved)
                return false;
            Debug.LogFormat("[Find The Invisible Cow #{0}] Pressed {1}.", _moduleId, i);
            if (i != _goalCow)
                Module.HandleStrike();
            else
            {
                _foundCow = true;
                if (_cowCowCow != null)
                    StopCoroutine(_cowCowCow);
                StartCoroutine(MooveCow());
            }
            return false;
        };
    }

    private Action CowHighlight(int i)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return;
            _highlightedCells[i] = true;
        };
    }

    private Action CowHighlightEnded(int i)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return;
            _highlightedCells[i] = false;
        };
    }

    private IEnumerator CowCowCow()
    {
        while (!_foundCow)
        {
            if (_highlightedCells.Contains(true) && Array.IndexOf(_highlightedCells, true) != -1)
            {
                int ix = _cowSoundCells[Array.IndexOf(_highlightedCells, true)];
                Audio.PlaySoundAtTransform("cow" + ix, transform);
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void GetAdjacents()
    {
        for (int i = 0; i < 324; i++)
        {
            if (i == _goalCow)
                _cowSoundCells[i] = 4;
            else if ((Math.Abs((i % 18) - (_goalCow % 18)) < 2) && (Math.Abs((i / 18) - (_goalCow / 18)) < 2))
                _cowSoundCells[i] = 3;
            else if ((Math.Abs((i % 18) - (_goalCow % 18)) < 3) && (Math.Abs((i / 18) - (_goalCow / 18)) < 3))
                _cowSoundCells[i] = 2;
            else if ((Math.Abs((i % 18) - (_goalCow % 18)) < 4) && (Math.Abs((i / 18) - (_goalCow / 18)) < 4))
                _cowSoundCells[i] = 1;
            else
                _cowSoundCells[i] = 0;
        }
    }

    private IEnumerator MooveCow()
    {
        CursorObj.SetActive(false);
        var duration = 0.7f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            CowQuad.transform.localPosition = new Vector3(Easing.InOutQuad(elapsed, 0.01f * (_goalCow % 18) - 0.085f, 0, duration), 0.0151f, Easing.InOutQuad(elapsed, -0.01f * (_goalCow / 18) + 0.085f, 0, duration));
            CowQuad.transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0, 0.15f, duration), Easing.InOutQuad(elapsed, 0, 0.15f, duration), Easing.InOutQuad(elapsed, 0, 0.15f, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        yield return new WaitForSeconds(0.7f);
        Audio.PlaySoundAtTransform("moo", transform);
        Module.HandlePass();
        _moduleSolved = true;
        CowQuad.GetComponent<MeshRenderer>().material = CowMats[1];
        yield return new WaitForSeconds(0.2f);
        CowQuad.GetComponent<MeshRenderer>().material = CowMats[0];
    }

    private bool _constantShouting;

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} move urdl [Move the cursor up, right, down, or left.] | !{0} focus 10 [Focus on the module for 10 seconds.] | !{0} toggle [Toggle the constant shouting] !{0} submit [Submit at the current position.]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (Regex.Match(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success)
        {
            yield return null;
            CowSels[_cPos].OnInteract();
            yield break;
        }
        if (Regex.Match(command, @"^\s*toggle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success)
        {
            yield return null;
            _constantShouting = !_constantShouting;
            _highlightedCells[_cPos] = _constantShouting;
            yield break;
        }
        var parameters = command.Split(' ');
        if (Regex.Match(parameters[0], @"^\s*focus\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success && parameters.Length == 2)
        {
            int val;
            if (!int.TryParse(parameters[1], out val) || val < 1)
                yield break;
            if (val > 30)
            {
                yield return "sendtochaterror Surely anything over 30 seconds is too much time!";
                yield break;
            }
            yield return null;
            yield return "trycancel";
            var duration = (float)val;
            var elapsed = 0f;
            _highlightedCells[_cPos] = true;
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            _highlightedCells[_cPos] = _constantShouting;
        }
        if (command.StartsWith("move "))
            command = command.Substring(4);
        var list = new List<int>();
        for (int i = 0; i < command.Length; i++)
        {
            int ix = "urdl ".IndexOf(command[i]);
            if (ix == 4)
                continue;
            if (ix == -1)
                yield break;
            list.Add(ix);
        }
        yield return null;
        for (int i = 0; i < list.Count; i++)
        {
            if (_cPos / 18 == 0 && list[i] == 0 || _cPos % 18 == 17 && list[i] == 1 || _cPos / 18 == 17 && list[i] == 2 || _cPos % 18 == 0 && list[i] == 3)
            {
                var arr = new string[] { "UP", "RIGHT", "DOWN", "LEFT" };
                yield return "sendtochaterror After " + i + " moves, you attempted to move the cursor " + arr[list[i]] + ", which would have moved you off the grid. Stopping command.";
                _highlightedCells[_cPos] = _constantShouting;
                yield break;
            }
            var oldPos = _cPos;
            _cPos = list[i] == 0 ? (_cPos - 18) : list[i] == 1 ? (_cPos + 1) : list[i] == 2 ? (_cPos + 18) : (_cPos - 1);
            var duration = 0.25f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                CursorObj.transform.localPosition =
                    new Vector3(
                        Mathf.Lerp(0.01f * (oldPos % 18) - 0.085f, 0.01f * (_cPos % 18) - 0.085f, elapsed / duration),
                        0.0153f,
                        Mathf.Lerp(-0.01f * (oldPos / 18) + 0.085f, -0.01f * (_cPos / 18) + 0.085f, elapsed / duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _highlightedCells[oldPos] = false;
            _highlightedCells[_cPos] = true;
            CursorObj.transform.localPosition = new Vector3(0.01f * (_cPos % 18) - 0.085f, 0.0153f, -0.01f * (_cPos / 18) + 0.085f);
        }
        if (!_constantShouting)
            _highlightedCells[_cPos] = false;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (_cPos % 18 < _goalCow % 18)
        {
            int g = (_cPos + 1);
            var duration = 0.25f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                
                CursorObj.transform.localPosition = new Vector3( Mathf.Lerp(0.01f * (_cPos % 18) - 0.085f, 0.01f * (g % 18) - 0.085f, elapsed / duration), 0.0153f, Mathf.Lerp(-0.01f * (_cPos / 18) + 0.085f, -0.01f * (g / 18) + 0.085f, elapsed / duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _highlightedCells[_cPos] = false;
            _highlightedCells[g] = true;
            _cPos = g;
        }
        while (_cPos % 18 > _goalCow % 18)
        {
            int g = (_cPos - 1);
            var duration = 0.25f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                CursorObj.transform.localPosition = new Vector3(Mathf.Lerp(0.01f * (_cPos % 18) - 0.085f, 0.01f * (g % 18) - 0.085f, elapsed / duration), 0.0153f, Mathf.Lerp(-0.01f * (_cPos / 18) + 0.085f, -0.01f * (g / 18) + 0.085f, elapsed / duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _highlightedCells[_cPos] = false;
            _highlightedCells[g] = true;
            _cPos = g;
        }
        while (_cPos / 18 < _goalCow / 18)
        {
            int g = (_cPos + 18);
            var duration = 0.25f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                CursorObj.transform.localPosition = new Vector3(Mathf.Lerp(0.01f * (_cPos % 18) - 0.085f, 0.01f * (g % 18) - 0.085f, elapsed / duration), 0.0153f, Mathf.Lerp(-0.01f * (_cPos / 18) + 0.085f, -0.01f * (g / 18) + 0.085f, elapsed / duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _highlightedCells[_cPos] = false;
            _highlightedCells[g] = true;
            _cPos = g;
        }
        while (_cPos / 18 > _goalCow / 18)
        {
            int g = (_cPos - 18);
            var duration = 0.25f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                CursorObj.transform.localPosition = new Vector3(Mathf.Lerp(0.01f * (_cPos % 18) - 0.085f, 0.01f * (g % 18) - 0.085f, elapsed / duration), 0.0153f, Mathf.Lerp(-0.01f * (_cPos / 18) + 0.085f, -0.01f * (g / 18) + 0.085f, elapsed / duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _highlightedCells[_cPos] = false;
            _highlightedCells[g] = true;
            _cPos = g;
        }
        yield return new WaitForSeconds(0.6f);
        CowSels[_cPos].OnInteract();
        while (!_moduleSolved)
            yield return true;
    }
}
