using System;
using System.Collections;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public static class AnimationTools
{
    //
    public static IEnumerator PlayAnimationAndThenFunction(
        Animator animator,
        string layerName, 
        string animationName, 
        Action action, 
        float crossFadeDuration = 0.2f
        )
    {
        animator.CrossFade(animationName, crossFadeDuration);
        yield return WaitForAnimationToEnd(animator, layerName, animationName, 0.8f);

        action?.Invoke();
    }

    // ==================== Последовательное воспроизведение ====================

    // Последовательность из анимаций и действий
    public static IEnumerator PlayeAnimationsSequentially(
        Animator animator,
        string layerName,
        string firstAnimationName,
        Action firstActions,
        string secondAnimationName,
        Action secondActions,
        string thirdAnimationName,
        float crossFadeDuration = 0.2f
        )
    {
        IEnumerator WaitForAnimation(string animationName)
        {
            animator.CrossFade(animationName, crossFadeDuration);
            yield return WaitForAnimationToEnd(animator, layerName, animationName);
        }

        // --------------------------------

        // Ждём завершения первой анимации
        if (!string.IsNullOrEmpty(firstAnimationName))
            yield return WaitForAnimation(firstAnimationName);

        // Выполняем все переданные действия между анимациями
        firstActions?.Invoke();

        // Ждём завершения второй анимации
        if (!string.IsNullOrEmpty(secondAnimationName))
            yield return WaitForAnimation(secondAnimationName);

        // Выполняем все переданные действия между анимациями
        secondActions?.Invoke();

        // Ждём завершения третьей анимации
        if (!string.IsNullOrEmpty(thirdAnimationName))
            yield return WaitForAnimation(thirdAnimationName);
    }

    // То же, только п отриггерам, а не названиям анимаций
    public static IEnumerator PlaySheatheDrawSequence(
        Animator animator,
        string layerName,
        string sheatheTriggerName,
        Action firstActions,
        string drawTriggerName,
        Action secondActions,
        string waitAnimationToEndName = "",
        float crossFadeDuration = 0.2f
        )
    {
        if (!string.IsNullOrEmpty(waitAnimationToEndName))
            yield return WaitForAnimationToEnd(animator, layerName, waitAnimationToEndName);

        animator.SetBool("TransitToStandart", false);
        if (!string.IsNullOrEmpty(sheatheTriggerName))
        {
            //animator.SetTrigger(sheatheTriggerName);
            animator.CrossFade(layerName+"."+ sheatheTriggerName, crossFadeDuration);
            // Ждём завершения анимации убирания (начало)
            yield return WaitForAnimationToEnd(animator, layerName, sheatheTriggerName);

            // Выполняем все переданные действия между анимациями (деспавн текущего оружия из рук)
            firstActions?.Invoke();

            string sheatheEndAnimationName = sheatheTriggerName + "2"; // (!) В аниматоре анимации имеют пару: начало и конец
            animator.CrossFade(layerName + "." + sheatheEndAnimationName, crossFadeDuration);
            // Ждём завершения анимации убирания (конец)
            yield return WaitForAnimationToEnd(animator, layerName, sheatheEndAnimationName);
        }

        animator.SetBool("TransitToStandart", true);
        if (!string.IsNullOrEmpty(drawTriggerName))
        {
            animator.CrossFade(layerName + "." + drawTriggerName, crossFadeDuration);
            // Ждём завершения анимации доставания (начало)
            yield return WaitForAnimationToEnd(animator, layerName, drawTriggerName);

            // Выполняем все переданные действия между анимациями (спавн нового оружия в руках)
            secondActions?.Invoke();

            string drawEndAnimationName = drawTriggerName + "2";
            Debug.Log(drawEndAnimationName);
            animator.CrossFade(layerName + "." + drawEndAnimationName, crossFadeDuration);
            // Ждём завершения анимации доставания (конец)
            yield return WaitForAnimationToEnd(animator, layerName, drawEndAnimationName);
        }
    }

    // Ожидание завершения анимации
    private static IEnumerator WaitForAnimationToEnd(Animator animator, string layerName, string animationName,
        float normalizedTime = 0.99f)
    {
        int layerIndex = animator.GetLayerIndex(layerName);

        // Ждём, пока анимация начнёт проигрываться
        while (true)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (stateInfo.IsName(animationName) && !animator.IsInTransition(layerIndex))
                break;
            yield return null;
        }

        // Ждём завершения анимации
        while (true)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (stateInfo.normalizedTime >= normalizedTime && !animator.IsInTransition(layerIndex))
                break;
            yield return null;
        }
    }

    // ======================================================

    public static IEnumerator PlaySequence(
        Animator animator,
        string layerName,
        string firstAnimation,
        Action firstAction,
        string secondAnimation,
        Action secondAction,
        string thirdAnimation,
        Action thirdAction,
        float crossFadeDuration = 0.2f
    )
    {
        // Вспомогательная функция ожидания завершения анимации
        IEnumerator WaitForAnimation(string animationName)
        {
            if (string.IsNullOrEmpty(animationName))
                yield break;

            int layerIndex = animator.GetLayerIndex(layerName);

            // Начинаем анимацию
            animator.CrossFade(animationName, crossFadeDuration);

            // Ждём, пока анимация начнёт проигрываться
            while (true)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
                if (stateInfo.IsName(animationName) && !animator.IsInTransition(layerIndex))
                    break;
                yield return null;
            }

            // Ждём завершения анимации
            while (true)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
                if (stateInfo.normalizedTime >= 1f && !animator.IsInTransition(layerIndex))
                    break;
                yield return null;
            }
        }

        // Первая анимация и действие
        yield return WaitForAnimation(firstAnimation);
        firstAction?.Invoke();

        // Вторая анимация и действие
        yield return WaitForAnimation(secondAnimation);
        secondAction?.Invoke();

        // Третья анимация и действие
        yield return WaitForAnimation(thirdAnimation);
        thirdAction?.Invoke();
    }

}
