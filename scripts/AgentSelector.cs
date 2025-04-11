using UnityEngine;
using System.Collections.Generic;

public class AgentSelector : MonoBehaviour
{
    // 引用信息面板脚本（用于弹窗）
    public AgentInfoPanel infoPanel;

    void Update()
    {
        // 每一帧检查是否点击了鼠标左键
        if (Input.GetMouseButtonDown(0)) // 0 代表鼠标左键
        {
            // 发出一条从鼠标位置出发的射线
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 检测射线是否击中任何物体
            if (Physics.Raycast(ray, out hit))
            {
                // 检查被点击的物体上是否挂载了 CustomAgent 组件
                CustomAgent agent = hit.collider.GetComponent<CustomAgent>();
                if (agent != null)
                {
                    Debug.Log("AgentSelector -- agent != null");
                    // 弹出 UI 面板，显示这个 Agent 的信息
                    infoPanel.Show(agent);
                    
                }
            }
        }
    }
}