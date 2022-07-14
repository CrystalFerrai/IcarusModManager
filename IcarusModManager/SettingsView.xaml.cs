using System.Windows;
using System.Windows.Controls;

namespace IcarusModManager
{
	internal partial class SettingsView : UserControl
	{
		public SettingsView()
		{
			InitializeComponent();
		}

		private void Commit_Click(object sender, RoutedEventArgs e)
		{
			((SettingsVM)DataContext).CommitChanges();
			Window.GetWindow(this).DialogResult = true;
		}
	}
}
