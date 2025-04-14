using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    [Header("AI Settings")]
    public bool smartRobberAI = false;

    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    private Tile FindFarthestTileFromCops(int robberPosition)
    {
        Queue<Tile> queue = new Queue<Tile>();
        bool[] visited = new bool[Constants.NumTiles];
        int[] distancesFromRobber = new int[Constants.NumTiles];

        queue.Enqueue(tiles[robberPosition]);
        visited[robberPosition] = true;
        distancesFromRobber[robberPosition] = 0;

        while (queue.Count > 0)
        {
            Tile current = queue.Dequeue();

            foreach (int neighborIndex in current.adjacency)
            {
                if (!visited[neighborIndex])
                {
                    visited[neighborIndex] = true;
                    distancesFromRobber[neighborIndex] = distancesFromRobber[current.numTile] + 1;
                    queue.Enqueue(tiles[neighborIndex]);
                }
            }
        }

        int[] minDistancesToCops = new int[Constants.NumTiles];
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            minDistancesToCops[i] = int.MaxValue;
        }

        foreach (GameObject cop in cops)
        {
            int copPosition = cop.GetComponent<CopMove>().currentTile;
            queue = new Queue<Tile>();
            visited = new bool[Constants.NumTiles];
            int[] distancesFromCop = new int[Constants.NumTiles];

            queue.Enqueue(tiles[copPosition]);
            visited[copPosition] = true;
            distancesFromCop[copPosition] = 0;

            while (queue.Count > 0)
            {
                Tile current = queue.Dequeue();
                minDistancesToCops[current.numTile] = Mathf.Min(minDistancesToCops[current.numTile], distancesFromCop[current.numTile]);

                foreach (int neighborIndex in current.adjacency)
                {
                    if (!visited[neighborIndex])
                    {
                        visited[neighborIndex] = true;
                        distancesFromCop[neighborIndex] = distancesFromCop[current.numTile] + 1;
                        queue.Enqueue(tiles[neighborIndex]);
                    }
                }
            }
        }

        Tile bestTile = null;
        int maxDistance = -1;
        int maxRobberDistance = -1;

        ResetTiles();
        FindSelectableTiles(false);

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (tiles[i].selectable && i != robberPosition)
            {
                if (minDistancesToCops[i] > maxDistance ||
                    (minDistancesToCops[i] == maxDistance && distancesFromRobber[i] > maxRobberDistance))
                {
                    maxDistance = minDistancesToCops[i];
                    maxRobberDistance = distancesFromRobber[i];
                    bestTile = tiles[i];
                }
            }
        }

        return bestTile ?? tiles[robberPosition];
    }

    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            int row = i / Constants.TilesPerRow;
            int col = i % Constants.TilesPerRow;

            // Arriba
            if (row > 0)
            {
                matriu[i, i - Constants.TilesPerRow] = 1;
            }
            // Abajo
            if (row < Constants.TilesPerRow - 1)
            {
                matriu[i, i + Constants.TilesPerRow] = 1;
            }
            // Izquierda
            if (col > 0)
            {
                matriu[i, i - 1] = 1;
            }
            // Derecha
            if (col < Constants.TilesPerRow - 1)
            {
                matriu[i, i + 1] = 1;
            }
        }

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            tiles[i].adjacency.Clear();
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                if (matriu[i, j] == 1)
                {
                    tiles[i].adjacency.Add(j);
                }
            }
        }
    }

    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        Tile targetTile;

        if (smartRobberAI)
        {
            targetTile = FindFarthestTileFromCops(clickedTile);
        }
        else
        {
            List<Tile> selectableTiles = new List<Tile>();
            for (int i = 0; i < Constants.NumTiles; i++)
            {
                if (tiles[i].selectable && i != robber.GetComponent<RobberMove>().currentTile)
                {
                    selectableTiles.Add(tiles[i]);
                }
            }

            targetTile = selectableTiles.Count > 0 ?
                selectableTiles[Random.Range(0, selectableTiles.Count)] :
                tiles[clickedTile];
        }

        robber.GetComponent<RobberMove>().MoveToTile(targetTile);
        robber.GetComponent<RobberMove>().currentTile = targetTile.numTile;
    }

    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop == true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        tiles[indexcurrentTile].current = true;

        Queue<Tile> nodes = new Queue<Tile>();

        tiles[indexcurrentTile].visited = true;
        tiles[indexcurrentTile].distance = 0;
        nodes.Enqueue(tiles[indexcurrentTile]);

        while (nodes.Count > 0)
        {
            Tile current = nodes.Dequeue();

            if (current.distance >= Constants.Distance)
                continue;

            foreach (int neighborIndex in current.adjacency)
            {
                Tile neighbor = tiles[neighborIndex];

                if (!neighbor.visited &&
                    (neighborIndex != cops[(clickedCop + 1) % 2].GetComponent<CopMove>().currentTile || !cop))
                {
                    neighbor.visited = true;
                    neighbor.distance = current.distance + 1;
                    neighbor.parent = current;
                    nodes.Enqueue(neighbor);

                    neighbor.selectable = true;
                }
            }
        }
    }









}
