using System.Diagnostics;
using UnityEngine;

[System.Serializable]
public class Timer
{
    public float duration;
    private float timer = 0f;
    public bool isSetup = false;


    public void Setup() 
    {
        timer = duration;
        isSetup = true;
    }
    public void Tick() // Not Update because we may want to call it manually 
    { 
        if (timer > 0f) timer -= Time.deltaTime; 
    }
    public bool Finished() 
    { 
        if (isSetup && timer <= 0f)
        {
            isSetup = false;
            return true;
        }
        else return false;
    }

}