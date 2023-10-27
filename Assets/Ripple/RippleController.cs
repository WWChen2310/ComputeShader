using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using wwc.utilities;

#region Data Structure

// total size: 7*4 = 28byte
[System.Serializable]
struct RippleStruct
{
    public int RID;
    public Vector2 RipplePos;
    public float RippleProgress;
    public float Radius;
    public float Cover;
    public float DeltaTime;
    public int t;
}

#endregion

public class RippleController : MonoBehaviour
{

    const int Ripple_MAX = 100;

    [Header("Ripples settings")]
    [SerializeField]
    float genRippleDistance = 50; // 50
    [SerializeField]
    Vector2 genRippleIntervalRange; // 0~0.1
    [SerializeField]
    Vector2 genRippleRadiusRange; // 250-300
    [SerializeField]
    Vector2 genRippleCoverRange; // 100-150
    [SerializeField]
    Vector2 genRippleDurationRange; // 1-1

    // ripple buffer
    RippleStruct[] ripples;
    float genRippleInterval;

    // find valid index in ripple buffer
    private int ID
    {
        get
        {
            for (int i = 0; i < Ripple_MAX; i++)
            {
                if (ripples[i].RID == -1) return i;
            }
            return -1;
        }
    }

    // flag for spawnable
    bool spawnFlag = true;

    // Compute Shader 
    [Header("Compute Shader")]

    public ComputeShader rippleShader;
    private ComputeBuffer ripplesBuffer;
    int _kernelIndex;
    
    RenderTexture tex;
    [SerializeField] RenderTexture RippleResult;

    [Header("Resolution")]
    public Vector2 ScreenRes;

    void SpawnRipple(Vector2 pos)
    {
        // data source id
        int rid = ID;
        float r = Random.Range(genRippleRadiusRange.x, genRippleRadiusRange.y);
        float c = Random.Range(genRippleCoverRange.x, genRippleCoverRange.y);
        float d = Random.Range(genRippleDurationRange.x, genRippleDurationRange.y);

        RippleStruct ripple;
        ripple.RID = rid;
        ripple.RipplePos = pos;
        ripple.RippleProgress = 0;
        ripple.Radius = r;
        ripple.Cover = c;

        // !!! Not percise time, depends on update fps
        // if need a correct time parameter, you might have to pass time to shader 
        ripple.DeltaTime = Time.fixedDeltaTime / d / 1500;
        ripple.t = 0;
        ripples[rid] = ripple;
    }

    // check if the position is valid to generate ripple
    bool CheckDistance(Vector2 pos)
    {
        for (int i = 0; i < ripples.Length; i++)
        {
            if (Vector2.Distance(ripples[i].RipplePos, pos) < genRippleDistance)
            {
                // print("too close");
                return false;
            }
        }
        return true;
    }

    public void ReleaseList(int rid)
    {
        for (int i = 0; i < Ripple_MAX; i++)
        {
            if (ripples[i].RID == rid)
            {
                RippleStruct ripple;
                ripple.RID = -1;
                ripple.RipplePos = new Vector2(0, 0);
                ripple.RippleProgress = 0;
                ripple.Radius = 0;
                ripple.Cover = 0;
                ripple.DeltaTime = 0;
                ripple.t = 0;
                ripples[i] = ripple;
            }
        }
    }

    void InitializeComputeShader()
    {
        // Initialize
        ripples = new RippleStruct[Ripple_MAX];
        for (int i = 0; i < Ripple_MAX; i++)
        {
            RippleStruct ripple;
            ripple.RID = -1;
            ripple.RipplePos = new Vector2(0, 0);
            ripple.RippleProgress = 0;
            ripple.Radius = 0;
            ripple.Cover = 0;
            ripple.DeltaTime = 0;
            ripple.t = 0;
            ripples[i] = ripple;
        }
        if (ripplesBuffer == null) ripplesBuffer = new ComputeBuffer(Ripple_MAX, 32);

        tex = new RenderTexture((int)ScreenRes.x, (int)ScreenRes.y, 24);
        tex.enableRandomWrite = true;
        tex.Create();
    }
    void RunShader()
    {
        _kernelIndex = rippleShader.FindKernel("CSMain");

        for (int i = 0; i < ripples.Length; i++)
        {
            if (ripples[i].RID != -1)
            {
                // edit here
                if (ripples[i].RippleProgress >= 1) // end of ripple
                {
                    ReleaseList(ripples[i].RID);
                }
            }
        }

        ripplesBuffer.SetData(ripples);
        rippleShader.SetBuffer(_kernelIndex, "rippleBuffer", ripplesBuffer);
        rippleShader.SetFloat("RippleDensity", 1f);
        rippleShader.SetVector("Resolution", new Vector2((int)ScreenRes.x, (int)ScreenRes.y));
        rippleShader.SetTexture(_kernelIndex, "Result", tex);
        rippleShader.Dispatch(_kernelIndex, (int)ScreenRes.x / 8, (int)ScreenRes.y / 8, 1);

        ripplesBuffer.GetData(ripples);
        Graphics.CopyTexture(tex, RippleResult);
    }

    public void OnReceivePoint(Vector2 pos)
    {
        if (!spawnFlag) return;
        if (CheckDistance(pos)) //tempPos = pos;
        {
            StartCoroutine(SpawnRippleRoutine(pos));
        }
    }
    IEnumerator SpawnRippleRoutine(Vector2 pos)
    {
        spawnFlag = false;
        SpawnRipple(pos);
        genRippleInterval = Random.Range(genRippleIntervalRange.x, genRippleIntervalRange.y);
        yield return new WaitForSeconds(genRippleInterval);
        spawnFlag = true;
    }
    void Start()
    {
        InitializeComputeShader();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            OnReceivePoint(Input.mousePosition);
        }
    }

    void FixedUpdate()
    {
        // Fix update rate to fix time
        RunShader();
    }

    private void OnDisable()
    {
        ripplesBuffer.Release();
    }

}
