using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UtilityToolkit.Editor;
using static UnityEngine.Debug;

public class FlockingDispatcher : MonoBehaviour
{
    [SerializeField] private ComputeShader _flockingShader;
    [SerializeField] private GameObject _agentPrefab;
    [SerializeField] private Material _agentShaderMaterial;
    [SerializeField] private Texture2D _vertexAnimationTexture;
    [SerializeField] private TerrainData _terrain;
    [SerializeField] private Terrain _terrainComponent;

    private int _kernelHandle;

    private GraphicsBuffer _agentsBuffer;

    private GraphicsBuffer _flockCenterBuffer;
    private GraphicsBuffer _flockAlignmentBuffer;
    private GraphicsBuffer _flockSeparationBuffer;

    private GraphicsBuffer _debugNumberBuffer;
    private GraphicsBuffer _debugFloat2Buffer;
    private GraphicsBuffer _debugFloat3Buffer;

    private GraphicsBuffer _vertexAnimationBuffer;

    private GraphicsBuffer _terrainBuffer;
    private float[] _heights;
    private Dictionary<Vector3, float> _worldPositionToHeight;

    [SerializeField] private int _agentCount;
    [SerializeField] private float _spawnRadius;
    [SerializeField] private float _maxDistance;
    [SerializeField] private float _minDistance;
    [SerializeField] private float _speed;
    [SerializeField] private float _cohesionWeight;
    [SerializeField] private float _alignmentWeight;
    [SerializeField] private float _separationWeight;

    [SerializeField] private bool _drawVelocity;
    [SerializeField] private bool _drawCenter;
    [SerializeField] private bool _drawAlignment;
    [SerializeField] private bool _drawSeparation;
    [SerializeField] private bool _drawMaxDistance;
    [SerializeField] private bool _drawMinDistance;
    [SerializeField] private bool _writeNumber;
    [SerializeField] private bool _writeFloat2;
    [SerializeField] private bool _writeFloat3;
    [SerializeField] private bool _writeAgents;
    

    private int _threadGroupCount;
    private RenderParams _materialRenderParams;
    private Mesh _mesh;
    private GraphicsBuffer _commandBuffer;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] _commandData;
    private const int CommandCount = 1;

    private void Start()
    {
        Random.InitState(0);
        CreateBuffers();
        // SetupTerrainBuffer();
        SetBuffers();
        SetupRenderingBuffer();
    }

    private void CreateBuffers()
    {
        _agentsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _agentCount, sizeof(float) * 5);
        _flockCenterBuffer =
            new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _agentCount, sizeof(float) * 2);
        _flockAlignmentBuffer =
            new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _agentCount, sizeof(float) * 2);
        _flockSeparationBuffer =
            new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _agentCount, sizeof(float) * 2);
        _debugFloat2Buffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(float) * 2);
        _debugFloat3Buffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(float) * 3);
        _debugNumberBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(float));
        _vertexAnimationBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 16544, sizeof(float) * 3);
    }

    private void SetupRenderingBuffer()
    {
        _materialRenderParams = new RenderParams(_agentShaderMaterial)
        {
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000)
        };

        if (_agentPrefab.TryGetComponent<MeshFilter>(out var meshFilter))
        {
            _mesh = meshFilter.sharedMesh;
        }
        else
        {
            _mesh = Utility.CombineMesh(_agentPrefab);
        }

        _commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, CommandCount,
            GraphicsBuffer.IndirectDrawIndexedArgs.size);
        _commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[CommandCount];
        _commandData[0].indexCountPerInstance = _mesh.GetIndexCount(0);
        _commandData[0].instanceCount = (uint)_agentCount;
        _commandData[0].startIndex = _mesh.GetIndexStart(0);
        _commandData[0].baseVertexIndex = _mesh.GetBaseVertex(0);
        _commandData[0].startInstance = 0;
        _commandBuffer.SetData(_commandData);
    }

    private CombineInstance[] CombineInstancesSkinnedMeshRenderers()
    {
        var skinnedMeshRenderers = _agentPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
        var combineInstances = new CombineInstance[skinnedMeshRenderers.Length];
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            var mesh = new Mesh();
            skinnedMeshRenderers[i].BakeMesh(mesh);

            combineInstances[i].mesh = mesh;
            combineInstances[i].transform = skinnedMeshRenderers[i].localToWorldMatrix;
        }

        return combineInstances;
    }

    private void SetupTerrainBuffer()
    {
        var terrainResolution2D = _terrain.heightmapResolution * _terrain.heightmapResolution;
        _terrainBuffer =
            new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, terrainResolution2D, sizeof(float));

        var heights2D = _terrain.GetHeights(0, 0, _terrain.heightmapResolution, _terrain.heightmapResolution);
        _heights = new float[terrainResolution2D];
        _worldPositionToHeight = new Dictionary<Vector3, float>();
        for (int i = 0; i < _terrain.heightmapResolution; i++)
        {
            for (int j = 0; j < _terrain.heightmapResolution; j++)
            {
                // _heights[i * _terrain.heightmapResolution + j] = heights2D[i, j] * _terrain.size.y;
                _heights[i * _terrain.heightmapResolution + j] = _terrainComponent.SampleHeight(Vector3.zero);

                var position = new Vector3(j, 0, i);
                _worldPositionToHeight.Add(position, _terrainComponent.SampleHeight(position));
            }
        }

        _terrainBuffer.SetData(_heights);

        _flockingShader.SetBuffer(_kernelHandle, "terrainHeight", _terrainBuffer);
        _flockingShader.SetFloat("TerrainOffsetX", _terrainComponent.transform.position.x);
        _flockingShader.SetFloat("TerrainOffsetZ", _terrainComponent.transform.position.z);
    }

    private void SetBuffers()
    {
        var agents = InitializeAgents();
        _agentsBuffer.SetData(agents);

        Vector3[] vertexAnimationPositions = Utility.ReadVectors().ToArray();
        _vertexAnimationBuffer.SetData(vertexAnimationPositions);
        _agentShaderMaterial.SetBuffer("vertex_animation_buffer", _vertexAnimationBuffer);

        _kernelHandle = _flockingShader.FindKernel("CSMain");
        _flockingShader.GetKernelThreadGroupSizes(_kernelHandle, out var threadGroupSize, out _, out _);
        _threadGroupCount = Mathf.CeilToInt(_agentCount / (float)threadGroupSize);
        _flockingShader.SetBuffer(_kernelHandle, "agents", _agentsBuffer);
        _flockingShader.SetBuffer(_kernelHandle, "flockCenter", _flockCenterBuffer);
        _flockingShader.SetBuffer(_kernelHandle, "flockAlignment", _flockAlignmentBuffer);
        _flockingShader.SetBuffer(_kernelHandle, "flockSeparation", _flockSeparationBuffer);
        _flockingShader.SetBuffer(_kernelHandle, "debugNumber", _debugNumberBuffer);
        _flockingShader.SetBuffer(_kernelHandle, "debugFloat2", _debugFloat2Buffer);
        _flockingShader.SetBuffer(_kernelHandle, "debugFloat3", _debugFloat3Buffer);
        _flockingShader.SetFloat("AgentsCount", _agentCount);
        _flockingShader.SetFloat("MaxDistance", _maxDistance);
        _flockingShader.SetFloat("MinDistance", _minDistance);
        _flockingShader.SetFloat("CohesionWeight", _cohesionWeight);
        _flockingShader.SetFloat("AlignmentWeight", _alignmentWeight);
        _flockingShader.SetFloat("SeparationWeight", _separationWeight);
        _flockingShader.SetFloat("Speed", _speed);

        _agentShaderMaterial.SetBuffer("agents", _agentsBuffer);
    }

    private Vector3[] ReadVertexAnimationTexture()
    {
        const int length = 16544;
        var vertexAnimationBuffer = new Vector3[length];
        var count = 0;
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 256; j++)
            {
                if (count == length) break;
                
                var pixel = _vertexAnimationTexture.GetPixel(j, i);

                if (i == 0 && j < 10)
                {
                    print(pixel.ToVector3());
                }
                
                vertexAnimationBuffer[count] = pixel.ToVector3();

                count++;
            }
        }

        return vertexAnimationBuffer;
    }

    private Agent[] InitializeAgents()
    {
        var agents = new Agent[_agentCount];
        for (int i = 0; i < _agentCount; i++)
        {
            var pointInCircle = Random.insideUnitCircle * _spawnRadius;
            agents[i] = new Agent
            {
                Position = new Vector3(pointInCircle.x, 0, pointInCircle.y),
                Velocity = Random.insideUnitCircle
            };
        }

        return agents;
    }

    private void Update()
    {
        _flockingShader.SetFloat("DeltaTime", Time.deltaTime);
        _flockingShader.Dispatch(_kernelHandle, _threadGroupCount, 1, 1);

        Graphics.RenderMeshIndirect(
            rparams: _materialRenderParams,
            mesh: _mesh,
            commandBuffer: _commandBuffer,
            commandCount: CommandCount);

        DebugLog();
    }

    private void DebugLog()
    {
        if (_writeAgents)
        {
            Agent[] agents = new Agent[_agentCount];
            _agentsBuffer.GetData(agents);
            for (int i = 0; i < _agentCount; i++)
            {
                Log($"Agent {i}: Position: {agents[i].Position}, Velocity: {agents[i].Velocity}");
            }
        }
        
        if (_writeNumber)
        {
            var number = new int[1];
            _debugNumberBuffer.GetData(number);
            Log($"Debug Number: {number[0]}");
        }

        if (_writeFloat2)
        {
            var float2 = new Vector2[1];
            _debugFloat2Buffer.GetData(float2);
            Log($"Debug Float2: {float2[0]}");
        }

        if (_writeFloat3)
        {
            var float3 = new Vector3[1];
            _debugFloat3Buffer.GetData(float3);
            Log($"Debug Float3: {float3[0]}");
        }
    }

    private void OnDrawGizmos()
    {
        if (_worldPositionToHeight is { Count: > 0 })
        {
            Gizmos.color = Color.red;
            foreach (var key in _worldPositionToHeight.Keys)
            {
                var position = key;
                position.y = _worldPositionToHeight[key];
                Gizmos.DrawSphere(position, 0.2f);
            }
        }
        
        
        if (!Application.isPlaying)
            return;

        // for (int i = 0; i < _terrain.heightmapResolution; i++)
        // {
        //     for (int j = 0; j < _terrain.heightmapResolution; j++)
        //     {
        //         var height = _heights[i * _terrain.heightmapResolution + j];
        //         var position = new Vector3(i, height, j) + _terrainComponent.transform.position;
        //         Gizmos.color = Color.white;
        //         Gizmos.DrawSphere(position, 0.1f);
        //     }
        // }

        if (!_drawVelocity && !_drawCenter && !_drawAlignment && !_drawSeparation && !_drawMinDistance &&
            !_drawMaxDistance)
            return;

        Agent[] agents = new Agent[_agentCount];
        _agentsBuffer.GetData(agents);

        if (_drawMaxDistance)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _agentCount; i++)
            {
                Gizmos.DrawWireSphere(agents[i].Position, Mathf.Sqrt(_maxDistance));
            }
        }

        if (_drawMinDistance)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < _agentCount; i++)
            {
                Gizmos.DrawWireSphere(agents[i].Position, _minDistance);
            }
        }

        if (_drawVelocity)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _agentCount; i++)
            {
                Gizmos.DrawLine(agents[i].Position, agents[i].Position.AddFlat(agents[i].Velocity));
            }
        }

        if (_drawCenter)
        {
            Vector2[] center = new Vector2[_agentCount];
            _flockCenterBuffer.GetData(center);
            for (int i = 0; i < _agentCount; i++)
            {
                DrawLine(agents[i].Position, agents[i].Position.AddFlat(center[i]), Color.yellow);
            }
        }

        if (_drawAlignment)
        {
            Vector2[] alignment = new Vector2[_agentCount];
            _flockAlignmentBuffer.GetData(alignment);
            for (int i = 0; i < _agentCount; i++)
            {
                DrawLine(agents[i].Position, agents[i].Position.AddFlat(alignment[i]), Color.magenta);
            }
        }

        if (_drawSeparation)
        {
            Vector2[] separation = new Vector2[_agentCount];
            _flockSeparationBuffer.GetData(separation);
            for (int i = 0; i < _agentCount; i++)
            {
                DrawLine(agents[i].Position, agents[i].Position.AddFlat(separation[i]), Color.red);
            }
        }
    }

    [Button]
    public void UpdateValues()
    {
        _flockingShader.SetFloat("MaxDistance", _maxDistance);
        _flockingShader.SetFloat("MinDistance", _minDistance);
        _flockingShader.SetFloat("CohesionWeight", _cohesionWeight);
        _flockingShader.SetFloat("AlignmentWeight", _alignmentWeight);
        _flockingShader.SetFloat("SeparationWeight", _separationWeight);
        _flockingShader.SetFloat("Speed", _speed);
    }

    private void OnDestroy()
    {
        _commandBuffer?.Dispose();
        _agentsBuffer?.Dispose();
        _flockCenterBuffer?.Dispose();
        _flockAlignmentBuffer?.Dispose();
        _flockSeparationBuffer?.Dispose();
        _debugNumberBuffer?.Dispose();
        _debugFloat2Buffer?.Dispose();
        _debugFloat3Buffer?.Dispose();
        _vertexAnimationBuffer?.Dispose();
        _terrainBuffer?.Dispose();
    }
}

struct Agent
{
    public Vector3 Position;
    public Vector2 Velocity;
}