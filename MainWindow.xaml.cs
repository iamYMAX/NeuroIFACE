using System.Windows;
using System.Windows.Input;

namespace NeuroIFACE
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }
        private SettingsWindow settingsWindow;
        private SettingsWindow _settingsWindow;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            this.LocationChanged += MainWindow_LocationChanged;
            

        }

        private void MainBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            MinimizeButton.Visibility = Visibility.Visible;
            CloseButton.Visibility = Visibility.Visible;
        }

        private void MainBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            // Add a small delay or check if the mouse is over the buttons themselves
            // For simplicity, this example hides them immediately.
            // A more robust solution might involve a timer or checking if e.OriginalSource is one of the buttons.
            if (!MinimizeButton.IsMouseOver && !CloseButton.IsMouseOver)
            {
                MinimizeButton.Visibility = Visibility.Collapsed;
                CloseButton.Visibility = Visibility.Collapsed;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрытие окна
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Проверяем, что это была левая кнопка мыши
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Перемещаем окно
                DragMove();
            }
        }
        
        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            // Обновляем положение второго окна при перемещении основного окна
            SetSettingsWindowPosition();
        }
        private void SetSettingsWindowPosition()
        {
            // Привязываем положение второго окна к положению основного
            if (_settingsWindow != null)
            {
                _settingsWindow.Left = this.Left - _settingsWindow.Width - 5; // Справа от основного окна с отступом
                _settingsWindow.Top = this.Top; // По вертикали совпадает с основным окном
                
            }
        }
        private void Window_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Открываем второе окно
            _settingsWindow = new SettingsWindow(ViewModel);
            _settingsWindow.Owner = this; // Устанавливаем основное окно как владельца
            _settingsWindow.Show();

            // Начальная привязка к положению основного окна
            SetSettingsWindowPosition();

        }
    }
}
