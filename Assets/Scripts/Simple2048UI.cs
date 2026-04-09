using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Simple2048UI : MonoBehaviour
{
    // 셀들이 생성될 부모
    public RectTransform BoardRoot;

    // 셀 프리팹
    public GameObject CellPrefab;

    // UI 텍스트
    public Text ScoreText;
    public Text InfoText;
    public Text GameOverText;

    [Header("게임 데이터")]
    public int[,] Board = new int[4, 4];
    public int Score;
    public bool IsGameOver;
    
    [Header("런타임 셀 참조")]
    public Image[,] CellImages = new Image[4, 4];
    public Text[,] CellTexts = new Text[4, 4];
    
    public void Start()
    {
        CreateCells();
        NewGame();
    }

    public void Update()
    {
        // Keyboard를 통해 키입력을 받을 수 있다.
        Keyboard keyboard = Keyboard.current;
        
        // 만약 키보드를 찾지 못하면 끝...
        if (keyboard == null)
            return;

        // 만약 R버튼이 클릭 됐다면?
        if (keyboard.rKey.wasPressedThisFrame)
        {
            // 새로운 게임
            NewGame();
            return;
        }
        
        // 죽었으면 움직이기 X
        if (IsGameOver)
            return;

        // 각 방향별 이동 로직
        if (keyboard.leftArrowKey.wasPressedThisFrame)
            // 각 방향별로 함수를 만드는게 아닌 변수로 방향을 받도록 제작해 구격화!
            TryMove(Vector2Int.left);
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
            TryMove(Vector2Int.right);
        else if (keyboard.upArrowKey.wasPressedThisFrame)
            TryMove(Vector2Int.up);
        else if (keyboard.downArrowKey.wasPressedThisFrame)
            TryMove(Vector2Int.down);
    }

    // ==============================
    // 초기화
    // ==============================

    public void NewGame()
    {
        // 데이터 초기화
        Board = new int[4, 4];
        Score = 0;
        IsGameOver = false;

        // 처음 2개 타일이 기본 제공
        AddRandomTile();
        AddRandomTile();

        // UI에 적용하자
        UpdateUI();
    }

    // ==============================
    // 셀 생성
    // ==============================

    public void CreateCells()
    {
        // 기존 오브젝트 전부 정리
        ClearBoardRootChildren();

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                GameObject cellObject = Instantiate(CellPrefab, BoardRoot);

                cellObject.name = $"Cell_{x}_{y}";

                Image cellImage = cellObject.GetComponent<Image>();
                Text cellText = cellObject.GetComponentInChildren<Text>();
                
                CellImages[x, y] = cellImage;
                CellTexts[x, y] = cellText;
            }
        }

        InfoText.text = "방향키 이동 / R 재시작";
        GameOverText.enabled = false;
    }

    public void ClearBoardRootChildren()
    {
        for (int i = BoardRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(BoardRoot.GetChild(i).gameObject);
        }
    }

    // ==============================
    // 이동 처리
    // ==============================

    public void TryMove(Vector2Int direction)
    {
        bool moved = false;

        if (direction == Vector2Int.left)
        {
            for (int y = 0; y < 4; y++)
            {
                int[] line = new int[4];

                for (int x = 0; x < 4; x++)
                    line[x] = Board[x, y];

                int[] mergedLine = MergeLine(line);

                for (int x = 0; x < 4; x++)
                {
                    if (Board[x, y] != mergedLine[x])
                        moved = true;

                    Board[x, y] = mergedLine[x];
                }
            }
        }
        else if (direction == Vector2Int.right)
        {
            for (int y = 0; y < 4; y++)
            {
                int[] line = new int[4];

                for (int x = 0; x < 4; x++)
                    line[x] = Board[3 - x, y];

                int[] mergedLine = MergeLine(line);

                for (int x = 0; x < 4; x++)
                {
                    if (Board[3 - x, y] != mergedLine[x])
                        moved = true;

                    Board[3 - x, y] = mergedLine[x];
                }
            }
        }
        else if (direction == Vector2Int.up)
        {
            for (int x = 0; x < 4; x++)
            {
                int[] line = new int[4];

                for (int y = 0; y < 4; y++)
                    line[y] = Board[x, y];

                int[] mergedLine = MergeLine(line);

                for (int y = 0; y < 4; y++)
                {
                    if (Board[x, y] != mergedLine[y])
                        moved = true;

                    Board[x, y] = mergedLine[y];
                }
            }
        }
        else if (direction == Vector2Int.down)
        {
            for (int x = 0; x < 4; x++)
            {
                int[] line = new int[4];

                for (int y = 0; y < 4; y++)
                    line[y] = Board[x, 3 - y];

                int[] mergedLine = MergeLine(line);

                for (int y = 0; y < 4; y++)
                {
                    if (Board[x, 3 - y] != mergedLine[y])
                        moved = true;

                    Board[x, 3 - y] = mergedLine[y];
                }
            }
        }

        // 움직였다면 새로운 타일을 생성한다
        if (moved)
        {
            AddRandomTile();
            
            // 만약 더 이상 움직일 수 없다면 게임 오버
            if (!CanMove())
                IsGameOver = true;

            UpdateUI();
        }
    }

    // 줄 병합용 함수
    public int[] MergeLine(int[] line)
    {
        List<int> numberList = new List<int>();

        for (int i = 0; i < 4; i++)
        {
            if (line[i] != 0)
                numberList.Add(line[i]);
        }

        for (int i = 0; i < numberList.Count - 1; i++)
        {
            if (numberList[i] == numberList[i + 1])
            {
                numberList[i] *= 2;
                Score += numberList[i];
                numberList.RemoveAt(i + 1);
            }
        }

        int[] result = new int[4];

        for (int i = 0; i < numberList.Count; i++)
            result[i] = numberList[i];

        return result;
    }

    // ==============================
    // 타일 생성 / 이동 가능 검사
    // ==============================

    public void AddRandomTile()
    {
        List<Vector2Int> emptyCellList = new List<Vector2Int>();

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if (Board[x, y] == 0)
                    emptyCellList.Add(new Vector2Int(x, y));
            }
        }

        if (emptyCellList.Count == 0)
            return;

        Vector2Int randomPosition = emptyCellList[Random.Range(0, emptyCellList.Count)];
        Board[randomPosition.x, randomPosition.y] = Random.value < 0.9f ? 2 : 4;
    }

    public bool CanMove()
    {
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if (Board[x, y] == 0)
                    return true;

                if (x < 3 && Board[x, y] == Board[x + 1, y])
                    return true;

                if (y < 3 && Board[x, y] == Board[x, y + 1])
                    return true;
            }
        }

        return false;
    }

    // ==============================
    // UI 갱신
    // ==============================

    public void UpdateUI()
    {
        ScoreText.text = "Score : " + Score;

        GameOverText.enabled = IsGameOver;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                int value = Board[x, y];

                CellImages[x, y].color = GetTileColor(value);

                CellTexts[x, y].text = value == 0 ? "" : value.ToString();
                CellTexts[x, y].fontSize = value >= 1024 ? 20 : 28;
                CellTexts[x, y].color = value <= 4
                    ? new Color(0.35f, 0.32f, 0.30f)
                    : Color.white;
            }
        }
    }

    // ==============================
    // 색상
    // ==============================

    public Color GetTileColor(int value)
    {
        switch (value)
        {
            case 0: return new Color(0.80f, 0.76f, 0.71f);
            case 2: return new Color(0.93f, 0.89f, 0.85f);
            case 4: return new Color(0.93f, 0.88f, 0.78f);
            case 8: return new Color(0.95f, 0.69f, 0.47f);
            case 16: return new Color(0.96f, 0.58f, 0.39f);
            case 32: return new Color(0.96f, 0.49f, 0.37f);
            case 64: return new Color(0.96f, 0.37f, 0.23f);
            case 128: return new Color(0.93f, 0.81f, 0.45f);
            case 256: return new Color(0.93f, 0.80f, 0.38f);
            case 512: return new Color(0.93f, 0.78f, 0.31f);
            case 1024: return new Color(0.93f, 0.76f, 0.25f);
            case 2048: return new Color(0.93f, 0.75f, 0.18f);
            default: return new Color(0.24f, 0.22f, 0.20f);
        }
    }
}