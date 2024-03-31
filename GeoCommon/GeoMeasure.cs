using System;
using System.Xml.Serialization;
using Windows.Devices.Geolocation;

namespace GeoCommon
{
    public struct GeoMeasure
    {
        public double Accuracy { get; set; }

        public double? AltitudeAccuracy { get; set; }

        public double? Heading { get; set; }

        public double? Speed { get; set; }

        public BasicGeoposition Position { get; set; }

        public long TimestampTicks
        {
            get => Timestamp.Ticks;
            set => Timestamp = new DateTimeOffset(new DateTime(value), Timestamp.Offset);
        }

        public long TimestampOffsetTicks
        {
            get => Timestamp.Offset.Ticks;
            set => Timestamp = new DateTimeOffset(Timestamp.DateTime, new TimeSpan(value));
        }

        [XmlIgnore]
        public DateTimeOffset Timestamp { get; set; }
    }
}
