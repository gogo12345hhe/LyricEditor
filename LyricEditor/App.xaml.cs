using System.Text;
using System.Windows;

namespace LyricEditor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args != null && e.Args.Length > 0)
            {
                Properties["arg1"] = e.Args[0];
            }

            base.OnStartup(e);
        }

        public App()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
