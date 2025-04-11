using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEditor;
using System.Collections;
using Unity.MLAgents.Policies; 

public class CustomAgent : Agent
{
    // 1. 基础移动属性
    [Header("移动属性")]
    public float moveSpeed = 5f;      // 移动速度
    public float turnSpeed = 180f;    // 转向速度
    public float acceleration = 2f;   // 加速度
    private Rigidbody rb;
    
    // 2. 感知属性
    [Header("感知属性")]
    public float visionRange = 10f;   // 可视范围
    public float fovAngle = 90f;      // 视野角度
    public LayerMask obstacleLayer; // 能检测到的层级
    
    // 3. 能量属性
    [Header("能量属性")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float energyConsumptionRate = 0.01f; // 每秒消耗
    
    // 4.训练模式
    public bool trainingMode = false;

    // 5.地图范围
    private float mapLength;
    private float mapHeight;
    private float mapWidth;

    // 6.目标点设置
    public Transform target;
    public bool useManualTarget = false;

    // 7.其余脚本设置
    public TargetAssignmentManager Assignment; // 引用目标分配管理器
    //public CommunicationManager communication;
    
    // 8.可视化设置
    private LineRenderer lineRenderer;


    void Start()
    {
        // 添加 LineRenderer 组件（如果还没有）
        lineRenderer = gameObject.AddComponent<LineRenderer>();

        // 设置线条样式（你也可以改颜色、宽度等）
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false; // 默认关闭，分配目标后再启用

        obstacleLayer = LayerMask.GetMask("obstacleLayer");
    }
    void Update()
    {
        if (target != null)
        {
            // 显示连线
            lineRenderer.enabled = true;

            // 设置连线两端的位置（稍微抬高，避免穿地）
            Vector3 agentPos = transform.position + Vector3.up * 0.5f;
            Vector3 targetPos = target.position + Vector3.up * 0.5f;
            lineRenderer.SetPosition(0, agentPos);
            lineRenderer.SetPosition(1, targetPos);
        }
        else
        {
            // 没有目标则不显示连线
            lineRenderer.enabled = false;
        }
    }
    public void StartTraining()
    {
        trainingMode = true;
        Debug.Log("训练已启动！");
        EndEpisode(); // 立即重置，进入训练流程
    }


    public override void Initialize()
    {
        base.Initialize();
        rb = GetComponent<Rigidbody>();
        currentEnergy = maxEnergy;

        GetComponent<Rigidbody>().isKinematic = false;// 启用物理模拟
        GetComponent<Collider>().enabled = true;// 开启碰撞检测
        
        // 获取地图范围
        MainLogic mainLogic = FindObjectOfType<MainLogic>();
        mapLength = mainLogic.mapLength;
        mapHeight = mainLogic.mapHeight;
        mapWidth = mainLogic.mapWidth;

        //脚本初始化
        Assignment = FindObjectOfType<TargetAssignmentManager>();
        //communication = FindObjectOfType<CommunicationManager>();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        //（all:10）
        // 1. 能量状态（归一化到0-1）（1）
        sensor.AddObservation(currentEnergy / maxEnergy);

        // 2. 障碍物信息（2）
        float distance;
        string tag;
        bool hasObstacle = DetectObstacle(out distance, out tag);
        sensor.AddObservation(hasObstacle ? 1f : 0f);
        sensor.AddObservation(hasObstacle ? distance / visionRange : 0f);

        // 3. 速度信息（1）
        sensor.AddObservation(rb.velocity.magnitude / moveSpeed);

        // 4. 目标位置的相对坐标（4）
        if (target != null)
        {
            Vector3 directionToTarget = target.position - transform.position;
            
            // 归一化后的相对位置（将地图边界视作1）
            sensor.AddObservation(directionToTarget.x / mapWidth); // x方向
            sensor.AddObservation(directionToTarget.y / mapHeight); // y方向
            sensor.AddObservation(directionToTarget.z / mapLength); // z方向

            // 距离目标的归一化值（将最大范围视作1）
            float distanceToTarget = directionToTarget.magnitude;
            sensor.AddObservation(distanceToTarget / Mathf.Max(mapWidth, mapHeight, mapLength));

            // 5. 智能体自身方向感知（2）
            // 获取朝向的单位向量
            Vector3 agentForward = transform.forward.normalized;
            Vector3 directionNormalized = directionToTarget.normalized;
            
            //方向向量的点积和夹角
            float dotProduct = Vector3.Dot(agentForward, directionNormalized); // -1到1
            float angleToTarget = Vector3.Angle(agentForward, directionNormalized) / 180f; // 0到1

            sensor.AddObservation(dotProduct); // 前后朝向
            sensor.AddObservation(angleToTarget); // 方向偏离程度（0表示正对目标，1表示反向）
        }
        else
        {
            // 没有目标时填充0
            sensor.AddObservation(0f); // x方向
            sensor.AddObservation(0f); // y方向
            sensor.AddObservation(0f); // z方向
            sensor.AddObservation(1f); // 距离
            sensor.AddObservation(0f); // dotProduct
            sensor.AddObservation(1f); // angleToTarget
        }
    }

    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            // 重置能量
            currentEnergy = maxEnergy;
            
            // 随机位置
            transform.position = new Vector3(
                Random.Range(-mapWidth / 2f, mapWidth / 2f),
                -0.4f,
                Random.Range(-mapLength / 2f, mapLength / 2f)
            );
            transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            // 如果不使用手动目标，自动寻找最近的目标
            if (!useManualTarget)
            {
                target = Assignment.AssignTarget(this);
            }
        }else{
            return;
        }
    }
    //检测前方障碍物
    public bool DetectObstacle(out float distance, out string tag)
    {
        RaycastHit hit;
        Vector3[] directions = { transform.forward, transform.right, -transform.right };
        foreach (var dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out hit, visionRange, obstacleLayer))
            {
                distance = hit.distance;
                tag = hit.collider.tag;
                return true;
            }
        }
        distance = 0;
        tag = "";
        return false;
}

    // 控制智能体移动
    public void MoveAgent(float moveInput, float turnInput)
    {
        if (currentEnergy <= 0)
        {
            AddReward(-0.01f);
            return;
        }
        
        float moveAmount = moveInput * moveSpeed * Time.deltaTime;
        float turnAmount = turnInput * turnSpeed * Time.deltaTime;

        Vector3 newPosition = transform.position + transform.forward * moveAmount;
        Quaternion newRotation = Quaternion.Euler(0, turnAmount, 0) * transform.rotation;

        rb.MovePosition(newPosition);
        rb.MoveRotation(newRotation);

        currentEnergy -= energyConsumptionRate * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
    }

    private void UpdateEnergyUI()
    {
        // 这里可以添加UI更新逻辑
        // 例如：energySlider.value = currentEnergy / maxEnergy;
    }
    


    public override void OnActionReceived(ActionBuffers actions)
    {
        // 1. 获取动作数据
        float move = actions.ContinuousActions[0]; // 前进/后退
        float turn = actions.ContinuousActions[1]; // 旋转

        MoveAgent(move, turn);

        if (target != null)
        {
            // ✅ 2. 只计算 xz 平面的距离（忽略 y 轴）
            Vector2 agentPosXZ = new Vector2(transform.position.x, transform.position.z);
            Vector2 targetPosXZ = new Vector2(target.position.x, target.position.z);
            float flatDistance = Vector2.Distance(agentPosXZ, targetPosXZ);

            float maxMapSize = Mathf.Max(mapWidth, mapHeight, mapLength);
            float distanceReward = Mathf.Exp(-flatDistance / maxMapSize * 5f);
            AddReward(distanceReward * 0.1f);

            // ✅ 3. 判断是否到达目标
            Vector3 flatDirection = (target.position - transform.position);
            flatDirection.y = 0; // 忽略垂直方向
            float angle = Vector3.Angle(transform.forward, flatDirection);

            if (flatDistance < 1f && angle < 45f){
                Debug.Log($"{this.name} 到达目标点！");
                AddReward(2f);
                VisualizeArrival(); // ✅ 调用可视化效果
                Assignment.ReleaseAgentTarget(this);
                EndEpisode();
            }
            else if(flatDistance > 100f){
                Debug.Log($"{this.name} 距离目标点过远，自动释放！");
                AddReward(-2f);
                Assignment.ReleaseAgentTarget(this);
                EndEpisode();
            }
        }

        // ✅ 4. 能量惩罚机制
        if (currentEnergy < maxEnergy * 0.2f)
        {
            AddReward(-0.005f);
        }
    }

    

    // 在目标点上方显示闪烁的小球
    void VisualizeArrival()
    {
        // 在目标点上方创建一个小球
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); // 创建一个球体
        ball.transform.position = target.position + Vector3.up * 2f;  // 将小球放在目标点上方
        ball.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); // 可以调整小球的大小
        // 设置小球颜色为绿色
        Renderer ballRenderer = ball.GetComponent<Renderer>();
        if (ballRenderer != null)
        {
            ballRenderer.material.color = Color.green;
        }

        // 启动闪烁效果
        StartCoroutine(FlashBall(ball));
    }

    // 闪烁小球的协程
    private IEnumerator FlashBall(GameObject ball)
    {
        Renderer ballRenderer = ball.GetComponent<Renderer>();

        if (ballRenderer != null)
        {
            // 闪烁效果：启用和禁用 Renderer 以实现闪烁
            for (int i = 0; i < 5; i++)  // 闪烁 5 次
            {
                ballRenderer.enabled = !ballRenderer.enabled; // 切换显示/隐藏
                yield return new WaitForSeconds(1f);  // 闪烁间隔
            }

            // 闪烁结束后销毁小球
            Destroy(ball);
        }
    }




    // 辅助方法：获取随机目标位置
    Vector3 GetRandomTargetPosition()
    {
        return new Vector3(
            Random.Range(-mapWidth/2f, mapWidth/2f),
            0f, // 假设目标在地面
            Random.Range(-mapLength/2f, mapLength/2f)
        );
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
    }

    void OnDrawGizmos()
    {
        // 1. 增强版视野锥体
        DrawEnhancedVisionCone();
        
        // 2. 立体能量条
        Draw3DEnergyMeter();
        
        // 3. 速度追踪器
        DrawSpeedTracker();
    }

    void DrawEnhancedVisionCone()
    {
        // 主视野线（加粗）
        Gizmos.color = new Color(0, 1, 1, 0.9f);
        Vector3 endPos = transform.position + transform.forward * visionRange;
        DrawThickLine(transform.position, endPos, 0.1f);

        // 多角度检测射线（5条）
        float[] scanAngles = { -45f, -20f, 0f, 20f, 45f };
        bool anyHit = false; // 用于判断是否任何一条射线命中

        foreach (float angle in scanAngles)
        {
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            RaycastHit hit;
            bool hasHit = Physics.Raycast(transform.position + Vector3.up * 1.0f, dir, out hit, visionRange, obstacleLayer);

            if (hasHit)
            {
                //Debug.Log("视野内有障碍物--" + this.name);
                anyHit = true;
            }

            // 射线颜色和粗细
            Gizmos.color = hasHit ? Color.red : new Color(0, 1, 0.5f, 0.7f);
            DrawThickLine(
                transform.position,
                transform.position + dir * (hasHit ? hit.distance : visionRange),
                hasHit ? 0.15f : 0.08f
            );

            if (hasHit)
            {
                Gizmos.DrawSphere(hit.point, 0.3f);
                Handles.Label(hit.point, $"{hit.distance:F1}m",
                    new GUIStyle { fontSize = 14, normal = { textColor = Color.red } });
            }
        }

        // 锥体底面颜色（根据是否有碰撞）
        Handles.color = anyHit ? new Color(1, 0, 0, 0.2f) : new Color(0, 0.8f, 1, 0.15f);

        Vector3 forward = transform.forward * visionRange;
        Vector3 right = transform.right * visionRange * Mathf.Tan(fovAngle * 0.5f * Mathf.Deg2Rad);
        Vector3[] coneVerts = new Vector3[]
        {
            transform.position,
            transform.position + forward + right,
            transform.position + forward - right
        };
        Handles.DrawAAConvexPolygon(coneVerts);
    }



    void Draw3DEnergyMeter()
    {
        // 能量柱底座
        Vector3 basePos = transform.position + Vector3.up * 1.8f;
        Gizmos.color = new Color(0.3f, 0.3f, 0.3f);
        Gizmos.DrawCube(basePos, new Vector3(1.2f, 0.3f, 1.2f));
        
        // 3D能量柱
        float energyHeight = currentEnergy / maxEnergy * 2f;
        Vector3 energyTop = basePos + Vector3.up * energyHeight;
        
        Gizmos.color = Color.Lerp(Color.red, Color.green, currentEnergy / maxEnergy);
        DrawThickLine(basePos, energyTop, 0.5f);
        
        // 刻度标记
        for (int i = 0; i <= 10; i++)
        {
            float h = i * 0.2f;
            Gizmos.color = h <= energyHeight ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Gizmos.DrawWireCube(
                basePos + Vector3.up * h,
                new Vector3(0.6f, 0.02f, 0.6f)
            );
        }
        
        // 数值显示
        Handles.Label(
            energyTop + Vector3.up * 0.3f,
            $"ENERGY: {currentEnergy:F0}/{maxEnergy}",
            new GUIStyle 
            { 
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan } 
            }
        );
    }

    void DrawSpeedTracker()
    {
        if (rb == null) return;
        
        // 速度轨迹（带历史记录）
        Vector3 currentVel = rb.velocity;
        if (currentVel.magnitude > 0.5f)
        {
            // 主方向箭头
            Gizmos.color = Color.yellow;
            DrawThickArrow(
                transform.position,
                transform.position + currentVel.normalized * 3f,
                0.3f,
                0.7f
            );
            
            // 速度值地面投影
            Handles.color = new Color(1, 0.8f, 0, 0.7f);
            Handles.DrawSolidDisc(
                transform.position + Vector3.up * 0.1f,
                Vector3.up,
                0.5f * (currentVel.magnitude / moveSpeed)
            );
            
            // 实时速度标签
            Handles.Label(
                transform.position + Vector3.up * 2f + Vector3.right * 1f,
                $"SPEED: {currentVel.magnitude:F1} m/s\n" +
                $"DIR: {(transform.forward * currentVel.magnitude).ToString("F1")}",
                new GUIStyle 
                { 
                    fontSize = 13,
                    normal = { textColor = Color.yellow },
                    padding = new RectOffset(5, 5, 5, 5),
                    alignment = TextAnchor.MiddleLeft
                }
            );
        }
    }

    // 辅助方法：绘制粗线
    void DrawThickLine(Vector3 start, Vector3 end, float width)
    {
        int segments = 6;
        Vector3 dir = (end - start).normalized;
        Vector3 cross = Vector3.Cross(dir, Vector3.up).normalized * width * 0.5f;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 360f / segments;
            Vector3 offset = Quaternion.AngleAxis(angle, dir) * cross;
            Gizmos.DrawLine(start + offset, end + offset);
        }
    }

    // 辅助方法：绘制3D箭头
    void DrawThickArrow(Vector3 start, Vector3 end, float shaftWidth, float headSize)
    {
        Vector3 dir = (end - start).normalized;
        
        // 箭杆
        DrawThickLine(start, end - dir * headSize, shaftWidth);
        
        // 箭头
        Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * headSize;
        Vector3 up = Vector3.Cross(dir, right).normalized * headSize;
        
        Vector3 arrowBase = end - dir * headSize;
        Gizmos.DrawLine(arrowBase, arrowBase + right + dir * headSize);
        Gizmos.DrawLine(arrowBase, arrowBase - right + dir * headSize);
        Gizmos.DrawLine(arrowBase, arrowBase + up + dir * headSize);
        Gizmos.DrawLine(arrowBase, arrowBase - up + dir * headSize);
    }
    private void OnCollisionEnter(Collision collision)
    {
        // 碰撞检测
        if (collision.gameObject.CompareTag("Static") || collision.gameObject.CompareTag("Hazard"))
        {
            // 碰撞惩罚
             Debug.Log($"{this.name} 碰撞障碍物，自动释放！");
            AddReward(-0.5f); // 根据任务难度调整惩罚值
            Assignment.ReleaseAgentTarget(this);
            EndEpisode();     // 提前结束回合
        }
    }

}