using UnityEngine;

public class RaceChange : MonoBehaviour
{
    [field: SerializeField] public PlayerRaceSO newRace { get; private set; }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name != "Player")
            return;

        PlayerManager playerManager = ServiceLocator.Instance.playerEntity.GetComponent<PlayerManager>();

        if (playerManager == null)
            return;

        playerManager.ChangePlayerRace(newRace);
    }
}
