using UnityEngine;

public static class SC_BattleMergeService
{
    public static bool TryResolveMergeTarget<TMergeController>(
        Collider2D sourceCollider,
        TMergeController self,
        System.Func<TMergeController, bool> isAlreadyMerged,
        out TMergeController otherMerge)
        where TMergeController : Component
    {
        otherMerge = null;
        if (sourceCollider == null || self == null)
        {
            return false;
        }

        otherMerge = sourceCollider.GetComponent<TMergeController>();
        if (otherMerge == null)
        {
            otherMerge = sourceCollider.GetComponentInParent<TMergeController>();
        }

        return otherMerge != null
            && otherMerge != self
            && (isAlreadyMerged == null || !isAlreadyMerged(otherMerge));
    }

    public static bool TryCalculateNextGrade(SC_CharacterPresenter myPresenter, SC_CharacterPresenter otherPresenter, out int nextGrade)
    {
        nextGrade = 1;
        if (myPresenter == null || otherPresenter == null)
        {
            return false;
        }

        int myGrade = myPresenter.MergeGrade;
        int otherGrade = otherPresenter.MergeGrade;
        if (myGrade != otherGrade)
        {
            return false;
        }

        nextGrade = Mathf.Clamp(myGrade + 1, 1, 10);
        return true;
    }

    public static bool AreCollidersTouching(Component first, Component second, float contactTolerance)
    {
        if (first == null || second == null)
        {
            return false;
        }

        Collider2D firstCollider = first.GetComponent<Collider2D>();
        Collider2D secondCollider = second.GetComponent<Collider2D>();
        if (firstCollider == null || secondCollider == null)
        {
            return false;
        }

        ColliderDistance2D colliderDistance = firstCollider.Distance(secondCollider);
        return colliderDistance.distance <= Mathf.Max(0f, contactTolerance);
    }

    public static GameObject CreateMergedObject(
        GameObject mergeObjectPrefab,
        Transform firstTransform,
        Transform secondTransform,
        Transform spawnParent,
        int nextGrade,
        bool useDropScale)
    {
        if (mergeObjectPrefab == null || firstTransform == null || secondTransform == null)
        {
            return null;
        }

        Vector3 spawnPosition = (firstTransform.position + secondTransform.position) * 0.5f;
        Transform parent = spawnParent != null ? spawnParent : firstTransform.parent;
        GameObject mergedObject = Object.Instantiate(mergeObjectPrefab, spawnPosition, Quaternion.identity, parent);
        ConfigurePresenter(mergedObject, nextGrade, useDropScale);
        return mergedObject;
    }

    public static SC_ComboManager.ComboMergeResult NotifyMergeCreated(int mergedGrade)
    {
        SC_ComboManager.ComboMergeResult comboMergeResult = SC_ComboManager.NotifyMergeCreatedGlobal();
        ReportMergedGradeToPreviewUI(mergedGrade);
        return comboMergeResult;
    }

    public static void NotifyBattleMergeAttack(SC_BattleManager battleManager, int mergedGrade, float comboDamageMultiplier)
    {
        if (battleManager == null)
        {
            battleManager = Object.FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager != null)
        {
            battleManager.NotifyMergeAttack(mergedGrade, comboDamageMultiplier);
        }
    }

    public static void SetPhysicsEnabled(GameObject targetObject, bool isEnabled, bool stopMotion)
    {
        if (targetObject == null)
        {
            return;
        }

        Rigidbody2D rb2D = targetObject.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            if (stopMotion)
            {
                rb2D.linearVelocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
            }

            rb2D.simulated = isEnabled;
        }

        Collider2D[] colliders = targetObject.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = isEnabled;
            }
        }
    }

    private static void ConfigurePresenter(GameObject mergedObject, int nextGrade, bool useDropScale)
    {
        if (mergedObject == null)
        {
            return;
        }

        SC_CharacterPresenter mergedPresenter = mergedObject.GetComponent<SC_CharacterPresenter>();
        if (mergedPresenter != null)
        {
            mergedPresenter.Configure(nextGrade, true, useDropScale);
        }
    }

    private static void ReportMergedGradeToPreviewUI(int mergedGrade)
    {
        SC_CharacterGradePreviewUI previewUI = Object.FindAnyObjectByType<SC_CharacterGradePreviewUI>();
        if (previewUI != null)
        {
            previewUI.ReportReachedGrade(mergedGrade);
        }
    }
}
