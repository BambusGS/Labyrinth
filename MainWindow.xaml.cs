using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading; //DispatcherTimer   https://docs.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatchertimer?view=net-5.0

/*  ===TODO===
 *
 *  https://github.com/OneLoneCoder/videos/blob/master/OneLoneCoder_Mazes.cpp - DFS generation algorithm
 *  http://csharphelper.com/blog/2016/11/make-draw-maze-c/ - rendering system general idea
 *
 *
 *  ===IDEAS===
 *  add kruskal's algorithm for maze generation
 *  add check to CalculatePath() whether the new path is shorter (not needed, because all edges are 1 in length)
 */

namespace Mazeinator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Omnipresent _controller = new Omnipresent();
        private DispatcherTimer _autoRenderTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            _controller = new Omnipresent();
            DataContext = _controller;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _controller.DPI = GetScaling();

            //preset these values for a possible MazeGeneration shortcut command
            _controller.CanvasSizeX = GetCanvasSizePixels().Item1;
            _controller.CanvasSizeY = GetCanvasSizePixels().Item2;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;    //override the closing operation
            CloseApp(null, null);
        }

        #region AutoRe-Render

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_controller.MainMaze != null)
            {
                //triggers the timer
                if (_autoRenderTimer.IsEnabled == false)
                {
                    _autoRenderTimer.Tick += new EventHandler(AutoRender);
                    _autoRenderTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                    _autoRenderTimer.Start();
                }

                //restart the timer - window has just been resized
                if (_autoRenderTimer.IsEnabled == true)
                {
                    _autoRenderTimer.Stop();
                    _autoRenderTimer.Start();
                }
            }
            else
            {
                _controller.CanvasSizeX = GetCanvasSizePixels().Item1;
                _controller.CanvasSizeY = GetCanvasSizePixels().Item2;
            }
        }

        private void AutoRender(object sender, EventArgs e)
        {
            _autoRenderTimer.Stop();
            _autoRenderTimer.Tick -= AutoRender;
            _controller.RenderAsync(GetCanvasSizePixels());
        }

        #endregion AutoRe-Render

        #region Menu

        private void NewMaze(object sender, RoutedEventArgs e)
        {
            _controller.NewMaze();
        }

        private void SaveMaze(object sender, RoutedEventArgs e)
        {
            _controller.SaveMaze();
        }

        private void LoadMaze(object sender, RoutedEventArgs e)
        {
            _controller.LoadMaze();
        }

        private void ExportMaze(object sender, RoutedEventArgs e)
        {
            _controller.Export();
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            if (Utilities.isWorking == true) //do not exit the app when file is being saved/loaded
            {
                MessageBox.Show("Cannot quit: File is being processed", "Unable to quit", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
            }
            else { Application.Current.Shutdown(); }
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            _controller.About();
        }

        #endregion Menu

        #region MazeFunctions

        private void MazeGeneration(object sender, RoutedEventArgs e)
        {
            _controller.MazeGeneration(GetCanvasSizePixels());
        }

        private void MazeBlankGeneration(object sender, RoutedEventArgs e)
        {
            _controller.MazeGenBlank(GetCanvasSizePixels());
        }

        private void Greedy_click(object sender, RoutedEventArgs e)
        {
            _controller.PathGreedy();
        }

        private void Dijkstra_click(object sender, RoutedEventArgs e)
        {
            _controller.PathDijkstra();
        }

        private void AStar_click(object sender, RoutedEventArgs e)
        {
            _controller.PathAStar();
        }

        private void HeuristicSelector(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _controller.Heuristic = (Maze.Heuristic)HeurSelect.SelectedIndex;
        }

        private void SelectNode(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (pictureBox.Source != null)
            {
                Point pointPicture = e.GetPosition(pictureBox);
                Point monitorPoint = PointToScreen(e.GetPosition(this));
                //(real_Width / WPF_Width) -> image scaling
                double x = pictureBox.Source.Width / pictureBox.ActualWidth;
                double y = pictureBox.Source.Height / pictureBox.ActualHeight;

                Point transfrom = new Point(x, y);

                _controller.MazeNodeSelect(monitorPoint, pointPicture, transfrom);
            }
        }

        private void SettingOpen(object sender, RoutedEventArgs e)
        {
            _controller.SettingOpen();
        }

        #endregion MazeFunctions

        #region CustomFunctions

        /// <summary>
        /// Function that gets the current "WPF scaling"
        /// </summary>
        private double GetScaling()
        {
            //get the current "WPF DPI measure units"   //https://docs.microsoft.com/en-us/archive/blogs/jaimer/getting-system-dpi-in-wpf-app
            Matrix m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            double scaling = m.M11;

            //check if X and Y scaling are the same - if not (this should never happen) throw an error
            if (m.M11 / m.M22 != 1)
            {
                throw new ApplicationException("Display scaling is not square?!");
            }

            return scaling;
        }

        private Tuple<int, int> GetCanvasSizePixels()
        {
            //get canvas size & work out the rectangular cell size from the current window size
            int canvasSizeX = (int)Math.Round(MainCanvas.ActualWidth * _controller.DPI);
            int canvasSizeY = (int)Math.Round(MainCanvas.ActualHeight * _controller.DPI);
            return new Tuple<int, int>(canvasSizeX, canvasSizeY);
        }

        #endregion CustomFunctions       
    }
}