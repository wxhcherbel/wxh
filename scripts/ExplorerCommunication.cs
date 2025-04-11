using System.Collections.Generic;
using UnityEngine;
using System.Linq; 

public class ExplorerCommunication : MonoBehaviour
{
    // 线程安全的资源声明字典
    private Dictionary<Transform, bool> resourceClaims = new Dictionary<Transform, bool>();
    private object lockObj = new object();

    // 检查资源是否被占用
    public bool IsResourceClaimed(Transform resource)
    {
        lock (lockObj) {
            return resourceClaims.ContainsKey(resource) && resourceClaims[resource];
        }
    }

    // 尝试声明资源（原子操作）
    public bool TryClaimResource(Transform resource)
    {
        lock (lockObj) {
            if (!resourceClaims.ContainsKey(resource)) {
                resourceClaims.Add(resource, true);
                return true;
            }
            return !resourceClaims[resource] && (resourceClaims[resource] = true);
        }
    }

    // 释放资源
    public void ReleaseResource(Transform resource)
    {
        lock (lockObj) {
            if (resourceClaims.ContainsKey(resource)) {
                resourceClaims[resource] = false;
            }
        }
    }

    // 重置所有声明状态
    public void ResetAllClaims()
    {
        lock (lockObj) {
            foreach (var key in resourceClaims.Keys.ToList()) {
                resourceClaims[key] = false;
            }
        }
    }
}