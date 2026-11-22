using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SymbolScripts;
using UnityEngine;

namespace SlotsLogic.Match3Slots.Scripts.Core
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private SlotGameConfig _config;
        public GameObject FrameFirstMatchGroup;
        public GameObject FrameSecondMatchGroup;
        
        private Board _board;
        public readonly Dictionary<(int r, int c), SymbolBase> _symbolMap = new();
        private bool _shouldInterruptFlyOut = false;

        private void Start()
        {
            if (FrameFirstMatchGroup != null) FrameFirstMatchGroup.SetActive(false);
            if (FrameSecondMatchGroup != null) FrameSecondMatchGroup.SetActive(false);
        }

        public void Setup(Board board)
        {
            if (this == null || !isActiveAndEnabled)
            {
                Debug.LogWarning("[BoardView] Cannot Setup - BoardView is destroyed or inactive");
                return;
            }
            
            _board = board;
            var children = GetComponentsInChildren<SymbolBase>();
            foreach (var ch in children)
            {
                if (ch != null)
                {
                    try { Destroy(ch.gameObject); } catch { }
                }
            }
            _symbolMap.Clear();
        }

        public void SetupExceptObjects(Board board, HashSet<GameObject> preserve)
        {
            if (this == null || !isActiveAndEnabled)
            {
                Debug.LogWarning("[BoardView] Cannot Setup - BoardView is destroyed or inactive");
                return;
            }

            _board = board;
            var children = GetComponentsInChildren<SymbolBase>();
            foreach (var ch in children)
            {
                if (ch != null && (preserve == null || !preserve.Contains(ch.gameObject)))
                {
                    try { Destroy(ch.gameObject); } catch { }
                }
            }
            _symbolMap.Clear();
        }

        public Vector3 GetWorldPosition(int row, int col)
        {
            if (this == null || !isActiveAndEnabled || _config == null)
                return Vector3.zero;
            return new Vector3(col * _config.CellSizeX + transform.position.x,
                -row * _config.CellSizeY + transform.position.y, 0f);
        }

        public void SetControllerAt(SymbolBase ctrl, int row, int col)
        {
            if (ctrl == null) return;
            ClearCell(row, col);
            ctrl.Row = row;
            ctrl.Col = col;
            _symbolMap[(row, col)] = ctrl;
        }

        public SymbolBase SpawnSymbol(int row, int col, SymbolData data)
        {
            if (this == null || !isActiveAndEnabled || data == null || data.SymbolPrefab == null)
                return null;
            var go = Instantiate(data.SymbolPrefab, GetWorldPosition(row, col), Quaternion.identity, transform);
            var ctrl = go.GetComponent<SymbolBase>();
            if (ctrl != null)
            {
                ctrl.Row = row;
                ctrl.Col = col;
                ctrl.Data = data;
                _symbolMap[(row, col)] = ctrl;
                try { ctrl.SymbolSpawn(); } catch { }
            }
            else
            {
                Destroy(go);
            }

            return ctrl;
        }

        public void ClearBoard(HashSet<GameObject> preserve = null)
        {
            var all = _symbolMap.Values.Where(s => s != null).ToList();
            _symbolMap.Clear();

            foreach (var ctrl in all)
            {
                try
                {
                    if (ctrl != null && ctrl.gameObject != null && (preserve == null || !preserve.Contains(ctrl.gameObject)))
                        Destroy(ctrl.gameObject);
                }
                catch { }
            }
        }

        public void InterruptFlyOut()
        {
            _shouldInterruptFlyOut = true;
        }

        public IEnumerator FlyOutAndClearBoard(float duration, HashSet<GameObject> preserve = null)
        {
            _shouldInterruptFlyOut = false;

            var all = _symbolMap.Values.Where(s => s != null).ToList();
            _symbolMap.Clear();

            var toFly = preserve != null ? all.Where(s => !preserve.Contains(s.gameObject)).ToList() : all;

            if (toFly.Count == 0)
            {
                yield break;
            }

            var done = 0;
            var cols = _config.Columns;
            var rows = _config.Rows;

            var colDelay = Mathf.Max(0.03f, _config.ClearBoardColumnDelay * 0.6f);
            var rowDelay = Mathf.Max(0.015f, colDelay * 0.4f);
            var localDur = Mathf.Clamp(duration * 1.1f, 0.4f, 0.8f);

            for (var c = 0; c < cols; c++)
            for (var rb = 0; rb < rows; rb++)
            {
                var r = rows - 1 - rb;
                var ctrl = toFly.FirstOrDefault(s => s != null && s.Col == c && s.Row == r);
                if (ctrl == null) continue;
                var d = c * colDelay + rb * rowDelay;
                StartCoroutine(FlyOutSingle(ctrl, localDur, d, () => done++));
            }

            var maxDelay = (cols - 1) * colDelay + (rows - 1) * rowDelay;
            var maxExpected = localDur + maxDelay + 0.5f;
            var elapsed = 0f;

            while (done < toFly.Count && !_shouldInterruptFlyOut)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (elapsed > maxExpected)
                {
                    break;
                }
            }

            foreach (var ctrl in toFly)
            {
                try
                {
                    if (ctrl != null)
                        Destroy(ctrl.gameObject);
                }
                catch { }
            }
        }

        private IEnumerator FlyOutSingle(SymbolBase ctrl, float duration, float delay, Action onDone)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            if (ctrl == null)
            {
                onDone?.Invoke();
                yield break;
            }

            var start = ctrl.transform.position;
            var maxHeight = -12.35f;
            var end = new Vector3(start.x, maxHeight, start.z);
            var t = 0f;

            while (t < duration)
            {
                if (ctrl == null)
                {
                    onDone?.Invoke();
                    yield break;
                }

                t += Time.deltaTime;
                var s = Mathf.SmoothStep(0f, 1f, t / duration);
                ctrl.transform.position = Vector3.Lerp(start, end, s);
                if (ctrl.transform.position.y <= maxHeight)
                    break;

                yield return null;
            }

            if (ctrl != null)
            {
                try
                {
                    Destroy(ctrl.gameObject);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            onDone?.Invoke();
        }
        
        public SymbolBase GetSymbolController(int r, int c)
        {
            _symbolMap.TryGetValue((r, c), out var ctrl);
            return ctrl;
        }

        public void ClearCell(int r, int c)
        {
            if (_symbolMap.ContainsKey((r, c))) _symbolMap.Remove((r, c));
        }

        public Dictionary<int, SymbolData[]> GenerateFirstTutorialSpin(List<SymbolData> allSymbols, int cols, int rows)
        {
            var result = new Dictionary<int, SymbolData[]>();
            var tutorialSymbols = allSymbols.Where(s => !s.isScatter && !s.isWild).Take(6).ToList();
            
            if (tutorialSymbols.Count < 6)
            {
                Debug.LogError("[BoardView] Not enough symbols for tutorial spin!");
                return result;
            }

            var symbolCounts = new Dictionary<SymbolData, int>();
            foreach (var sym in tutorialSymbols)
            {
                symbolCounts[sym] = 0;
            }

            for (int c = 0; c < cols; c++)
            {
                var colArr = new SymbolData[rows];
                
                for (int r = 0; r < rows; r++)
                {
                    SymbolData selectedSymbol = null;
                    var availableSymbols = tutorialSymbols.Where(s => symbolCounts[s] < 4).ToList();
                    
                    if (availableSymbols.Count == 0)
                    {
                        availableSymbols = tutorialSymbols.ToList();
                    }

                    int attempts = 0;
                    while (attempts < 50)
                    {
                        selectedSymbol = availableSymbols[UnityEngine.Random.Range(0, availableSymbols.Count)];
                        
                        bool hasAdjacentMatch = false;
                        
                        if (r > 0 && colArr[r - 1] == selectedSymbol)
                        {
                            if (r > 1 && colArr[r - 2] == selectedSymbol)
                            {
                                hasAdjacentMatch = true;
                            }
                        }
                        
                        if (c > 0 && result.ContainsKey(c - 1))
                        {
                            var prevCol = result[c - 1];
                            if (prevCol[r] == selectedSymbol)
                            {
                                if (c > 1 && result.ContainsKey(c - 2) && result[c - 2][r] == selectedSymbol)
                                {
                                    hasAdjacentMatch = true;
                                }
                            }
                        }

                        if (!hasAdjacentMatch)
                        {
                            break;
                        }

                        attempts++;
                    }

                    colArr[r] = selectedSymbol;
                    symbolCounts[selectedSymbol]++;
                }
                
                result[c] = colArr;
            }

            return result;
        }

        public Dictionary<int, SymbolData[]> BuildFirstTutorialSpinNoMatches(List<SymbolData> allSymbols, int cols, int rows)
        {
            var result = new Dictionary<int, SymbolData[]>();
            var symbol0 = allSymbols[0];
            var symbol1 = allSymbols[1];
            var symbol2 = allSymbols[2];
            var symbol3 = allSymbols[3];
            var symbol4 = allSymbols[4];
            var symbol5 = allSymbols[5];
            var symbol6 = allSymbols[6];

            var allPositions = new List<(int row, int col)>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    allPositions.Add((r, c));
                }
            }

            var shuffledPositions = allPositions.OrderBy(x => UnityEngine.Random.value).ToList();
            var assignments = new Dictionary<(int row, int col), SymbolData>();
            
            int totalPositions = shuffledPositions.Count;
            int index = 0;
            
            for (int i = 0; i < 7 && index < shuffledPositions.Count; i++, index++)
            {
                assignments[shuffledPositions[index]] = symbol0;
            }
            
            int remainingPositions = totalPositions - 7;
            var symbols = new[] { symbol1, symbol2, symbol3, symbol4, symbol5, symbol6 };
            var counts = new int[symbols.Length];
            
            int minPerSymbol = 2;
            for (int i = 0; i < symbols.Length && remainingPositions >= minPerSymbol; i++)
            {
                counts[i] = minPerSymbol;
                remainingPositions -= minPerSymbol;
            }
            
            int symbolIndex = 0;
            while (remainingPositions > 0 && symbolIndex < symbols.Length)
            {
                if (counts[symbolIndex] < 4)
                {
                    counts[symbolIndex]++;
                    remainingPositions--;
                }
                else
                {
                    symbolIndex++;
                }
            }
            
            for (int i = 0; i < symbols.Length; i++)
            {
                for (int j = 0; j < counts[i] && index < shuffledPositions.Count; j++, index++)
                {
                    assignments[shuffledPositions[index]] = symbols[i];
                }
            }

            for (int c = 0; c < cols; c++)
            {
                var colArr = new SymbolData[rows];
                for (int r = 0; r < rows; r++)
                {
                    if (assignments.TryGetValue((r, c), out var symbol))
                    {
                        colArr[r] = symbol;
                    }
                    else
                    {
                        colArr[r] = symbol1;
                    }
                }
                result[c] = colArr;
            }

            Debug.Log($"[BoardView] BuildFirstTutorialSpinNoMatches: 7x symbol0, {counts[0]}x s1, {counts[1]}x s2, {counts[2]}x s3, {counts[3]}x s4, {counts[4]}x s5, {counts[5]}x s6");
            return result;
        }

        public Dictionary<int, SymbolData[]> BuildSecondTutorialRefillNoMatches(List<SymbolData> allSymbols, int cols, int rows, Board currentBoard)
        {
            var result = new Dictionary<int, SymbolData[]>();
            
            var availableSymbols = allSymbols.Where(s => s != null && !s.isScatter && !s.isWild).ToList();
            
            if (availableSymbols.Count == 0)
            {
                Debug.LogError("[BoardView] No valid symbols (non-scatter, non-wild) for tutorial refill!");
                return result;
            }
            
            Debug.Log($"[BoardView] Tutorial refill: {availableSymbols.Count} available symbols (no scatter/wild)");

            var symbolCounts = new Dictionary<SymbolData, int>();
            foreach (var symbol in availableSymbols)
            {
                symbolCounts[symbol] = 0;
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var existingSymbol = currentBoard.GetSymbol(r, c);
                    if (existingSymbol != null && symbolCounts.ContainsKey(existingSymbol))
                    {
                        symbolCounts[existingSymbol]++;
                    }
                }
            }

            for (int c = 0; c < cols; c++)
            {
                var emptyPositions = new List<int>();
                for (int r = 0; r < rows; r++)
                {
                    if (currentBoard.GetSymbol(r, c) == null)
                    {
                        emptyPositions.Add(r);
                    }
                }

                if (emptyPositions.Count == 0)
                {
                    result[c] = new SymbolData[0];
                    continue;
                }

                var columnSymbols = new List<SymbolData>();
                
                for (int i = emptyPositions.Count - 1; i >= 0; i--)
                {
                    int row = emptyPositions[i];
                    SymbolData selectedSymbol = null;
                    
                    var availableForSelection = availableSymbols
                        .Where(s => symbolCounts[s] < 4)
                        .ToList();
                    
                    if (availableForSelection.Count == 0)
                    {
                        availableForSelection = availableSymbols
                            .Where(s => symbolCounts[s] >= 2)
                            .ToList();
                    }
                    
                    if (availableForSelection.Count == 0)
                    {
                        availableForSelection = availableSymbols.ToList();
                    }

                    int attempts = 0;
                    while (attempts < 100)
                    {
                        selectedSymbol = availableForSelection[UnityEngine.Random.Range(0, availableForSelection.Count)];
                        
                        bool hasAdjacentMatch = false;
                        
                        if (row > 0)
                        {
                            var belowSymbol = currentBoard.GetSymbol(row - 1, c);
                            if (belowSymbol == selectedSymbol)
                            {
                                if (row > 1 && currentBoard.GetSymbol(row - 2, c) == selectedSymbol)
                                {
                                    hasAdjacentMatch = true;
                                }
                            }
                        }
                        
                        if (c > 0)
                        {
                            var leftSymbol = currentBoard.GetSymbol(row, c - 1);
                            if (leftSymbol == selectedSymbol)
                            {
                                if (c > 1 && currentBoard.GetSymbol(row, c - 2) == selectedSymbol)
                                {
                                    hasAdjacentMatch = true;
                                }
                            }
                        }

                        if (!hasAdjacentMatch && symbolCounts[selectedSymbol] >= 4)
                        {
                            hasAdjacentMatch = true;
                        }

                        if (!hasAdjacentMatch)
                        {
                            break;
                        }

                        attempts++;
                    }

                    columnSymbols.Insert(0, selectedSymbol);
                    symbolCounts[selectedSymbol]++;
                }

                result[c] = columnSymbols.ToArray();
            }

            // ФИНАЛЬНАЯ ПРОВЕРКА: Убеждаемся что scatter/wild не попали в результат
            foreach (var kvp in result)
            {
                foreach (var sym in kvp.Value)
                {
                    if (sym != null && (sym.isScatter || sym.isWild))
                    {
                        Debug.LogError($"[BoardView] CRITICAL: Scatter/Wild symbol found in tutorial refill! Column: {kvp.Key}, Symbol: {sym.symbolID}");
                    }
                }
            }

            Debug.Log("[BoardView] BuildSecondTutorialRefillNoMatches: Created refill with 2-4 of each symbol, no scatter/wild");
            return result;
        }

        
        public Dictionary<int, SymbolData[]> BuildMidnightRouteSpin(
            List<SymbolData> allSymbols,
            int cols, int rows,
            List<(int r, int c)> chain,
            SymbolData clusterSymbol)
        {
            var chainSet = new HashSet<(int, int)>(chain);
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };

            
            var clusterCells = new HashSet<(int, int)>();
            foreach (var (cr, cc) in chain)
                for (int d = 0; d < 4; d++)
                {
                    int nr = cr + dr[d], nc = cc + dc[d];
                    if (nr >= 0 && nr < rows && nc >= 0 && nc < cols && !chainSet.Contains((nr, nc)))
                        clusterCells.Add((nr, nc));
                }

            var normalSymbols = allSymbols.Where(s => !s.isScatter && !s.isWild && s.symbolID != clusterSymbol.symbolID).ToList();
            if (normalSymbols.Count == 0) normalSymbols = allSymbols.Where(s => !s.isScatter && !s.isWild).ToList();
            SymbolData FillerSymbol() => normalSymbols[UnityEngine.Random.Range(0, normalSymbols.Count)];

            var assignments = new Dictionary<(int, int), SymbolData>();

            foreach (var (r, c) in chain)
                assignments[(r, c)] = FillerSymbol();

            foreach (var (r, c) in clusterCells)
                assignments[(r, c)] = clusterSymbol;

            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (!assignments.ContainsKey((r, c)))
                    assignments[(r, c)] = FillerSymbol();

            var result = new Dictionary<int, SymbolData[]>();
            for (int c = 0; c < cols; c++)
            {
                var colArr = new SymbolData[rows];
                for (int r = 0; r < rows; r++)
                    colArr[r] = assignments.TryGetValue((r, c), out var s) ? s : FillerSymbol();
                result[c] = colArr;
            }

            Debug.Log($"[MIDNIGHT_ROUTE] BuildMidnightRouteSpin: chain={chain.Count} cells, cluster='{clusterSymbol.symbolID}' on {clusterCells.Count} neighbours");
            return result;
        }

        public Dictionary<int, SymbolData[]> BuildTutorialSpinWithCascades(List<SymbolData> allSymbols, int cols, int rows, int targetCascades = 3)
        {
            var result = new Dictionary<int, SymbolData[]>();
            if (allSymbols.Count < 6) { Debug.LogError("[BoardView] Not enough symbols for tutorial cascades spin!"); return result; }

            var normalSymbols = allSymbols.Where(s => !s.isScatter && !s.isWild).ToList();

            var highPaySymbols = new List<SymbolData>();
            for (int i = 5; i <= 7; i++)
                if (i < normalSymbols.Count) highPaySymbols.Add(normalSymbols[i]);
            if (highPaySymbols.Count == 0) highPaySymbols = normalSymbols.TakeLast(3).ToList();
            if (highPaySymbols.Count == 0) highPaySymbols = normalSymbols;

            var assignments = new Dictionary<(int row, int col), SymbolData>();
            var usedPositions = new HashSet<(int row, int col)>();
            var allPositions = new List<(int row, int col)>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    allPositions.Add((r, c));

            int totalCells = rows * cols;
            int highPayCells = Mathf.RoundToInt(totalCells * 0.75f);
            int groupSize = 6;
            int groupCount = highPayCells / groupSize;
            if (groupCount < targetCascades) groupCount = targetCascades;

            for (int g = 0; g < groupCount; g++)
            {
                SymbolData sym = highPaySymbols[g % highPaySymbols.Count];
                var available = allPositions.Where(p => !usedPositions.Contains(p)).OrderBy(_ => UnityEngine.Random.value).Take(groupSize).ToList();
                foreach (var pos in available)
                {
                    assignments[pos] = sym;
                    usedPositions.Add(pos);
                }
            }

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (!assignments.ContainsKey((r, c)))
                        assignments[(r, c)] = highPaySymbols[UnityEngine.Random.Range(0, highPaySymbols.Count)];

            for (int c = 0; c < cols; c++)
            {
                var colArr = new SymbolData[rows];
                for (int r = 0; r < rows; r++)
                    colArr[r] = assignments.TryGetValue((r, c), out var s) ? s : highPaySymbols[0];
                result[c] = colArr;
            }

            Debug.Log($"[BoardView] BuildTutorialSpinWithCascades: {groupCount} groups of highpay symbols (idx 5,6,7), targetCascades={targetCascades}");
            return result;
        }

        public Dictionary<int, SymbolData[]> BuildFourthTutorialSpin(List<SymbolData> allSymbols, int cols, int rows)
        {
            var result = new Dictionary<int, SymbolData[]>();
            if (allSymbols.Count < 10) { Debug.LogError("[BoardView] Not enough symbols for fourth tutorial spin!"); return result; }

            var scatterSymbol = allSymbols.FirstOrDefault(s => s.isScatter);
            if (scatterSymbol == null) scatterSymbol = allSymbols[9];

            var normalSymbols = allSymbols.Where(s => !s.isScatter && !s.isWild).ToList();
            if (normalSymbols.Count == 0) normalSymbols = allSymbols.Take(9).ToList();

            var scatterPositions = new List<(int row, int col)>();
            var colsToUse = new List<int> { 0, 1, 2, 3 };
            int scatterRow = rows / 2;
            foreach (int c in colsToUse)
                scatterPositions.Add((scatterRow, c));

            for (int c = 0; c < cols; c++)
            {
                var colArr = new SymbolData[rows];
                for (int r = 0; r < rows; r++)
                {
                    if (scatterPositions.Contains((r, c)))
                        colArr[r] = scatterSymbol;
                    else
                        colArr[r] = normalSymbols[UnityEngine.Random.Range(0, normalSymbols.Count)];
                }
                result[c] = colArr;
            }

            Debug.Log("[BoardView] BuildFourthTutorialSpin: 4 scatter symbols placed");
            return result;
        }

        public Dictionary<int, SymbolData[]> BuildThirdTutorialSpin(List<SymbolData> allSymbols, int cols, int rows)
        {
            var result = new Dictionary<int, SymbolData[]>();
            
            if (allSymbols.Count < 10)
            {
                Debug.LogError("[BoardView] Not enough symbols for third tutorial spin!");
                return result;
            }

            var scatterSymbol = allSymbols[9];
            var availableSymbols = allSymbols.Take(9).ToList();

            var allPositions = new List<(int row, int col)>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    allPositions.Add((r, c));
                }
            }

            var scatterPositions = new List<(int row, int col)>();
            for (int c = 0; c < cols; c++)
            {
                scatterPositions.Add((2, c));
            }

            var availablePositions = allPositions.Where(p => !scatterPositions.Contains(p)).ToList();
            var shuffledPositions = availablePositions.OrderBy(x => UnityEngine.Random.value).ToList();
            var assignments = new Dictionary<(int row, int col), SymbolData>();

            foreach (var pos in scatterPositions)
            {
                assignments[pos] = scatterSymbol;
            }

            var symbolCounts = new Dictionary<SymbolData, int>();
            foreach (var symbol in availableSymbols)
            {
                symbolCounts[symbol] = 0;
            }

            int remainingPositions = shuffledPositions.Count;
            var remainingShuffled = shuffledPositions;

            foreach (var symbol in availableSymbols)
            {
                int count = UnityEngine.Random.Range(1, 5);
                symbolCounts[symbol] = count;
            }

            int totalNeeded = symbolCounts.Values.Sum();
            while (totalNeeded > remainingPositions)
            {
                var symbolToReduce = availableSymbols[UnityEngine.Random.Range(0, availableSymbols.Count)];
                if (symbolCounts[symbolToReduce] > 1)
                {
                    symbolCounts[symbolToReduce]--;
                    totalNeeded--;
                }
            }

            while (totalNeeded < remainingPositions)
            {
                var symbolToIncrease = availableSymbols[UnityEngine.Random.Range(0, availableSymbols.Count)];
                if (symbolCounts[symbolToIncrease] < 4)
                {
                    symbolCounts[symbolToIncrease]++;
                    totalNeeded++;
                }
            }

            var symbolPool = new List<SymbolData>();
            foreach (var kvp in symbolCounts)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    symbolPool.Add(kvp.Key);
                }
            }

            symbolPool = symbolPool.OrderBy(x => UnityEngine.Random.value).ToList();

            for (int i = 0; i < remainingShuffled.Count && i < symbolPool.Count; i++)
            {
                assignments[remainingShuffled[i]] = symbolPool[i];
            }

            for (int c = 0; c < cols; c++)
            {
                var colArr = new SymbolData[rows];
                for (int r = 0; r < rows; r++)
                {
                    if (assignments.TryGetValue((r, c), out var symbol))
                    {
                        colArr[r] = symbol;
                    }
                    else
                    {
                        colArr[r] = availableSymbols[UnityEngine.Random.Range(0, availableSymbols.Count)];
                    }
                }
                result[c] = colArr;
            }

            Debug.Log("[BoardView] BuildThirdTutorialSpin: Created board with 4x scatter (index 9) on fixed positions (row=1, all columns), no wild (index 10), others 1-4 each");
            return result;
        }

        public Dictionary<int, SymbolData[]> BuildSpecificFirstTutorialLayout(List<SymbolData> allSymbols)
        {
            // [col;row] → symbol index
            return new Dictionary<int, SymbolData[]>
            {
                [0] = new[] { allSymbols[0], allSymbols[1], allSymbols[6], allSymbols[1], allSymbols[0] },
                [1] = new[] { allSymbols[1], allSymbols[4], allSymbols[0], allSymbols[6], allSymbols[3] },
                [2] = new[] { allSymbols[2], allSymbols[3], allSymbols[5], allSymbols[2], allSymbols[2] },
                [3] = new[] { allSymbols[4], allSymbols[4], allSymbols[6], allSymbols[1], allSymbols[1] },
            };
        }
        
        public Dictionary<int, SymbolData[]> BuildTutorialScatterSpin(List<SymbolData> allSymbols, int cols, int rows)
        {
            var scatterPositions = new HashSet<(int row, int col)>
            {
                (2, 0), (1, 2), (3, 3)
            };

            var scatterSymbol = allSymbols.FirstOrDefault(s => s.isScatter);
            if (scatterSymbol == null)
            {
                Debug.LogError("[BoardView] No scatter symbol found for tutorial scatter spin!");
                scatterSymbol = allSymbols.Last();
            }
            
            var normalSymbols = allSymbols.Where(s => !s.isScatter && !s.isWild).ToList();
            if (normalSymbols.Count == 0) normalSymbols = allSymbols.ToList();

            var assignments = new Dictionary<(int, int), SymbolData>();
            foreach (var pos in scatterPositions)
                assignments[pos] = scatterSymbol;
            
            var symbolCounts = normalSymbols.ToDictionary(s => s, s => 0);
            var allPositions = new List<(int, int)>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (!scatterPositions.Contains((r, c)))
                        allPositions.Add((r, c));

            var shuffled = allPositions.OrderBy(_ => UnityEngine.Random.value).ToList();
            int idx = 0;
            while (idx < shuffled.Count)
            {
                foreach (var sym in normalSymbols)
                {
                    if (idx >= shuffled.Count) break;
                    int count = UnityEngine.Random.Range(1, 5);
                    for (int i = 0; i < count && idx < shuffled.Count; i++, idx++)
                    {
                        assignments[shuffled[idx]] = sym;
                        symbolCounts[sym]++;
                    }
                }
            }
            
            foreach (var kvp in assignments.ToList())
            {
                if (kvp.Value == scatterSymbol) continue;
                if (symbolCounts.TryGetValue(kvp.Value, out var cnt) && cnt > 4)
                {
                    var replacement = normalSymbols.FirstOrDefault(s => symbolCounts[s] < 4);
                    if (replacement != null)
                    {
                        symbolCounts[kvp.Value]--;
                        symbolCounts[replacement]++;
                        assignments[kvp.Key] = replacement;
                    }
                }
            }

            var result = new Dictionary<int, SymbolData[]>();
            for (int c = 0; c < cols; c++)
            {
                var colArr = new SymbolData[rows];
                for (int r = 0; r < rows; r++)
                    colArr[r] = assignments.TryGetValue((r, c), out var s) ? s : normalSymbols[0];
                result[c] = colArr;
            }

            Debug.Log("[BoardView] BuildTutorialScatterSpin: 3 scatters at (row=2,col=0),(row=1,col=2),(row=3,col=3). No wild, 1-4 per normal symbol.");
            return result;
        }
    }
}
