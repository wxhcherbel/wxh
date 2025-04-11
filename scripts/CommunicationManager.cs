using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CommunicationManager : MonoBehaviour
{
    public float communicationRange = 10f;  // 设置通信范围
    public List<CustomAgent> allAgents;    // 所有智能体
    public TargetAssignmentManager targetManager; // 引用目标分配管理器

    
    void Start()
    {
       MainLogic.OnResourcesInitialized += LoadResourcesAndTargets;
    }

    private void LoadResourcesAndTargets()
    {
        var agentParent = GameObject.Find("ExplorerNode");
        if (agentParent) {
            allAgents = agentParent.transform.Cast<Transform>()
                .Select(t => t.GetComponent<CustomAgent>())
                .Where(agent => agent != null) // 避免 null 引用
                .ToList();

            Debug.Log($"找到 {allAgents.Count} 个Explorer智能体");
        }

        targetManager = FindObjectOfType<TargetAssignmentManager>();
    }

    void Update()
    {
        foreach (var agent in allAgents)
        {
            // 每个智能体发出目标信息
            if (targetManager.agentTargetMap.ContainsKey(agent))
            {
                SendTargetInfo(agent);  // 发送目标信息
            }
        }
        VisualizeCommunication();
    }

    // 发送目标信息给范围内的其他智能体
    void SendTargetInfo(CustomAgent agent)
    {
        Transform agentTarget = targetManager.agentTargetMap[agent];  // 获取当前智能体的目标
        foreach (var otherAgent in allAgents)
        {
            // 排除自己，且仅在通信范围内才进行目标传递
            if (otherAgent != agent && Vector3.Distance(agent.transform.position, otherAgent.transform.position) <= communicationRange)
            {
                // 向其他智能体发送目标信息
                ReceiveTargetInfo(agent, agentTarget);
            }
        }
    }

    // 接收目标信息，进行冲突检测
    public void ReceiveTargetInfo(CustomAgent sender, Transform target)
    {
        // 如果接收者的目标已经被其他智能体占用，进行处理
        if (IsTargetOccupied(target) && targetManager.agentTargetMap.ContainsKey(sender))
        {
            Debug.Log($"{sender.name}目标已被占用，尝试重新分配目标！");
            targetManager.AssignTarget(sender);
            // 在这里可以选择重新分配目标，或者进行其他策略
            // 例如：让发送者重新选择目标，或通知其他智能体的目标分配策略
        }
    }

    // 检查目标是否被占用
    public bool IsTargetOccupied(Transform target)
    {
        return targetManager.agentTargetMap.ContainsValue(target);
    }

    void VisualizeCommunication()
    {
        foreach (var sender in allAgents)
        {
            Transform senderTarget = null;

            if (targetManager.agentTargetMap.TryGetValue(sender, out senderTarget))
            {
                foreach (var receiver in allAgents)
                {
                    if (receiver == sender) continue;

                    float distance = Vector3.Distance(sender.transform.position, receiver.transform.position);
                    if (distance <= communicationRange)
                    {
                        // 绘制蓝色线条表示信息传递路径
                        Debug.DrawLine(sender.transform.position, receiver.transform.position, Color.cyan);
                        
                        // 绘制从智能体指向目标的线（可选）
                        if (senderTarget != null)
                        {
                            Debug.DrawLine(sender.transform.position, senderTarget.position, Color.yellow);
                        }
                    }
                }
            }
        }
    }

}
