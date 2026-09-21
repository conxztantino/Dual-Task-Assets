using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        // Executa antes de qualquer outro script na cena
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        PlayerPrefs.DeleteAll();
        Debug.Log("[GameManager] Cache e PlayerPrefs limpos para nova sessão.");
#endif
    }
}