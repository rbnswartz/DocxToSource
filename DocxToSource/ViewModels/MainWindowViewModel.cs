using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocxToSource.Languages;
using DocxToSource.Models;
using ReactiveUI;

namespace DocxToSource.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        #region Private Static Fields

        /// <summary>
        /// Holds the default <see cref="IHighlightingDefinition"/> to use for
        /// the Xml window.
        /// </summary>
        private static readonly IHighlightingDefinition _defaultXmlDefinition;

        #endregion 

        #region Private Instance Fields
        
        /// <summary>
        /// Holds the source code of the current selected openxml object.
        /// </summary>
        private TextDocument _codeDocument;

        /// <summary>
        /// Holds the highlighting definition for the source code text editor.
        /// </summary>
        private IHighlightingDefinition _codeSyntax;

        /// <summary>
        /// Holds the <see cref="DirectoryInfo"/> object containing the full path
        /// to use as the initial path in any OpenFileDialog windows.
        /// </summary>
        private DirectoryInfo _currentFileDirectory;

        /// <summary>
        /// Holds the full path and filename of the file that is currently open.
        /// </summary>
        private string _fileName;

        /// <summary>
        /// Indicates whether or not to automatically generate source code when 
        /// selecting DOM nodes.
        /// </summary>
        private bool _generateSourceCode;

        /// <summary>
        /// Indicates whether or not to enable syntax highlighting in the source code
        /// windows.
        /// </summary>
        private bool _highlightSyntax;

        /// <summary>
        /// Indicates whether or not the selected item represents an
        /// <see cref="OpenXmlElement"/> object.
        /// </summary>
        private bool _isOpenXmlElement;

        /// <summary>
        /// Holds the openxml file package the is currently being reviewed.
        /// </summary>
        private OpenXmlPackage _oPkg;

        /// <summary>
        /// Holds the raw package used to stage the stream information for 
        /// validation purposes.
        /// </summary>
        private Package _pkg;

        /// <summary>
        /// Holds the current treeviewitem that is currently selected in the treeview.
        /// </summary>
        private TreeItemBase _selectedItem;

        /// <summary>
        /// Holds the currently selected <see cref="LanguageDefinition"/> object.
        /// </summary>
        private LanguageDefinition _selectedLanguage;

        /// <summary>
        /// Holds the io stream containing the contents of the openxml file package.
        /// </summary>
        private Stream _stream;

        /// <summary>
        /// Holds the detailed exception information to display in the tree list view.
        /// </summary>
        private ObservableCollection<TreeItemBase> _treeData;

        /// <summary>
        /// Indicates whether or not to have the text in the source code windows word wrap.
        /// </summary>
        private bool _wordWrap;

        /// <summary>
        /// Holds the xml code fo the current selected openxml element.
        /// </summary>
        private TextDocument _xmlDocument;

        /// <summary>
        /// Holds the highlighting definition for the XML Text Editor
        /// </summary>
        private IHighlightingDefinition _xmlDocumentSyntax;

        private TextDocument _test;

        #endregion


        

        #region Static Constructors

        /// <summary>
        /// Static Constructor.
        /// </summary>
        static MainWindowViewModel()
        {
            // Setup the default Xml definition to use when syntax highlighting is requested
            _defaultXmlDefinition = HighlightingManager.Instance.GetDefinition("XML");
        }

        #endregion
        
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowModel"/> class that
        /// is empty.
        /// </summary>
        public MainWindowViewModel() : base()
        {
            TestDocument = new TextDocument("Hello world");
            _currentFileDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            _treeData = new ObservableCollection<TreeItemBase>();

            ObservableCollection<LanguageDefinition> langeDefs = new();
            LanguageDefinitions = new ReadOnlyObservableCollection<LanguageDefinition>(langeDefs);

            // Load the language definition list
            langeDefs.Add(new CSharpLanguageDefinition());
            langeDefs.Add(new VBLanguageDefinition());

            // Set the default language
            SelectedLanguage = LanguageDefinitions[0];

            CodeDocument = new TextDocument();
            XmlSourceDocument = new TextDocument();

            CloseCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Dispose();
                _treeData.Clear();
                RefreshSourceCodeWindows(null);
            });
            OpenCommand = ReactiveCommand.CreateFromTask(OpenOfficeDocument);

            QuitCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Dispose();
                Environment.Exit(0);
            });
        }

        #endregion

        #region Public Instance Properties

        /// <summary>
        /// Gets the command to close the current document.
        /// </summary>
        public ReactiveCommand<Unit,Unit> CloseCommand { get; private set; }

        public TextDocument TestDocument
        {
            get => _test;
            set => this.RaiseAndSetIfChanged(ref _test, value);
        }
        
        /// <summary>
        /// Gets or sets the source code document object to display to the user.
        /// </summary>
        public TextDocument CodeDocument
        {
            get => _codeDocument;
            set
            {
                _codeDocument = value;
                this.RaisePropertyChanged(nameof(CodeDocument));
            }
        }

        /// <summary>
        /// Gets or sets the syntax highlighting definition for the source code text editor.
        /// </summary>
        public IHighlightingDefinition CodeDocumentSyntax
        {
            get => _codeSyntax;
            set
            {
                _codeSyntax = value;
                this.RaisePropertyChanged(nameof(CodeDocumentSyntax));
            }
        }

        /// <summary>
        /// Gets or sets the source code text to display to the user.
        /// </summary>
        public string CodeDocumentText
        {
            get => _codeDocument.Text;
            private set
            {
                _codeDocument.Text = value;
                this.RaisePropertyChanged(nameof(CodeDocument));
            }
        }

        /// <summary>
        /// Indicates whether or not to automatically generate source code when 
        /// selecting DOM nodes.
        /// </summary>
        public bool GenerateSourceCode
        {
            get => _generateSourceCode;
            set
            {
                _generateSourceCode = value;
                var item = GenerateSourceCode ? SelectedItem as OpenXmlTreeItem : null;
                RefreshSourceCodeWindows(item);
                this.RaisePropertyChanged(nameof(GenerateSourceCode));
            }
        }

        /// <summary>
        /// Indicates whether or not to enable syntax highlighting in the source code
        /// windows.
        /// </summary>
        public bool HighlightSyntax
        {
            get => _highlightSyntax;
            set
            {
                _highlightSyntax = value;
                ToggleSyntaxHighlighting(GenerateSourceCode && !(SelectedItem is null) && _highlightSyntax);
                this.RaisePropertyChanged(nameof(HighlightSyntax));
            }
        }

        /// <summary>
        /// Indicates whether or not the selected item represents an
        /// <see cref="OpenXmlElement"/> object.
        /// </summary>
        public bool IsOpenXmlElement
        {
            get => _isOpenXmlElement;
            set
            {
                _isOpenXmlElement = value;
                this.RaisePropertyChanged(nameof(IsOpenXmlElement));
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="LanguageDefinition"/> objects that
        /// the user can select.
        /// </summary>
        public ReadOnlyObservableCollection<LanguageDefinition> LanguageDefinitions
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to open a new office 2007+ document.
        /// </summary>
        public ReactiveCommand<Unit,Unit> OpenCommand { get; private set; }

        /// <summary>
        /// Gets the command that shuts down the application.
        /// </summary>
        public ReactiveCommand<Unit,Unit> QuitCommand { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="OpenXmlTreeViewItem"/> that is currently selected.
        /// </summary>
        public TreeItemBase SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;

                if (GenerateSourceCode && _selectedItem is OpenXmlTreeItem)
                {
                    RefreshSourceCodeWindows(_selectedItem as OpenXmlTreeItem);
                }
                this.RaisePropertyChanged(nameof(SelectedItem));
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="LanguageDefinition"/> object currently
        /// selected by the user.
        /// </summary>
        public LanguageDefinition SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                if (GenerateSourceCode)
                {
                    RefreshSourceCodeWindows(SelectedItem as TreeItemBase);
                }
                this.RaisePropertyChanged(nameof(SelectedLanguage));
            }
        }

        /// <summary>
        /// Gets all of the openxml objects to display in the tree.
        /// </summary>
        public ObservableCollection<TreeItemBase> TreeData
        {
            get => _treeData;
            private set
            {
                _treeData = value;
                this.RaisePropertyChanged(nameof(TreeData));
            }
        }

        /// <summary>
        /// Indicates whether or not to have the text in the source code windows word wrap.
        /// </summary>
        public bool WordWrap
        {
            get => _wordWrap;
            set
            {
                _wordWrap = value;
                this.RaisePropertyChanged(nameof(WordWrap));
            }
        }

        /// <summary>
        /// Gets or sets the source code to display to the user.
        /// </summary>
        public TextDocument XmlSourceDocument
        {
            get => _xmlDocument;
            set
            {
                _xmlDocument = value;
                this.RaisePropertyChanged(nameof(XmlSourceDocument));
            }
        }

        /// <summary>
        /// Gets or sets the syntax highlighting definition for the xml document text editor.
        /// </summary>
        public IHighlightingDefinition XmlSourceDocumentSyntax
        {
            get => _xmlDocumentSyntax;
            set
            {
                _xmlDocumentSyntax = value;
                this.RaisePropertyChanged(nameof(XmlSourceDocumentSyntax));
            }
        }

        /// <summary>
        /// Gets or sets the source code text to display to the user.
        /// </summary>
        public string XmlSourceDocumentText
        {
            get => _xmlDocument.Text;
            set
            {
                _xmlDocument.Text = value;
                this.RaisePropertyChanged(nameof(XmlSourceDocument));
            }
        }

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Method to make sure that all unmanaged resources are released properly.
        /// </summary>
        public void Dispose()
        {
            if (_oPkg != null)
            {
                _oPkg.Close();
                _oPkg.Dispose();
                _oPkg = null;
            }
            if (_pkg != null)
            {
                _pkg.Close();
                _pkg = null;
            }
            if (_stream != null)
            {
                _stream.Close();
                _stream.Dispose();
                _stream = null;
            }
        }

        #endregion
        
        
        #region Private Instance Methods

        /// <summary>
        /// Shortcut method to raise the <see cref="PropertyChangedEventHandler"/>
        /// event.
        /// </summary>
        /// <param name="name">
        /// Name of the property raising the event.
        /// </param>
        private void FireChangeEvent(string name) =>
            this.RaisePropertyChanged(name);

        /// <summary>
        /// Resets the main window controls and loads a requested OpenXml based file.
        /// </summary>
        private async Task OpenOfficeDocument()
        {
            const string docxIdUri = "/word/document.xml";
            const string xlsxIdUri = "/xl/workbook.xml";
            const string pptxIdUri = "/ppt/presentation.xml";
            const string fileFilter =
                "All Microsoft Office 2007+ valid documents (*.xlsx;*.xlsm;*.pptx;*.pptm;*.docx;*.docm)|*.xlsx;*.xlsm;*.pptx;*.pptm;*.docx;*.docm" +
                "|Microsoft Excel 2007+ documents (*.xlsx;*.xlsm)|*.xlsx;*.xlsm" +
                "|Microsoft Powerpoint 2007+ documents (*.pptx;*.pptm)|*.pptx;*.pptm" +
                "|Microsoft Word 2007+ documents (*.docx;*.docm)|*.docx;*.docm" +
                "|All files | *.*";

            var fileFilters = new List<FileDialogFilter>
            {
                new FileDialogFilter()
                    { Name = "All Microsoft Office 2007+ valid documents", 
                        Extensions = new List<string>() {"xlsx","xlsm","pptx","pptm","docx","docm"} },
                new FileDialogFilter()
                    { Name = "Microsoft Excel 2007+ documents", 
                        Extensions = new List<string>() { "xlsx", "xlsm" } },
                new FileDialogFilter()
                    { Name = "Microsoft Powerpoint 2007+ documents", 
                        Extensions = new List<string>() { "pptx", "pptm" } },
                new FileDialogFilter()
                    { Name = "Microsoft Word 2007+ documents", 
                        Extensions = new List<string>() { "docx", "docm" } },
            };
            
            OpenFileDialog ofDialog = new()
            {
                InitialDirectory = _currentFileDirectory.FullName,
                AllowMultiple = false,
                Filters =fileFilters,
            };

            var currentLifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var dialogResult = await ofDialog.ShowAsync(currentLifetime.MainWindow);
            if (dialogResult == null || dialogResult.Length == 0)
            {
                // If the user cancels out; exit method.
                return;
            }

            // Ensure that everything is cleared out before proceeding
            Dispose();
            CodeDocument.FileName = null;
            XmlSourceDocument.FileName = null;
            CodeDocumentText = String.Empty;
            XmlSourceDocumentText = String.Empty;
            _fileName = String.Empty;

            // Get the selected file details
            FileInfo fi = new(dialogResult[0]);
            _currentFileDirectory = fi.Directory;
            _fileName = fi.Name;
            _stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _pkg = Package.Open(_stream);

            // Setup a quick look up for easier package validation
            Dictionary<string, Func<Package, OpenXmlPackage>> quickPicks = new(3)
            {
                { docxIdUri, WordprocessingDocument.Open },
                { xlsxIdUri, SpreadsheetDocument.Open },
                { pptxIdUri, PresentationDocument.Open }
            };

            foreach (KeyValuePair<string, Func<Package, OpenXmlPackage>> qp in quickPicks)
            {
                if (_pkg.PartExists(new Uri(qp.Key, UriKind.Relative)))
                {
                    _oPkg = qp.Value.Invoke(_pkg);
                    break;
                }
            }

            // Make sure that a valid package was found before proceeding.
            if (_oPkg == null)
            {
                throw new InvalidDataException("Selected file is not a known/valid OpenXml document");
            }

            // Wrap it up
            PackageTreeItem mainItem = new(_oPkg) { Header = _fileName };
            TreeData.Clear();
            TreeData.Add(mainItem);
        }

        /// <summary>
        /// Refreshes the <see cref="TextEditor"/> controls
        /// in the main window.
        /// </summary>
        /// <param name="item">
        /// The <see cref="OpenXmlTreeViewItem"/> currently selected by the user.
        /// </param>
        /// <remarks>
        /// Passing <see langword="null"/> as the <paramref name="item"/> will cause
        /// the <see cref="TextEditor"/> controls to clear their
        /// contents.
        /// </remarks>
        private void RefreshSourceCodeWindows(TreeItemBase item)
        {
            if (item is null)
            {
                CodeDocument.FileName = null;
                XmlSourceDocument.FileName = null;
                CodeDocumentText = String.Empty;
                XmlSourceDocumentText = String.Empty;
                ToggleSyntaxHighlighting(false);
            }
            else
            {
                var randName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                CodeDocument.FileName = randName + "." + SelectedLanguage.Provider.FileExtension;
                XmlSourceDocument.FileName = randName + ".xml";

                CodeDocumentText = item.BuildCodeDomTextDocument(SelectedLanguage.Provider);
                XmlSourceDocumentText = item.BuildXmlTextDocument();

                ToggleSyntaxHighlighting(HighlightSyntax);
            }
            IsOpenXmlElement = !String.IsNullOrWhiteSpace(XmlSourceDocumentText);
            FireChangeEvent(nameof(IsOpenXmlElement));
        }

        /// <summary>
        /// Enables or disables syntax highlighting in the code windows.
        /// </summary>
        /// <param name="enable">
        /// <see langword="true"/> to turn on syntax highlighting; <see langword="false"/>
        /// to turn it off.
        /// </param>
        private void ToggleSyntaxHighlighting(bool enable)
        {
            CodeDocumentSyntax = enable ? SelectedLanguage.Highlighting : null;
            XmlSourceDocumentSyntax = enable ? _defaultXmlDefinition : null;
        }

        #endregion
    }
}