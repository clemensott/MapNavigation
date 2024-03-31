using GeoCommon;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Devices.Geolocation;

namespace MapNavigation
{
    public class ViewModel : INotifyPropertyChanged
    {
        private static TaskCompletionSource<ViewModel> instance;

        public static Task<ViewModel> GetInstanceAsync()
        {
            if (instance == null) instance = new TaskCompletionSource<ViewModel>();

            return instance.Task;
        }

        public static void SetInstance(ViewModel viewModel)
        {
            if (instance == null) instance = new TaskCompletionSource<ViewModel>();

            instance.SetResult(viewModel);
        }

        private BasicGeoposition currentPosition;
        private BasicGeoposition? latestPosition;
        private double zoomLevel;
        private bool activateGeopositioning, focusGeoposition, enableRotating, autoRotate, addingMode;
        private Route currentRoute;
        private ObservableCollection<Route> routes;
        private DirectionLineSourceType directionLineSrc;
        private DirectionLineDestType directionLineDest;
        private BasicGeoposition directionLineDestPoint;

        public BasicGeoposition CurrentPosition
        {
            get => currentPosition;
            set
            {
                if (Equals(value, currentPosition)) return;

                currentPosition = value;
                OnPropertyChanged(nameof(CurrentPosition));
            }
        }

        public double ZoomLevel
        {
            get => zoomLevel;
            set
            {
                if (value == zoomLevel) return;

                zoomLevel = value;
                OnPropertyChanged(nameof(ZoomLevel));
            }
        }

        [XmlIgnore]
        public BasicGeoposition? LatestPosition
        {
            get => latestPosition;
            set
            {
                if (Equals(value, latestPosition)) return;

                latestPosition = value;
                OnPropertyChanged(nameof(LatestPosition));
            }
        }

        public bool ActivateGeopositioning
        {
            get => activateGeopositioning;
            set
            {
                if (value == activateGeopositioning) return;

                activateGeopositioning = value;
                OnPropertyChanged(nameof(ActivateGeopositioning));
            }
        }

        [XmlIgnore]
        public bool FocusGeoposition
        {
            get => focusGeoposition;
            set
            {
                if (value == focusGeoposition) return;

                focusGeoposition = value;
                OnPropertyChanged(nameof(FocusGeoposition));
            }
        }

        public bool EnableRotating
        {
            get => enableRotating;
            set
            {
                if (value == enableRotating) return;

                enableRotating = value;
                OnPropertyChanged(nameof(EnableRotating));
            }
        }

        public bool AutoRotate
        {
            get => autoRotate;
            set
            {
                if (value == autoRotate) return;

                autoRotate = value;
                OnPropertyChanged(nameof(AutoRotate));
            }
        }

        public bool AddingMode
        {
            get => addingMode;
            set
            {
                if (value == addingMode) return;

                addingMode = value;
                OnPropertyChanged(nameof(AddingMode));
            }
        }

        [XmlIgnore]
        public Route CurrentRoute
        {
            get => currentRoute;
            set
            {
                if (value == currentRoute) return;

                currentRoute = value;
                OnPropertyChanged(nameof(CurrentRoute));
            }
        }

        public ObservableCollection<Route> Routes
        {
            get => routes;
            set
            {
                if (value == routes) return;

                routes = value;
                OnPropertyChanged(nameof(Routes));
            }
        }

        public int CurrentRouteIndex
        {
            get => Routes.IndexOf(CurrentRoute);
            set => CurrentRoute = Routes.ElementAtOrDefault(value);
        }

        public DirectionLineSourceType DirectionLineSrc
        {
            get => directionLineSrc;
            set
            {
                if (value == directionLineSrc) return;

                directionLineSrc = value;
                OnPropertyChanged(nameof(DirectionLineSrc));
            }
        }

        public DirectionLineDestType DirectionLineDest
        {
            get => directionLineDest;
            set
            {
                if (value == directionLineDest) return;

                directionLineDest = value;
                OnPropertyChanged(nameof(DirectionLineDest));
            }
        }

        public BasicGeoposition DirectionLineDestPoint
        {
            get => directionLineDestPoint;
            set
            {
                if (Equals(value, directionLineDestPoint)) return;

                directionLineDestPoint = value;
                OnPropertyChanged(nameof(DirectionLineDestPoint));
            }
        }

        [XmlIgnore]
        public string Token { get; }

        public ViewModel()
        {
            Token = "AiNqrP4OPxB5k2yeIL3H1vHVKyo-XyLUnL3UDXljWOAJjSUkBwOlOOG0zjXOTsVQ";
            Routes = new ObservableCollection<Route>();
        }

        internal void SetCurrentPosition(BasicGeoposition position)
        {
            CurrentPosition = position;
            OnPropertyChanged(nameof(CurrentPosition));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
