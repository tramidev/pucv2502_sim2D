using System.Runtime.InteropServices;
using UnityEngine;

public class GPUBallManagerSimple : MonoBehaviour
{
    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public int ballCount = 200;
    public float ballRadius = 0.25f;
    public float minSpeed = 2f;
    public float maxSpeed = 5f;

    [Header("Simulation Area (World Space)")]
    public Vector2 areaMin = new Vector2(-9f, -5f);
    public Vector2 areaMax = new Vector2(9f, 5f);

    [Header("Compute Shader")]
    public ComputeShader ballSimulationCompute;
    
    struct BallData
    {
        public Vector2 position;
        public Vector2 velocity;
    }

    private Transform[] _ballTransforms;
    private BallData[]  _ballDataCPU; // we keep a CPU-side array to read back from GPU
    private ComputeBuffer _ballsBuffer;

    private int _kernelHandle;
    
    void Start()
    {
        if (ballPrefab == null || ballSimulationCompute == null)
        {
            Debug.LogError("Assign Ball Prefab and BallSimulation.compute in inspector.");
            enabled = false;
            return;
        }

        _kernelHandle = ballSimulationCompute.FindKernel("CSMain");

        CreateBalls();
        InitBuffers();
        _ballsBuffer.SetData(_ballDataCPU);
    }

    void OnDestroy()
    {
        if (_ballsBuffer != null)
        {
            _ballsBuffer.Release();
            _ballsBuffer = null;
        }
    }
    
    void CreateBalls()
    {
        _ballTransforms = new Transform[ballCount];
        _ballDataCPU    = new BallData[ballCount];

        for (int i = 0; i < ballCount; i++)
        {
            // Instantiate ball GameObject
            GameObject go = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
            go.name = "Ball_" + i;
            _ballTransforms[i] = go.transform;

            // Random position
            Vector2 pos = new Vector2(
                Random.Range(areaMin.x + ballRadius, areaMax.x - ballRadius),
                Random.Range(areaMin.y + ballRadius, areaMax.y - ballRadius)
            );

            // Random velocity
            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(minSpeed, maxSpeed);
            Vector2 vel = dir * speed;

            _ballDataCPU[i].position = pos;
            _ballDataCPU[i].velocity = vel;

            _ballTransforms[i].position = pos;
        }
    }

    void InitBuffers()
    {
        int stride = Marshal.SizeOf(typeof(BallData));

        _ballsBuffer = new ComputeBuffer(ballCount, stride, ComputeBufferType.Structured);
        ballSimulationCompute.SetBuffer(_kernelHandle, "_Balls", _ballsBuffer);
    }
    
    void Update()
    {
        float dt = Time.deltaTime;

        // 1) Set parameters for the compute shader
        ballSimulationCompute.SetFloat("_Radius", ballRadius);
        ballSimulationCompute.SetInt("_BallCount", ballCount);
        ballSimulationCompute.SetVector("_AreaMin", areaMin);
        ballSimulationCompute.SetVector("_AreaMax", areaMax);
        ballSimulationCompute.SetFloat("_DeltaTime", dt);

        // 2) Dispatch compute shader
        int threadGroupCount = Mathf.CeilToInt(ballCount / 64.0f);
        ballSimulationCompute.Dispatch(_kernelHandle, threadGroupCount, 1, 1);

        // 3) Read back data from GPU and update GameObject transforms
        _ballsBuffer.GetData(_ballDataCPU);

        for (int i = 0; i < ballCount; i++)
        {
            _ballTransforms[i].position = _ballDataCPU[i].position;
        }
    }
}

