using RootMotion.FinalIK;
using RootMotion;
using UnityEngine;
using System;

// Иструменты работы с физическим обличием персонажа игрока (моделью и ее компонентами)
public static class PlayerCharacterTools
{
    // ==================== Создание персонажа игрока =====================

    // Создать сущность игрока (объект-префаб со всеми нужными компонентами)
    public static GameObject InstantiatePlayerEntity(Transform spawnPosition,
        GameObject playerEntityPrefab, PlayerRaceSO playerRaceConfig)
    {
        // Параметры точки спавна
        Vector3 position = spawnPosition.position;
        Quaternion rotation = spawnPosition.rotation;

        // Сущность - родитель всех элементов персонажа
        GameObject playerEntity;
        playerEntity = GameObject.Instantiate(playerEntityPrefab, position, rotation);
        playerEntity.name = "Player entity";

        // Объект-носитель (родитель) меша и костей – физически сам персонаж игрока
        GameObject player = playerEntity.transform.GetChild(0).gameObject;
        player.transform.position = position;

        // ---------- Регистрация в ServiceLocator ----------
        ScriptTools.RegisterPlayerEntity(playerEntity);

        return playerEntity;
    }

    // Сменить расу персонажа (визуально): удалить старые модель и кости, вставить новые
    public static PlayerCharacterPoints ChangePCRace(GameObject player, PlayerRaceSO playerRaceConfig)
    {
        // Возвращаемое значение – набор точек связи
        PlayerCharacterPoints characterPoints = new PlayerCharacterPoints();

        // Удаляем старые модель и кости, если есть
        GameObjectTools.DestroyAllChildren(player.transform);

        // Инстанцируем объект-носитель модель и кости (не по-отдельности чтобы сохранилась связь между мешем и костьми)
        GameObject modelAndBonesParent = GameObject.Instantiate(playerRaceConfig.rigAndBones.transform.gameObject,
            player.transform);
        Animator animator = player.GetComponent<Animator>();
        animator.avatar = modelAndBonesParent.GetComponent<Animator>().avatar;
        // Настраиваем локальные координаты и масштаб для модели
        Transform modelAndBonesParentTransform = modelAndBonesParent.transform;
        modelAndBonesParentTransform.localPosition = Vector3.zero;
        modelAndBonesParentTransform.localRotation = Quaternion.identity;
        modelAndBonesParentTransform.localScale = Vector3.one;
        // Отделение от родители и разделение меша и костей
        GameObjectTools.MoveChildrenUp(modelAndBonesParent);
        GameObject.Destroy(modelAndBonesParent);
        Transform playerTransform = player.transform;
        GameObject bones = playerTransform.GetChild(0).gameObject;
        GameObject riggedModel = playerTransform.GetChild(1).gameObject;
        // Перезапуск аниматора чтобы он нашел кости и меш
        animator.Rebind();
        animator.Update(0);

        // ---------- Точки хвата ----------
        // Правая руки: инструмент/оружие
        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand).transform;
        Vector3 pointLocalRotation = new Vector3(-55, 80, 100);
        Vector3 pointLocalOffset = new Vector3(0.03f, 0.06f, 0);
        characterPoints.rightHandHoldingPoint = CreateHoldingPoint(hand, "Holding point",
            pointLocalRotation, pointLocalOffset);

        // ---------- Обновление параметров компонентов ----------

        // Правка размеров коллайдера в соответствии с размерами (высотой) новой модели
        AdjustFeetCollider(player, riggedModel); // Коллайдер шага
        AdjustBodyCollider(player, riggedModel); // Коллайдер тела

        // Инверсная кинематика – FullBodyBipedIK
        FullBodyBipedIK fullBodyBipedIK = player.GetComponent<FullBodyBipedIK>();
        if (fullBodyBipedIK == null)
            fullBodyBipedIK = player.AddComponent<FullBodyBipedIK>();
        var references = new BipedReferences();
        BipedReferences.AutoDetectReferences(ref references, player.transform,
            BipedReferences.AutoDetectParams.Default);
        fullBodyBipedIK.SetReferences(references, null);

        // Инверсная кинематика – GrounderFBBIK
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

    // ==================== Сцепка =====================

    // Создать точку хвата/сцепки с другой моделью
    private static Transform CreateHoldingPoint(Transform parentBone, string name, Vector3 rotation, Vector3 posOffset)
    {
        Transform handHoldingPoint = new GameObject("Holding point").transform;
        handHoldingPoint.parent = parentBone;
        handHoldingPoint.localPosition = posOffset;
        handHoldingPoint.localRotation = Quaternion.Euler(rotation);

        return handHoldingPoint;
    }
    // В нулевой точке (локальной)
    private static Transform CreateHoldingPoint(Transform parentBone, string name, Vector3 rotation)
    {
        return CreateHoldingPoint(parentBone, name, rotation, Vector3.zero);
    }

    /// <summary>
    /// Сцепить объект <paramref name="objectToAttach"/> с точкой <paramref name="attachingPoint"/> на персонаже
    /// </summary>
    /// <param name="objectToAttach"></param>
    /// <param name="attachingPoint"></param>
    /// <returns></returns>
    public static GameObject AttachObjectToAttachingPoint(GameObject objectToAttach, Transform attachingPoint)
    {
        // Создаем объект из префаба
        Transform attachedObject = GameObject.Instantiate(objectToAttach, attachingPoint).transform;
        attachedObject.localPosition = Vector3.zero;
        // Специальная "точка сцепки" сцепляемого объекта (рукоять для инструмента/оружия)
        var holdingPoint = GameObjectTools.FindChildWithTag(attachedObject, "HoldingPoint")?.transform;

        // Если есть специальная "точка сцепки" – сцепить объект в ее координате
        if (holdingPoint != null)
            attachedObject.localPosition -= holdingPoint.localPosition;
        // Нету – повернуть объект "дном" к ладони
        else
            attachedObject.localRotation = Quaternion.Euler(new Vector3(0, 20, 90));

        return attachedObject.gameObject;
    }

    // ==================== Коллайдер =====================

    // Подправить высоту колалйдера ног ("парящей" зоны)
    private static void AdjustFeetCollider(GameObject gameObject, GameObject gameObjectAdjustColliderTo)
    {
        if (gameObject.GetComponent<BoxCollider>() == null)
            gameObject.AddComponent<BoxCollider>();
        gameObject.GetComponent<BoxCollider>().center = new Vector3(0, 0.15f, 0);
        gameObject.GetComponent<BoxCollider>().size = new Vector3(0.3f, 0.3f, 0.3f);
        gameObject.GetComponent<BoxCollider>().center = CharacterColliderTools.GetAdjustedFeetColliderCenter
            (gameObject.GetComponent<BoxCollider>(), gameObjectAdjustColliderTo);
        gameObject.GetComponent<BoxCollider>().enabled = false; //!!! Отключение ради "левитирующего коллайдера" – в будущем
                                                                //заменить на переменную
    }

    // Подправить высоту коллайдера тело относительно физического размера тела (модели)
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