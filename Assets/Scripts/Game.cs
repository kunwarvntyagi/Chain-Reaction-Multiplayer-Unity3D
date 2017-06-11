using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using Photon;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class Game : PunBehaviour, IPunTurnManagerCallbacks
{

	private PunTurnManager turnManager;
	private bool[] isGameOver,isTurnCame;

	public Sprite[] ballSprites;

	int[,] gridValues,currTurnMat;
	int[,] lastValues,lastTurnMat;

	public Text[,] textsValues;

	public GameObject[] rows;

	bool isBlasting = false;
	bool canUndo = false;

	int totalPlayers = 0;
	int currentTurn;
	public Text playerTurnText;

	private bool isClicked = false;

	Color32[] textColors;

	private int onGameScreen = 0;

	// Use this for initialization
	void OnEnable () 
	{
		init_grid ();
		init_color ();
		isBlasting = false;
		totalPlayers = PhotonNetwork.room.PlayerCount;
		isGameOver = new bool[totalPlayers];
		isTurnCame = new bool[totalPlayers];
		for (int i = 0; i < totalPlayers; i++) 
		{
			isGameOver [i] = false;
			isTurnCame [i] = false;
		}
		turnManager = this.gameObject.AddComponent<PunTurnManager>();
		turnManager.TurnManagerListener = this;

		if (!PhotonNetwork.isMasterClient) 
		{
			PhotonView photonView = PhotonView.Get(this);
			if (photonView != null) 
			{
				photonView.RPC ("updateOnMainScreen",PhotonTargets.MasterClient);
			}
		}


	}

	void init_color()
	{
		textColors = new Color32[5];
		textColors [0] = Color.red;
		textColors [1] = Color.green;
		textColors [2] = Color.blue;
		textColors [3] = Color.cyan;
		textColors [4] = Color.magenta;
	}

	void init_grid()
	{
		gridValues = new int[9, 6];
		currTurnMat = new int[9, 6];
		lastValues = new int[9, 6];
		lastTurnMat = new int[9, 6];

		for (int i = 0; i < 9; i++) 
		{
			for (int j = 0; j < 6; j++)
			{
				gridValues [i,j] = 0;
				currTurnMat [i,j] = -1;
			}
		}

		StartCoroutine(init_all_texts ());

	}

	IEnumerator init_all_texts()
	{
		textsValues = new Text[9, 6];
		for (int i = 0; i < 9; i++) 
		{
			for (int j = 0; j < 6; j++) 
			{
				textsValues [i, j] = rows [i].transform.GetChild (j).transform.GetChild(0).GetComponent<Text>();
				rows [i].transform.GetChild (j).tag = "-1";
				textsValues [i, j].tag = "-1";
				textsValues [i, j].gameObject.SetActive (false);
			}
		}
		yield return null;

		currentTurn = -1;
		// playerTurnText.text = "Player " + (currentTurn+1)  + " Turn";

	}

	void changeRowColor(int val)
	{
		for (int i = 0; i < rows.Length; i++) 
		{
			rows[i].GetComponent<Image>().color = textColors[val];
		}
	}

	void changeTurn()
	{
		currentTurn++;
		if (currentTurn >= totalPlayers)
			currentTurn = 0;
		// turnManager.SendMove ("",true);
		if (isGameOver [currentTurn]) 
		{
			this.turnManager.SendMove ("", true);
			return;
		}
		isClicked = false;
		PhotonPlayer[] playerList = PhotonNetwork.playerList;
		string currPlayer = "" ;
		for (int i = 0; i < playerList.Length; i++) 
		{
			if ((currentTurn + 1) == playerList [i].ID) 
			{
				currPlayer = playerList [i].NickName;
				break;
			}
		}
		if (!isGameOver [PhotonNetwork.player.ID - 1]) 
		{
			if((currentTurn+1)==PhotonNetwork.player.ID)
				playerTurnText.text = "Your Turn";
			else playerTurnText.text = currPlayer  + "'s Turn";
			changeRowColor (currentTurn);
		}


	}

	// Update is called once per frame
	void Update () 
	{
		
	}


	public void onGridTabClick(string val)
	{
		if (isGameOver [PhotonNetwork.player.ID - 1])
			return;

		if (isClicked)
			return;
		
		if (isBlasting)
			return;
		
		int x = Int32.Parse(val[0].ToString());
		int y = Int32.Parse(val[1].ToString());

		if (!textsValues [x, y].transform.parent.CompareTag ("-1")) 
		{
			if (!textsValues [x, y].transform.parent.CompareTag (currentTurn.ToString()))
				return;
		}

		if ((PhotonNetwork.player.ID - 1) != currentTurn)
			return;

		isClicked = true;
		turnManager.SendMove (val,false);
		callLogic (val);
	}

	void callLogic(string val)
	{
		
		int x = Int32.Parse(val[0].ToString());
		int y = Int32.Parse(val[1].ToString());
		setMatrix ();
		canUndo = true;
		changeValue (x, y, gridValues [x, y] + 1);

		StartCoroutine(chkLogic (x,y,gridValues[x,y]));
	}


	IEnumerator chkLogic(int x,int y,int val)
	{
		isBlasting = true;
		yield return new WaitForSeconds (0.15f);
		StartCoroutine (chkForBlast(x,y,val));
		yield return null;
		print ("Blasting Stopped.");
		chkGameFinishLogic ();
		isTurnCame [currentTurn] = true;
		turnManager.SendMove ("",true);
		isBlasting = false;
		// changeTurn ();
	}

	IEnumerator chkForBlast(int x,int y,int val)
	{
		if (isCornerPoint (x, y) && val == 2) 
		{
			changeValue (x, y, 0);
			StartCoroutine(blastPoint (x+1,y,1));
			StartCoroutine(blastPoint (x-1,y,1));
			StartCoroutine(blastPoint (x,y+1,1));
			StartCoroutine(blastPoint (x,y-1,1));
		}
		else if (isTwoCornerPoint (x, y) && val == 3) 
		{
			changeValue (x, y, 0);
			StartCoroutine(blastPoint (x+1,y,1));
			StartCoroutine(blastPoint (x-1,y,1));
			StartCoroutine(blastPoint (x,y+1,1));
			StartCoroutine(blastPoint (x,y-1,1));
		}
		else if (isValidPoint (x, y) && val == 4) 
		{
			changeValue (x, y, 0);
			StartCoroutine(blastPoint (x+1,y,1));
			StartCoroutine(blastPoint (x-1,y,1));
			StartCoroutine(blastPoint (x,y+1,1));
			StartCoroutine(blastPoint (x,y-1,1));
		}
		yield return null;
	}

	IEnumerator blastPoint(int x, int y, int inc)
	{

		if (!isValidPoint (x, y))
			yield return null;
		else 
		{
			changeValue (x,y,gridValues[x,y]+inc);
			StartCoroutine(chkForBlast (x,y,gridValues[x,y]));
		}
	}

	bool isCornerPoint(int x, int y)
	{
		if (x == 0 && y == 0)
			return true;
		else if (x == 0 && y == 5)
			return true;
		else if (x == 8 && y == 0)
			return true;
		else if (x == 8 && y == 5)
			return true;

		return false;
	}

	bool isTwoCornerPoint(int x, int y)
	{
		if (x == 0 && (y >= 1 && y <= 4))
			return true;
		else if ((x >= 1 && x <= 7) && y == 0)
			return true;
		else if (x == 8 && (y >= 1 && y <= 4))
			return true;
		else if ((x >= 1 && x <= 7) && y == 5)
			return true;
		return false;
	}

	bool isValidPoint(int x,int y)
	{
		if ((x >= 0 && x <= 8) && (y >= 0 && y <= 5))
			return true;
		return false;
	}

	void changeValue(int x,int y, int val)
	{
		if (val == 0)
			resetTab (x,y);
		else 
		{
			gridValues [x, y] = val;
			textsValues [x, y].transform.parent.gameObject.GetComponent<Image>().sprite = ballSprites[val];
			textsValues [x, y].transform.parent.gameObject.GetComponent<Image>().color = textColors [currentTurn];
			// textsValues [x, y].text = val.ToString ();
			// textsValues [x, y].color = textColors [currentTurn];
			textsValues [x, y].tag = currentTurn.ToString ();
			textsValues [x, y].transform.parent.tag = currentTurn.ToString ();
			currTurnMat [x, y] = currentTurn;
		}
	}

	void resetTab(int x,int y)
	{
		gridValues[x,y] = 0;
		textsValues [x, y].transform.parent.gameObject.GetComponent<Image>().sprite = ballSprites[0];
		textsValues [x, y].transform.parent.gameObject.GetComponent<Image>().color = textColors [0];
		textsValues [x, y].tag = "-1";
		textsValues [x, y].transform.parent.tag = "-1";
	}

	/*public void onUndo()
	{
		if (!canUndo)
		{
			print ("Cann't Undo");
			return;
		}
		for (int i = 0; i < 9; i++) 
		{
			for (int j = 0; j < 6; j++)
			{
				int x = i, y = j;
				gridValues [x, y] = lastValues[i,j];
				textsValues [x, y].text = lastValues[i,j].ToString ();
				if(lastValues[i,j]==0)
					textsValues [x, y].color = Color.black;
				else
					textsValues [x, y].color = textColors [lastTurnMat[i,j]];
				if (lastValues [i, j] == 0) 
				{
					textsValues [x, y].tag = "-1";
					textsValues [x, y].transform.parent.tag = "-1";
				}
				else 
				{
					textsValues [x, y].tag = lastTurnMat [i, j].ToString ();
					textsValues [x, y].transform.parent.tag = lastTurnMat [i, j].ToString ();
				}
			}
		}

		currentTurn--;
		if (currentTurn < 0)
			currentTurn = totalPlayers - 1;
		playerTurnText.text = "Player " + currPlayer + " Turn";
		canUndo = false;
	}*/

	void setMatrix()
	{
		for (int i = 0; i < 9; i++) 
		{
			for (int j = 0; j < 6; j++)
			{
				lastValues[i,j] = gridValues [i,j];
				lastTurnMat[i,j] = currTurnMat [i,j];
			}
		}
	}

	void chkGameFinishLogic()
	{
		int[] h = new int[totalPlayers];
		for (int i = 0; i < totalPlayers; i++)
		{
			h [i] = 0;
		}
		for (int i = 0; i < 9; i++) 
		{
			for (int j = 0; j < 6; j++)
			{
				if (gridValues [i, j] > 0) 
				{
					h [Int32.Parse (textsValues [i, j].tag)]++;
				}
			}
		}

		int left = 0, leftindex = -1, turnsCame = 0 ;

		for (int i = 0; i < totalPlayers; i++) 
		{
			if (isTurnCame [i]) 
			{
				turnsCame++;
				if (h [i] == 0) 
				{
					if (i == (PhotonNetwork.player.ID - 1)) 
					{
						playerTurnText.text = "You Lost.";
						isGameOver [i] = true;
					}
				}
				else
				{
					left++;
					leftindex = i;
				}			
			}
		}

		Debug.Log (" Left  " + left + " " + turnsCame + " " + totalPlayers);

		if (left == 1 && turnsCame == totalPlayers) 
		{
			if (leftindex == (PhotonNetwork.player.ID - 1)) 
			{
				playerTurnText.text = "You Won.";
				isGameOver [leftindex] = true;
			}
		}

		/*if (cnt == 54) 
		{
			// Game End
			playerTurnText.text = "Game Over : Player" + (currentTurn +1 ) + " Wins.";
			currentTurn = -1;
		}*/
	}

	public override void OnPhotonPlayerDisconnected(PhotonPlayer player)
	{
		Debug.Log("<Color=Green>Player Left : </Color>  "+ player.NickName);
		isGameOver [player.ID - 1] = true;
		isTurnCame [player.ID - 1] = true;
		if (PhotonNetwork.playerList.Length == 1) 
		{
			playerTurnText.text = "No Player Left to play.";
			isGameOver [PhotonNetwork.player.ID - 1] = true;
				
		}

		if ((player.ID - 1) == currentTurn)
		{
			this.turnManager.SendMove ("",true);
		}


	}


	#region TurnManager Callbacks

	public void OnTurnBegins(int turn)
	{
		Debug.Log("OnTurnBegins() turn: "+ turn);
		changeTurn ();

	}

	public void OnTurnCompleted(int obj)
	{
		Debug.Log("OnTurnCompleted: " + obj);
		this.StartTurn ();
	}


	// when a player moved (but did not finish the turn)
	public void OnPlayerMove(PhotonPlayer photonPlayer, int turn, object move)
	{
		Debug.Log("OnPlayerMove: " + photonPlayer + " turn: " + turn + " action: " + move);
		if (move.ToString ().Length > 0 && photonPlayer.ID != PhotonNetwork.player.ID) 
		{
			Debug.Log ("calling logic");
			callLogic (move.ToString());
		}
	}


	// when a player made the last/final move in a turn
	public void OnPlayerFinished(PhotonPlayer photonPlayer, int turn, object move)
	{
		Debug.Log("OnTurnFinished: " + photonPlayer + " turn: " + turn + " action: " + move);

	}



	public void OnTurnTimeEnds(int obj)
	{
	}

	#endregion

	#region Photon Calls

	/// <summary>Call to start the turn (only the Master Client will send this).</summary>
	public void StartTurn()
	{
		if(PhotonNetwork.isMasterClient)
				this.turnManager.BeginTurn();
	}

	#endregion

	[PunRPC]
	void updateOnMainScreen()
	{
		onGameScreen++;
		if (onGameScreen == (totalPlayers - 1))
		{
			if (this.turnManager.Turn == 0)
			{
				// when the room has two players, start the first turn (later on, joining players won't trigger a turn)
				this.StartTurn();
			}
		}
	}
}
