using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TargetAssignmentManager : MonoBehaviour
{
    public List<Transform> allTargets = new List<Transform>();  // 所有目标
    public List<Transform> allResources = new List<Transform>();  // 所有资源
    public Dictionary<CustomAgent, Transform> agentTargetMap = new Dictionary<CustomAgent, Transform>();  // 智能体与目标的映射

    public CommunicationManager communication;
    void Start()
    {
        MainLogic.OnResourcesInitialized += LoadResourcesAndTargets;
    }
    private void LoadResourcesAndTargets()
    {
        Debug.Log("TargetAssignmentManager LoadResourcesAndTargets()");

        var resourceParent = GameObject.Find("ResourceNode");
        var targetParent = GameObject.Find("TargetNode");

        if (resourceParent == null)
            Debug.LogError("❌ 没有找到 ResourceNode！");
        if (targetParent == null)
            Debug.LogError("❌ 没有找到 TargetNode！");

        if (resourceParent || targetParent)
        {
            allResources = resourceParent.transform.Cast<Transform>().ToList();
            allTargets = targetParent.transform.Cast<Transform>().ToList();
            Debug.Log($"✅ 找到 {allResources.Count} 个资源点，{allTargets.Count} 个目标点");
        }

        if (allResources.Count == 0 || allTargets.Count == 0)
        {
            Debug.LogWarning("⚠️ 资源点或目标点数量为 0，请检查 ResourceNode/TargetNode 是否有子物体！");
        }

        communication = FindObjectOfType<CommunicationManager>();
        if (communication == null)
        {
            Debug.LogError("❌ 场景中缺少 CommunicationManager 组件！");
        }
    }

    public Transform AssignTarget(CustomAgent agent)
    {
        HashSet<Transform> usedTargets = new HashSet<Transform>(agentTargetMap.Values); // 已被分配的目标

        Transform bestTarget = null;
        float bestScore = float.MinValue;

        foreach (var target in allTargets)
        {
            // 如果目标已被占用，则跳过
            if (usedTargets.Contains(target)) continue;

            float distance = Vector3.Distance(agent.transform.position, target.position);
            float energyFactor = agent.currentEnergy / agent.maxEnergy;
            float angle = Vector3.Angle(agent.transform.forward, target.position - agent.transform.position);

            // 目标评分
            float score = -distance + energyFactor * 10f - angle * 0.1f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        if (bestTarget != null){
            agentTargetMap[agent] = bestTarget;  // 设置目标
            Debug.Log($"{agent.name} 的目标 {bestTarget.name} 已设置！");
        }
            

        return bestTarget;
    }

    public void ClearAssignments()
    {
        agentTargetMap.Clear();
    }

    public void ReleaseAgentTarget(CustomAgent agent)
    {
        if (agentTargetMap.ContainsKey(agent))
        {
            // 如果字典中存在该智能体的目标，则移除
            Transform targetToRelease = agentTargetMap[agent];
            agentTargetMap.Remove(agent);  // 从字典中移除智能体与目标的映射
            
            // 可选：你可以在这里添加额外逻辑，譬如将目标回收到可用目标池中。
            Debug.Log($"{agent.name} 的目标 {targetToRelease.name} 已释放！");
        }
        else
        {
            Debug.LogWarning($"{agent.name} 未被分配目标，无法释放！");
        }
    }

}
