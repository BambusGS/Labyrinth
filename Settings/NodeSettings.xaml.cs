﻿using System.Windows;
using System.Windows.Controls;  //Button

namespace Mazeinator
{
    /// <summary>
    /// Interaction logic for NodeSettings.xaml
    /// </summary>
    public partial class NodeSettings : Window
    {
        public int selector = 0;

        public NodeSettings(int nodeSize)
        {
            InitializeComponent();
            SetVisualIndicator(Utilities.Clamp(nodeSize, 2, 12));
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag)
            {
                case "NorthNodeSelect":
                    selector = Node.North;
                    break;

                case "EastNodeSelect":
                    selector = Node.East;
                    break;

                case "SouthNodeSelect":
                    selector = Node.South;
                    break;

                case "WestNodeSelect":
                    selector = Node.West;
                    break;

                case "StartNodeSelect":
                    selector = 10;
                    break;

                case "EndNodeSelect":
                    selector = 11;
                    break;

                case "AUX":
                    selector = 20;
                    break;
            }

            this.DialogResult = true;
        }

        private void SetVisualIndicator(int nodeSize)
        {
            //works in WPF units
            const int gridWidth = 40;

            lineTL.X2 = gridWidth / 2 - nodeSize;
            lineTL.Y2 = gridWidth / 2 - nodeSize;

            lineTR.X2 = gridWidth / 2 + nodeSize;
            lineTR.Y2 = gridWidth / 2 - nodeSize;

            lineBL.X2 = gridWidth / 2 - nodeSize;
            lineBL.Y2 = gridWidth / 2 + nodeSize;

            lineBR.X2 = gridWidth / 2 + nodeSize;
            lineBR.Y2 = gridWidth / 2 + nodeSize;
        }

        public void TargetSwap(int swapDirection = -1)
        {
            switch (swapDirection)
            {
                case Node.North:
                    this.Top += this.Height / 3;

                    NorthBtn.SetValue(Grid.RowProperty, 1);
                    TargetGrid.SetValue(Grid.RowProperty, 0);
                    break;

                case Node.East:
                    this.Left -= this.Width / 3;

                    EastBtn.SetValue(Grid.ColumnProperty, 1);
                    TargetGrid.SetValue(Grid.ColumnProperty, 2);
                    break;

                case Node.South:
                    this.Top -= this.Height / 3;

                    SouthBtn.SetValue(Grid.RowProperty, 1);
                    TargetGrid.SetValue(Grid.RowProperty, 2);
                    break;

                case Node.West:
                    this.Left += this.Width / 3;

                    WestBtn.SetValue(Grid.ColumnProperty, 1);
                    TargetGrid.SetValue(Grid.ColumnProperty, 0);
                    break;

                default:    //reset it
                    NorthBtn.SetValue(Grid.RowProperty, 0);
                    EastBtn.SetValue(Grid.ColumnProperty, 2);
                    SouthBtn.SetValue(Grid.RowProperty, 2);
                    WestBtn.SetValue(Grid.ColumnProperty, 0);

                    TargetGrid.SetValue(Grid.ColumnProperty, 1);
                    TargetGrid.SetValue(Grid.RowProperty, 1);
                    break;
            }
        }
    }
}