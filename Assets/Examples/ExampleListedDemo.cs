using UnityEngine;
using Spiral.EditorToolkit.EditorSandbox;
using System;
using System.Collections.Generic;

public class ExampleListedDemo : MonoBehaviour
{
    public float R = 5;
    public float slowdownDirection = 20;
    public float slowdownRadius = 10;
    public Color color = Color.red;

    [Space][Header("Example Sandboxes")]
    public List<SandboxField> sandboxFields;

    public static bool useEditorTime = false;
    public static float deltaTime 
    { 
        get 
        {
#if UNITY_EDITOR 
            return useEditorTime ? Sandbox.editorDeltaTime : Time.fixedDeltaTime;
#else
            return Time.fixedDeltaTime;
#endif
        }
    }

    private static float publicStaticTime = 0;
    private static float size = 1;
    /// <summary>
    /// Проверка публичного статика
    /// </summary>
    [RunInSandbox]
    public static void RunStatic_ChangeSize()
    {
        publicStaticTime += deltaTime;
        size = 1.005f + Mathf.Cos(publicStaticTime / 5);
        if (publicStaticTime >= 2 * Mathf.PI * 5) publicStaticTime = 0;
    }

    private Vector3 direction = new Vector3(1, 0, 0);
    private float angle = 0;
    private float privateTime = 0;
    /// <summary>
    /// Проверка привата - меняет направление
    /// </summary>
    [RunInSandbox]
    private void RunPrivate_ChangeDirection()
    {
        privateTime += deltaTime;
        angle = privateTime / slowdownDirection;
        direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0).normalized;
        if (privateTime >= 2 * Mathf.PI * slowdownDirection) privateTime = 0;
    }

    private float protectedRadius = 0f;
    private float protectedTime = 0;
    /// <summary>
    /// Проверка протекта - меняет радиус, на которой отрисовывается кубик
    /// </summary>
    [RunInSandbox]
    protected void RunProtected_ChangeRadius()
    {
        protectedTime += deltaTime;
        protectedRadius = R * Mathf.Sin(protectedTime / slowdownRadius);
        if (protectedTime >= 2 * Mathf.PI * slowdownRadius) protectedTime = 0;
    }

    private float exceptionTime = 0;
    /// <summary>
    /// Функция, имитирующая сбой через 10 секунд своего выполнения
    /// </summary>
    [RunInSandbox]
    internal void RunInternal_ThrowExceptionByTimer()
    {
        exceptionTime += deltaTime;
        string timeRemains = (10 - exceptionTime).ToString("F1");
        Debug.Log($"<color=red>{timeRemains} remains</color>");
        if (exceptionTime > 10)
        {
            exceptionTime = 0;
            throw new TimeoutException("Woops");
        }
    }

    //---------------------------------------------------------------------------------------------
    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Vector3 position = transform.position + protectedRadius * direction;
        Gizmos.DrawWireCube(position, size * Vector3.one);
        Gizmos.DrawLine(transform.position, position);
    }
}
