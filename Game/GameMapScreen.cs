///
/// WF.Player.Android - A Wherigo Player for Android, which use the Wherigo Foundation Core.
/// Copyright (C) 2012-2014  Dirk Weltz <web@weltz-online.de>
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
using System.ComponentModel;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using WF.Player.Core;
using WF.Player.Core.Engines;
using Android.Gms.Common;
using Android.Graphics;

namespace WF.Player.Game
{
	#region GameMapScreen

	public class GameMapScreen : SupportMapFragment
	{
		GameController ctrl;
		UIObject activeObject;
		float zoom = 16f;
		bool headingOrientation = false;
		Thing thing;
		View mapView;
		GoogleMap map;
		RelativeLayout layoutButtons;
		ImageButton btnMapCenter;
		ImageButton btnMapOrientation;
		ImageButton btnMapType;
		Dictionary<int, GroundOverlay> overlays = new Dictionary<int, GroundOverlay> ();
		Dictionary<int, Marker> markers = new Dictionary<int, Marker> ();
		Dictionary<int, Polygon> zones = new Dictionary<int, Polygon> ();
		Polyline distanceLine;
		string[] properties = {"Name", "Icon", "Active", "Visible", "ObjectLocation"};


		#region Constructor

		public GameMapScreen(GameController ctrl, UIObject obj)
		{
			this.ctrl = ctrl;
			this.activeObject = obj;
		}

		#endregion

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			mapView = base.OnCreateView (inflater, container, savedInstanceState);

			// Save ScreenController for later use
			ctrl = ((GameController)this.Activity);

			RelativeLayout view = new RelativeLayout(ctrl);
			view.AddView(mapView, new RelativeLayout.LayoutParams(-1, -1));

//			var view = inflater.Inflate(Resource.Layout.ScreenMap, container, false);
//
//			mapView = new MapView(ctrl);
//			mapView.OnCreate(savedInstanceState);
//
			// Set all relevant data for map
			map = this.Map;
			map.MapType = GoogleMap.MapTypeNormal;
			map.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(Main.GPS.Location.Latitude, Main.GPS.Location.Longitude), (float)Main.Prefs.GetDouble("MapZoom", 16)));
			map.MyLocationEnabled = true;
			map.BuildingsEnabled = true;
			map.UiSettings.ZoomControlsEnabled = false;
			map.UiSettings.MyLocationButtonEnabled = false;
			map.UiSettings.CompassEnabled = false;
			map.UiSettings.TiltGesturesEnabled = false;
			map.UiSettings.RotateGesturesEnabled = false;

			// Create the zones the first time
			CreateZones();

			// Create layout for map buttons
			layoutButtons = new RelativeLayout(ctrl);

			// Button for center menu
			var lp = new RelativeLayout.LayoutParams(62, 62);
			lp.SetMargins(16, 16, 16, 16);
			lp.AddRule(LayoutRules.AlignParentLeft);
			lp.AddRule(LayoutRules.AlignParentTop);

			btnMapCenter = new ImageButton(ctrl);
			btnMapCenter.Id = 1;
			btnMapCenter.SetImageResource(Resource.Drawable.ic_button_center);
			btnMapCenter.SetBackgroundResource(Resource.Drawable.MapButton);
			btnMapCenter.Click += OnMapCenterButtonClick;

			layoutButtons.AddView(btnMapCenter, lp);

			// Button for the orientation: north up or always in direction
			lp = new RelativeLayout.LayoutParams(62, 62);
			lp.SetMargins(16, 8, 16, 16);
			lp.AddRule(LayoutRules.Below, btnMapCenter.Id);
			lp.AddRule(LayoutRules.AlignParentLeft);

			btnMapOrientation = new ImageButton(ctrl);
			btnMapOrientation.SetImageResource(Resource.Drawable.ic_button_orientation);
			btnMapOrientation.SetBackgroundResource(Resource.Drawable.MapButton);
			btnMapOrientation.Click += OnMapOrientationButtonClick;

			layoutButtons.AddView(btnMapOrientation, lp);

			// Button for selecting the map type
			lp = new RelativeLayout.LayoutParams(62, 62);
			lp.SetMargins(16, 16, 16, 8);
			lp.AddRule(LayoutRules.AlignParentTop);
			lp.AddRule(LayoutRules.AlignParentRight);

			btnMapType = new ImageButton(ctrl);
			btnMapType.SetImageResource(Resource.Drawable.ic_button_layer);
			btnMapType.SetBackgroundResource(Resource.Drawable.MapButton);
			btnMapType.Click += OnMapTypeButtonClick;

			layoutButtons.AddView(btnMapType, lp);

			view.AddView(layoutButtons);

			return view;
		}

		/// <summary>
		/// Raised, when this fragment gets focus.
		/// </summary>
		public override void OnResume()
		{
			base.OnResume();

			// Update all zones
			CreateZones();

			// Update distance line
			UpdateDistanceLine();

			ctrl.Engine.PropertyChanged += OnPropertyChanged;

			if (activeObject != null) {
				activeObject.PropertyChanged += OnDetailPropertyChanged;
			}

			map.CameraChange += OnCameraChange;
			//			map.MyLocationButtonClick += OnMyLocationButtonClick;
			// Use the common location listener, so that we have everywhere the same location
			map.SetLocationSource(Main.GPS);
			Main.GPS.AddLocationListener(OnLocationChanged);
			if (!Main.Prefs.GetBool("MapOrientationNorth",true))
				Main.GPS.AddOrientationListener(OnOrientationChanged);
		}

		/// <summary>
		/// Raised, when this fragment stops.
		/// </summary>
		public override void OnStop()
		{
			base.OnStop();

			//			mapView.OnPause();

			ctrl.Engine.PropertyChanged -= OnPropertyChanged;

			if (activeObject != null) {
				activeObject.PropertyChanged -= OnDetailPropertyChanged;
			}

			map.CameraChange -= OnCameraChange;
			//map.MyLocationButtonClick -= OnMyLocationButtonClick;
			Main.GPS.RemoveLocationListener(OnLocationChanged);
			if (!Main.Prefs.GetBool("MapOrientationNorth", true))
				Main.GPS.RemoveOrientationListener(OnOrientationChanged);
		}

		void OnPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals("ActiveVisibleZones")) {
				CreateZones ();
			}
		}

		void OnDetailPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			UpdateDistanceLine ();
		}

		void OnLocationChanged (object sender, WF.Player.Location.LocationChangedEventArgs e)
		{
			UpdateDistanceLine();
		}

		void OnOrientationChanged (object sender, WF.Player.Location.OrientationChangedEventArgs e)
		{
			RotateCamera((float)Main.GPS.Bearing);
		}

		void OnCameraChange (object sender, GoogleMap.CameraChangeEventArgs e)
		{
			Main.Prefs.SetDouble("MapZoom", e.P0.Zoom);
		}

		void OnMapCenterButtonClick (object sender, EventArgs args)
		{
			string[] items = new string[] {GetString(Resource.String.menu_screen_map_location_mylocation), GetString(Resource.String.menu_screen_map_location_game)};

			// Show selection dialog for location
			AlertDialog.Builder builder = new AlertDialog.Builder(ctrl);
			builder.SetTitle(Resource.String.menu_screen_map_location_header);
			builder.SetItems(items.ToArray(), delegate(object s, DialogClickEventArgs e) {
				if (e.Which == 0)
					FocusOnLocation();
				if (e.Which == 1)
					FocusOnGame();
			});
			AlertDialog alert = builder.Create();
			alert.Show();
		}

		void OnMapOrientationButtonClick (object sender, EventArgs args)
		{
			// Toogle button
			Main.Prefs.SetBool("MapOrientationNorth", !Main.Prefs.GetBool("MapOrientationNorth", true));

			// Set icon
			if (Main.Prefs.GetBool("MapOrientationNorth", true)) {
				btnMapOrientation.SetImageResource(Resource.Drawable.ic_button_orientation);
				RotateCamera(0.0f);
				Main.GPS.RemoveOrientationListener(OnOrientationChanged);
			} else {
				btnMapOrientation.SetImageResource(Resource.Drawable.ic_button_layer);
				Main.GPS.AddOrientationListener(OnOrientationChanged);
			}
		}

		void OnMapTypeButtonClick (object sender, EventArgs args)
		{
			string[] items = new string[] {GetString(Resource.String.menu_screen_map_type_google_maps), 
				GetString(Resource.String.menu_screen_map_type_google_satelitte),
				GetString(Resource.String.menu_screen_map_type_google_terrain), 
				GetString(Resource.String.menu_screen_map_type_google_hybrid), 
				GetString(Resource.String.menu_screen_map_type_osm), 
				GetString(Resource.String.menu_screen_map_type_ocm),
				GetString(Resource.String.menu_screen_map_type_none)
			};

			// Show selection dialog for location
			AlertDialog.Builder builder = new AlertDialog.Builder(ctrl);
			builder.SetTitle(Resource.String.menu_screen_map_type_header);
			builder.SetItems(items.ToArray(), delegate(object s, DialogClickEventArgs e) {
				if (e.Which == 0) {
					map.MapType = GoogleMap.MapTypeNormal;
				}
				if (e.Which == 1) {
					map.MapType = GoogleMap.MapTypeSatellite;
				}
				if (e.Which == 2) {
					map.MapType = GoogleMap.MapTypeTerrain;
				}
				if (e.Which == 3) {
					map.MapType = GoogleMap.MapTypeHybrid;
				}
				if (e.Which == 4) {
				}
				if (e.Which == 5) {
				}
				if (e.Which == 6) {
					map.MapType = GoogleMap.MapTypeNone;
				}
			});
			AlertDialog alert = builder.Create();
			alert.Show();
		}

		#endregion

		#region Map handling

		/// <summary>
		/// Creates all visible zones.
		/// </summary>
		void CreateZones ()
		{
			// Remove all active zones
			while (zones.Count > 0) {
				zones.ElementAt (0).Value.Remove ();
				zones.Remove (zones.ElementAt (0).Key);
			}
			// Now create all zones new
			foreach (Zone z in ctrl.Engine.ActiveVisibleZones) {
				CreateZone (z);
			}
		}

		/// <summary>
		/// Creates a visible zone.
		/// </summary>
		/// <param name="z">The z coordinate.</param>
		void CreateZone (Zone z)
		{
			// Create new polygon for zone
			PolygonOptions po = new PolygonOptions ();
			foreach (var zp in z.Points) {
				po.Points.Add (new LatLng (zp.Latitude, zp.Longitude));
			}
			po.InvokeStrokeColor (Color.Argb (160, 255, 0, 0));
			po.InvokeStrokeWidth (2);
			po.InvokeFillColor (Color.Argb (80, 255, 0, 0));
			// Add polygon to list of active zones
			zones.Add (z.ObjIndex, map.AddPolygon (po));
		}

		void RotateCamera(float bearing)
		{
			// Get old position of map
			CameraPosition oldPos = map.CameraPosition;
			// Calculate new orientation
			CameraPosition pos = new CameraPosition.Builder(oldPos).Bearing(bearing).Build();
			// Set new orientation
			map.MoveCamera(CameraUpdateFactory.NewCameraPosition(pos));
		}

		/// <summary>
		/// Draw line from active location to active object, if it is a zone.
		/// </summary>
		void UpdateDistanceLine ()
		{
			if (activeObject != null && activeObject is Zone) {
				// Delete line
				if (distanceLine != null) {
					distanceLine.Remove ();
					distanceLine = null;
				}
				// Draw line
				PolylineOptions po = new PolylineOptions();
				po.Points.Add (new LatLng (Main.GPS.Location.Latitude, Main.GPS.Location.Longitude));
				po.Points.Add (new LatLng (((Zone)activeObject).ObjectLocation.Latitude, ((Zone)activeObject).ObjectLocation.Longitude)); //.ObjectLocation.Latitude, ((Zone)activeObject).ObjectLocation.Longitude));
				po.InvokeColor(Color.Cyan);
				po.InvokeWidth(2);
				distanceLine = map.AddPolyline(po);
			}
		}

		/// <summary>
		/// Focuses the on whole game.
		/// </summary>
		void FocusOnGame()
		{
			double latNorth, latSouth;
			double longWest, longEast;

			// Get correct latitude
			if (ctrl.Engine.Bounds.Right > ctrl.Engine.Bounds.Left) {
				latNorth = ctrl.Engine.Bounds.Right;
				latSouth = ctrl.Engine.Bounds.Left;
			} else {
				latNorth = ctrl.Engine.Bounds.Left;
				latSouth = ctrl.Engine.Bounds.Right;
			}

			// Get correct latitude
			if (ctrl.Engine.Bounds.Top > ctrl.Engine.Bounds.Bottom) {
				longWest = ctrl.Engine.Bounds.Bottom;
				longEast = ctrl.Engine.Bounds.Top;
			} else {
				longWest = ctrl.Engine.Bounds.Top;
				longEast = ctrl.Engine.Bounds.Bottom;
			}

			CameraUpdate cu = CameraUpdateFactory.NewLatLngBounds(new LatLngBounds(new LatLng(latSouth, longWest), new LatLng(latNorth, longEast)), 10);

			map.MoveCamera(cu);
		}

		/// <summary>
		/// Focuses the on active location.
		/// </summary>
		void FocusOnLocation()
		{
			CameraUpdate cu = CameraUpdateFactory.NewLatLng(new LatLng(Main.GPS.Location.Latitude, Main.GPS.Location.Longitude));

			map.MoveCamera(cu);
		}

		#endregion

	}

}