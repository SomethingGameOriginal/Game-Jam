using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RootMotionController : MonoBehaviour
{
    public Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Включает Root Motion. Вызывается из Timeline через Signal.
    /// </summary>
    public void EnableRootMotion()
    {
        if (_animator != null)
        {
            _animator.applyRootMotion = true;
        }
    }

    /// <summary>
    /// Выключает Root Motion. Вызывается из Timeline через Signal.
    /// </summary>
    public void DisableRootMotion()
    {
        if (_animator != null)
        {
            _animator.applyRootMotion = false;
        }
    }

    /// <summary>
    /// Универсальный метод с параметром (не для Signal, а для прямого вызова).
    /// </summary>
    public void SetRootMotion(bool enable)
    {
        if (_animator != null)
        {
            _animator.applyRootMotion = enable;
            Debug.Log("SS");    
        }
    }
}