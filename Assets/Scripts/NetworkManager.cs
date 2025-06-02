using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    [Header("Network Mode")]
    public bool isServer = false;

    [Header("Prefabs")]
    public GameObject serverPrefab;
    public GameObject clientPrefab;

    void Start()
    {
        if (isServer)
        {
            if (serverPrefab != null)
            {
                Instantiate(serverPrefab);
            }
            else
            {
                gameObject.AddComponent<UDPServer>();
            }
        }
        else
        {
            if (clientPrefab != null)
            {
                Instantiate(clientPrefab);
            }
            else
            {
                gameObject.AddComponent<UDPClient>();
            }
        }
    }
}