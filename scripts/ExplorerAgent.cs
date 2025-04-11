using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ExplorerAgent : Agent
{
    // 基础参数
    [Header("Agent Settings")]
    public float moveSpeed = 5f; // 提高移动速度
    public float rotationSpeed = 500f; // 提高旋转速度
    public bool isManualControl = false;
    public float stoppingDistance = 2.0f; // 停止距离阈值

    // 目标相关
    [Header("Environment References")]
    public Transform targetResource;
    public Transform targetTarget;
    private List<Transform> allResources;
    private List<Transform> allTargets;

    // 状态变量
    private Vector3 startPosition;
    public bool hasResource = false;
    private Rigidbody rb;
    private ExplorerCommunication communication;
    private float lastDistance = float.MaxValue; // 跟踪上次距离

    // 避障相关参数
    public float rayDistance = 5f;
    public LayerMask obstacleLayer;

    // 地图范围
    private float mapLength;
    private float mapHeight;
    private float mapWidth;
    

    void Update() 
    {
        // 按下M键切换手动/自动模式
        if (Input.GetKeyDown(KeyCode.M))
        {
            isManualControl = !isManualControl;
            Debug.Log("手动控制模式: " + isManualControl);
        }
        if (targetResource == null) 
            return;

        bool isClaimed = communication.IsResourceClaimed(targetResource);
        Color lineColor = isClaimed ? Color.green : Color.red;

        // 加粗效果：绘制多条平行线
        Vector3 offset1 = Vector3.right * 0.1f; // 横向偏移
        Vector3 offset2 = Vector3.up * 0.1f;    // 纵向偏移

        // 主线
        Debug.DrawLine(transform.position, targetResource.position, lineColor);
        // 偏移线条（加粗）
        Debug.DrawLine(transform.position + offset1, targetResource.position + offset1, lineColor);
        Debug.DrawLine(transform.position - offset1, targetResource.position - offset1, lineColor);
        Debug.DrawLine(transform.position + offset2, targetResource.position + offset2, lineColor);
        Debug.DrawLine(transform.position - offset2, targetResource.position - offset2, lineColor);
    }


    public override void Initialize()
    {
        base.Initialize();
        rb = GetComponent<Rigidbody>();
        rb.drag = 1f; // 增加阻力防止滑动
        rb.angularDrag = 2f;
        //rb.useGravity = false;
        startPosition = transform.position;
        // 获取地图范围
        MainLogic mainLogic = FindObjectOfType<MainLogic>();
        mapLength = mainLogic.mapLength;
        mapHeight = mainLogic.mapHeight;
        mapWidth = mainLogic.mapWidth;
        MainLogic.OnResourcesInitialized += LoadResourcesAndTargets;
        var behaviorParameters = GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        if (behaviorParameters != null)
        {
            behaviorParameters.BehaviorType = isManualControl 
                ? Unity.MLAgents.Policies.BehaviorType.Default 
                : Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;
        }
    }

    private void LoadResourcesAndTargets()
    {
        var resourceParent = GameObject.Find("ResourceNode");
        var targetParent = GameObject.Find("TargetNode");

        if (resourceParent && targetParent) {
            allResources = resourceParent.transform.Cast<Transform>().ToList();
            allTargets = targetParent.transform.Cast<Transform>().ToList();
            Debug.Log($"找到 {allResources.Count} 个资源点，{allTargets.Count} 个目标点");
        }
        communication = FindObjectOfType<ExplorerCommunication>();
        if (communication == null) {
            Debug.LogError("场景中缺少ExplorerCommunication组件！");
        }
        communication.ResetAllClaims();
    }

    public override void OnEpisodeBegin()
    {
        if (targetResource != null) {
            communication.ReleaseResource(targetResource);
        }
        // 随机生成智能体位置
        Vector3 randomPosition = new Vector3(
            Random.Range(-mapWidth / 2f, mapWidth / 2f),
            -0.4f,
            Random.Range(-mapLength / 2f, mapLength / 2f)
        );
        transform.position = randomPosition;
        transform.rotation = Quaternion.identity;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        hasResource = false;
        targetResource = null;
        targetTarget = null;
        lastDistance = float.MaxValue;

        foreach (var res in allResources) {
            res.gameObject.SetActive(true);
        }
        FindAndClaimResource();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. 基础状态 (5 values)
        sensor.AddObservation(transform.localPosition.x / 20f);
        sensor.AddObservation(transform.localPosition.z / 20f);
        sensor.AddObservation(rb.velocity.x / moveSpeed);
        sensor.AddObservation(rb.velocity.z / moveSpeed);
        sensor.AddObservation(hasResource ? 1f : 0f);

        // 2. 当前阶段的目标信息 (4 values)
        if (!hasResource) {
            AddTargetObservation(sensor, targetResource);
        } else {
            AddTargetObservation(sensor, targetTarget);
        }
        // 3.添加避障射线检测 (1 values)
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance, obstacleLayer))
        {
            sensor.AddObservation(hit.distance / rayDistance); // 障碍物距离归一化
        }
        else
        {
            sensor.AddObservation(1f); // 未检测到障碍物
        }
    }

    private void AddTargetObservation(VectorSensor sensor, Transform target)
    {
        if (target != null) {
            Vector3 relativePos = target.position - transform.position;
            sensor.AddObservation(relativePos.normalized.x);
            sensor.AddObservation(relativePos.normalized.z);
            sensor.AddObservation(relativePos.magnitude / 20f);
            
            // 添加相对角度观察
            float angle = Vector3.SignedAngle(transform.forward, relativePos, Vector3.up);
            sensor.AddObservation(angle / 180f); // 归一化角度
        } else {
            sensor.AddObservation(0f); // 方向X
            sensor.AddObservation(0f); // 方向Z
            sensor.AddObservation(0f); // 距离
            sensor.AddObservation(0f); // 角度
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isManualControl) return;

        // 解析动作 - 使用更平滑的控制
        float move = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float turn = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        // 根据距离调整速度
        Transform currentTarget = hasResource ? targetTarget : targetResource;
        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            float speedMultiplier = Mathf.Clamp(distance / 5f, 0.3f, 1f); // 距离越远速度越快
            
            // 执行移动
            // 碰撞检测与避障
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance, obstacleLayer))
            {
                Debug.Log("检测到障碍物，距离：" + hit.distance);
                AddReward(-0.05f); // 碰撞惩罚
            }
            else
            {
                Vector3 movement = transform.forward * move * moveSpeed * speedMultiplier * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + movement);
            }
            
            // 根据距离调整旋转速度
            float rotationMultiplier = Mathf.Clamp(distance / 2f, 0.5f, 1.5f);
            transform.Rotate(0, turn * rotationSpeed * rotationMultiplier * Time.fixedDeltaTime, 0);
            
            // 距离奖励改进
            if (distance < lastDistance) {
                AddReward(0.01f * (lastDistance - distance)); // 动态奖励
            } else {
                AddReward(-0.005f); // 小惩罚
            }
            lastDistance = distance;
        }
        else
        {
            // 没有目标时的基本移动
            Vector3 movement = transform.forward * move * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
            transform.Rotate(0, turn * rotationSpeed * Time.fixedDeltaTime, 0);
        }

        // 状态检测与奖励
        if (!hasResource) {
            HandleResourcePhase();
        } else {
            HandleDeliveryPhase();
        }
    }

    private void HandleResourcePhase()
    {
        if (targetResource != null && !targetResource.gameObject.activeSelf) {
            communication.ReleaseResource(targetResource);
            targetResource = null;
            AddReward(-0.1f);
            return;
        }

        if (targetResource == null) {
            FindAndClaimResource();
            return;
        }

        float distance = Vector3.Distance(transform.position, targetResource.position);
        
        // 检查是否到达资源
        if (distance < stoppingDistance) {
            if (communication.IsResourceClaimed(targetResource)) {
                AcquireResource();
            } else {
                targetResource = null;
                AddReward(-0.2f);
            }
        }
    }

    private void HandleDeliveryPhase()
    {
        Debug.Log("HandleDeliveryPhase()");
        if (targetTarget == null) {
            FindNearestTarget();
            return;
        }

        float distance = Vector3.Distance(
            new Vector3(rb.position.x, 0, rb.position.z),
            new Vector3(targetTarget.position.x, 0, targetTarget.position.z)
        );
        Debug.Log("rb.position: " + rb.position.ToString());
        Debug.Log("targetTarget.position: " + targetTarget.position.ToString());

        
        if (distance < 3f) {
            DeliverResource();
        }
    }

    private void AcquireResource()
    {
        if (!communication.IsResourceClaimed(targetResource)) {
            AddReward(-0.3f);
            targetResource = null;
            return;
        }

        hasResource = true;
        targetResource.gameObject.SetActive(false);
        communication.ReleaseResource(targetResource);
        
        AddReward(1.0f); // 提高获取资源奖励
        FindNearestTarget();
    }

    private void DeliverResource()
    {
        Debug.Log("DeliverResource()");
        if (hasResource)
        {
            // 在目标点位置实例化一个临时可视化对象
            GameObject deliveredResource = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            deliveredResource.transform.position = targetTarget.position + Vector3.up * 5f;
            deliveredResource.transform.localScale = Vector3.one * 0.8f;
            deliveredResource.GetComponent<Renderer>().material.color = Color.green;
            
        
            Destroy(deliveredResource, 30f);
        }

        hasResource = false;
        AddReward(2.0f);  // 提高最终奖励
        targetTarget = null;
        EndEpisode();
    }

    private void FindAndClaimResource()
    {
        if (hasResource || targetResource != null) return;

        var availableResources = allResources
            .Where(r => r.gameObject.activeSelf && !communication.IsResourceClaimed(r))
            .OrderBy(r => Vector3.Distance(transform.position, r.position))
            .ToList();

        if (availableResources.Count == 0) {
            AddReward(-0.01f);
            return;
        }

        // 尝试声明前3个最近资源
        for (int i = 0; i < Mathf.Min(3, availableResources.Count); i++) {
            if (communication.TryClaimResource(availableResources[i])) {
                targetResource = availableResources[i];
                AddReward(0.1f); // 提高声明奖励
                lastDistance = Vector3.Distance(transform.position, targetResource.position);
                return;
            }
        }

        AddReward(-0.005f);
    }

    private void FindNearestTarget()
    {
        if (allTargets == null || allTargets.Count == 0) return;

        Transform closest = null;
        float minDistance = float.MaxValue;

        foreach (var target in allTargets) {
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance < minDistance) {
                minDistance = distance;
                closest = target;
            }
        }

        targetTarget = closest;
        if (targetTarget != null) {
            lastDistance = minDistance;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
    }
}