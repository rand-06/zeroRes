using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class script : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo bombInfo;
    public FakeStatusLight FakeStatusLight;
    public KMSelectable Cover;
    public KMSelectable Status;
    public TextMesh Center;
    public KMAudio Audio;
    public AudioClip[] clips = new AudioClip[3];
    public KMSelectable[] corners = new KMSelectable[4];

    private bool highlighted = false;
    private int globalTimer = 300;
    private int maxTimer = 300;
    private int unTimer = 0;
    private bool stop = false;
    private int timerDelay = 0;
    private int stage = 7;
    private bool sleepingMode;
    private int penalty = 75;
    private bool READY = false;
    private bool FirstStageDone = false;
    private int moduleNumber = 128;
    int timeNum = DateTime.Now.Hour * 100 + DateTime.Now.Minute;
    bool night;

    string divideByTen(int num)
    {
        return (num / 10).ToString() + "." + (num % 10).ToString();
    }
    bool primeDigit(int digit)
    {
        List<int> list = new List<int> { 2, 3, 5, 7 };
        return list.Contains(digit);
    }
    void FakeStrike()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, Module.transform);
        FakeStatusLight.FlashStrike();
    }
    IEnumerator timerChange()
    {
        while (globalTimer > 0)
        {
            yield return new WaitForSeconds(.1f);
            if (highlighted) globalTimer--;
            else unTimer++;
            if (unTimer > 29)
            {
                unTimer = 0;
                globalTimer++;
                if (globalTimer > maxTimer) maxTimer = globalTimer;
            }
            Center.text = divideByTen(globalTimer);

            if (highlighted)
            {
                if (!stop && (globalTimer > maxTimer / 2))
                {
                    if (timerDelay == 0)
                    {
                        timerDelay = (int)(UnityEngine.Random.Range(2, 8) * ((float)Math.Log(2 * maxTimer / globalTimer - 1) / 2 * 20 + 1));
                        if (UnityEngine.Random.Range(0f, 1f) < (.5f - Math.Sqrt(globalTimer / 2 / maxTimer)))
                            corners[UnityEngine.Random.Range(0,4)].AddInteractionPunch((float)Math.Sqrt(UnityEngine.Random.Range(0f, 36f)));
                        if (UnityEngine.Random.Range(0f, 1f) < (.5f - Math.Sqrt(globalTimer / 2 / maxTimer)))
                            FakeStrike();
                    }
                    else timerDelay--;
                }
                else if (!stop && (globalTimer <= maxTimer / 2)) stop = true;
            }
            yield return null;
        }
    }
    void roll(int chance)
    {
        if (UnityEngine.Random.Range(1, chance + 1) == 1)
        {
            Center.color = Color.green;
            READY = true;
        }
    }
    bool isPrime(int num) // works up to num = 5959.
    {
        foreach (int i in new int[]{ 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73 }) if (num % i == 0) return false;
        return true;
    }
    bool isPalimdrome(int num)
    {
        if (num > 999)
        {
            return (num / 1000 == num % 10) && (num / 10 % 10 == num % 1000 / 100);
        }
        else if (num > 99)
        {
            return (num / 100 == num % 10);
        }
        else if (num > 9)
        {
            return (num / 10 == num % 10);
        }
        else return true;
    }

    IEnumerator solve()
    {
        Audio.PlaySoundAtTransform(clips[2].name, transform);
        Center.text = night ? "Good night." : "You can do it.";
        yield return new WaitForSeconds(1f);
        Module.HandlePass();
        FakeStatusLight.HandlePass();
        Center.text = "Zero\nResistance";
        yield return null;
    }
    bool ans()
    {
        int time = (int)bombInfo.GetTime();
        int seconds = time % 60;
        int minutes = time / 60 % 60;
        switch (stage)
        {
            case 7: return true;
            case 6: return (seconds % 10) == bombInfo.GetSerialNumberNumbers().Last();
            case 5: return primeDigit(seconds / 10) ^ primeDigit(seconds % 10);
            case 4: return isPalimdrome(minutes * 100 + seconds);
            case 3: return (bombInfo.GetSerialNumberNumbers().Sum() * bombInfo.GetBatteryCount() % 60) == seconds;
            case 2: return time % bombInfo.GetSerialNumberLetters().Sum(i => "ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(i)+1) == 0;
            case 1: return isPrime(minutes * 100 + seconds);
        }
        return true;
    }
    void strikeSleep()
    {
        Module.HandleStrike();
        FakeStatusLight.FlashStrike();
        sleepingMode = false;
        penalty = 100;
        globalTimer = 900;
        maxTimer = 900;
        Center.color = Color.white;
        Center.text = "128";
        stage = 7;
    }
    void Check(bool status = false)
    {
        if (globalTimer == 0 && status)
        {
            StartCoroutine(solve());
            return;
        }
        if (FirstStageDone) return;
        if (!status) Audio.PlaySoundAtTransform(clips[0].name, transform);
        if (sleepingMode)
        {
            switch (stage)
            {
                case 7:
                    {
                        if (!status)
                        {
                            Center.color = Color.yellow;
                            stage--;
                            Center.text = "64";
                        }
                        else
                        {
                            strikeSleep();
                        }
                        return;
                    }
                case 6:
                    {
                        int second = (int)bombInfo.GetTime() % 10;
                        if (((second + '0') == bombInfo.GetSerialNumber().ToString()[5]) && !status)
                        {
                            stage--;
                            Center.text = "32";
                        }
                        else
                        {
                            strikeSleep();
                        }
                        return;
                    }
                case 5:
                    {
                        if (((int)bombInfo.GetTime() % 60 == 0) && status)
                        {
                            stage--;
                            Center.text = "?";
                            Audio.PlaySoundAtTransform(clips[1].name, transform);
                        }
                        else
                        {
                            strikeSleep();
                        }
                        return;
                    }
                case 4:
                    {
                        int seconds = (int)bombInfo.GetTime() % 60;
                        if ((primeDigit(seconds / 10) ^ primeDigit(seconds % 10)) && !status)
                        {
                            globalTimer = 600;
                            Center.color = Color.white;
                            maxTimer = globalTimer;
                            StartCoroutine(timerChange());
                            FirstStageDone = true;
                        }
                        else
                        {
                            strikeSleep();
                            Audio.PlaySoundAtTransform(clips[2].name, transform);
                        }
                        return;
                    }
            }
        }
        else
        {
            if (READY && status)
            {
                FirstStageDone = true;
                Center.color = Color.white;
                maxTimer = globalTimer;
                StartCoroutine(timerChange());
                Audio.PlaySoundAtTransform(clips[1].name, transform);
                FirstStageDone = true;
                return;
            }
            else if (READY ^ status)
            {
                Module.HandleStrike();
                FakeStatusLight.FlashStrike();
                globalTimer += penalty;
                return;
            }

            if (ans())
            {
                stage--;
                moduleNumber /= 2;
                Center.text = moduleNumber.ToString();
                roll(moduleNumber);
            }
            else
            {
                Module.HandleStrike();
                FakeStatusLight.FlashStrike();
                globalTimer += penalty;
                stage++;
                moduleNumber *= 2;
                Center.text = moduleNumber.ToString();
            }
            return;
        }
    }

    void Awake()
    {
        GetComponent<KMSelectable>().OnHighlight += delegate () { highlighted = true; };
        GetComponent<KMSelectable>().OnHighlightEnded += delegate () { highlighted = false; };
        Cover.OnInteract += delegate() { Check(); return true; };
        Status.OnInteract += delegate () { Check(true); return true; };
        night = (timeNum > 99 && timeNum < 501);
        sleepingMode = UnityEngine.Random.Range(0, night ? 1 : 16) == 0;

        FakeStatusLight = Instantiate(FakeStatusLight);
        FakeStatusLight.GetStatusLights(transform);
        FakeStatusLight.Module = Module;
    }
}
