﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Mazeinator
{
    //https://www.wpf-tutorial.com/dialogs/creating-a-custom-input-dialog/
    //https://www.hanselman.com/blog/learning-wpf-with-babysmash-configuration-with-databinding
    
    /// <summary>
    /// Interaction logic for StyleSettings.xaml
    /// </summary>
    public partial class StyleSettings : Window
    {
        public Style SettingsStyle = null;
        private Maze _maze = new Maze(2, 2);

        public StyleSettings(Style style)
        {
            InitializeComponent();

            SettingsStyle = style;
            _maze.GenerateMaze(_maze.nodes[0, 0]);
            RedrawPreview();

            this.DataContext = SettingsStyle;
        }

        private void btnDialogOK_Click(object sender, RoutedEventArgs e)
        {                        
            this.DialogResult = true;            
        }

        private void DefaultValues(object sender, RoutedEventArgs e)
        {
            SettingsStyle = new Style();
            RedrawPreview();
        }

        private void RedrawPreview()
        {

            Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
            double dx = m.M11, dy = m.M22;    //get the current "WPF DPI units"

            //get canvas size & work out the rectangular cell size from the current window size
            int canvasSizeX = (int)Math.Round((MainCanvas.Width * dx));
            int canvasSizeY = (int)Math.Round((MainCanvas.Height * dy));

            MazePreview.Source = Utilities.BitmapToImageSource(_maze.RenderMaze(canvasSizeX, canvasSizeY, SettingsStyle));
        }

        private void SelectedColorChangedEvent(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            RedrawPreview();
        }
    }
}