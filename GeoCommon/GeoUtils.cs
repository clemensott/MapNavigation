using System;
using System.Collections.Generic;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls.Maps;
using StdOttStandard.Linq;

namespace GeoCommon
{
    public static class GeoUtils
    {
        public const string EndMutixName = "end_geo_recording", UpdateMutexName = "update_geo_recording";

        public static double DistanceBetweenPlaces(BasicGeoposition pos1, BasicGeoposition pos2)
        {
            double R = 6371; // km

            double sLat1 = Math.Sin(Radians(pos1.Latitude));
            double sLat2 = Math.Sin(Radians(pos2.Latitude));
            double cLat1 = Math.Cos(Radians(pos1.Latitude));
            double cLat2 = Math.Cos(Radians(pos2.Latitude));
            double cLon = Math.Cos(Radians(pos1.Longitude) - Radians(pos2.Longitude));

            double cosD = sLat1 * sLat2 + cLat1 * cLat2 * cLon;

            double d = Math.Acos(cosD);

            return R * d;
        }

        private static double Radians(double x)
        {
            return x * Math.PI / 180;
        }

        public static double DistanceBetweenPlaces(IEnumerable<BasicGeoposition> positions)
        {
            BasicGeoposition last;
            double dist = 0;

            foreach (BasicGeoposition pos in positions.ExtractOrDefault(out last))
            {
                dist += DistanceBetweenPlaces(last, pos);
                last = pos;
            }

            return dist;
        }

        public static MapPolyline ToMapPolyline(this Route route)
        {
            return new MapPolyline()
            {
                StrokeThickness = route.StrokeThickness,
                StrokeDashed = route.StrokeDashed,
                StrokeColor = route.StrokeColor,
                Path = route.Path.ToGeopath()
            };
        }

        public static Geopath ToGeopath(this BasicGeoposition[] positions)
        {
            if (positions == null || positions.Length < 2)
            {
                positions = new BasicGeoposition[]
                {
                    new BasicGeoposition()
                    {
                        Altitude=500,
                        Latitude= 15.5644,
                        Longitude= 15.5644
                    },
                    new BasicGeoposition()
                    {
                        Altitude=500,
                        Latitude= 16.5644,
                        Longitude= 15.5644
                    }
                };
            }

            return new Geopath(positions, AltitudeReferenceSystem.Unspecified);
        }

        public static GeoMeasure ToGeoMeasure(this Geocoordinate geo)
        {
            return new GeoMeasure()
            {
                Accuracy = geo.Accuracy,
                AltitudeAccuracy = geo.AltitudeAccuracy,
                Heading = geo.Heading,
                Speed = geo.Speed,
                Position = geo.Point.Position,
                Timestamp = geo.Timestamp
            };
        }
    }
}
