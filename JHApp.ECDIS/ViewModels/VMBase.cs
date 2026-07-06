using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WPFLocalizeExtension.Engine;

namespace JHApp.ECDIS.ViewModels
{
    public abstract class VMBase : ObservableObject
    {
        private static readonly PropertyChangedEventArgs VisibleProperty = new(nameof(Visible));

        private int _flagAfterVisible;
        private int _flagVisible;

        public event Action OnFirstVisible;

        /// <summary> 
        /// UI 요소가 처음으로 화면에 표시된 이후의 상태를 나타낸다 <br/>
        /// 이 변수를 사용하여 UI 초기 표시 시에만 특정 로직을 실행하거나 <br/>
        /// 첫 노출 이후에 다른 동작을 수행할 수 있도록 조건을 설정할 수 있다
        /// </summary>
        public bool AfterFirstVisible => _flagAfterVisible != 0;
        public bool Visible
        {
            get => _flagVisible != 0;
            set
            {
                var nVisible = value ? 1 : 0;
                if (Interlocked.Exchange(ref _flagVisible, nVisible) != nVisible)
                {
                    OnVisibleChanged(value);
                    OnPropertyChanged(VisibleProperty);

                    if (_flagAfterVisible == 0 && Interlocked.Exchange(ref _flagAfterVisible, 1) == 0)
                        OnFirstVisible?.Invoke();
                }
            }
        }

        private ICommand _stringCommandBinder;
        public ICommand StringCommandBinder => _stringCommandBinder ??= new RelayCommand<string>(OnStringCommand);

        protected virtual void OnLoaded() { }
        protected virtual void OnVisibleChanged(bool visible) { }
        protected virtual void OnStringCommand(string command) { }

        public void SetLoaded(object s, EventArgs e) => OnLoaded();
        public void VisibleToggle()
        {
            var dispatcher = Application.Current?.Dispatcher;
            dispatcher?.BeginInvoke(() => Visible = !Visible);
        }

        public VMBase()
        {
            // 언어가 변경될 때 발생하는 이벤트 구독
            LocalizeDictionary.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LocalizeDictionary.Instance.Culture))
                    OnPropertyChanged(string.Empty);
            };
        }

        public bool IsInit = false;
    }
}