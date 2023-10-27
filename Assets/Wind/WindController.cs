using System.Collections.Generic;
using UnityEngine;
using wwc.utilities;
using SimpleJSON;
using System;

#region Data Structure

// total size: 5*4 = 20byte
[System.Serializable]
struct WindStruct
{
    public int WID;
    public float WindProgress;
    public float WindWidth;
    public float NoiseWidth;
    public float DeltaTime;
}

#endregion

public class WindController : SingletonMonoBehaviour<WindController>
{
    #region Origin Version

    // [SerializeField] private Material wallEffect;
    // [SerializeField] private float windDuration = 5f;

    // float windProgress = 0;
    // Tween windAnimation = null;


    // public void SetConfig(JSONNode json)
    // {
    //    try
    //    {

    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogException(e);
    //    }
    // }

    // void SetEffect()
    // {
    //    wallEffect.SetFloat("_WindProgress", windProgress);
    // }
    #endregion

    const int Wind_MAX = 10;

    [SerializeField] RenderTexture WindResult;

    [Header("Wind settings")]
    [SerializeField] private float windDuration = 5f;
    [SerializeField] private float windWidth = 500;
    [SerializeField] private float noiseWidth = 80;

    // wind buffer
    WindStruct[] winds;

    // find valid index in ripple buffer
    private int ID
    {
        get
        {
            for (int i = 0; i < Wind_MAX; i++)
            {
                if (winds[i].WID == -1) return i;
            }
            return -1;
        }
    }

    // Compute Shader 
    [Header("Compute Shader")]
    public ComputeShader windShader;
    private ComputeBuffer windsBuffer;
    int _kernelIndex;
    RenderTexture tex;

    [Header("Resolution")]
    public Vector2 ScreenRes;

    void RunLight()
    {
        int wid = ID;

        // print(rid);
        WindStruct wind;
        wind.WID = wid;
        wind.WindProgress = 0;
        wind.WindWidth = windWidth;
        wind.NoiseWidth = noiseWidth;
        wind.DeltaTime = Time.fixedDeltaTime / windDuration / 100;

        winds[wid] = wind;
    }

    public void ReleaseList(int wid)
    {
        for (int i = 0; i < Wind_MAX; i++)
        {
            if (winds[i].WID == wid)
            {
                WindStruct wind;
                wind.WID = -1;
                wind.WindProgress = 0;
                wind.WindWidth = 0;
                wind.NoiseWidth = 0;
                wind.DeltaTime = 0;
                winds[i] = wind;
            }
        }
    }

    void InitializeComputeShader()
    {
        // Initialize
        winds = new WindStruct[Wind_MAX];
        for (int i = 0; i < Wind_MAX; i++)
        {
            WindStruct wind;
            wind.WID = -1;
            wind.WindProgress = 0;
            wind.WID = -1;
            wind.WindWidth = 0;
            wind.NoiseWidth = 0;
            wind.DeltaTime = 0;
            winds[i] = wind;
        }
        if (windsBuffer == null) windsBuffer = new ComputeBuffer(Wind_MAX, 20);

        tex = new RenderTexture((int)ScreenRes.x, (int)ScreenRes.y, 24);
        tex.enableRandomWrite = true;
        tex.Create();
    }

    void RunShader()
    {
        _kernelIndex = windShader.FindKernel("CSMain");

        for (int i = 0; i < winds.Length; i++)
        {
            if (winds[i].WID != -1)
            {
                // edit here
                if (winds[i].WindProgress >= 1) // end of wind
                {
                    ReleaseList(winds[i].WID);
                }
            }
        }

        windsBuffer.SetData(winds);
        windShader.SetBuffer(_kernelIndex, "windBuffer", windsBuffer);
        windShader.SetInt("WindFlod", 1);
        windShader.SetFloat("Time", Time.time);
        windShader.SetVector("Resolution", new Vector2((int)ScreenRes.x, (int)ScreenRes.y));
        windShader.SetTexture(_kernelIndex, "Result", tex);
        windShader.Dispatch(_kernelIndex, (int)ScreenRes.x / 8, (int)ScreenRes.y / 8, 1);

        windsBuffer.GetData(winds);
        Graphics.CopyTexture(tex, WindResult);
    }

    public void OnWindReceive(int i)
    {
        RunLight();
    }

    private void Start()
    {
        InitializeComputeShader();
    }

    private void Update()
    {
        RunShader();

        if (Input.GetMouseButtonDown(0)) OnWindReceive(1);
    }

    private void OnDisable()
    {
        windsBuffer.Release();
    }
}
