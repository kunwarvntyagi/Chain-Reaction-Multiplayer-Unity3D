using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;
using Photon;

public class SimpleNetMan : PunBehaviour
{
	public GameObject connectCanvas, lobbyCanvas, optionsPanel,lobbyPanel,startGameButton,gameCanvas;

	[Tooltip("The maximum number of players per room")]
	public byte maxPlayersPerRoom = 4;

	public InputField myNameInput, createRoomNameInput, joinRoomNameInput;
	public Text roomName, roomList;

	string playersList = "";

	public GameObject parent, prefab;

	void Start()
	{
	}

	public void Connect()
	{
		if (!PhotonNetwork.connected)
		{
			PhotonNetwork.ConnectUsingSettings("1");
		}   
	}

	public override void OnConnectedToMaster()
	{
		Debug.Log("Region:"+PhotonNetwork.networkingPeer.CloudRegion);
		PhotonNetwork.playerName = myNameInput.text;
		connectCanvas.SetActive (false);
		lobbyCanvas.SetActive (true); 
	}

	public void joinRandomRoom()
	{
		PhotonNetwork.JoinRandomRoom();
	}

	public void createYourRoom()
	{
		RoomOptions roomOptions = new RoomOptions ();
		roomOptions.MaxPlayers = 4;
		roomOptions.IsVisible = false;

		PhotonNetwork.CreateRoom (createRoomNameInput.text);
	}

	public void joinFriendsRoom()
	{
		PhotonNetwork.JoinRoom (joinRoomNameInput.text);
	}

	public override void OnJoinedRoom()
	{
		Debug.Log ("Room Name is " + PhotonNetwork.room.Name);
		Debug.Log("<Color=Green>OnJoinedRoom</Color> with "+PhotonNetwork.room.PlayerCount+" Player(s)");

		roomName.text = "Room : " + PhotonNetwork.room.Name;
		optionsPanel.SetActive (false);
		lobbyPanel.SetActive (true);

		updatePlayerList ();

		// #Critical: We only load if we are the first player, else we rely on  PhotonNetwork.automaticallySyncScene to sync our instance scene.
		if (PhotonNetwork.room.PlayerCount == 1)
		{
			// Debug.Log("We load the 'Room for 1' ");

			// #Critical
			// Load the Room Level. 
			// PhotonNetwork.LoadLevel("PunBasics-Room for 1");

		}
	}

	public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
	{
		Debug.Log("<Color=Red>OnPhotonRandomJoinFailed</Color>: Next -> Create a new Room");

		// #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
		PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = this.maxPlayersPerRoom}, null);
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer player)
	{
		Debug.Log("<Color=Green>Toatal Players : </Color>  "+PhotonNetwork.room.PlayerCount);
		if (PhotonNetwork.isMasterClient && PhotonNetwork.room.PlayerCount>1) 
		{
			startGameButton.SetActive (true);
		}
		updatePlayerList ();
	}

	void updatePlayerList()
	{
		playersList = "";
		PhotonPlayer[] playerList = PhotonNetwork.playerList;
		for (int i = 1; i < parent.transform.childCount; i++) 
		{
			Destroy (parent.transform.GetChild(i).gameObject);
		}
		for (int i = 0; i < playerList.Length; i++) 
		{
			playersList += playerList [i].NickName + "\n";
			GameObject obj = Instantiate (prefab, parent.transform) as GameObject;
			obj.SetActive (true);
			obj.transform.GetComponentInChildren<Text> ().text = playerList [i].NickName;
		}
		roomList.text = playersList;
	}

	public override void OnPhotonPlayerDisconnected(PhotonPlayer player)
	{
		Debug.Log("<Color=Green>Toatal Players : </Color>  "+PhotonNetwork.room.PlayerCount);
		updatePlayerList ();
	}

	[PunRPC]
	public void startGame()
	{
		// 
		lobbyCanvas.SetActive (false);
		gameCanvas.SetActive (true);

		if (PhotonNetwork.isMasterClient) 
		{
			PhotonNetwork.room.IsOpen = false;
			PhotonView photonView = PhotonView.Get(this);
			if (photonView != null) 
			{
				Debug.Log ("RPC Call");
				photonView.RPC ("startGame",PhotonTargets.Others);
			}
		}

			

	}

}
