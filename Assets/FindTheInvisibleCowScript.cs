using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

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
    private bool _isFocused;
    private bool _foundCow;
    private Coroutine _cowCowCow;
    private int[] _cowSoundCells = new int[324];
    private int _goalCow;
    private Vector3 _cowQuadPos;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        Module.GetComponent<KMSelectable>().OnFocus += delegate () { _isFocused = true; };
        Module.GetComponent<KMSelectable>().OnDefocus += delegate () { _isFocused = false; };
        _cowCowCow = StartCoroutine(CowCowCow());
        _goalCow = Rnd.Range(0, 324);
        Debug.LogFormat("[Find The Invisible Cow #{0}] Goal: {1}", _moduleId, _goalCow);
        GetAdjacents();
        _cowQuadPos = new Vector3(0.01f * (_goalCow % 18) - 0.085f, 0.0151f, -0.01f * (_goalCow / 18) + 0.085f);
        CowQuad.transform.localPosition = _cowQuadPos;
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
            if (_isFocused && Array.IndexOf(_highlightedCells, true) != -1)
            {
                int ix = _cowSoundCells[Array.IndexOf(_highlightedCells, true)];
                Audio.PlaySoundAtTransform("cow" + ix, transform);
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void GetAdjacents()
    {
        // _cowSoundCells[_goalCow] = 4;
        // if (_goalCow % 18 > 0)
        //     _cowSoundCells[_goalCow - 1] = 3;
        // if (_goalCow % 18 < 17)
        //     _cowSoundCells[_goalCow + 1] = 3;
        // if (_goalCow / 18 > 0)
        //     _cowSoundCells[_goalCow - 18] = 3;
        // if (_goalCow / 18 < 17)
        //     _cowSoundCells[_goalCow + 18] = 3;
        // if (_goalCow % 18 > 0 && _goalCow / 18 > 0)
        //     _cowSoundCells[_goalCow - 19] = 3;
        // if (_goalCow % 18 > 0 && _goalCow / 18 < 17)
        //     _cowSoundCells[_goalCow + 17] = 3;
        // if (_goalCow % 18 < 17 && _goalCow / 18 > 0)
        //     _cowSoundCells[_goalCow + 19] = 3;
        // if (_goalCow % 18 < 17 && _goalCow / 18 < 17)
        //     _cowSoundCells[_goalCow - 17] = 3;
        // if (_goalCow % 18 > 1)
        //     _cowSoundCells[_goalCow - 2] = 2;
        // if (_goalCow % 18 < 16)
        //     _cowSoundCells[_goalCow + 2] = 2;
        // if (_goalCow / 18 > 1)
        //     _cowSoundCells[_goalCow - 36] = 2;
        // if (_goalCow / 18 < 16)
        //     _cowSoundCells[_goalCow + 36] = 2;
        // if (_goalCow % 18 > 0 && _goalCow / 18 > 1)
        //     _cowSoundCells[_goalCow - 37] = 2;
        // if (_goalCow % 18 < 17 && _goalCow / 18 > 1)
        //     _cowSoundCells[_goalCow - 35] = 2;
        // if (_goalCow % 18 > 1 && _goalCow / 18 > 0)
        //     _cowSoundCells[_goalCow - 20] = 2;
        // if (_goalCow % 18 < 16 && _goalCow / 18 > 0)
        //     _cowSoundCells[_goalCow - 16] = 2;
        // if (_goalCow % 18 > 1 && _goalCow / 18 < 17)
        //     _cowSoundCells[_goalCow + 16] = 2;
        // if (_goalCow % 18 < 16 && _goalCow / 18 > 1)
        //     _cowSoundCells[_goalCow + 20] = 2;
        // if (_goalCow % 18 > 0 && _goalCow / 18 > 1)
        //     _cowSoundCells[_goalCow - 37] = 2;

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
        var duration = 0.7f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            CowQuad.transform.localPosition = new Vector3(Easing.InOutQuad(elapsed, _cowQuadPos.x, 0, duration), 0.0151f, Easing.InOutQuad(elapsed, _cowQuadPos.z, 0, duration));
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
}
