using UnityEngine;
using UtilityToolkit.Editor;
using static UnityEngine.Debug;

public class FlockingDispatcher : MonoBehaviour
{
    [SerializeField] private ComputeShader _flockingShader;
    [SerializeField] private GameObject _agentPrefab;
    [SerializeField] private Material _agentShaderMaterial;
    [SerializeField] private TerrainData _terrain;
    
    private int _kernelHandle;
    private GraphicsBuffer _positionsBuffer;
    private GraphicsBuffer _velocitiesBuffer;

    private GraphicsBuffer _flockCenterBuffer;
    private GraphicsBuffer _flockAlignmentBuffer;
    private GraphicsBuffer _flockSeparationBuffer;
    
    private GraphicsBuffer _debugNumberBuffer;
    private GraphicsBuffer _debugFloat2Buffer;
    private GraphicsBuffer _debugFloat3Buffer;

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
    
    private RenderParams _materialRenderParams;
    private Mesh _mesh;
    private GraphicsBuffer _commandBuffer;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] _commandData;
    private const int CommandCount = 1;

    private void Start()
    {
        _positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _agentCount, sizeof(float) * 3);
        _velocitiesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _agentCount, sizeof(float) * 2);
        _flockCenterBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _agentCount, sizeof(float) * 2);
        _flockAlignmentBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _agentCount, sizeof(float) * 2);
        _flockSeparationBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _agentCount, sizeof(float) * 2);
        _debugFloat2Buffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(float) * 2);
        _debugFloat3Buffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(float) * 3);
        _debugNumberBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(float));

        // var heights2D = _terrain.GetHeights(0, 0, _terrain.heightmapResolution, _terrain.heightmapResolution);
        // var heights = new float[_terrain.heightmapResolution * _terrain.heightmapResolution];
        // print(_terrain.heightmapResolution);
        // print(_terrain.heightmapResolution * _terrain.heightmapResolution);
        
        
        var positions = new Vector3[_agentCount];
        var velocities = new Vector2[_agentCount];
        for (int i = 0; i < _agentCount; i++)
        {
            var pointInCircle = Random.insideUnitCircle * _spawnRadius;
            positions[i] = new Vector3(pointInCircle.x, 0, pointInCircle.y); // + Vector3.up * 10.0f;
            velocities[i] = Vector2.zero;
        }
        _positionsBuffer.SetData(positions);
        _velocitiesBuffer.SetData(velocities);

        _kernelHandle = _flockingShader.FindKernel("CSMain");
        _flockingShader.SetBuffer(_kernelHandle, "positions", _positionsBuffer);
        _flockingShader.SetBuffer(_kernelHandle, "velocities", _velocitiesBuffer);
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

        _agentShaderMaterial.SetBuffer("positions", _positionsBuffer);
        
        _materialRenderParams = new RenderParams(_agentShaderMaterial)
        {
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000)
        };
        
        _mesh = _agentPrefab.GetComponent<MeshFilter>().sharedMesh;
        _commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, CommandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        _commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[CommandCount];
        _commandData[0].indexCountPerInstance = _mesh.GetIndexCount(0);
        _commandData[0].instanceCount = (uint)_agentCount;
        _commandData[0].startIndex = _mesh.GetIndexStart(0);
        _commandData[0].baseVertexIndex = _mesh.GetBaseVertex(0);
        _commandData[0].startInstance = 0;
        _commandBuffer.SetData(_commandData);
    }

    private void Update()
    {
        _flockingShader.SetFloat("Time", Time.time);
        _flockingShader.Dispatch(_kernelHandle, Mathf.CeilToInt(_agentCount / 32.0f), 1, 1);
        
        Graphics.RenderMeshIndirect(
            rparams: _materialRenderParams,
            mesh: _mesh,
            commandBuffer: _commandBuffer,
            commandCount: CommandCount);
        
        DebugLog();
    }

    private void DebugLog()
    {
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
        if (!Application.isPlaying)
            return;
        
        if (!_drawVelocity && !_drawCenter && !_drawAlignment && !_drawSeparation)
            return;
        
        Vector3[] positions = new Vector3[_agentCount];
        _positionsBuffer.GetData(positions);
        
        if (_drawMaxDistance)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _agentCount; i++)
            {
                Gizmos.DrawWireSphere(positions[i], _maxDistance);
            }
        }
        
        if (_drawMinDistance)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < _agentCount; i++)
            {
                Gizmos.DrawWireSphere(positions[i], _minDistance);
            }
        }
        
        if (_drawVelocity)
        {
            Vector2[] velocities = new Vector2[_agentCount];
            _velocitiesBuffer.GetData(velocities);
            for (int i = 0; i < _agentCount; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawLine(positions[i], positions[i].AddFlat(velocities[i]));
            }
        }

        if (_drawCenter)
        {
            Vector2[] center = new Vector2[_agentCount];
            _flockCenterBuffer.GetData(center);
            for (int i = 0; i < _agentCount; i++)
            {
                DrawLine(positions[i], positions[i].AddFlat(center[i]), Color.yellow);
            }
        }
        
        if (_drawAlignment)
        {
            Vector2[] alignment = new Vector2[_agentCount];
            _flockAlignmentBuffer.GetData(alignment);
            for (int i = 0; i < _agentCount; i++)
            {
                DrawLine(positions[i], positions[i].AddFlat(alignment[i]), Color.magenta);
            }
        }
        
        if (_drawSeparation)
        {
            Vector2[] separation = new Vector2[_agentCount];
            _flockSeparationBuffer.GetData(separation);
            for (int i = 0; i < _agentCount; i++)
            {
                DrawLine(positions[i], positions[i].AddFlat(separation[i]), Color.red);
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
        _positionsBuffer?.Dispose();
        _velocitiesBuffer?.Dispose();
        _flockCenterBuffer?.Dispose();
        _flockAlignmentBuffer?.Dispose();
        _flockSeparationBuffer?.Dispose();
        _debugNumberBuffer?.Dispose();
        _debugFloat2Buffer?.Dispose();
    }
}