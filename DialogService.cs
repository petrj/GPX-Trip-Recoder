using Android.Content;
using AndroidX.AppCompat.App;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GPX_trip_recorder
{
    public class DialogService
    {
        public DialogService(AppCompatActivity activity = null)
        {
            Activity = activity;
        }

        public AppCompatActivity Activity { get; set; }

        private void ShowDialog(string message, string title, string btnTitle, Action action, int? iconResId)
        {
            var dialog = new AlertDialog.Builder(Activity);
            var alert = dialog.Create();
            alert.SetTitle(title);
            alert.SetMessage(message);
            if (iconResId.HasValue)
            {
                alert.SetIcon(Resource.Drawable.exclamation);
            }

            alert.SetButton((int)DialogButtonType.Positive, btnTitle, (senderAlert, args) =>
            {
                if (action != null)
                {
                    action();
                }
            });

            alert.Show();
        }

        public void Information(string message, string title = "Information")
        {
            ShowDialog(message, title, "OK", null, null);
        }

        public void Warning(string message, string title = "Warning")
        {
            ShowDialog(message, title, "OK", null, Resource.Drawable.exclamation);
        }

        public async Task<bool> ConfirmDialog(string question, string title = "Confirmation", string yesButtonTitle = "Yes", string noButtonTitle = "No")
        {
            var dialog = new AlertDialog.Builder(Activity);
            var alert = dialog.Create();
            alert.SetTitle(title);
            alert.SetMessage(question);

            var result = false;

            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            alert.SetButton((int)DialogButtonType.Positive, yesButtonTitle, (senderAlert, args) =>
            {
                result = true;
                waitHandle.Set();
            });

            alert.SetButton((int)DialogButtonType.Negative, noButtonTitle, (senderAlert, args) =>
            {
                result = false;
                waitHandle.Set();
            });


            alert.Show();
            await Task.Run(() => waitHandle.WaitOne());

            return result;
        }

        public async Task<bool?> ConfirmYesNoContinueDialog(string question, string title = "Confirmation", string yesButtonTitle = "Yes", string noButtonTitle = "No", string continueButtonTitle = "Continue")
        {
            var dialog = new AlertDialog.Builder(Activity);
            var alert = dialog.Create();
            alert.SetTitle(title);
            alert.SetMessage(question);

            bool? result = false;

            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            alert.SetButton((int)DialogButtonType.Positive, yesButtonTitle, (senderAlert, args) =>
            {
                result = true;
                waitHandle.Set();
            });

            alert.SetButton((int)DialogButtonType.Negative, noButtonTitle, (senderAlert, args) =>
            {
                result = false;
                waitHandle.Set();
            });

            alert.SetButton((int)DialogButtonType.Neutral, continueButtonTitle, (senderAlert, args) =>
            {
                result = null;
                waitHandle.Set();
            });


            alert.Show();
            await Task.Run(() => waitHandle.WaitOne());

            return result;
        }
    }
}
