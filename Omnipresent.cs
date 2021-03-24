using Microsoft.Win32;  //FileDialogs
using System;
using System.ComponentModel;    //INotifyPropertyChanged
using System.Diagnostics;   //Stopwatch
using System.Drawing.Imaging; //ImageFormat (export operation)
using System.Threading.Tasks;   //Tasks so as not to freeze the UI  //https://stackoverflow.com/questions/27089263/how-to-run-and-interact-with-an-async-task-from-a-wpf-gui
using System.Windows;   //MessageBox
using System.Windows.Input; //ICommands
using System.Windows.Media.Imaging; //BitmapImage

namespace Mazeinator
{
    /// <summary>
    /// Class that implements global shortcut binding and subsequent commands (Ctrl+S -> save file)
    /// </summary>
    public class ActionCommand : ICommand
    {
        private readonly Action _action;

        public ActionCommand(Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }

    /// <summary>
    /// Controller for the main window's action; holds objects and performs maze manipulation
    /// </summary>
    internal class Omnipresent : INotifyPropertyChanged
    {
        #region INotify_Binding

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));  //= if(PropertyChanged != null) {}
        }

        #endregion INotify_Binding

        #region Variables

        //define the powerful Maze class
        public Maze MainMaze = null;

        //define the maze style class
        public Style MazeStyle = new Style();

        //holds the current window scaling factor
        public double DPI = 1;

        //defines the internal private variable; AND their "public variable wrapper" for WPF binding
        private int _nodeCount = 0; public int NodeCount { get => _nodeCount; set { _nodeCount = value; OnPropertyChanged(nameof(NodeCount)); } }

        private int _nodeCountX = 3; public int NodeCountX { get => _nodeCountX; set { _nodeCountX = value; OnPropertyChanged(nameof(NodeCountX)); } }
        private int _nodeCountY = 3; public int NodeCountY { get => _nodeCountY; set { _nodeCountY = value; OnPropertyChanged(nameof(NodeCountY)); } }
        private long _lastGenTime = 0; public long LastGenTime { get => _lastGenTime; set { _lastGenTime = value; OnPropertyChanged(nameof(LastGenTime)); } }
        private long _lastRenderTime = 0; public long LastRenderTime { get => _lastRenderTime; set { _lastRenderTime = value; OnPropertyChanged(nameof(LastRenderTime)); } }
        private int _renderSizeX = 0; public int RenderSizeX { get => _renderSizeX; set { _renderSizeX = value; OnPropertyChanged(nameof(RenderSizeX)); } }
        private int _renderSizeY = 0; public int RenderSizeY { get => _renderSizeY; set { _renderSizeY = value; OnPropertyChanged(nameof(RenderSizeY)); } }
        private int _canvasSizeX = 0; public int CanvasSizeX { get => _canvasSizeX; set { _canvasSizeX = value; OnPropertyChanged(nameof(CanvasSizeX)); } }
        private int _canvasSizeY = 0; public int CanvasSizeY { get => _canvasSizeY; set { _canvasSizeY = value; OnPropertyChanged(nameof(CanvasSizeY)); } }

        //TESTING↓
        private int _percent = 20; public int Percent { get => _percent; set { _percent = value; OnPropertyChanged(nameof(Percent)); } }

        private string _status = "Ready"; public string Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }
        private string _currentFilePath = null;

        private BitmapImage _maze = null; public BitmapImage Maze { get => _maze; set { _maze = value; OnPropertyChanged(nameof(Maze)); } }
        private System.Drawing.Bitmap _mazeBMP = null;      //stored raw rendered maze; used as a base image for further drawing of start/end nodes and path

        public long AvgGenTime = 0, AvgRenderTime = 0;

        #endregion Variables

        #region Global_shortcuts

        private ICommand _newFileCMD; public ICommand NewFileCMD { get { return _newFileCMD ?? (_newFileCMD = new ActionCommand(() => { NewMaze(); })); } }
        private ICommand _saveFileCMD; public ICommand SaveFileCMD { get { return _saveFileCMD ?? (_saveFileCMD = new ActionCommand(() => { SaveMaze(); })); } }
        private ICommand _loadFileCMD; public ICommand LoadFileCMD { get { return _loadFileCMD ?? (_loadFileCMD = new ActionCommand(() => { LoadMaze(); })); } }
        private ICommand _generateCMD; public ICommand GenerateCMD { get { return _generateCMD ?? (_generateCMD = new ActionCommand(() => { MazeGeneration(new Tuple<int, int>(CanvasSizeX, CanvasSizeY)); ; })); } }
        private ICommand _exportCMD; public ICommand ExportCMD { get { return _exportCMD ?? (_exportCMD = new ActionCommand(() => { Export(); })); } }
        private ICommand _quitCMD; public ICommand QuitCMD { get { return _quitCMD ?? (_quitCMD = new ActionCommand(() => { Application.Current.MainWindow.Close(); })); } }

        #endregion Global_shortcuts

        #region Maze_functions

        public void MazeGeneration(Tuple<int, int> CanvasSize)
        {
            _currentFilePath = null; //reset the "save without asking" path

            //stopwatch to measure the generation time and make user predictions
            Stopwatch GenTime = new Stopwatch();
            GenTime.Start();

            MainMaze = new Maze(NodeCountX, NodeCountY);
            MainMaze.GenerateMaze();

            GenTime.Stop();
            LastGenTime = GenTime.ElapsedMilliseconds;

            Status = "Generating done";
            NodeCount = MainMaze.nodes.Length;

            Render(CanvasSize);
        }

        public void MazeGenBlank(Tuple<int, int> CanvasSize)
        {
            _currentFilePath = null; //reset the "save without asking" path

            //stopwatch to measure the generation time and make user predictions
            Stopwatch GenTime = new Stopwatch();
            GenTime.Start();

            MainMaze = new Maze(NodeCountX, NodeCountY);
            MainMaze.GenerateMazeBlank();

            GenTime.Stop();
            LastGenTime = GenTime.ElapsedMilliseconds;

            Status = "Generating done";
            NodeCount = MainMaze.nodes.Length;
            MazeStyle.RenderPoint = true;   //so the user can see the individual nodes

            Render(CanvasSize);
        }

        public void PathGreedy()
        {
            Stopwatch ProcessTime = new Stopwatch();
            ProcessTime.Start();

            if (MainMaze == null || !MainMaze.GreedyBFS())
                Status = "Pathfinding failed";
            else
                Status = "Greedy best-first search done";

            LastGenTime = ProcessTime.ElapsedMilliseconds;
            ProcessTime.Restart();

            RenderPath(MainMaze.GreedyPath);

            ProcessTime.Stop();
            LastRenderTime = ProcessTime.ElapsedMilliseconds;
        }

        public void PathDijkstra()
        {
            Stopwatch ProcessTime = new Stopwatch();
            ProcessTime.Start();

            if (MainMaze == null || !MainMaze.Dijkstra())
                Status = "Pathfinding failed";
            else
                Status = "Dijkstra done";

            LastGenTime = ProcessTime.ElapsedMilliseconds;
            ProcessTime.Restart();

            RenderPath(MainMaze.DijkstraPath);

            ProcessTime.Stop();
            LastRenderTime = ProcessTime.ElapsedMilliseconds;
        }

        public void PathAStar()
        {
            Stopwatch ProcessTime = new Stopwatch();
            ProcessTime.Start();

            if (MainMaze == null || !MainMaze.AStar())
                Status = "Pathfinding failed";
            else
                Status = "A* done";

            LastGenTime = ProcessTime.ElapsedMilliseconds;
            ProcessTime.Restart();

            RenderPath(MainMaze.AStarPath);

            ProcessTime.Stop();
            LastRenderTime = ProcessTime.ElapsedMilliseconds;
        }

        /// <summary>
        /// Function that selects the start and end Node in the Maze class
        /// </summary>
        /// <param name="monitorClick">RAW WPF units where I clicked in the image relative to the active monitor</param>
        /// <param name="imageClick">Image clicked pixels</param>
        /// <param name="transfrom">Current image scaling</param>
        public void MazeNodeSelect(Point monitorClick, Point imageClick, Point transfrom)
        {
            if (MainMaze != null) //means that the node[0,0] must exist
            {
                //Point * display_scaling * (real_Width / WPF_Width) -> x clicked coordinate in image
                double imageXinPX = imageClick.X * DPI * transfrom.X;
                double imageYinPX = imageClick.Y * DPI * transfrom.Y;

                //(x coordinates - wallThickness) / cell.width -> floor & clamp (0 and nodes length) -> maze node selector
                int cellWidth = MainMaze.nodes[0, 0].Bounds.Width;
                int cellHeight = MainMaze.nodes[0, 0].Bounds.Height;

                int cellXStart = MainMaze.nodes[0, 0].Bounds.X;
                int cellYStart = MainMaze.nodes[0, 0].Bounds.Y;

                int selectX = Utilities.Clamp((int)((imageXinPX - cellXStart) / cellWidth), 0, MainMaze.nodes.GetLength(0) - 1);
                int selectY = Utilities.Clamp((int)((imageYinPX - cellYStart) / cellHeight), 0, MainMaze.nodes.GetLength(1) - 1);

                //string text = (selectX).ToString() + " | " + (selectY).ToString();
                //Console.WriteLine(text);
                //text = (coordX).ToString() + " | " + (coordY).ToString();
                //Console.WriteLine(text);

                //node snap_to_grid functionality - calculates offset (where_is_the_center - where_user_clicked)
                double gridSnapX = (selectX * cellWidth + cellXStart + (double)cellWidth / 2) - (imageXinPX) + 0.5;
                double gridSnapY = (selectY * cellHeight + cellYStart + (double)cellHeight / 2) - (imageYinPX) + 0.5;

                int cellSize = (cellWidth < cellHeight) ? cellWidth : cellHeight;
                NodeSettings NodeSelector = new NodeSettings((int)(cellSize / DPI / 5));
                //centers the window on the current cell - where user clicked + grid_snap_offset + half_the_window_width
                //divided by the current monitor_scaling_DPI -> get back to WPF units
                NodeSelector.Left = ((monitorClick.X + gridSnapX / transfrom.X) / DPI - NodeSelector.Width / 2);
                NodeSelector.Top = ((monitorClick.Y + gridSnapY / transfrom.Y) / DPI - NodeSelector.Height / 2);

                //window rendering near the edges screen fix
                //gets the active screen's X, Y, Width, Height properties in pixels
                System.Windows.Interop.WindowInteropHelper windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow);
                System.Windows.Forms.Screen Screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
                //Console.WriteLine(Screen.Bounds);

                //a little conversion from (pixels)Screen to (WPF unites)NodeSelector by dividing pixels/DPI
                if (NodeSelector.Left < Screen.Bounds.Left / DPI)
                    NodeSelector.TargetSwap(Node.West);
                else if (NodeSelector.Left + NodeSelector.Width > Screen.Bounds.Right / DPI)
                    NodeSelector.TargetSwap(Node.East);
                else if (NodeSelector.Top + NodeSelector.Height > Screen.Bounds.Bottom / DPI)
                    NodeSelector.TargetSwap(Node.South);
                else if (NodeSelector.Top < Screen.Bounds.Top / DPI)
                    NodeSelector.TargetSwap(Node.North);

                if (NodeSelector.ShowDialog() == true)
                {
                    int selector = NodeSelector.selector;
                    MainMaze.pathToRender.Clear();

                    Node targetNode = MainMaze.nodes[selectX, selectY];

                    switch (selector)
                    {
                        case Node.North:
                            MainMaze.ToggleNeighbour(targetNode, selector);
                            MainMaze.RenderNode(_mazeBMP, targetNode, MazeStyle);
                            break;

                        case Node.East:
                            MainMaze.ToggleNeighbour(targetNode, selector);
                            MainMaze.RenderNode(_mazeBMP, targetNode, MazeStyle);
                            break;

                        case Node.South:
                            MainMaze.ToggleNeighbour(targetNode, selector);
                            MainMaze.RenderNode(_mazeBMP, targetNode, MazeStyle);
                            break;

                        case Node.West:
                            MainMaze.ToggleNeighbour(targetNode, selector);
                            MainMaze.RenderNode(_mazeBMP, targetNode, MazeStyle);
                            break;

                        case 10: //startNode
                            if (targetNode == MainMaze.endNode)
                                MainMaze.endNode = null;

                            if (targetNode == MainMaze.startNode)
                                MainMaze.startNode = null;
                            else
                                MainMaze.startNode = targetNode;
                            break;

                        case 11: //endNode
                            if (targetNode == MainMaze.startNode)
                                MainMaze.startNode = null;

                            if (targetNode == MainMaze.endNode)
                                MainMaze.endNode = null;
                            else
                                MainMaze.endNode = targetNode;
                            break;

                        case 20: //auxilary button
                            PathDijkstra();
                            break;

                        default:
                            break;
                    }
                    RenderPath();
                }
            }
        }

        public void SettingOpen()
        {
            Style currentStyle = (Style)MazeStyle.Clone();

            StyleSettings settings = new StyleSettings(currentStyle, DPI);
            if (settings.ShowDialog() == true)
            {
                MazeStyle = settings.SettingsStyle;
                Render();
                Status = "Setting applied";
            }
        }

        #endregion Maze_functions

        #region Maze_render

        public void Render()
        {
            Render(new Tuple<int, int>(CanvasSizeX, CanvasSizeY));
        }

        public void Render(Tuple<int, int> CanvasSize)
        {
            if (MainMaze != null)
            {
                //_mazeBMP = MainMaze.RenderMaze(CanvasSize.Item1, CanvasSize.Item2, MazeStyle);
                //Maze = Utilities.BitmapToImageSource(MainMaze.RenderPath((System.Drawing.Bitmap)_mazeBMP.Clone(), MazeStyle));

                new Task(() =>
                {
                    Stopwatch RenderTime = new Stopwatch();
                    RenderTime.Start();
                    _mazeBMP = MainMaze.RenderMaze(CanvasSize.Item1, CanvasSize.Item2, MazeStyle);

                    BitmapImage mazeRender = Utilities.BitmapToImageSource(MainMaze.RenderPath((System.Drawing.Bitmap)_mazeBMP.Clone(), MazeStyle));
                    mazeRender.Freeze();
                    Maze = mazeRender;

                    RenderTime.Stop();
                    LastRenderTime = RenderTime.ElapsedMilliseconds;

                    Status = "Rendering done";
                    RenderSizeX = MainMaze.renderSizeX;
                    RenderSizeY = MainMaze.renderSizeY;
                    CanvasSizeX = CanvasSize.Item1;
                    CanvasSizeY = CanvasSize.Item2;
                    GC.Collect();
                }).Start();
            }
        }

        public void RenderPath()
        {
            //wanted to draw on a bitmap, but Bitmap is a class -> I've to copy it, then draw on it, then return and display it
            Maze = Utilities.BitmapToImageSource(MainMaze.RenderPath((System.Drawing.Bitmap)_mazeBMP.Clone(), MazeStyle));

            GC.Collect();   //collect the leftovers
        }

        //overloaded method (similar to the one in Maze) to render specific paths
        public void RenderPath(Path path)
        {
            //wanted to draw on a bitmap, but Bitmap is a class -> I've to copy it, then draw on it, then return and display it
            Maze = Utilities.BitmapToImageSource(MainMaze.RenderPath((System.Drawing.Bitmap)_mazeBMP.Clone(), MazeStyle, path));

            GC.Collect();   //collect the leftovers
        }

        #endregion Maze_render

        #region Data_manipulation

        public void NewMaze()
        {
            MainMaze = null;
            _currentFilePath = null;
            Maze = null;
        }

        public void SaveMaze()
        {
            if (MainMaze == null)   //only save maze if there is one
            {
                MessageBox.Show("Cannot save an empty maze", "Unable to save", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Save Maze As",
                FileName = "Maze",
                Filter = "Maze files (*.maze)|*.maze|All files (*.*)|*.*",
                FilterIndex = 1,
                AddExtension = true,
                //dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                RestoreDirectory = true
            };

            if (_currentFilePath == null)
            {
                if (dialog.ShowDialog() == true)
                    _currentFilePath = dialog.FileName;
            }

            if (_currentFilePath != null)
            {
                Status = "Saving...";
                new Task(() =>
                {
                    try
                    {
                        Stopwatch ProcessTime = new Stopwatch();
                        ProcessTime.Start();

                        Utilities.SaveBySerializing<Maze>(MainMaze, _currentFilePath);

                        ProcessTime.Stop();
                        LastGenTime = ProcessTime.ElapsedMilliseconds;
                        Status = "Saving done";
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("An unhandled saving exception just occured: " + exc.Message, "Unhandled saving exception", MessageBoxButton.OK, MessageBoxImage.Error);
                        Status = "Saving failed";
                    }
                }).Start();
            }
        }

        public void LoadMaze()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Load Maze",
                FileName = "Maze",
                Filter = "Maze files (*.maze)|*.maze|All files (*.*)|*.*",
                FilterIndex = 1,
                AddExtension = true,
                //dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                RestoreDirectory = true
            };

            _currentFilePath = null;
            if (dialog.ShowDialog() == true)
                _currentFilePath = dialog.FileName;

            //asynhronously load the maze, in a try/catch
            if (_currentFilePath != null)
            {
                Status = "Commencing loading operation...";

                new Task(() =>
                {
                    try
                    {
                        Stopwatch ProcessTime = new Stopwatch();
                        ProcessTime.Start();

                        MainMaze = Utilities.LoadFromTheDead<Maze>(dialog.FileName);

                        //if there is no start/end-node selected, load the generating tree
                        if (MainMaze.startNode == null && MainMaze.endNode == null)
                        {
                            MainMaze.pathToRender = (Path)MainMaze.DFSTree.Clone();
                        }
                        else    //calculate all the algorithms
                        {
                            //recalculate the paths (using the best algorithm)
                            MainMaze.AStar();
                            MainMaze.pathToRender = MainMaze.AStarPath;
                        }

                        Render();

                        ProcessTime.Stop();
                        LastGenTime = ProcessTime.ElapsedMilliseconds;
                        Status = "Loading done";
                        NodeCountX = MainMaze.NodeCountX;
                        NodeCountY = MainMaze.NodeCountY;
                        NodeCount = MainMaze.nodes.Length;
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("An unhandled loading exception just occured: " + exc.Message, "Unhandled loading exception", MessageBoxButton.OK, MessageBoxImage.Error);
                        Status = "Loading failed";
                    }
                }).Start();
            }
        }

        public void Export()
        {
            if (MainMaze == null)   //only save maze if there is one
            {
                MessageBox.Show("Cannot export an empty maze", "Export failed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ImageFormat[] ImageFormats = { ImageFormat.Bmp, ImageFormat.Jpeg, ImageFormat.Gif, ImageFormat.Tiff, ImageFormat.Png, ImageFormat.Icon };
            ExportSettings exportWindow = new ExportSettings(RenderSizeX, RenderSizeY, NodeCountX, NodeCountY, MazeStyle.WallThickness, MazeStyle.IsSquare);
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Export image",
                FileName = "Labyrinth",
                //filter order is the same as in the ImageFormats array
                Filter = "BMP files (*.bmp)|*.bmp|JPG files (*.jpg)|*.jpg|GIF files (*.gif)|*.gif|TIFF files (*.tiff)|*.tiff|PNG files (*.png)|*.png|ICON files (*.ico)|*.ico",
                FilterIndex = 5,
                AddExtension = true,
                //dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == true && exportWindow.ShowDialog() == true)
            {
                Status = "Commencing export";
                new Task(() =>
                {
                    try
                    {
                        Stopwatch ProcessTime = new Stopwatch();
                        ProcessTime.Start();

                        MazeStyle.IsSquare = exportWindow.IsSquare;
                        //draws the path(generates bitmap)
                        System.Drawing.Bitmap mazeRender = MainMaze.RenderPath(MainMaze.RenderMaze(exportWindow.ExportSizeX, exportWindow.ExportSizeY, MazeStyle, true), MazeStyle);

                        //resize the rendered bitmap (only does it, if it's needed)
                        new System.Drawing.Bitmap(mazeRender, exportWindow.ExportSizeX, exportWindow.ExportSizeY).Save(dialog.FileName, ImageFormats[dialog.FilterIndex - 1]);

                        ProcessTime.Stop();
                        LastRenderTime = ProcessTime.ElapsedMilliseconds;

                        Render();

                        MessageBox.Show("Export done", "Export done", MessageBoxButton.OK, MessageBoxImage.Information);
                        Status = "Export done";
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("An unhandled exporting exception just occured: " + exc.Message, "Unhandled export exception", MessageBoxButton.OK, MessageBoxImage.Error);
                        Status = "Export failed";
                    }
                }).Start();
            }
        }

        #endregion Data_manipulation
    }
}