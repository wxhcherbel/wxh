using UnityEngine;
using System.Collections.Generic;

public class AgentTrainingManager : MonoBehaviour
{
    private CustomAgent[] allAgents;

    void Start()
    {
        MainLogic.OnResourcesInitialized += LoadResourcesAndTargets;
    }
    private void LoadResourcesAndTargets()
    {
        allAgents = FindObjectsOfType<CustomAgent>();
    }
    public void StartAllTraining()
    {
        foreach (var agent in allAgents)
        {
            agent.StartTraining();
        }

        Debug.Log("所有 Agent 已开始训练！");
    }
}
