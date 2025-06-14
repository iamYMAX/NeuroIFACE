using System.Windows;
using System.Windows.Input;

namespace NeuroIFACE
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }
        // private SettingsWindow settingsWindow; // This seems like a duplicate or unused variable
        private SettingsWindow _settingsWindow;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            this.LocationChanged += MainWindow_LocationChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainWindow_Loaded: Positioning window.");
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            // Ensure the window has rendered and ActualWidth/ActualHeight are available.
            // Loaded event is generally a good place for this.
            double windowWidth = this.ActualWidth;
            // double windowHeight = this.ActualHeight; // Not used for current logic but good for reference

            double topOffsetPercentage = 0.15;
            double rightOffsetPercentage = 0.15;

            this.Top = screenHeight * topOffsetPercentage;
            this.Left = screenWidth - windowWidth - (screenWidth * rightOffsetPercentage);

            // Basic bounds check to ensure the window is not positioned completely off-screen
            if (this.Left < 0)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded: Calculated Left ({this.Left}) was off-screen. Resetting to 0.");
                this.Left = 0;
            }
            if (this.Top < 0)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded: Calculated Top ({this.Top}) was off-screen. Resetting to 0.");
                this.Top = 0;
            }
            // Optional: ensure it's not too far to the right/bottom if window is very large
            if (this.Left + windowWidth > screenWidth)
            {
                 this.Left = screenWidth - windowWidth;
                 System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded: Calculated Left placed window too far right. Adjusting.");
            }
            if (this.Top + this.ActualHeight > screenHeight) // using ActualHeight here
            {
                 this.Top = screenHeight - this.ActualHeight;
                 System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded: Calculated Top placed window too far down. Adjusting.");
            }
            System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded: Final position Left={this.Left}, Top={this.Top}");
        }

        // This method seems to be for a button that doesn't exist on MainWindow itself
        // private void CloseButton_Click(object sender, RoutedEventArgs e)
        // {
        //     this.Close(); // Закрытие окна
        // }
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

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Prevent the double-click from causing undesired actions, like closing the window.
            // If the action is specifically from the left mouse button,
            // which might be initiating a DragMove or other interaction.
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                e.Handled = true;
            }
        }
    }
}
