using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ShortestOpenPath_Algorithm
{
    public class ShortestOpenPath : MonoBehaviour
    {
        // represents a sigle point in "forest"
        [Serializable]
        public class Tree
        {
            public int treeID;
            public List<double> coordinates;
            public List<Tree> neighbours;

            public Tree()
            {
                coordinates = new List<double>();
                neighbours = new List<Tree>();
            }
        }

        [SerializeField]
        private Transform treeRoot;
        [SerializeField]
        private Transform connectionRoot;

        [SerializeField]
        private List<GameObject> forest;
        [SerializeField]
        private List<Tree> trees;
        [SerializeField]
        private List<GameObject> connectionRenderers;

        [SerializeField]
        private GameObject treePrefab;
        [SerializeField]
        private GameObject connectionPrefab;

        private bool initStep;
        private List<int> visitedIds;
        List<int> colors;

        private void Start()
        {
            forest = new List<GameObject>();
            visitedIds = new List<int>();
            colors = new List<int>();
        }

        public void CalculationScenario()
        {
            // read file and save data as separate items
            trees = ReadCsv_("D:/Programming/MachineLearning/Yefimov/testing_1.csv", true, 1);
            // spawn trees in 3d space with 2D coordinates
            SpawnTrees(trees);

            // setting connection with nearest neighbour
            for (int i = 0; i < forest.Count; i++)
                SetNearestNeighbour(trees[i], trees, i);

            /*
            List<Tree> nonConnected = new List<Tree>();
            for (int i = 0; i < forest.Count; i++)
                if (trees[i].neighbours.Count == 0)
                    nonConnected.Add(trees[i]);
            Debug.Log("NonConnected: " + nonConnected.Count);

            for (int i = 0; i < nonConnected.Count; i++)
                SetNearestNeighbour(nonConnected[i], trees, i);
            */

            for (int i = 0; i < forest.Count; i++)
                SetNearestNeighbour(trees[i], trees, i);

            for (int i = 0; i < trees.Count; i++)
            {
                print("***");
                print("Current: "+(i+1));
                //DoDfs(trees, trees[i]);
            }

            // draw set connections
            for (int i = 0; i < trees.Count; i++)
            {
                for (int j = 0; j < trees[i].neighbours.Count; j++)
                {
                    GameObject connection = Instantiate(connectionPrefab, connectionRoot);
                    connection.GetComponent<LineRenderer>().positionCount = 2;

                    if (trees[i].treeID == trees[i].neighbours[j].treeID)
                        continue;

                    connection.GetComponent<LineRenderer>().SetPosition(0, forest[trees[i].treeID].transform.position);
                    connection.GetComponent<LineRenderer>().SetPosition(1, forest[trees[i].neighbours[j].treeID].transform.position);
                    connectionRenderers.Add(connection);
                }
            }
        }

        private void SpawnTrees(List<Tree> _forest)
        {
            for (int i = 0; i < _forest.Count; i++)
            {
                _forest[i].treeID = i;
                GameObject _tree = Instantiate(treePrefab, treeRoot);
                _tree.transform.localPosition = new Vector3((float)_forest[i].coordinates[0] * 0.1f, (float)_forest[i].coordinates[1] * 0.1f);
                forest.Add(_tree);
            }
        }

        private List<Tree> ReadCsv_(string filepath, bool skipFirst, int classColumnId)
        {
            int currentPos = 0;

            List<Tree> tokens = new List<Tree>();
            using (var reader = new StreamReader(filepath))
            {
                while (!reader.EndOfStream)
                {
                    if (skipFirst)
                    {
                        reader.ReadLine();
                        currentPos++;
                        skipFirst = false;
                        continue;
                    }

                    Tree token = new Tree();
                    int i = 0;
                    string line = reader.ReadLine();
                    if (line == null)
                        break;
                    line = line.Replace('"', ' ');
                    currentPos++;

                    foreach (var item in line.Split(','))
                    {
                        i++;
                        if (i == classColumnId)
                            continue;

                        if (!Double.TryParse(item.Replace('.', ','), out double _item))
                        {
                            Console.WriteLine("Something went wrong. [" + item + "]");
                            continue;
                        }
                        if (token.coordinates.Count >= 2)
                            continue;
                        token.coordinates.Add(_item);
                    }
                    tokens.Add(token);
                }
            }
            Debug.Log("Tokens: " + tokens.Count);
            return tokens;
        }

        public void SetNearestNeighbour(Tree currentTree, List<Tree> forest, int curTreeId)
        {
            int nearestTreeId = -1;
            double minDistance = -1;
            List<double> distance = new List<double>();
            for (int i = 0; i < forest.Count; i++)
            {
                if (i == curTreeId)
                {
                    distance.Add(0);
                    continue;
                }

                double sum = 0;
                for (int j = 0; j < forest[i].coordinates.Count; j++)
                    sum += Math.Pow(forest[i].coordinates[j] - currentTree.coordinates[j], 2);

                distance.Add(Math.Sqrt(sum));

                if (minDistance == -1)
                    minDistance = sum;
                bool isConnected = false;
                foreach (var item in currentTree.neighbours)
                    if (item.treeID == i) {
                        isConnected = true;
                        break;
                    } else continue;
                bool doSave = minDistance > sum && !isConnected;

                nearestTreeId = doSave ? i : nearestTreeId;
                minDistance = doSave ? sum : minDistance;
            }

            if (nearestTreeId == -1 || minDistance == -1)
                nearestTreeId = currentTree.treeID;

            currentTree.neighbours.Add(forest[nearestTreeId]);
            forest[nearestTreeId].neighbours.Add(currentTree);
        }

        private void RemoveCycle(Tree currentTree)
        {
            print("Started");
            // if no neighbours - return
            if (currentTree.neighbours.Count == 0)
                return;
            visitedIds.Add(currentTree.treeID);

            foreach (var neighbour in currentTree.neighbours)
            {
                foreach (var item in neighbour.neighbours)
                {
                    if (item.treeID == currentTree.treeID)
                        continue;
                    if (visitedIds.Contains(item.treeID))
                    {
                        print("cycle found: " + currentTree.treeID + " - " + neighbour.treeID + " - " + item.treeID);
                        //neighbour.neighbours.Remove(item);
                        break;
                    }
                    //else RemoveCycle(item);
                }
            }
            //visitedIds.Add(currentTree.treeID);
            //foreach (var neighbour in currentTree.neighbours)
                //RemoveCycle(neighbour);
        }


        private int prevId, curId;
        private List<Tuple<int, int>> remove = new List<Tuple<int, int>>();

        private void DoDfs(List<Tree> forest, Tree tree)
        {
            colors.Clear();
            remove.Clear();

            foreach (var item in forest)
                colors.Add(0); // white

            Dfs(tree);

            print("Pairs: " + remove.Count);
            foreach (var item in remove)
            {
                foreach (var treeItem in trees[item.Item1].neighbours)
                {
                    if (treeItem.treeID != item.Item2)
                        continue;
                    trees[item.Item1].neighbours.Remove(treeItem);
                    break;
                }
            }
        }

        private void Dfs(Tree tree)
        {
            colors[tree.treeID] = 1; // grey
            prevId = tree.treeID;

            foreach (var item in tree.neighbours)
            {
                curId = -1;
                if (colors[item.treeID] == 0) // if white
                    Dfs(item);
                else if (colors[item.treeID] == 1) // if grey
                {
                    curId = item.treeID;
                    if(prevId != curId)
                        remove.Add(new Tuple<int, int>(prevId, curId));
                    return;
                }
            }
            colors[tree.treeID] = 2; // black
        }
    }
}
