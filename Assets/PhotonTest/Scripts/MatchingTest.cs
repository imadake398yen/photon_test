using UnityEngine;
using UnityEngine.UI;
using System.Collections;
// using System.Collections.Hashtable;

public class MatchingTest : MonoBehaviour {

	public Text stateLabel;
	public Text roomLabel;
	public Text player1Name;
	public Text player2Name;

	public Text inputRoomName;
	public Text inputPlayerName;
	public Text inputRemoteMessage;

	public Slider levelSlider;

	public Text remoteMessage;

	string roomName;
	bool joinedLobby = false;
	bool host;

	public GameObject loginButton;

	string myName = "yamada";
	ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
	void Start () {
	}


	void Awake () {
		PhotonNetwork.ConnectUsingSettings("0.1");
	}
	
	string lastState;
	void Update () {
		if (lastState != PhotonNetwork.connectionStateDetailed.ToString()){
			stateLabel.text = "CurrentState : " +PhotonNetwork.connectionStateDetailed.ToString();
			// print ("CurrentState : "+PhotonNetwork.connectionStateDetailed.ToString());
		}
		lastState = PhotonNetwork.connectionStateDetailed.ToString();

		if (inputPlayerName.text.Length > 0) {
			loginButton.SetActive(true);
		} else {
			loginButton.SetActive(false);
		}

	}

	void OnJoinedLobby () {
		joinedLobby = true;
	}

	public void PushLoginButton () {
		if (inputPlayerName != null) {
			PhotonNetwork.playerName = inputPlayerName.text;
			player1Name.text = "Player1 : " + PhotonNetwork.playerName;

			ExitGames.Client.Photon.Hashtable status = 
				new ExitGames.Client.Photon.Hashtable() { 
					{ "level", levelSlider.value },
					{ "hp", 300f },
					{ "attackValue", 30f },
					{ "defenseValue", 20f }
				};

			PhotonNetwork.SetPlayerCustomProperties(status);
		}
	}

	public void PushJoinButton () {
		if (joinedLobby && PhotonNetwork.connectionStateDetailed.ToString() == "JoinedLobby") {
			roomName = inputRoomName.text;

			PhotonNetwork.CreateRoom(roomName, true, true, 2);
		}
	}

	public void PushRandomMatchingButton () {
		if (joinedLobby && PhotonNetwork.connectionStateDetailed.ToString() == "JoinedLobby") {
			PhotonNetwork.JoinRandomRoom();
		}
	}

	public void PushLeaveRoomButton () {
		PhotonNetwork.LeaveRoom();
	}

	void OnPhotonRandomJoinFailed () {
		if (joinedLobby && PhotonNetwork.connectionStateDetailed.ToString() == "JoinedLobby") {
			roomName = System.Guid.NewGuid().ToString();
			PhotonNetwork.CreateRoom(roomName, true, true, 2);
		}
	}

	void OnJoinedRoom () {
		roomLabel.text = "Room : " + PhotonNetwork.room.name;
		host = PhotonNetwork.isMasterClient;

		PhotonPlayer[] player = PhotonNetwork.otherPlayers;

		if (host) {
			player1Name.text = "Player1 : " + PhotonNetwork.playerName;
		} else {
			if (player[0] != null)
				player1Name.text = "Player1 : " + player[0].name;
			player2Name.text = "Player2 : " + PhotonNetwork.playerName;
			ExitGames.Client.Photon.Hashtable enemyStatus = player[0].customProperties;
			print ( "enemyLevel : " + enemyStatus["level"] );
			print ( "enemyHP : " + player[0].customProperties["hp"]);
		}


	}

	[RPC]
	public void PushMessage () {
		remoteMessage.text = inputRemoteMessage.text;
	}

	void OnLeftRoom () {
		roomLabel.text = "Room : ";
		PhotonNetwork.JoinLobby();
	}

	void OnPhotonCreateRoomFailed () {
		stateLabel.text = "Create Room failed";
	}



}






