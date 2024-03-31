using System.ComponentModel;
using Windows.Devices.Geolocation;
using Windows.UI;

namespace GeoCommon
{
  public  class Route : INotifyPropertyChanged
    {
        private string name;
        private double strokeThickness;
        private bool strokeDashed;
        private Color strokeColor;
        private BasicGeoposition[] path;

        public string Name
        {
            get => name;
            set
            {
                if (value == name) return;

                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public double StrokeThickness
        {
            get => strokeThickness;
            set
            {
                if (value == strokeThickness) return;

                strokeThickness = value;
                OnPropertyChanged(nameof(StrokeThickness));
            }
        }

        public bool StrokeDashed
        {
            get => strokeDashed;
            set
            {
                if (value == strokeDashed) return;

                strokeDashed = value;
                OnPropertyChanged(nameof(StrokeDashed));
            }
        }

        public Color StrokeColor
        {
            get => strokeColor;
            set
            {
                if (value == strokeColor) return;

                strokeColor = value;
                OnPropertyChanged(nameof(StrokeColor));
            }
        }

        public BasicGeoposition[] Path
        {
            get => path;
            set
            {
                if (value == path) return;

                path = value;
                OnPropertyChanged(nameof(Path));
            }
        }

        public Route()
        {
            Name = "Route";
            StrokeThickness = 3;
            StrokeColor = Colors.Blue;
            Path = new BasicGeoposition[0];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
