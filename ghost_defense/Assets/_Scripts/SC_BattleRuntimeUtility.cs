using System.Collections.Generic;
using UnityEngine;

public static class SC_BattleRuntimeUtility
{
    public static void CancelAllWaitingFieldCharacters()
    {
        List<IFieldCharacterRuntime> runtimes = SC_FieldCharacterRegistry.GetSnapshot();
        if (runtimes.Count > 0)
        {
            for (int i = 0; i < runtimes.Count; i++)
            {
                CancelWaitingRuntime(runtimes[i]);
            }

            return;
        }

        CancelFallbackShootCharacters();
        CancelFallbackDropCharacters();
    }

    public static bool IsBattleInputBlocked(SC_FinalMergePopup finalMergePopup, SC_ClearPopup clearPopup)
    {
        if (finalMergePopup != null && finalMergePopup.IsPopupOpen)
        {
            return true;
        }

        if (clearPopup != null && clearPopup.IsPopupOpen)
        {
            return true;
        }

        SC_DefeatPopup defeatPopup = Object.FindAnyObjectByType<SC_DefeatPopup>();
        if (defeatPopup != null && defeatPopup.IsPopupOpen)
        {
            return true;
        }

        SC_ExitPopup exitPopup = Object.FindAnyObjectByType<SC_ExitPopup>();
        return exitPopup != null && exitPopup.IsPopupOpen;
    }

    public static IFieldCharacterRuntime GetFieldRuntime(Collider2D collider)
    {
        if (collider == null)
        {
            return null;
        }

        IFieldCharacterRuntime runtime = collider.GetComponent<IFieldCharacterRuntime>();
        if (runtime == null)
        {
            runtime = collider.GetComponentInParent<IFieldCharacterRuntime>();
        }

        return runtime;
    }

    private static void CancelWaitingRuntime(IFieldCharacterRuntime runtime)
    {
        if (runtime == null || !runtime.IsWaiting)
        {
            return;
        }

        runtime.CancelInputAndReset();
    }

    private static void CancelFallbackShootCharacters()
    {
        SC_PlayerDragAndShoot[] shooters = Object.FindObjectsByType<SC_PlayerDragAndShoot>(FindObjectsInactive.Exclude);
        for (int i = 0; i < shooters.Length; i++)
        {
            SC_PlayerDragAndShoot shooter = shooters[i];
            if (shooter == null || !shooter.IsWaiting)
            {
                continue;
            }

            shooter.CancelInputAndReset();
        }
    }

    private static void CancelFallbackDropCharacters()
    {
        SC_DropCharacterController[] dropControllers = Object.FindObjectsByType<SC_DropCharacterController>(FindObjectsInactive.Exclude);
        for (int i = 0; i < dropControllers.Length; i++)
        {
            SC_DropCharacterController dropController = dropControllers[i];
            if (dropController == null || !dropController.IsWaiting)
            {
                continue;
            }

            dropController.CancelInputAndReset();
        }
    }
}
