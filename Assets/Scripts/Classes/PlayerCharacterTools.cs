using RootMotion.FinalIK;
using RootMotion;
using UnityEngine;
using System;

// ���������� ������ � ���������� �������� ��������� ������ (������� � �� ������������)
public static class PlayerCharacterTools
{
    // ==================== �������� ��������� ������ =====================

    // ������� �������� ������ (������-������ �� ����� ������� ������������)
    public static GameObject InstantiatePlayerEntity(Transform spawnPosition,
        GameObject playerEntityPrefab, PlayerRaceSO playerRaceConfig)
    {
        // ��������� ����� ������
        Vector3 position = spawnPosition.position;
        Quaternion rotation = spawnPosition.rotation;

        // �������� - �������� ���� ��������� ���������
        GameObject playerEntity;
        playerEntity = GameObject.Instantiate(playerEntityPrefab, position, rotation);
        playerEntity.name = "Player entity";

        // ������-�������� (��������) ���� � ������ � ��������� ��� �������� ������
        GameObject player = playerEntity.transform.GetChild(0).gameObject;
        player.transform.position = position;

        // ---------- ����������� � ServiceLocator ----------
        ScriptTools.RegisterPlayerEntity(playerEntity);

        return playerEntity;
    }

    // ������� ���� ��������� (���������): ������� ������ ������ � �����, �������� �����
    public static PlayerCharacterPoints ChangePCRace(GameObject player, PlayerRaceSO playerRaceConfig)
    {
        // ������������ �������� � ����� ����� �����
        PlayerCharacterPoints characterPoints = new PlayerCharacterPoints();

        // ������� ������ ������ � �����, ���� ����
        GameObjectTools.DestroyAllChildren(player.transform);

        // ������������ ������-�������� ������ � ����� (�� ��-����������� ����� ����������� ����� ����� ����� � �������)
        GameObject modelAndBonesParent = GameObject.Instantiate(playerRaceConfig.rigAndBones.transform.gameObject,
            player.transform);
        Animator animator = player.GetComponent<Animator>();
        animator.avatar = modelAndBonesParent.GetComponent<Animator>().avatar;
        // ����������� ��������� ���������� � ������� ��� ������
        Transform modelAndBonesParentTransform = modelAndBonesParent.transform;
        modelAndBonesParentTransform.localPosition = Vector3.zero;
        modelAndBonesParentTransform.localRotation = Quaternion.identity;
        modelAndBonesParentTransform.localScale = Vector3.one;
        // ��������� �� �������� � ���������� ���� � ������
        GameObjectTools.MoveChildrenUp(modelAndBonesParent);
        GameObject.Destroy(modelAndBonesParent);
        Transform playerTransform = player.transform;
        GameObject bones = playerTransform.GetChild(0).gameObject;
        GameObject riggedModel = playerTransform.GetChild(1).gameObject;
        // ���������� ��������� ����� �� ����� ����� � ���
        animator.Rebind();
        animator.Update(0);

        // ---------- ����� ����� ----------
        // ������ ����: ����������/������
        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand).transform;
        Vector3 pointLocalRotation = new Vector3(-55, 80, 100);
        Vector3 pointLocalOffset = new Vector3(0.03f, 0.06f, 0);
        characterPoints.rightHandHoldingPoint = CreateHoldingPoint(hand, "Holding point",
            pointLocalRotation, pointLocalOffset);

        // ---------- ���������� ���������� ����������� ----------

        // ������ �������� ���������� � ������������ � ��������� (�������) ����� ������
        AdjustFeetCollider(player, riggedModel); // ��������� ����
        AdjustBodyCollider(player, riggedModel); // ��������� ����

        // ��������� ���������� � FullBodyBipedIK
        FullBodyBipedIK fullBodyBipedIK = player.GetComponent<FullBodyBipedIK>();
        if (fullBodyBipedIK == null)
            fullBodyBipedIK = player.AddComponent<FullBodyBipedIK>();
        var references = new BipedReferences();
        BipedReferences.AutoDetectReferences(ref references, player.transform,
            BipedReferences.AutoDetectParams.Default);
        fullBodyBipedIK.SetReferences(references, null);

        // ��������� ���������� � GrounderFBBIK
        GrounderFBBIK grounderFBBIK = player.GetComponent<GrounderFBBIK>();
        if (grounderFBBIK != null)
            GameObject.Destroy(grounderFBBIK);
        grounderFBBIK = player.AddComponent<GrounderFBBIK>();
        grounderFBBIK.solver.layers = LayerMask.GetMask("Default");
        grounderFBBIK.ik = fullBodyBipedIK;

        return characterPoints;
    }

    public static void LoadPlayer()
    {
        //LoadPlayerProgression();
        //LoadPlayerStates();
        //LoadPlayerInventory();
    }

    // ==================== ������ =====================

    // ������� ����� �����/������ � ������ �������
    private static Transform CreateHoldingPoint(Transform parentBone, string name, Vector3 rotation, Vector3 posOffset)
    {
        Transform handHoldingPoint = new GameObject("Holding point").transform;
        handHoldingPoint.parent = parentBone;
        handHoldingPoint.localPosition = posOffset;
        handHoldingPoint.localRotation = Quaternion.Euler(rotation);

        return handHoldingPoint;
    }
    // � ������� ����� (���������)
    private static Transform CreateHoldingPoint(Transform parentBone, string name, Vector3 rotation)
    {
        return CreateHoldingPoint(parentBone, name, rotation, Vector3.zero);
    }

    /// <summary>
    /// ������� ������ <paramref name="objectToAttach"/> � ������ <paramref name="attachingPoint"/> �� ���������
    /// </summary>
    /// <param name="objectToAttach"></param>
    /// <param name="attachingPoint"></param>
    /// <returns></returns>
    public static GameObject AttachObjectToAttachingPoint(GameObject objectToAttach, Transform attachingPoint)
    {
        // ������� ������ �� �������
        Transform attachedObject = GameObject.Instantiate(objectToAttach, attachingPoint).transform;
        attachedObject.localPosition = Vector3.zero;
        // ����������� "����� ������" ����������� ������� (������� ��� �����������/������)
        var holdingPoint = GameObjectTools.FindChildWithTag(attachedObject, "HoldingPoint")?.transform;

        // ���� ���� ����������� "����� ������" � ������� ������ � �� ����������
        if (holdingPoint != null)
            attachedObject.localPosition -= holdingPoint.localPosition;
        // ���� � ��������� ������ "����" � ������
        else
            attachedObject.localRotation = Quaternion.Euler(new Vector3(0, 20, 90));

        return attachedObject.gameObject;
    }

    // ==================== ��������� =====================

    // ���������� ������ ���������� ��� ("�������" ����)
    private static void AdjustFeetCollider(GameObject gameObject, GameObject gameObjectAdjustColliderTo)
    {
        if (gameObject.GetComponent<BoxCollider>() == null)
            gameObject.AddComponent<BoxCollider>();
        gameObject.GetComponent<BoxCollider>().center = new Vector3(0, 0.15f, 0);
        gameObject.GetComponent<BoxCollider>().size = new Vector3(0.3f, 0.3f, 0.3f);
        gameObject.GetComponent<BoxCollider>().center = CharacterColliderTools.GetAdjustedFeetColliderCenter
            (gameObject.GetComponent<BoxCollider>(), gameObjectAdjustColliderTo);
        gameObject.GetComponent<BoxCollider>().enabled = false; //!!! ���������� ���� "������������� ����������" � � �������
                                                                //�������� �� ����������
    }

    // ���������� ������ ���������� ���� ������������ ����������� ������� ���� (������)
    private static void AdjustBodyCollider(GameObject gameObject, GameObject gameObjectAdjustColliderTo)
    {
        if (gameObject.GetComponent<CapsuleCollider>() == null)
            gameObject.AddComponent<CapsuleCollider>();
        gameObject.GetComponent<CapsuleCollider>().radius = 0.25f;
        gameObject.GetComponent<CapsuleCollider>().height = CharacterColliderTools.GetAdjustedBodyColliderHeight
            (gameObject.GetComponent<BoxCollider>(), gameObjectAdjustColliderTo);
        gameObject.GetComponent<CapsuleCollider>().center = CharacterColliderTools.GetAdjustedBodyColliderCenter
            (gameObject.GetComponent<BoxCollider>(), gameObjectAdjustColliderTo);
    }
}