// Cross-platform stand-in for System.Device.Location.GeoCoordinate (Windows-only,
// not available on .NET 8). Implements only what LocationUtils/Navigation use.
using System;

namespace System.Device.Location
{
    public class GeoCoordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }

        public GeoCoordinate()
        {
        }

        public GeoCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public GeoCoordinate(double latitude, double longitude, double altitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

        /// <summary>Great-circle distance in meters (Haversine).</summary>
        public double GetDistanceTo(GeoCoordinate other)
        {
            const double earthRadiusMeters = 6371000;
            var dLat = ToRad(other.Latitude - Latitude);
            var dLon = ToRad(other.Longitude - Longitude);
            var lat1 = ToRad(Latitude);
            var lat2 = ToRad(other.Latitude);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusMeters * c;
        }

        private static double ToRad(double degrees) => degrees * (Math.PI / 180);
    }
}
