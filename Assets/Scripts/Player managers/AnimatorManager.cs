using Unity.VisualScripting;
using UnityEngine;
using System;
using NUnit.Framework.Internal;

public class AnimatorManager : InitializableMonoBehaviour
{
    public Animator animator;
    int horizontal;
    int vertical;

    private void Awake()
    {
        ScriptTools.RegisterScript(this);
    }

    private void Start()
    {
        ServiceLocator serviceLocator = ServiceLocator.Instance;
        GameObject player = serviceLocator.player;

        if (AreDependenciesInitialized(player))
            Initialize();

        if (!IsInitialized())
            return;

        animator = player.GetComponent<Animator>();
        horizontal = Animator.StringToHash("Horizontal");
        vertical = Animator.StringToHash("Vertical");

    }

    // ==================================================

    // Воспроизвести анимацию с интерполяцией
    public void PlayTargetAnimation(string targetAnimation, bool isInteracting)
    {
        if (!IsInitialized())
            return;

        animator.SetBool("isInteracting", isInteracting);
        animator.CrossFade(targetAnimation, 0.2f);
    }
    //
    public void PlayAnimationAndThenFunction(
        string layerName,
        string targetAnimation,
        Action action
        )
    {
        this.StartCoroutine(
            AnimationTools.PlayAnimationAndThenFunction(
            animator,
            layerName,
            targetAnimation,
            action
            ));
    }
    //
    public void PlaySequence(
        string layerName,
        string firstAnimationName,
        Action firstAction,
        string secondAnimationName,
        Action secondAction,
        string thirdAnimationName
        )
    {
        StartCoroutine(AnimationTools.PlaySequence(
        animator,
        layerName, // Укажите название слоя
        firstAnimationName, // Название первой анимации
        () => Debug.Log("Первое действие: убрали оружие"), // Действие после первой анимации
        secondAnimationName, // Название второй анимации
        () => Debug.Log("Второе действие: смена оружия"), // Действие после второй анимации
        thirdAnimationName, // Название третьей анимации
        () => Debug.Log("Третье действие: завершение") // Действие после третьей анимации
    ));
    }
    //
    public void PlayAnimations(
        string layerName, 
        string firstAnimationName,
        Action firstAction,
        string secondAnimationName,
        Action secondAction,
        string waitAnimationToEndName = ""
        )
    {
        this.StartCoroutine(
            AnimationTools.PlaySheatheDrawSequence(
            animator,
            layerName,
            firstAnimationName,
            firstAction,
            secondAnimationName,
            secondAction,
            waitAnimationToEndName
            ));
    }

    // Нормализация значений вектора движения
    private float GetSnappedValue(float movement)
    {
        if (movement > 0 && movement < 0.55f) { return 0.5f; }
        else if (movement > 0.55f) { return 1; }
        else if (movement < 0 && movement > -0.55f) { return -0.5f; }
        else if (movement < -0.55f) { return -1; }
        else { return 0; };
    }

    // Обновить значения векторов направлений аниматора
    public void UpdateAnimatorValues(float horizontalMovement, float verticalMovement, bool isSprinting, bool isWalking)
    {
        if (!IsInitialized())
            return;

        // Running (Default)
        float snappedHorizontal = GetSnappedValue(horizontalMovement);
        float snappedVertical = GetSnappedValue(verticalMovement);

        if (isWalking & !isSprinting)
        {
            snappedHorizontal = horizontalMovement;
            snappedVertical = 0.5f;
        }
        if (isSprinting)
        {
            snappedHorizontal = horizontalMovement;
            snappedVertical = 2;
        }

        animator.SetFloat(horizontal, snappedHorizontal, 0.1f, Time.deltaTime);
        animator.SetFloat(vertical, snappedVertical, 0.1f, Time.deltaTime);
    }
}
