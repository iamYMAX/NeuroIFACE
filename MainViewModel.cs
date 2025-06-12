using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace NeuroIFACE
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly RandomPhraseModel _phraseModel;
        private readonly DispatcherTimer _timer;
        private string _currentPhrase;
        private double _sliderValue;
        private double _fontSize = 14;
        public string CurrentPhrase
        {
            get { return _currentPhrase; }
            set
            {
                _currentPhrase = value;
                OnPropertyChanged();
            }
        }
        public double FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                OnPropertyChanged();
            }
        }
        public double SliderValue
        {
            get { return _sliderValue; }
            set
            {
                _sliderValue = value;
                OnPropertyChanged();
                UpdateTimerInterval();
            }
        }

        public MainViewModel()
        {
            _phraseModel = new RandomPhraseModel();
            _timer = new DispatcherTimer();
            _timer.Tick += (s, e) => UpdatePhrase();
            SliderValue = 1; // Инициализируем начальное значение для слайдера
        }

        private void UpdateTimerInterval()
        {
            // Интервал зависит от значения слайдера (переводим из диапазона 1-25 в миллисекунды)
            _timer.Interval = TimeSpan.FromMilliseconds(1000 / SliderValue);
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }

        private void UpdatePhrase()
        {
            CurrentPhrase = _phraseModel.GetRandomPhrase();
        }

        public void SetSliderValue(double value)
        {
            SliderValue = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
