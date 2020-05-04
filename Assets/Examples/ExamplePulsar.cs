using UnityEngine;
using Spiral.EditorToolkit.EditorSandbox;

public class ExamplePulsar : MonoBehaviour
{
    public float R = 1f;
    public Color color = Color.red;
    private float time = 0;
    private float r = 1f;

    [Space][Header("Example Sandbox Inspector Field")]
    public SandboxField sandboxField;

    /// <summary>
    /// Просто функция для демонстрации особенностей работы песочницы
    /// </summary>
    [RunInSandbox] 
    public void Pulse()
    {
        time += Time.fixedDeltaTime;
        r = R * Mathf.Sin(time);
        if (time >= 2 * Mathf.PI) time = 0;
    }

    //---------------------------------------------------------------------------------------------
    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, r);
    }
}
