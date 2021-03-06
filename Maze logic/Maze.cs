using System;
using System.Collections.Generic;   //List<T>

namespace Mazeinator
{
    [Serializable]
    internal class Maze
    {
        public enum Heuristic       //http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html
        {
            Euclidean,
            EuclideanSquared,
            Manhattan,
            Diagonal,
            None
        }

        #region Variables

        public Node[,] nodes = null;

        public Node startNode, endNode;
        public Path DFSTree;     

        [NonSerialized]
        public Path pathToRender;

        [NonSerialized]
        public Path GreedyPath = new Path(AlgoType.Greedy);    //has to be assigned, because if pathfinding fails, there would be no path to be rendered!

        [NonSerialized]
        public Path DijkstraPath = new Path(AlgoType.Dijkstra);

        [NonSerialized]
        public Path AStarPath = new Path(AlgoType.Astar);

        private int _nodeCountX, _nodeCountY;
        public int NodeCountX => _nodeCountX;
        public int NodeCountY => _nodeCountY;
        public int renderSizeX, renderSizeY;

        #endregion Variables

        #region MazeGeneration

        /// <summary>
        /// Maze Node constructor
        /// </summary>
        /// <param name="width">Maze width in node count</param>
        /// <param name="height">Maze height in node count</param>
        /// <returns></returns>
        public Maze(int width, int height)
        {
            _nodeCountX = width;
            _nodeCountY = height;
            nodes = new Node[_nodeCountX, _nodeCountY];
            for (int column = 0; column < _nodeCountX; column++)
            {
                for (int row = 0; row < _nodeCountY; row++)
                {
                    nodes[column, row] = new Node(column, row);
                }
            }
        }

        /// <summary>
        /// Maze generating algorithm; requires the Maze class to have nodes array initialized
        /// </summary>
        /// <param name="startNode">Specific node to start generating from</param>
        public bool GenerateMaze(Node startNode = null)
        {
            Random rnd = new Random();
            //if start node wasn't defined, choose it randomly from the nodes array
            if (startNode == null)
            {
                startNode = nodes[rnd.Next(nodes.GetLength(0)), rnd.Next(nodes.GetLength(1))];
            }
            DFSTree = new Path
            {
                exploredNodes = new Node[_nodeCountX, _nodeCountY],
                heuristic = Heuristic.None
            };
            DFSTree.exploredNodes[startNode.X, startNode.Y] = startNode;                //startNode.Root = startNode;

            //create the Visited array and the backtracking stack
            bool[,] Visited = new bool[_nodeCountX, _nodeCountY];
            int VisitedCellsCount = 0;
            Stack<Node> BackTheTrack = new Stack<Node>();
            List<int> UnvisitedNeighbours = new List<int>();

            //start with the starting node
            BackTheTrack.Push(startNode);
            Visited[startNode.X, startNode.Y] = true;
            VisitedCellsCount++;

            //loop until all the cells have been visited
            while (VisitedCellsCount < _nodeCountX * _nodeCountY)
            {
                UnvisitedNeighbours.Clear();
                if (BackTheTrack.Count == 0)
                {
                    break;
                }

                Node currentNode = BackTheTrack.Peek();

                //North check   not at border && the neighbouring cell has NOT been visited
                if (currentNode.Y > 0 && !Visited[currentNode.X, currentNode.Y - 1])
                {
                    UnvisitedNeighbours.Add(Node.North);
                }
                //East check
                if (currentNode.X < _nodeCountX - 1 && !Visited[currentNode.X + 1, currentNode.Y])
                {
                    UnvisitedNeighbours.Add(Node.East);
                }
                //South check
                if (currentNode.Y < _nodeCountY - 1 && !Visited[currentNode.X, currentNode.Y + 1])
                {
                    UnvisitedNeighbours.Add(Node.South);
                }
                //West check
                if (currentNode.X > 0 && !Visited[currentNode.X - 1, currentNode.Y])
                {
                    UnvisitedNeighbours.Add(Node.West);
                }

                if (UnvisitedNeighbours.Count != 0)
                {
                    int direction = UnvisitedNeighbours[rnd.Next(UnvisitedNeighbours.Count)];   //choose one of the unexplored directions
                    switch (direction)
                    {
                        case Node.North:
                            currentNode.Neighbours[Node.North] = nodes[currentNode.X, currentNode.Y - 1]; //set this node's neigbour to be the northern one
                            Visited[currentNode.X, currentNode.Y - 1] = true;                             //set the northern one to be also visited

                            currentNode.Neighbours[Node.North].Neighbours[Node.South] = currentNode;            //set the northern one's neighbour to be this node
                            //currentNode.Neighbours[Node.North].Root = currentNode.Neighbours[Node.North];
                            //currentNode.Neighbours[Node.North].Root = currentNode;
                            DFSTree.exploredNodes[currentNode.X, currentNode.Y - 1] = currentNode;

                            BackTheTrack.Push(currentNode.Neighbours[Node.North]);       //move to the northern node by pushing it onto the stack
                            break;

                        case Node.East:
                            currentNode.Neighbours[Node.East] = nodes[currentNode.X + 1, currentNode.Y];
                            Visited[currentNode.X + 1, currentNode.Y] = true;

                            currentNode.Neighbours[Node.East].Neighbours[Node.West] = currentNode;
                            //currentNode.Neighbours[Node.East].Root = currentNode.Neighbours[Node.East];
                            //currentNode.Neighbours[Node.East].Root = currentNode;
                            DFSTree.exploredNodes[currentNode.X + 1, currentNode.Y] = currentNode;

                            BackTheTrack.Push(currentNode.Neighbours[Node.East]);
                            break;

                        case Node.South:
                            currentNode.Neighbours[Node.South] = nodes[currentNode.X, currentNode.Y + 1];
                            Visited[currentNode.X, currentNode.Y + 1] = true;

                            currentNode.Neighbours[Node.South].Neighbours[Node.North] = currentNode;
                            //currentNode.Neighbours[Node.South].Root = currentNode.Neighbours[Node.South];
                            //currentNode.Neighbours[Node.South].Root = currentNode;
                            DFSTree.exploredNodes[currentNode.X, currentNode.Y + 1] = currentNode;

                            BackTheTrack.Push(currentNode.Neighbours[Node.South]);
                            break;

                        case Node.West:
                            currentNode.Neighbours[Node.West] = nodes[currentNode.X - 1, currentNode.Y];
                            Visited[currentNode.X - 1, currentNode.Y] = true;

                            currentNode.Neighbours[Node.West].Neighbours[Node.East] = currentNode;
                            //currentNode.Neighbours[Node.West].Root = currentNode.Neighbours[Node.West];
                            //currentNode.Neighbours[Node.West].Root = currentNode;
                            DFSTree.exploredNodes[currentNode.X - 1, currentNode.Y] = currentNode;

                            BackTheTrack.Push(currentNode.Neighbours[Node.West]);
                            break;
                    }
                    VisitedCellsCount++;
                }
                else { BackTheTrack.Pop(); }    //return one back, because there is nowhere to go
            }

            pathToRender = (Path)DFSTree.Clone();
            return true;
        }

        /// <summary>
        /// Blank maze generation algorithm that connects every cell's four sides
        /// </summary>
        /// <param name="startNode">Specific node to start generating from</param>
        /// <returns>Blank maze logic map</returns>
        public bool GenerateMazeBlank()
        {
            //fill in NORTH
            for (int column = 0; column < _nodeCountX; column++)
            {
                for (int row = 1; row < _nodeCountY; row++)
                {
                    nodes[column, row].Neighbours[Node.North] = nodes[column, row - 1];
                }
            }

            //fill in EAST
            for (int column = 0; column < _nodeCountX - 1; column++)
            {
                for (int row = 0; row < _nodeCountY; row++)
                {
                    nodes[column, row].Neighbours[Node.East] = nodes[column + 1, row];
                }
            }

            //fill in SOUTH
            for (int column = 0; column < _nodeCountX; column++)
            {
                for (int row = 0; row < _nodeCountY - 1; row++)
                {
                    nodes[column, row].Neighbours[Node.South] = nodes[column, row + 1];
                }
            }

            //fill in WEST
            for (int column = 1; column < _nodeCountX; column++)
            {
                for (int row = 0; row < _nodeCountY; row++)
                {
                    nodes[column, row].Neighbours[Node.West] = nodes[column - 1, row];
                }
            }
            DFSTree = new Path(new Node[_nodeCountX, _nodeCountY]);
            pathToRender = (Path)DFSTree.Clone();
            //foreach (Node node in nodes)
            //    node.Root = node;

            return true;
        }

        /// <summary>
        /// Toggles node's neighbouring nodes
        /// </summary>
        /// <param name="node">The node to be edited</param>
        /// <param name="direction">the Node.direction of the neighbour to toggle</param>
        public void ToggleNeighbour(Node node, int direction)
        {
            GreedyPath = null;      //delete all the paths, because the underlying maze has changed (this works cooperatively with path not being recalculated if start/end-nodes haven't changed)
            DijkstraPath = null;
            AStarPath = null;
            switch (direction)
            {
                case Node.North:
                    if (node.Y > 0)
                    {
                        if (node.Neighbours[Node.North] == null)
                        {
                            node.Neighbours[Node.North] = nodes[node.X, node.Y - 1];
                            node.Neighbours[Node.North].Neighbours[Node.South] = node;
                        }
                        else
                        {
                            node.Neighbours[Node.North].Neighbours[Node.South] = null;
                            node.Neighbours[Node.North] = null;
                        }
                    }
                    break;

                case Node.East:
                    if (node.X < nodes.GetLength(0) - 1)
                    {
                        if (node.Neighbours[Node.East] == null)
                        {
                            node.Neighbours[Node.East] = nodes[node.X + 1, node.Y];
                            node.Neighbours[Node.East].Neighbours[Node.West] = node;
                        }
                        else
                        {
                            node.Neighbours[Node.East].Neighbours[Node.West] = null;
                            node.Neighbours[Node.East] = null;
                        }
                    }
                    break;

                case Node.South:
                    if (node.Y < nodes.GetLength(1) - 1)
                    {
                        if (node.Neighbours[Node.South] == null)
                        {
                            node.Neighbours[Node.South] = nodes[node.X, node.Y + 1];
                            node.Neighbours[Node.South].Neighbours[Node.North] = node;
                        }
                        else
                        {
                            node.Neighbours[Node.South].Neighbours[Node.North] = null;
                            node.Neighbours[Node.South] = null;
                        }
                    }
                    break;

                case Node.West:
                    if (node.X > 0)
                    {
                        if (node.Neighbours[Node.West] == null)
                        {
                            node.Neighbours[Node.West] = nodes[node.X - 1, node.Y];
                            node.Neighbours[Node.West].Neighbours[Node.East] = node;
                        }
                        else
                        {
                            node.Neighbours[Node.West].Neighbours[Node.East] = null;
                            node.Neighbours[Node.West] = null;
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        #endregion MazeGeneration

        #region PathPlanning

        public bool GreedyBFS(Heuristic heuristic)
        {
            if (startNode == null || endNode == null || nodes == null)
            {
                return false;
            }

            if (GreedyPath != null && GreedyPath.startNode == startNode && GreedyPath.endNode == endNode && GreedyPath.heuristic == heuristic)        //no need to recalculate, because everything is the same
            {
                return true;
            }

            GreedyPath = new Path(AlgoType.Greedy)
            {
                startNode = startNode,
                endNode = endNode,
                heuristic = heuristic
            };

            return CalculatePath(GreedyPath, (double distToFinish, int distFromStart, Node node) => new Tuple<double, int, Node>(distToFinish, distFromStart, node));     //the final tuple is Tuple<double, int, Node>(sortValue; distance to node from start; node itself)
        }

        public bool Dijkstra()       //Dijkstra does not utilize heuristic
        {
            if (startNode == null || endNode == null || nodes == null)
            {
                return false;
            }

            if (DijkstraPath != null && DijkstraPath.startNode == startNode && DijkstraPath.endNode == endNode)        //no need to recalculate, because everything is the same
            {
                return true;
            }

            DijkstraPath = new Path(AlgoType.Dijkstra)
            {
                startNode = startNode,
                endNode = endNode,
                heuristic = Heuristic.None
            };

            return CalculatePath(DijkstraPath, (double distToFinish, int distFromStart, Node node) => new Tuple<double, int, Node>(distFromStart, distFromStart, node));     //the final tuple is Tuple<double, int, Node>(sortValue; distance to node from start; node itself)
        }

        public bool AStar(Heuristic heuristic)
        {
            if (startNode == null || endNode == null || nodes == null)
            {
                return false;
            }

            if (AStarPath != null && AStarPath.startNode == startNode && AStarPath.endNode == endNode && AStarPath.heuristic == heuristic)        //no need to recalculate, because everything is the same
            {
                return true;
            }

            AStarPath = new Path(AlgoType.Astar)
            {
                startNode = startNode,
                endNode = endNode,
                heuristic = heuristic
            };

            return CalculatePath(AStarPath, (double distToFinish, int distFromStart, Node node) => new Tuple<double, int, Node>(distToFinish + distFromStart, distFromStart, node));     //the final tuple is Tuple<double, int, Node>(sortValue; distance to node from start; node itself)
        }

        private bool CalculatePath(Path pathCacluated, Func<double, int, Node, Tuple<double, int, Node>> calcTuple)     //https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions    
        {
            int edgeLength = 1;
            bool pathFindErrored = false;

            LinkedList<Tuple<double, int, Node>> frontier = new LinkedList<Tuple<double, int, Node>>(); //holds sorting value, distance from start, actual Node
            //bool[,] frontierWasHere = new bool[_nodeCountX, _nodeCountY];
            Node[,] whereDidIComeFrom = new Node[_nodeCountX, _nodeCountY];
            int[,] distanceToNode = new int[_nodeCountX, _nodeCountY];      // not used in this algorithm, because I search for only 1 target
            for (int x = 0; x < distanceToNode.GetLength(0); x++)
            {
                for (int y = 0; y < distanceToNode.GetLength(1); y++)
                {
                    distanceToNode[x, y] = int.MaxValue;
                }
            }


            //add the starting node to the tree
            frontier.AddFirst(new Tuple<double, int, Node>(0, 0, startNode));
            distanceToNode[startNode.X, startNode.Y] = 0;
            //try to find distance from the startnode for all reachable nodes
            while (frontier.First.Value.Item3 != endNode)
            {
                int currentNodeDistance = frontier.First.Value.Item2;
                Node currentNode = frontier.First.Value.Item3;
                frontier.RemoveFirst();

                //frontierWasHere[currentNode.X, currentNode.Y] = true;
                for (int i = 0; i < 4; i++)
                {
                    Node nodeToVisit = currentNode.Neighbours[i];
                    if (nodeToVisit != null && (currentNodeDistance + edgeLength) < (distanceToNode[nodeToVisit.X, nodeToVisit.Y]))
                    {
                        double diffX = Math.Abs(endNode.X - currentNode.X);
                        double diffY = Math.Abs(endNode.Y - currentNode.Y);
                        double sortValue = 0;

                        switch (pathCacluated.heuristic)        //no need to multiply by the edgeLength, because it's a square grid with a length of 1 in all directions
                        {
                            case Heuristic.Euclidean:
                                sortValue = Math.Sqrt(diffX * diffX + diffY * diffY);
                                break;
                            case Heuristic.EuclideanSquared:
                                sortValue = diffX * diffX + diffY * diffY;
                                break;
                            case Heuristic.Manhattan:
                                sortValue = diffX + diffY;
                                break;
                            case Heuristic.Diagonal:
                                sortValue = (diffX > diffY) ? ((diffX - diffY) + Math.Sqrt(2) * diffY) : ((diffY - diffX) + Math.Sqrt(2) * diffX);
                                break;
                            case Heuristic.None:                                
                                break;
                            default:
                                break;
                        }
                        Tuple<double, int, Node> sortValueTuple = calcTuple(sortValue, currentNodeDistance + edgeLength, nodeToVisit);   //this line makes all the difference in algorithms

                        //use of insertion sort - newly found nodes are being added to an already sorted Linked list for further exploration
                        if (frontier.First == null || frontier.First.Value.Item1 > sortValueTuple.Item1)  //if nothing is in frontier or if the new value is smaller -> add it to the start
                        {
                            frontier.AddFirst(sortValueTuple);
                        }
                        else
                        {
                            LinkedListNode<Tuple<double, int, Node>> currentList = frontier.First;
                            while (currentList.Next != null && currentList.Next.Value.Item1 < sortValueTuple.Item1)        //try it from the closest nodes first(calculation of A* takes into account node distance +it's distance from the finish node)
                            {
                                currentList = currentList.Next;
                            }
                            frontier.AddAfter(currentList, sortValueTuple);
                        }

                        //frontierWasHere[nodeToVisit.X, nodeToVisit.Y] = true;       //mark it as frontierWasHere so they do not duplicate in the frontier
                        whereDidIComeFrom[nodeToVisit.X, nodeToVisit.Y] = currentNode;
                        distanceToNode[nodeToVisit.X, nodeToVisit.Y] = currentNodeDistance + edgeLength;
                    }
                }

                if (frontier.Count == 0)        //we exhausted all available nodes and still haven't found the end
                {
                    pathCacluated.exploredNodes = whereDidIComeFrom;
                    return false;
                }

                ////FORTESTING algorithm logic
                //for (int y = 0; y < distanceToNode.GetLength(1); y++)
                //{
                //    for (int x = 0; x < distanceToNode.GetLength(0); x++)
                //    {
                //        Console.Write(((distanceToNode[x, y] != int.MaxValue) ? distanceToNode[x, y].ToString() : "�") + " \t");
                //    }
                //    Console.WriteLine();
                //}

                ////FORTESTING insertion sort
                //foreach (var item in frontier)
                //{
                //    Console.Write(item.Item1.ToString() + "|d:" + item.Item2 + " \t");
                //}
                //Console.WriteLine("\n");
            }

            if (pathFindErrored == false)
            {
                //clear and write the backtracked shortest path
                pathCacluated.path = new List<Node>
                {
                    endNode
                };
                Node backTrackNode = endNode;
                while (backTrackNode != startNode && backTrackNode != null)
                {
                    pathCacluated.path.Add(whereDidIComeFrom[backTrackNode.X, backTrackNode.Y]);
                    backTrackNode = whereDidIComeFrom[backTrackNode.X, backTrackNode.Y];
                }

                //add the spanning tree to the root visualized
                pathCacluated.exploredNodes = whereDidIComeFrom;
            }

            return true;
        }

        #endregion PathPlanning
    }
}