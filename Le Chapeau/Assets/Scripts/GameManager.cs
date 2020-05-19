using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Stats")]
    public bool gameEnded = false;
    public float timeToWin;             // total time to hold the hat to win
    public float invincibleDuration;    // prevents players from instantly losing hat
    private float hatPickupTime;        // the time that hat was picked up

    [Header("Players")]
    public string playerPrefabLocation; // path in Resources folder
    public Transform[] spawnPoints;     // array of all available spawn points
    public PlayerController[] players;  // array of all the players
    public int playerWithHat;           // id of the player with the hat
    private int playersInGame;          // number of players in the game

    // instance
    public static GameManager instance;

    private void Awake()
    {
        // lazy singleton - see NetworkManager for better implementation of this pattern
        // narrator justifies this based on the idea that we're not persisting this via DontDestroyOnLoad
        instance = this;
    }

    private void Start()
    {
        players = new PlayerController[PhotonNetwork.PlayerList.Length];
        photonView.RPC("ImInGame", RpcTarget.AllBuffered);
    }

    // called everytime a player finishes its start function
    [PunRPC]
    void ImInGame()
    {
        playersInGame++;

        // when the last player announces they're in game, each player can spawn themselves
        if (playersInGame == PhotonNetwork.PlayerList.Length)
            SpawnPlayer();
    }

    void SpawnPlayer()
    {
        // instantiate the player across the network
        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefabLocation, spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.identity);

        // get the player script
        PlayerController playerScript = playerObj.GetComponent<PlayerController>();

        // initialize the player
        playerScript.photonView.RPC("Initialize", RpcTarget.All, PhotonNetwork.LocalPlayer);
    }

    public PlayerController GetPlayer(int playerId)
    {
        return players.First(x => x.id == playerId);
    }

    public PlayerController GetPlayer(GameObject playerObject)
    {
        return players.First(x => x.gameObject == playerObject);
    }

    // called when the player hits the hatted player - giving them the hat
    [PunRPC]
    public void GiveHat(int playerId, bool initialGive)
    {
        // remove the hat from the currently hatted player
        if (!initialGive)
            GetPlayer(playerWithHat).SetHat(false);

        // give the hat to the new player
        playerWithHat = playerId;
        GetPlayer(playerId).SetHat(true);
        hatPickupTime = Time.time;
    }

    // is the player able to take the hat at this current time?
    public bool CanGetHat()
    {
        if (Time.time > hatPickupTime + invincibleDuration)
            return true;
        else
            return false;
    }
}
