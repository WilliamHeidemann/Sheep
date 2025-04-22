using UnityEngine;

[CreateAssetMenu]
public class AgentCounter : ScriptableObject
{
    public int AgentCount;

    public static implicit operator int(AgentCounter agentCounter)
    {
        return agentCounter.AgentCount;
    }

    public static implicit operator uint(AgentCounter agentCounter)
    {
        return (uint)agentCounter.AgentCount;
    }

    public static implicit operator float(AgentCounter agentCounter)
    {
        return agentCounter.AgentCount;
    }
}