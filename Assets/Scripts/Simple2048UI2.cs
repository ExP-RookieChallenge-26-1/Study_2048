using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Simple2048UI2 : MonoBehaviour
{
    // 움직이는 타일들이 생성될 부모
    // 주의: 여기에 GridLayoutGroup 넣으면 안 됨
    public RectTransform TileRoot;

    // 움직이는 타일 프리팹
    // 루트에 Image, 자식에 Text 하나가 있다고 가정
    public GameObject TilePrefab;

    // UI 텍스트
    public Text ScoreText;
    public Text InfoText;
    public Text GameOverText;

    // ==============================
    // 보드 설정
    // ==============================

    public float TileSize = 80f;
    public float TileGap = 10f;

    // 타일 이동 시간
    public float MoveDuration = 0.08f;

    // ==============================
    // 게임 데이터
    // ==============================

    public int[,] Board = new int[4, 4];
    public Simple2048TileView[,] TileViews = new Simple2048TileView[4, 4];
    public Vector2[,] CellPositions = new Vector2[4, 4];

    public int Score;
    public bool IsGameOver;
    public bool IsAnimating;

    // ==============================
    // Unity 기본 함수
    // ==============================

    public void Start()
    {
        BuildCellPositions();
        NewGame();
    }

    public void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // R로 재시작
        if (keyboard.rKey.wasPressedThisFrame)
        {
            StopAllCoroutines();
            NewGame();
            return;
        }

        // 애니메이션 중이면 입력 막음
        if (IsAnimating || IsGameOver)
            return;
        
        // 동일하게 키를 받아 로직을 작동시키지만
        // StartCoroutine이라는게 존재한다.
        if (keyboard.leftArrowKey.wasPressedThisFrame)
            StartCoroutine(MoveRoutine(Vector2Int.left));
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
            StartCoroutine(MoveRoutine(Vector2Int.right));
        else if (keyboard.upArrowKey.wasPressedThisFrame)
            StartCoroutine(MoveRoutine(Vector2Int.up));
        else if (keyboard.downArrowKey.wasPressedThisFrame)
            StartCoroutine(MoveRoutine(Vector2Int.down));
    }

    // ==============================
    // 초기화
    // ==============================

    public void NewGame()
    {
        ClearAllTiles();

        Board = new int[4, 4];
        TileViews = new Simple2048TileView[4, 4];

        Score = 0;
        IsGameOver = false;
        IsAnimating = false;
        
        // Version1에서는 GridLayout를 통해 자동 정렬이 됐지만
        // 이동 모션으로 인해 위 기능은 사용 못합니다.
        // 이에따라서 직접 위치를 지정하기 위해 위치 값을 미리 구해둡니다.
        BuildCellPositions();

        SpawnRandomTile();
        SpawnRandomTile();

        UpdateStaticUI();
    }

    public void ClearAllTiles()
    {
        if (TileRoot == null)
            return;

        for (int i = TileRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(TileRoot.GetChild(i).gameObject);
        }
    }

    public void BuildCellPositions()
    {
        float step = TileSize + TileGap;
        float startX = -step * 1.5f;
        float startY = step * 1.5f;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                CellPositions[x, y] = new Vector2(
                    startX + x * step,
                    startY - y * step
                );
            }
        }
    }

    // ==============================
    // 타일 생성
    // ==============================

    public void SpawnRandomTile()
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
        int value = Random.value < 0.9f ? 2 : 4;

        Board[randomPosition.x, randomPosition.y] = value;
        TileViews[randomPosition.x, randomPosition.y] = CreateTileView(randomPosition.x, randomPosition.y, value);
    }

    public Simple2048TileView CreateTileView(int x, int y, int value)
    {
        GameObject tileObject = Instantiate(TilePrefab, TileRoot);
        tileObject.name = $"Tile_{x}_{y}_{value}";

        Simple2048TileView tileView = tileObject.GetComponent<Simple2048TileView>();

        tileView.CacheComponents();
        
        tileView.RectTransform.sizeDelta = new Vector2(TileSize, TileSize);
        tileView.RectTransform.anchoredPosition = CellPositions[x, y];

        tileView.GridX = x;
        tileView.GridY = y;

        tileView.SetValue(value, GetTileColor(value));

        return tileView;
    }

    // ==============================
    // 이동 처리
    // ==============================

    public IEnumerator MoveRoutine(Vector2Int direction)
    {
        IsAnimating = true;

        int[,] nextBoard = new int[4, 4];
        Simple2048TileView[,] nextTileViews = new Simple2048TileView[4, 4];

        List<TileMoveData> moveDataList = new List<TileMoveData>();
        List<TileMergeData> mergeDataList = new List<TileMergeData>();

        bool moved = BuildMoveData(direction, nextBoard, nextTileViews, moveDataList, mergeDataList);

        if (!moved)
        {
            IsAnimating = false;
            yield break;
        }

        // 코루틴으로 실제 이동 애니메이션
        yield return StartCoroutine(AnimateMoveDataList(moveDataList));

        // 애니메이션 끝난 뒤 실제 보드/타일 상태 반영
        Board = nextBoard;
        TileViews = nextTileViews;

        ApplyMergeResults(mergeDataList);
        SyncTileViewsToBoard();

        SpawnRandomTile();

        if (!CanMove())
            IsGameOver = true;

        UpdateStaticUI();

        IsAnimating = false;
    }

    public bool BuildMoveData(
        Vector2Int direction,
        int[,] nextBoard,
        Simple2048TileView[,] nextTileViews,
        List<TileMoveData> moveDataList,
        List<TileMergeData> mergeDataList)
    {
        bool moved = false;

        for (int lineIndex = 0; lineIndex < 4; lineIndex++)
        {
            List<LineItemData> lineItemList = new List<LineItemData>();

            // 현재 방향 기준으로 한 줄을 앞에서부터 수집
            for (int stepIndex = 0; stepIndex < 4; stepIndex++)
            {
                Vector2Int position = GetGridPositionByDirection(direction, lineIndex, stepIndex);
                int value = Board[position.x, position.y];

                if (value == 0)
                    continue;

                lineItemList.Add(new LineItemData
                {
                    Value = value,
                    TileView = TileViews[position.x, position.y]
                });
            }

            int writeIndex = 0;

            for (int i = 0; i < lineItemList.Count; i++)
            {
                // 다음 타일과 값이 같으면 합치기
                if (i + 1 < lineItemList.Count && lineItemList[i].Value == lineItemList[i + 1].Value)
                {
                    LineItemData firstItem = lineItemList[i];
                    LineItemData secondItem = lineItemList[i + 1];

                    Vector2Int targetPosition = GetGridPositionByDirection(direction, lineIndex, writeIndex);
                    int mergedValue = firstItem.Value * 2;

                    nextBoard[targetPosition.x, targetPosition.y] = mergedValue;
                    nextTileViews[targetPosition.x, targetPosition.y] = firstItem.TileView;

                    // 첫 번째 타일 이동
                    if (firstItem.TileView != null)
                    {
                        if (firstItem.TileView.GridX != targetPosition.x || firstItem.TileView.GridY != targetPosition.y)
                        {
                            moved = true;

                            moveDataList.Add(new TileMoveData
                            {
                                TileView = firstItem.TileView,
                                StartPosition = firstItem.TileView.RectTransform.anchoredPosition,
                                EndPosition = CellPositions[targetPosition.x, targetPosition.y]
                            });
                        }
                    }

                    // 두 번째 타일 이동
                    if (secondItem.TileView != null)
                    {
                        moved = true;

                        moveDataList.Add(new TileMoveData
                        {
                            TileView = secondItem.TileView,
                            StartPosition = secondItem.TileView.RectTransform.anchoredPosition,
                            EndPosition = CellPositions[targetPosition.x, targetPosition.y]
                        });
                    }

                    mergeDataList.Add(new TileMergeData
                    {
                        MainTileView = firstItem.TileView,
                        RemovedTileView = secondItem.TileView,
                        ResultValue = mergedValue
                    });

                    Score += mergedValue;

                    writeIndex++;
                    i++;
                }
                else
                {
                    LineItemData item = lineItemList[i];
                    Vector2Int targetPosition = GetGridPositionByDirection(direction, lineIndex, writeIndex);

                    nextBoard[targetPosition.x, targetPosition.y] = item.Value;
                    nextTileViews[targetPosition.x, targetPosition.y] = item.TileView;

                    if (item.TileView != null)
                    {
                        if (item.TileView.GridX != targetPosition.x || item.TileView.GridY != targetPosition.y)
                        {
                            moved = true;

                            moveDataList.Add(new TileMoveData
                            {
                                TileView = item.TileView,
                                StartPosition = item.TileView.RectTransform.anchoredPosition,
                                EndPosition = CellPositions[targetPosition.x, targetPosition.y]
                            });
                        }
                    }

                    writeIndex++;
                }
            }
        }

        return moved;
    }

    public IEnumerator AnimateMoveDataList(List<TileMoveData> moveDataList)
    {
        if (moveDataList.Count == 0)
            yield break;

        float elapsed = 0f;

        while (elapsed < MoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / MoveDuration);

            for (int i = 0; i < moveDataList.Count; i++)
            {
                TileMoveData moveData = moveDataList[i];

                if (moveData.TileView == null)
                    continue;

                moveData.TileView.RectTransform.anchoredPosition = Vector2.Lerp(
                    moveData.StartPosition,
                    moveData.EndPosition,
                    t
                );
            }

            // null이지만 한프레임 대기한다.
            yield return null;
        }

        // 마지막 위치 정확히 보정
        for (int i = 0; i < moveDataList.Count; i++)
        {
            TileMoveData moveData = moveDataList[i];

            if (moveData.TileView == null)
                continue;

            moveData.TileView.RectTransform.anchoredPosition = moveData.EndPosition;
        }
    }

    public void ApplyMergeResults(List<TileMergeData> mergeDataList)
    {
        for (int i = 0; i < mergeDataList.Count; i++)
        {
            TileMergeData mergeData = mergeDataList[i];

            if (mergeData.MainTileView != null)
            {
                mergeData.MainTileView.SetValue(
                    mergeData.ResultValue,
                    GetTileColor(mergeData.ResultValue)
                );
            }

            if (mergeData.RemovedTileView != null)
            {
                Destroy(mergeData.RemovedTileView.gameObject);
            }
        }
    }

    public void SyncTileViewsToBoard()
    {
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                Simple2048TileView tileView = TileViews[x, y];

                if (tileView == null)
                    continue;

                tileView.GridX = x;
                tileView.GridY = y;
                tileView.RectTransform.anchoredPosition = CellPositions[x, y];
                tileView.SetValue(Board[x, y], GetTileColor(Board[x, y]));
            }
        }
    }

    public Vector2Int GetGridPositionByDirection(Vector2Int direction, int lineIndex, int stepIndex)
    {
        if (direction == Vector2Int.left)
            return new Vector2Int(stepIndex, lineIndex);

        if (direction == Vector2Int.right)
            return new Vector2Int(3 - stepIndex, lineIndex);

        if (direction == Vector2Int.up)
            return new Vector2Int(lineIndex, stepIndex);

        // down
        return new Vector2Int(lineIndex, 3 - stepIndex);
    }

    // ==============================
    // 이동 가능 검사
    // ==============================

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

    public void UpdateStaticUI()
    {
        ScoreText.text = "Score : " + Score;
        InfoText.text = "방향키 이동 / R 재시작";
        GameOverText.enabled = IsGameOver;
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

    // ==============================
    // 내부 데이터 클래스
    // ==============================

    public class LineItemData
    {
        public int Value;
        public Simple2048TileView TileView;
    }

    public class TileMoveData
    {
        public Simple2048TileView TileView;
        public Vector2 StartPosition;
        public Vector2 EndPosition;
    }

    public class TileMergeData
    {
        public Simple2048TileView MainTileView;
        public Simple2048TileView RemovedTileView;
        public int ResultValue;
    }
}
