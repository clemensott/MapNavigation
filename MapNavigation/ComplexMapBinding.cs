using GeoCommon;
using StdOttStandard;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Xaml.Controls.Maps;

namespace MapNavigation
{
    class ComplexMapBinding : IDisposable
    {
        private MapControl map;
        private ViewModel viewModel;

        private MapIcon currentPositionIcon;

        private BasicGeoposition directionLineSrcPoint, directionLineDestPoint;
        private MapPolyline directionLine;

        private ObservableCollection<Route> routes;
        private Dictionary<Route, MapPolyline> mapRoutes;

        public ComplexMapBinding(MapControl map, ViewModel viewModel)
        {
            this.map = map;
            this.viewModel = viewModel;

            currentPositionIcon = new MapIcon();
            directionLine = new MapPolyline() { StrokeColor = Colors.Green, StrokeThickness = 5 };
            mapRoutes = new Dictionary<Route, MapPolyline>();

            sub(viewModel);
        }

        private void sub(ViewModel viewModel)
        {
            if (viewModel == null) return;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            map.MapElements.Add(currentPositionIcon);

            UpdateCurrentPositionIcon();
            SetDirectionLineElement();

            sub(viewModel.Routes);
        }

        private void usub(ViewModel viewModel)
        {
            if (viewModel == null) return;

            viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            map.MapElements.Remove(currentPositionIcon);

            usub(viewModel.Routes);
        }

        private void sub(ObservableCollection<Route> routes)
        {
            if (routes == null) return;

            routes.CollectionChanged += Routes_CollectionChanged;

            foreach (Route route in routes)
            {
                sub(route);
            }
        }

        private void usub(ObservableCollection<Route> routes)
        {
            if (routes == null) return;

            routes.CollectionChanged -= Routes_CollectionChanged;

            foreach (Route route in routes)
            {
                usub(route);
            }
        }

        private void sub(Route route)
        {
            if (route == null) return;

            route.PropertyChanged += Route_PropertyChanged;

            MapPolyline line = route.ToMapPolyline();

            mapRoutes.Add(route, line);
            map.MapElements.Add(line);
        }

        private void usub(Route route)
        {
            if (route == null) return;

            route.PropertyChanged -= Route_PropertyChanged;

            MapPolyline line;
            if (!mapRoutes.TryGetValue(route, out line)) return;

            mapRoutes.Remove(route);
            map.MapElements.Remove(line);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(viewModel.Routes):
                    usub(routes);
                    routes = viewModel.Routes;
                    sub(routes);
                    break;

                case nameof(viewModel.CurrentPosition):
                    SetDirectionLineElement();
                    break;

                case nameof(viewModel.LatestPosition):
                    UpdateCurrentPositionIcon();
                    SetDirectionLineElement();
                    break;

                case nameof(viewModel.DirectionLineSrc):
                case nameof(viewModel.DirectionLineDest):
                case nameof(viewModel.DirectionLineDestPoint):
                    SetDirectionLineElement();
                    break;
            }
        }

        private void Routes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (Route route in e.OldItems.ToNotNull())
            {
                usub(route);
            }

            foreach (Route route in e.NewItems.ToNotNull())
            {
                sub(route);
            }
        }

        private void Route_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Route route = (Route)sender;

            if (e.PropertyName == nameof(route.Path)) mapRoutes[route].Path = route.Path.ToGeopath();
        }

        private void UpdateCurrentPositionIcon()
        {
            if (viewModel.LatestPosition.HasValue)
            {
                currentPositionIcon.Location = new Geopoint(viewModel.LatestPosition.Value, AltitudeReferenceSystem.Unspecified);
                currentPositionIcon.Visible = true;
            }
            else
            {
                currentPositionIcon.Visible = false;
            }
        }

        private void SetDirectionLineElement()
        {
            Geopath path = directionLine.Path;

            if (GetDirectionLineGeoPath(ref path))
            {
                directionLine.Path = path;

                if (!map.MapElements.Contains(directionLine)) map.MapElements.Add(directionLine);
            }
            else map.MapElements.Remove(directionLine);
        }

        private bool GetDirectionLineGeoPath(ref Geopath path)
        {
            BasicGeoposition src, dest;

            Route route = viewModel.CurrentRoute;

            if (route?.Path == null || route.Path.Length == 0) return false;

            switch (viewModel.DirectionLineSrc)
            {
                case DirectionLineSourceType.CurrentPosition:
                    src = viewModel.CurrentPosition;
                    break;

                case DirectionLineSourceType.LatestPosition:
                    if (viewModel.LatestPosition.HasValue) src = viewModel.LatestPosition.Value;
                    else return false;
                    break;

                default:
                    return false;
            }

            switch (viewModel.DirectionLineDest)
            {
                case DirectionLineDestType.Nearest:
                    dest = route.Path.MinElement(p => GeoUtils.DistanceBetweenPlaces(p, src));
                    break;

                case DirectionLineDestType.Custom:
                    dest = viewModel.DirectionLineDestPoint;
                    break;

                default:
                    return false;
            }

            if (!Equals(src, directionLineSrcPoint) || !Equals(dest, directionLineSrcPoint))
            {
                BasicGeoposition[] points = new BasicGeoposition[] { src, dest };
                path = new Geopath(points, AltitudeReferenceSystem.Unspecified);

                directionLineSrcPoint = src;
                directionLineDestPoint = dest;
            }

            return true;
        }

        public void Dispose()
        {
            usub(viewModel);
        }
    }
}
