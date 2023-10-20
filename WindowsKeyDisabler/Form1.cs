namespace WindowsKeyDisabler
{
	public partial class Form1 : Form
	{

		//private KeyboardHook _hook;

		public Form1()
		{
			InitializeComponent();
			this.Visible = false;
		}

		protected override void OnLoad(EventArgs e)
		{
			Visible = false; // Hide form window.
			ShowInTaskbar = false; // Remove from taskbar.

			base.OnLoad(e);
		}
	}
}