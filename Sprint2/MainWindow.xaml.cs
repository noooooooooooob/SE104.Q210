using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Sprint2
{
    public partial class MainWindow : Window
    {
        private List<Game> dsNhap =
            new List<Game>();

        public MainWindow()
        {
            InitializeComponent();

            cbLoaiGame.ItemsSource =
                DataManager.LoaiGames;

            dpNgayNhap.SelectedDate =
                DateTime.Now;
        }
