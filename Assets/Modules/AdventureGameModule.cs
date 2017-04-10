using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    protected enum ENEMY { DRAGON = 0, DEMON, EAGLE, GOBLIN, TROLL, WIZARD, GOLEM, LIZARD };

    protected enum STAT { STR = 0, DEX, INT, HEIGHT, TEMP, GRAV, ATM };

    protected enum ITEM
    {
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

    private static int moduleIdCounter = 1;
    private int moduleId;

    protected string ItemName(ITEM i)
    {
        return i.ToString().Replace("_", " ");
    }

    private void UpdateStatDisplay()
    {
        TextStatus.text = GetStatDisplay(SelectedStat);
    }

    private string GetStatDisplay(int stat)
    {
        switch (stat)
        {
            case 0:
                return StatValues[stat] + " STR";
            case 1:
                return StatValues[stat] + " DEX";
            case 2:
                return StatValues[stat] + " INT";
            case 3:
                return (StatValues[stat] / 12) + "' " + (StatValues[stat] % 12) + "''";
            case 4:
                return StatValues[stat] + "°C";
            case 5:
                return (StatValues[stat] / 10) + "." + (StatValues[stat] % 10) + " m/s²";
            default:
                return StatValues[stat] + " kPa";
        }
    }

    protected void UpdateInvDisplay()
    {
        TextInventory.text = ItemName(InvValues[SelectedItem]);
    }

    protected int GetStr()
    {
        return StatValues[(int) STAT.STR];
    }

    protected int GetDex()
    {
        return StatValues[(int) STAT.DEX];
    }

    protected int GetInt()
    {
        return StatValues[(int) STAT.INT];
    }

    protected int GetHeight()
    {
        return StatValues[(int) STAT.HEIGHT];
    }

    protected int GetTemp()
    {
        return StatValues[(int) STAT.TEMP];
    }

    protected int GetGrav()
    {
        return StatValues[(int) STAT.GRAV];
    }

    protected int GetAtm()
    {
        return StatValues[(int) STAT.ATM];
    }

    protected bool ShouldUseItem(int i)
    {
        if (i < InvWeaponCount)
        {
            return CorrectWeapon[i];
        }
        else
        {
            switch (InvValues[i])
            {
                case ITEM.BALLOON:
                    return (GetGrav() < 93 || GetAtm() > 110) && SelectedEnemy != ENEMY.EAGLE;
                case ITEM.BATTERY:
                    return batteryCount < 2 && SelectedEnemy != ENEMY.WIZARD && SelectedEnemy != ENEMY.GOLEM;
                case ITEM.BELLOWS:
                    if (SelectedEnemy == ENEMY.DRAGON || SelectedEnemy == ENEMY.EAGLE)
                    {
                        return GetAtm() > 105;
                    }
                    else
                    {
                        return GetAtm() < 95;
                    }
                case ITEM.CHEAT_CODE:
                    return false;
                case ITEM.CRYSTAL_BALL:
                    return GetInt() > lastDigit && SelectedEnemy != ENEMY.WIZARD;
                case ITEM.FEATHER:
                    return GetDex() > GetStr() || GetDex() > GetInt();
                case ITEM.HARD_DRIVE:
                    return doublePort;
                case ITEM.LAMP:
                    return GetTemp() < 12 && SelectedEnemy != ENEMY.LIZARD;
                case ITEM.MOONSTONE:
                    return numUnlit >= 2;
                case ITEM.POTION:
                    return true;
                case ITEM.SMALL_DOG:
                    return SelectedEnemy != ENEMY.DEMON && SelectedEnemy != ENEMY.DRAGON && SelectedEnemy != ENEMY.TROLL;
                case ITEM.STEPLADDER:
                    return GetHeight() < 48 && SelectedEnemy != ENEMY.GOBLIN && SelectedEnemy != ENEMY.LIZARD;
                case ITEM.SUNSTONE:
                    return numLit >= 2;
                case ITEM.SYMBOL:
                    return SelectedEnemy == ENEMY.DEMON || SelectedEnemy == ENEMY.GOLEM || GetTemp() > 31;
                case ITEM.TICKET:
                    return GetHeight() >= 54 && GetGrav() >= 92 && GetGrav() <= 104;
                case ITEM.TROPHY:
                    return GetStr() > firstDigit || SelectedEnemy == ENEMY.TROLL;
                default:
                    return false;
            }
        }
    }

    protected int CalculateWeaponScore(ITEM i)
    {
        int stat = (int) (i) / 2;
        int playerStat = StatValues[stat] + ((int) (i) % 2) * 2;
        int enemyStat = EnemyStats[(int) (SelectedEnemy) * NumBaseStats + stat];
        Debug.LogFormat("[Adventure Game #{0}] Weapon {1}: player stat={2}, enemy stat={3}, score {4}", moduleId, ItemName(i), playerStat, enemyStat, playerStat - enemyStat);
        return playerStat - enemyStat;
    }

    protected void RegenerateWeaponScores()
    {
        int maxScore = -1000;
        for (int i = 0; i < InvWeaponCount; i++)
        {
            ITEM item = InvValues[i];

            int thisScore = CalculateWeaponScore(item);
            if (thisScore == maxScore)
            {
                CorrectWeapon[i] = true;
            }
            else if (thisScore > maxScore)
            {
                for (int j = 0; j < i; j++)
                {
                    CorrectWeapon[j] = false;
                }
                CorrectWeapon[i] = true;
                maxScore = thisScore;
            }
            else
            {
                CorrectWeapon[i] = false;
            }
        }

        for (int i = 0; i < InvWeaponCount; i++)
        {
            ITEM item = InvValues[i];
            Debug.LogFormat("[Adventure Game #{0}] Weapon {1}: {2}", moduleId, ItemName(item), CorrectWeapon[i] ? "CAN USE" : "don’t use");
        }
    }

    protected void Start()
    {
        moduleId = moduleIdCounter++;

        GetComponent<KMBombModule>().OnActivate += OnActivate;

        ButtonStatLeft.OnInteract += delegate () { HandlePress(CodeStatLeft); return false; };
        ButtonStatRight.OnInteract += delegate () { HandlePress(CodeStatRight); return false; };
        ButtonInvLeft.OnInteract += delegate () { HandlePress(CodeInvLeft); return false; };
        ButtonInvRight.OnInteract += delegate () { HandlePress(CodeInvRight); return false; };
        ButtonUse.OnInteract += delegate () { HandlePress(CodeUse); return false; };
    }

    protected void OnActivate()
    {
        isActive = true;

        SelectedEnemy = (ENEMY) (Random.Range(0, NumEnemies));
        TextEnemy.text = SelectedEnemy.ToString();
        Debug.LogFormat("[Adventure Game #{0}] Enemy: {1}", moduleId, SelectedEnemy);

        StatValues = new int[7];
        for (int i = 0; i < NumStats; i++)
        {
            StatValues[i] = Random.Range(StatRanges[i * 2], StatRanges[i * 2 + 1] + 1);
        }

        Debug.LogFormat("[Adventure Game #{0}] Player stats: {1}", moduleId, logPlayerStats());
        Debug.LogFormat("[Adventure Game #{0}] Environment stats: {1}", moduleId, logEnvStats());

        serialNum = BombInfo.GetSerialNumber();

        bool foundDigit = false;
        foreach (char c in serialNum)
        {
            if (c >= '0' && c <= '9')
            {
                if (!foundDigit)
                {
                    foundDigit = true;
                    firstDigit = c - '0';
                }
                lastDigit = c - '0';
            }
        }

        numUnlit = BombInfo.GetOffIndicators().Count();
        numLit = BombInfo.GetOnIndicators().Count();

        doublePort = false;
        HashSet<string> portList = new HashSet<string>();
        foreach (string s in KMBombInfoExtensions.GetPorts(BombInfo))
        {
            if (portList.Contains(s))
            {
                doublePort = true;
                break;
            }
            portList.Add(s);
        }

        batteryCount = KMBombInfoExtensions.GetBatteryCount(BombInfo);

        // Generate weapons
        InvValues = new List<ITEM>();
        CorrectWeapon = new bool[InvWeaponCount];
        for (int i = 0; i < InvWeaponCount; i++)
        {
            ITEM item;
            do
            {
                item = (ITEM) (Random.Range(0, NumWeapons));
            } while (InvValues.Contains(item));
            InvValues.Add(item);
        }

        RegenerateWeaponScores();

        // Generate other items
        for (int i = 0; i < InvMiscCount; i++)
        {
            ITEM item;
            do
                item = (ITEM) (Random.Range(NumWeapons, NumItems));
            while (InvValues.Contains(item));
            InvValues.Add(item);
        }

        logItemUsage();

        UpdateStatDisplay();
        UpdateInvDisplay();
    }

    private void logItemUsage()
    {
        for (int i = InvWeaponCount; i < InvValues.Count; i++)
            Debug.LogFormat("[Adventure Game #{0}] Item {1}: {2}", moduleId, ItemName(InvValues[i]), ShouldUseItem(i) ? "MUST USE" : "don’t use");
    }

    protected bool HandlePress(int button)
    {
        KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);

        if (!isActive || (isComplete && button == CodeUse))
        {
            BombModule.HandleStrike();
        }
        else
        {
            float punch = 0;
            if (button == CodeStatLeft)
            {
                punch = 0.25f;
                SelectedStat--;
                if (SelectedStat < 0)
                {
                    SelectedStat = NumStats - 1;
                }
                UpdateStatDisplay();
            }

            if (button == CodeStatRight)
            {
                punch = 0.25f;
                SelectedStat++;
                if (SelectedStat >= NumStats)
                {
                    SelectedStat = 0;
                }
                UpdateStatDisplay();
            }

            if (button == CodeInvLeft)
            {
                punch = 0.25f;
                SelectedItem--;
                if (SelectedItem < 0)
                {
                    SelectedItem = InvValues.Count - 1;
                }
                UpdateInvDisplay();
            }

            if (button == CodeInvRight)
            {
                punch = 0.25f;
                SelectedItem++;
                if (SelectedItem >= InvValues.Count)
                {
                    SelectedItem = 0;
                }
                UpdateInvDisplay();
            }

            if (button == CodeUse)
            {
                punch = 1f;
                if (!ShouldUseItem(SelectedItem))
                {
                    Debug.LogFormat("[Adventure Game #{0}] Strike because you used the {1}, which you should not use.", moduleId, ItemName(InvValues[SelectedItem]));
                    BombModule.HandleStrike();
                }
                else
                {
                    if (SelectedItem >= InvWeaponCount)
                    {
                        var item = InvValues[SelectedItem];
                        InvValues.RemoveAt(SelectedItem);

                        // Potion changes base stats
                        if (item == ITEM.POTION)
                        {
                            for (int i = 0; i < NumBaseStats; i++)
                            {
                                StatValues[i] += Random.Range(-1, 4); // + -1 to 3
                            }
                            Debug.LogFormat("[Adventure Game #{0}] Took a potion. Stats are now: {1}", moduleId, logPlayerStats());
                            RegenerateWeaponScores();
                            UpdateStatDisplay();
                            logItemUsage();
                        }
                        else
                        {
                            Debug.LogFormat("[Adventure Game #{0}] Item {1} used correctly.", moduleId, ItemName(item));
                        }

                        if (SelectedItem >= InvValues.Count)
                        {
                            SelectedItem = 0;
                        }
                        UpdateInvDisplay();
                    }
                    else
                    {
                        // Make sure no other object should be used
                        int? itemToUse = null;
                        for (int i = InvWeaponCount; i < InvValues.Count; i++)
                        {
                            if (ShouldUseItem(i))
                            {
                                itemToUse = i;
                                break;
                            }
                        }

                        if (itemToUse == null)
                        {
                            Debug.LogFormat("[Adventure Game #{0}] Weapon {1} used correctly. Module passed.", moduleId, ItemName(InvValues[SelectedItem]));
                            BombModule.HandlePass();
                        }
                        else
                        {
                            Debug.LogFormat("[Adventure Game #{0}] Strike: You muse use the {1} before you use the {2}.", moduleId, ItemName(InvValues[itemToUse.Value]), ItemName(InvValues[SelectedItem]));
                            BombModule.HandleStrike();
                        }
                    }
                }
            }

            if (punch > 0)
            {
                GetComponent<KMSelectable>().AddInteractionPunch(punch);
            }
        }

        return false;
    }

    private string logPlayerStats()
    {
        return string.Format("{0}, {1}, {2}", GetStatDisplay(0), GetStatDisplay(1), GetStatDisplay(2));
    }

    private string logEnvStats()
    {
        return string.Format("{0}, {1}, {2}, {3}", GetStatDisplay(3), GetStatDisplay(4), GetStatDisplay(5), GetStatDisplay(6));
    }

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim();
        if (command.Equals("cycle stats", System.StringComparison.InvariantCultureIgnoreCase))
        {
            for (int i = 0; i < NumStats; i++)
            {
                yield return ButtonStatRight;
                yield return new WaitForSeconds(1.3f);
                yield return ButtonStatRight;
            }
            yield break;
        }

        else if (
            command.Equals("cycle items", System.StringComparison.InvariantCultureIgnoreCase) ||
            command.Equals("cycle weapons", System.StringComparison.InvariantCultureIgnoreCase) ||
            command.Equals("cycle inventory", System.StringComparison.InvariantCultureIgnoreCase))
        {
            for (int i = 0; i < InvWeaponCount + InvMiscCount; i++)
            {
                yield return ButtonInvRight;
                yield return new WaitForSeconds(1.3f);
                yield return ButtonInvRight;
            }
            yield break;
        }

        else if (command.StartsWith("use ", System.StringComparison.InvariantCultureIgnoreCase))
        {
            command = command.Substring(4).Trim();
            for (int i = 0; i < InvValues.Count; i++)
            {
                if (i > 0)
                {
                    yield return ButtonInvRight;
                    yield return new WaitForSeconds(.1f);
                    yield return ButtonInvRight;
                }

                if (ItemName(InvValues[SelectedItem]).Equals(command, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return ButtonUse;
                    yield return new WaitForSeconds(.1f);
                    yield return ButtonUse;
                    yield break;
                }
            }
            yield break;
        }
    }
}
