using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// A simple module that requires the player to push the exactly button 50 times, but only
/// when the timer has a "4" in any position.
/// </summary>
public class AdventureGameModule : MonoBehaviour
{
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMAudio KMAudio;
	public KMSelectable ButtonStatLeft;
	public KMSelectable ButtonStatRight;
	public KMSelectable ButtonInvLeft;
	public KMSelectable ButtonInvRight;
	public KMSelectable ButtonUse;

	public TextMesh TextEnemy;
	public TextMesh TextStatus;
	public TextMesh TextInventory;

	private int CodeStatLeft = 0;
	private int CodeStatRight = 1;
	private int CodeInvLeft = 2;
	private int CodeInvRight = 3;
	private int CodeUse = 4;

	private bool isActive = false;
	private bool isComplete = false;

	private int[] StatRanges = {
		1, 10, // STR
		1, 10, // DEX
		1, 10, // INT
		36, 78, // HEIGHT
		-5, 45, // TEMPERATURE
		85, 111, // GRAVITY
		85, 120 // AIR PRESSURE
	};

	private int[] EnemyStats = {
		10, 11, 13, // DRAGON ID
		50, 50, 50, // DEMON
		4, 7, 3, // EAGLE DS
		3, 6, 5, // GOBLIN DI
		8, 5, 4, // TROLL SD
		4, 3, 8, // WIZARD IS
		9, 4, 7, // GOLEM SI
		4, 6, 3, // LIZARD DS
	};

	protected enum ENEMY {DRAGON = 0, DEMON, EAGLE, GOBLIN, TROLL, WIZARD, GOLEM, LIZARD};

	protected enum STAT {STR = 0, DEX, INT, HEIGHT, TEMP, GRAV, ATM};

	protected enum ITEM {
		BROADSWORD = 0, CABER, NASTY_KNIFE, LONGBOW, MAGIC_ORB, GRIMOIRE,

		BALLOON, BATTERY, BELLOWS, CHEAT_CODE, CRYSTAL_BALL, FEATHER,
		HARD_DRIVE, LAMP, MOONSTONE, POTION, SMALL_DOG, STEPLADDER,
		SUNSTONE, SYMBOL, TICKET, TROPHY
	};

	private int NumStats = System.Enum.GetNames(typeof(STAT)).Length;
	private int NumBaseStats = 3;
	private int NumItems = System.Enum.GetNames(typeof(ITEM)).Length;
	private int NumWeapons = 6;
	private int NumEnemies = System.Enum.GetNames(typeof(ENEMY)).Length;

	private ENEMY SelectedEnemy;
	private int[] StatValues;
	private List<ITEM> InvValues;

	private int InvWeaponCount = 3; // How many unique weapons to add
	private int InvMiscCount = 5; // How many unique misc items to add

	private int SelectedStat = 0;
	private int SelectedItem = 0;
	private bool[] CorrectWeapon;

	private string serialNum;
	private int firstDigit;
	private int lastDigit;
	private int numUnlit;
	private int numLit;
	private bool doublePort;
	private int batteryCount;

	protected string ItemName(ITEM i)
	{
		return i.ToString ().Replace ("_", " ");
	}

	protected void UpdateStatDisplay()
	{
		int stat = StatValues [SelectedStat];
		string text = stat + "";

		switch (SelectedStat) {
		case 0:
			text += " STR";
			break;
		case 1:
			text += " DEX";
			break;
		case 2:
			text += " INT";
			break;
		case 3:
			text = (stat / 12) + "' " + (stat % 12) + "''";
			break;
		case 4:
			text += "°C";
			break;
		case 5:
			text = (stat / 10) + "." + (stat % 10) + " m/s²";
			break;
		case 6:
			text += " kPa";
			break;
		}

		TextStatus.text = text;
	}

	protected void UpdateInvDisplay()
	{
		TextInventory.text = ItemName (InvValues[SelectedItem]);
	}

	protected int GetStr()
	{
		return StatValues [(int)STAT.STR];
	}

	protected int GetDex()
	{
		return StatValues [(int)STAT.DEX];
	}

	protected int GetInt()
	{
		return StatValues [(int)STAT.INT];
	}

	protected int GetHeight()
	{
		return StatValues [(int)STAT.HEIGHT];
	}

	protected int GetTemp()
	{
		return StatValues [(int)STAT.TEMP];
	}

	protected int GetGrav()
	{
		return StatValues [(int)STAT.GRAV];
	}

	protected int GetAtm()
	{
		return StatValues [(int)STAT.ATM];
	}

	protected bool ShouldUseItem(int i)
	{
		if(i < InvWeaponCount) {
			return CorrectWeapon [i];
		} else {
			switch(InvValues[i]) {
			case ITEM.BALLOON:
				return (GetGrav () < 93 || GetAtm () > 110) && SelectedEnemy != ENEMY.EAGLE;
			case ITEM.BATTERY:
				return batteryCount < 2 && SelectedEnemy != ENEMY.WIZARD && SelectedEnemy != ENEMY.GOLEM;
			case ITEM.BELLOWS:
				if (SelectedEnemy == ENEMY.DRAGON || SelectedEnemy == ENEMY.EAGLE) {
					return GetAtm () > 105;
				} else {
					return GetAtm () < 95;
				}
			case ITEM.CHEAT_CODE:
				return false;
			case ITEM.CRYSTAL_BALL:
				return GetInt () > lastDigit && SelectedEnemy != ENEMY.WIZARD;
			case ITEM.FEATHER:
				return GetDex () > GetStr () || GetDex () > GetInt();
			case ITEM.HARD_DRIVE:
				return doublePort;
			case ITEM.LAMP:
				return GetTemp () < 12 && SelectedEnemy != ENEMY.LIZARD;
			case ITEM.MOONSTONE:
				return numUnlit >= 2;
			case ITEM.POTION:
				return true;
			case ITEM.SMALL_DOG:
				return SelectedEnemy != ENEMY.DEMON && SelectedEnemy != ENEMY.DRAGON && SelectedEnemy != ENEMY.TROLL;
			case ITEM.STEPLADDER:
				return GetHeight () < 48 && SelectedEnemy != ENEMY.GOBLIN && SelectedEnemy != ENEMY.LIZARD;
			case ITEM.SUNSTONE:
				return numLit >= 2;
			case ITEM.SYMBOL:
				return SelectedEnemy == ENEMY.DEMON || SelectedEnemy == ENEMY.GOLEM || GetTemp () > 31;
			case ITEM.TICKET:
				return GetHeight () >= 54 && GetGrav () >= 92 && GetGrav() <= 104;
			case ITEM.TROPHY:
				return GetStr () > firstDigit || SelectedEnemy == ENEMY.TROLL;
			default:
				return false;
			}
		}
	}

	protected int CalculateWeaponScore(ITEM i)
	{
		int stat = (int) (i) / 2;
		int playerStat = StatValues [stat] + ((int) (i) % 2) * 2;
		int enemyStat = EnemyStats [(int) (SelectedEnemy) * NumBaseStats + stat];

		return playerStat - enemyStat;
	}

	protected void RegenerateWeaponScores()
	{
		int maxScore = -1000;
		int thisScore;
		for (int i = 0; i < InvWeaponCount; i++) {
			ITEM item = InvValues [i];

			thisScore = CalculateWeaponScore (item);
			Debug.Log ("Weapon: " + item.ToString() + ", score " + thisScore);
			if (thisScore == maxScore) {
				CorrectWeapon [i] = true;
			} else if (thisScore > maxScore) {
				for (int j = 0; j < i; j++) {
					CorrectWeapon [j] = false;
				}
				CorrectWeapon [i] = true;
				maxScore = thisScore;
			} else {
				CorrectWeapon [i] = false;
			}
		}

		for (int i = 0; i < InvWeaponCount; i++) {
			ITEM item = InvValues [i];
			Debug.Log ("Weapon " + item.ToString () + " is " + CorrectWeapon [i].ToString ());
		}
	}

	protected void Start()
	{
		Debug.Log("Adventure game start!");
		GetComponent<KMBombModule>().OnActivate += OnActivate;
		//OnActivate();

		ButtonStatLeft.OnInteract += delegate () { HandlePress(CodeStatLeft); return false; };
		ButtonStatRight.OnInteract += delegate () { HandlePress(CodeStatRight); return false; };
		ButtonInvLeft.OnInteract += delegate () { HandlePress(CodeInvLeft); return false; };
		ButtonInvRight.OnInteract += delegate () { HandlePress(CodeInvRight); return false; };
		ButtonUse.OnInteract += delegate () { HandlePress(CodeUse); return false; };
	}

	protected void OnActivate()
	{
		isActive = true;

		SelectedEnemy = (ENEMY) (Random.Range (0, NumEnemies));
		TextEnemy.text = SelectedEnemy.ToString ();

		StatValues = new int[7];
		for (int i = 0; i < NumStats; i++) {
			StatValues [i] = Random.Range (StatRanges [i * 2], StatRanges [i * 2 + 1] + 1);
		}

		serialNum = KMBombInfoExtensions.GetSerialNumber (BombInfo);
		//serialNum = "a6b5c4";

		bool foundDigit = false;
		foreach (char c in serialNum) {
			if (c >= '0' && c <= '9') {
				if (!foundDigit) {
					foundDigit = true;
					firstDigit = c - '0';
				}
				lastDigit = c - '0';
			}
		}

		/*numUnlit = 1;
		numLit = 1;
		doublePort = false;
		batteryCount = 2;*/

		numUnlit = 0;
		foreach (string s in KMBombInfoExtensions.GetOffIndicators (BombInfo)) {
			numUnlit++;
		}

		numLit = 0;
		foreach (string s in KMBombInfoExtensions.GetOnIndicators (BombInfo)) {
			numLit++;
		}

		doublePort = false;
		HashSet<string> portList = new HashSet<string> ();
		foreach (string s in KMBombInfoExtensions.GetPorts(BombInfo)) {
			if (portList.Contains (s)) {
				doublePort = true;
				break;
			}
			portList.Add (s);
		}

		batteryCount = KMBombInfoExtensions.GetBatteryCount (BombInfo);

		// Generate weapons
		InvValues = new List<ITEM> ();
		CorrectWeapon = new bool[InvWeaponCount];
		for (int i = 0; i < InvWeaponCount; i++) {
			ITEM item;
			do {
				item = (ITEM) (Random.Range (0, NumWeapons));
			} while(InvValues.Contains (item));
			InvValues.Add (item);
		}

		RegenerateWeaponScores ();

		// Generate other items
		string outStr = "";
		for (int i = 0; i < InvMiscCount; i++) {
			if (i != 0) {
				outStr += "\n";
			}
			ITEM item;
			do {
				item = (ITEM) (Random.Range (NumWeapons, NumItems));
			} while(InvValues.Contains (item));
			InvValues.Add (item);

			outStr += "Item " + ItemName (item) + ", use = " + ShouldUseItem (i + InvWeaponCount);
		}
		Debug.Log (outStr);

		UpdateStatDisplay ();
		UpdateInvDisplay ();
	}

	protected bool HandlePress(int button)
	{
		KMAudio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.ButtonPress, this.transform);

		if (!isActive || (isComplete && button == CodeUse)) {
			BombModule.HandleStrike ();
		} else {
			float punch = 0;
			if (button == CodeStatLeft) {
				punch = 0.25f;
				SelectedStat--;
				if (SelectedStat < 0) {
					SelectedStat = NumStats - 1;
				}
				UpdateStatDisplay ();
			}

			if (button == CodeStatRight) {
				punch = 0.25f;
				SelectedStat++;
				if (SelectedStat >= NumStats) {
					SelectedStat = 0;
				}
				UpdateStatDisplay ();
			}

			if (button == CodeInvLeft) {
				punch = 0.25f;
				SelectedItem--;
				if (SelectedItem < 0) {
					SelectedItem = InvValues.Count - 1;
				}
				UpdateInvDisplay ();
			}

			if (button == CodeInvRight) {
				punch = 0.25f;
				SelectedItem++;
				if (SelectedItem >= InvValues.Count) {
					SelectedItem = 0;
				}
				UpdateInvDisplay ();
			}

			if (button == CodeUse) {
				punch = 1f;
				if (!ShouldUseItem (SelectedItem)) {
					BombModule.HandleStrike ();
				} else {
					if (SelectedItem >= InvWeaponCount) {
						// Potion changes base stats
						if (InvValues [SelectedItem] == ITEM.POTION) {
							for (int i = 0; i < InvWeaponCount; i++) {
								StatValues [i] += Random.Range (-1, 4); // + -1 to 3
							}
							RegenerateWeaponScores ();
							UpdateStatDisplay ();
						}
						InvValues.RemoveAt (SelectedItem);
						if (SelectedItem >= InvValues.Count) {
							SelectedItem = 0;
						}
						UpdateInvDisplay ();
					} else {
						// Make sure no other object should be used
						bool success = true;
						for (int i = InvWeaponCount; i < InvValues.Count; i++) {
							if (ShouldUseItem (i)) {
								success = false;
								break;
							}
						}

						if (success) {
							BombModule.HandlePass ();
						} else {
							BombModule.HandleStrike ();
						}
					}
				}
			}

			if (punch > 0) {
				GetComponent<KMSelectable> ().AddInteractionPunch (punch);
			}
		}

		return false;
	}

	/*void Update()
	{
		if (Input.GetKeyDown ("q")) {
			HandlePress (CodeStatLeft);
		}
		if (Input.GetKeyDown ("e")) {
			HandlePress (CodeStatRight);
		}
		if (Input.GetKeyDown ("a")) {
			HandlePress (CodeInvLeft);
		}
		if (Input.GetKeyDown ("d")) {
			HandlePress (CodeInvRight);
		}
		if (Input.GetKeyDown ("s")) {
			HandlePress (CodeUse);
		}
	}*/
}
