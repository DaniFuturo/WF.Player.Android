///
/// WF.Player.Android - A Wherigo Player for Android, which use the Wherigo Foundation Core.
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
using System.ComponentModel;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views.InputMethods;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using WF.Player.Core;
using WF.Player.Core.Engines;
using WF.Player.Types;

namespace WF.Player.Game
{
	#region GameDialogScreen

	public class GameDialogScreen : global::Android.Support.V4.App.Fragment
	{
		GameController ctrl;
		MessageBox messageBox;
		Input input;
		ImageView imageView;
		EditText editInput;
		Spinner spinnerInput;
		Button btnView1;
		Button btnView2;
		Button btnInput;
		LinearLayout layoutDialog;
		LinearLayout layoutInput;
		LinearLayout layoutButton;
		TextView textDescription;
		string inputResult;

		ScreenTypes Type = ScreenTypes.Dialog;

		#region Constructor

		public GameDialogScreen(MessageBox messageBox)
		{
			this.messageBox = messageBox;
			this.input = null;
		}

		public GameDialogScreen(Input input)
		{
			this.messageBox = null;
			this.input = input;
		}

		#endregion

		#region Android Event Handlers

		public void OnButtonClicked(object sender, EventArgs e)
		{
			ctrl.Feedback();

			// Remove dialog from screen
			ctrl.RemoveScreen (this);

			// Execute callback if there is one
			if (sender is Button) {
				messageBox.GiveResult (sender.Equals(btnView1) ? MessageBoxResult.FirstButton : MessageBoxResult.SecondButton);
			}
		}

		public void OnChoiceClicked(object sender, EventArgs e)
		{
			ctrl.Feedback();

			// Remove dialog from screen
			ctrl.RemoveScreen (this);

			if (input != null) {
				string result = ((Button)sender).Text;
				input.GiveResult (result);
			}
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			base.OnCreateView (inflater, container, savedInstanceState);

			// Save ScreenController for later use
			ctrl = ((GameController)this.Activity);

			if (container == null)
				return null;

			var view = inflater.Inflate(Resource.Layout.GameDialogScreen, container, false);

			imageView = view.FindViewById<ImageView> (Resource.Id.imageView);
			textDescription = view.FindViewById<TextView> (Resource.Id.textDescription);
			spinnerInput = view.FindViewById<Spinner> (Resource.Id.spinnerMulti);
			editInput = view.FindViewById<EditText> (Resource.Id.editInput);
			layoutDialog = view.FindViewById<LinearLayout> (Resource.Id.layoutDialog);
			layoutInput = view.FindViewById<LinearLayout> (Resource.Id.layoutInput);
			layoutButton = view.FindViewById<LinearLayout> (Resource.Id.layoutButton);

			// Don't know a better way
			layoutDialog.SetBackgroundResource(Main.BottomBackground);
			layoutButton.SetBackgroundResource(Main.BottomBackground);

			if (input == null) {
				// Normal dialog
				layoutDialog.Visibility = ViewStates.Visible;
				layoutInput.Visibility = ViewStates.Gone;

				btnView1 = view.FindViewById<Button> (Resource.Id.button1);
				btnView1.SetTextColor(Color.White);
				btnView1.SetBackgroundResource(Main.ButtonBackground);
				btnView1.Click += OnButtonClicked;

				btnView2 = view.FindViewById<Button> (Resource.Id.button2);
				btnView2.SetTextColor(Color.White);
				btnView2.SetBackgroundResource(Main.ButtonBackground);
				btnView2.Click += OnButtonClicked;
			} else {
				if (input.InputType == InputType.MultipleChoice) {
					// Multiple choice dialog
					layoutDialog.Visibility = ViewStates.Gone;
					layoutInput.Visibility = ViewStates.Visible;
					spinnerInput.Visibility = ViewStates.Visible;
					editInput.Visibility = ViewStates.Gone;
				} else {
					// Input dialog
					layoutDialog.Visibility = ViewStates.Gone;
					layoutInput.Visibility = ViewStates.Visible;
					spinnerInput.Visibility = ViewStates.Gone;
					editInput.Visibility = ViewStates.Visible;

					editInput.Text = "";
					editInput.EditorAction += HandleEditorAction;  
				}

				btnInput = view.FindViewById<Button> (Resource.Id.buttonInput);
				btnInput.Text = ctrl.Resources.GetString(Resource.String.done);
				btnInput.SetBackgroundResource(Resource.Drawable.apptheme_btn_default_holo_light);
				btnInput.Click += OnInputClicked;
			}

			return view;
		}

		public void OnInputClicked(object sender, EventArgs e)
		{
			ctrl.Feedback();

			// Remove keyboard
			new Handler().Post(delegate
				{
					var view = ctrl.CurrentFocus;
					if (view != null)
					{
						InputMethodManager manager = (InputMethodManager)ctrl.GetSystemService(Context.InputMethodService);
						manager.HideSoftInputFromWindow(view.WindowToken, 0);
					}
				});

			// Remove dialog from screen
			ctrl.RemoveScreen (this);

			if (input != null) {
				if (input.InputType != InputType.MultipleChoice) {
					inputResult = editInput.Text;
				}
				input.GiveResult (inputResult);
			}
		}

		public override void OnResume()
		{
			base.OnResume();

			ctrl.SupportActionBar.Title = "";
			ctrl.SupportActionBar.SetDisplayShowHomeEnabled(false);

			Refresh();
		}

		private void OnSpinnerItemSelected (object sender, AdapterView.ItemSelectedEventArgs e)
		{
			ctrl.Feedback();

			if (input != null) {
				inputResult = input.Choices.ElementAt(e.Position);
			}
		}

		#endregion

		#region Private Functions

		void Refresh()
		{
			if (input == null) {
				// Normal dialog
				// TODO: HTML
				textDescription.Text = messageBox.Text; // Html.FromHtml(messageBox.HTML.Replace("&lt;BR&gt;", "<br>").Replace("<br>\n", "<br>").Replace("\n", "<br>"));
				textDescription.Gravity = Main.Prefs.TextAlignment.ToSystem();
				textDescription.SetTextSize(global::Android.Util.ComplexUnitType.Sp, (float)Main.Prefs.TextSize);
				if (messageBox.Image != null) {
					imageView.SetImageBitmap(null);
					using (Bitmap bm = ctrl.ConvertMediaToBitmap(messageBox.Image)) {
						imageView.SetImageBitmap(null);
						imageView.SetImageBitmap(bm);
					}
					imageView.Visibility = ViewStates.Visible;
				} else {
					imageView.Visibility = ViewStates.Gone;
				}
				if (!String.IsNullOrEmpty (messageBox.FirstButtonLabel)) {
					btnView1.Visibility = ViewStates.Visible;
					btnView1.Text = messageBox.FirstButtonLabel;
					btnView1.LayoutChange += (object sender, View.LayoutChangeEventArgs e) => SetTextScale(btnView1);
				} else
					btnView1.Visibility = ViewStates.Gone;
				if (!String.IsNullOrEmpty (messageBox.SecondButtonLabel)) {
					btnView2.Visibility = ViewStates.Visible;
					btnView2.Text = messageBox.SecondButtonLabel;
					btnView2.LayoutChange += (object sender, View.LayoutChangeEventArgs e) => SetTextScale(btnView2);
				} else
					btnView2.Visibility = ViewStates.Gone;
			} else {
				// TODO: HTML
				textDescription.Text = input.Text; // Html.FromHtml(input.HTML.Replace("&lt;BR&gt;", "<br>").Replace("<br>\n", "<br>").Replace("\n", "<br>"));
				textDescription.Gravity = Main.Prefs.TextAlignment.ToSystem();
				textDescription.SetTextSize(global::Android.Util.ComplexUnitType.Sp, (float)Main.Prefs.TextSize);
				if (input.Image != null) {
					using (Bitmap bm = ctrl.ConvertMediaToBitmap(input.Image)) {
						imageView.SetImageBitmap (bm);
					}
					imageView.Visibility = ViewStates.Visible;
				} else {
					imageView.Visibility = ViewStates.Gone;
				}
				if (input.InputType == InputType.MultipleChoice) {
					// Multiple choice dialog
					Spinner spinner = ctrl.FindViewById<Spinner> (Resource.Id.spinnerMulti);
					spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs> (OnSpinnerItemSelected);
					ArrayAdapter<string> adapter = new ArrayAdapter<string>(this.Activity, Android.Resource.Layout.SimpleSpinnerItem, input.Choices.ToArray());
					adapter.SetDropDownViewResource (Android.Resource.Layout.SimpleSpinnerDropDownItem);
					spinner.Adapter = adapter;
				} else {
					// Input dialog
					// ToDo: Clear text field editInput
					if (Main.Prefs.InputFocus) {
						editInput.RequestFocus();
						((InputMethodManager)ctrl.GetSystemService(Context.InputMethodService)).ShowSoftInput(editInput, ShowFlags.Implicit);
					}
				}
			}
		}

		void SetTextScale(Button button)
		{
			// Set default scale
			button.TextScaleX = 1.0f;
			// Calculate new scale of text
			// Found at http://catchthecows.com/?p=72
			Rect bounds = new Rect();
			// ask the paint for the bounding rect if it were to draw this text.
			string text = button.Text;
			int length = button.Text.Length;
			float buttonTextWidth = (float)(button.Right - button.Left - button.TotalPaddingLeft - button.TotalPaddingRight);
			// get bounds of text
			button.Paint.GetTextBounds(text, 0, text.Length, bounds);
			// Calc scale
			float scale = (float)(button.Right - button.Left - button.TotalPaddingLeft - button.TotalPaddingRight) / (bounds.Right - bounds.Left);
			// When scale to small, shorten the string and append ...
			while (scale < 0.6f) {
				length -= 1;
				text = button.Text.Substring(0, length) + "...";
				button.Paint.GetTextBounds(text, 0, text.Length, bounds);
				scale = buttonTextWidth / (bounds.Right - bounds.Left);
			}
			scale = scale > 1.0f ? 1.0f : scale;
			button.TextScaleX = scale;
		}

		// Add this method to your class
		private void HandleEditorAction(object sender, EditText.EditorActionEventArgs e)
		{
			e.Handled = false;
			if (e.ActionId == global::Android.Views.InputMethods.ImeAction.Done || e.Event.UnicodeChar == 10)
			{
//				InputMethodManager inputMethodManager = Application.GetSystemService(Context.InputMethodService) as InputMethodManager;
//				inputMethodManager.HideSoftInputFromWindow(editInput.WindowToken, HideSoftInputFlags.None);
				OnInputClicked (sender, e);
				e.Handled = true;   
			}
		}

		#endregion

	}

	#endregion
}

