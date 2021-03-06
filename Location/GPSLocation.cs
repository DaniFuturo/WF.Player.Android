﻿///
/// WF.Player - A Wherigo Player, which use the Wherigo Foundation Core.
/// Copyright (C) 2012-2014  Dirk Weltz <mail@wfplayer.com>
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU Lesser General Public License as
/// published by the Free Software Foundation, either version 3 of the
/// License, or (at your option) any later version.
/// 
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU Lesser General Public License for more details.
/// 
/// You should have received a copy of the GNU Lesser General Public License
/// along with this program.  If not, see <http://www.gnu.org/licenses/>.
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Vernacular;

namespace WF.Player.Location
{
	public enum GPSFormat {
		Decimal,
		DecimalMinutes,
		DecimalMinutesSeconds
	}

	public partial class GPSLocation
	{
		#region Constructor

		public GPSLocation()
		{
			SetDefaults();
		}

		public GPSLocation(GPSLocation loc)
		{
			SetDefaults();

			// Copy position
			_lat = loc.Latitude;
			_lon = loc.Longitude;
			_time = loc.Time;
			_provider = loc.Provider;
			_isValid = loc.IsValid;

			// Copy Altitude only if exists
			if (loc.HasAltitude)
				_alt = loc.Altitude;
			else
				_alt = double.NaN;

			// Copy Accruracy only if exists
			if (loc.HasAccuracy) {
				_accuracy = loc.Accuracy;
				_hasAccuracy = true;
			} 

			// Copy Speed only if exists
			if (loc.HasSpeed) {
				_speed = loc.Speed;
				_hasSpeed = true;
			}

			// Copy Bearing only if exists
			if (loc.HasBearing) {
				_bearing = loc.Bearing;
				_hasBearing = true;
			}
		}

		#endregion

		#region Members

		string _provider;

		/// <summary>
		/// Provider of this location.
		/// </summary>
		/// <value>The provider.</value>
		public string Provider
		{
			get { return _provider; }
			set { _provider = value; }
		}

		bool _isValid;

		/// <summary>
		/// Gets or sets a value indicating whether this instance is valid.
		/// </summary>
		/// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
		public bool IsValid
		{
			get { return _isValid; }
			internal set { _isValid = value; }
		}


		double _lat;

		/// <summary>
		/// Gets or sets the latitude.
		/// </summary>
		/// <value>The latitude.</value>
		public double Latitude
		{
			get { return _lat; }
			set { _lat = value; }
		}

		double _lon;

		/// <summary>
		/// Gets or sets the longitude.
		/// </summary>
		/// <value>The longitude.</value>
		public double Longitude
		{
			get { return _lon; }
			set { _lon = value; }
		}

		double _alt;
		bool _hasAltitude;

		/// <summary>
		/// Gets or sets the altitude.
		/// </summary>
		/// <value>The altitude.</value>
		public double Altitude
		{
			get { return _alt; }
			set { 
				_alt = value;
				_hasAltitude = true;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance has altitude.
		/// </summary>
		/// <value><c>true</c> if this instance has altitude; otherwise, <c>false</c>.</value>
		public bool HasAltitude
		{
			get { return _hasAltitude; }
		}

		double _speed;
		bool _hasSpeed;

		/// <summary>
		/// Gets or sets the speed.
		/// </summary>
		/// <value>The speed.</value>
		public double Speed
		{
			get { return _speed; }
			set { 
				_speed = value;
				_hasSpeed = true;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance has speed.
		/// </summary>
		/// <value><c>true</c> if this instance has speed; otherwise, <c>false</c>.</value>
		public bool HasSpeed
		{
			get { return _hasSpeed; }
		}

		double _accuracy;
		bool _hasAccuracy;

		/// <summary>
		/// Gets or sets the accuracy.
		/// </summary>
		/// <value>The accuracy.</value>
		public double Accuracy
		{
			get { return _accuracy; }
			set { 
				_accuracy = value;
				_hasAccuracy = true;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance has accuracy.
		/// </summary>
		/// <value><c>true</c> if this instance has accuracy; otherwise, <c>false</c>.</value>
		public bool HasAccuracy
		{
			get { return _hasAccuracy; }
		}

		double _bearing;
		bool _hasBearing;

		/// <summary>
		/// Gets or sets the bearing.
		/// </summary>
		/// <value>The bearing.</value>
		public double Bearing
		{
			get { return _bearing; }
			set { 
				_bearing = value;
				_hasBearing = true;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance has bearing.
		/// </summary>
		/// <value><c>true</c> if this instance has bearing; otherwise, <c>false</c>.</value>
		public bool HasBearing
		{
			get { return _hasBearing; }
		}

		double _declination;

		/// <summary>
		/// Gets the declination for the actual location
		/// </summary>
		public double Declination
		{
			get { return _declination; }
		}

		DateTime _time;

		/// <summary>
		/// Gets or sets the time.
		/// </summary>
		/// <value>The time.</value>
		public DateTime Time
		{
			get { return _time; }
			set { _time = value; }
		}

		#endregion

		#region Methods

		public bool Equals(double lat, double lon, double alt, double accuracy)
		{
			return (lat == _lat && lon == _lon && alt == _alt && accuracy == _accuracy);
		}

		#endregion

		#region Display

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="WF.Player.Location.GPSLocation" in default format./>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="WF.Player.Location.GPSLocation"/>.</returns>
		public override string ToString ()
		{
			return ToString(GPSFormat.DecimalMinutes);
		}

		/// <summary>
		/// Convert this location in a readable format.
		/// </summary>
		/// <returns>Location in readable format.</returns>
		/// <param name="format">Format.</param>
		public string ToString(GPSFormat format)
		{
			return Converters.CoordinatToString(_lat, _lon, format);
		}

		public string ToAccuracyString()
		{
			if (_hasAccuracy)
				return String.Format("{0} m", (int)_accuracy);
			else
				return String.Format("{0} m", Strings.Infinite);
		}

		#endregion

		#region Private Functions

		void SetDefaults()
		{
			_provider = "";
			_time = DateTime.Now;
			_lat = double.NaN;
			_lon = double.NaN;
			_alt = double.NaN;
			_hasAltitude = false;
			_accuracy = double.NaN;
			_hasAccuracy = false;
			_speed = double.NaN;
			_hasSpeed = false;
			_bearing = double.NaN;
			_hasBearing = false;
			_isValid = false;
		}

		#endregion
	}
}

