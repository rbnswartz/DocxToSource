using Avalonia.Controls;
using AvaloniaEdit.Search;

namespace DocxToSource.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SearchPanel.Install(this.xXmlSourceEditor);
            SearchPanel.Install(this.xCodeSourceEditor);
        }
    }
}