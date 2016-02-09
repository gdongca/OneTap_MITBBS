using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Controls;
using HtmlAgilityPack;
using Microsoft.Xna.Framework.Media;

namespace Naboo.MitbbsReader.Pages
{
    public partial class NewPostPage : PhoneApplicationPage
    {
        private static String _sendFromText = "--\n◆ Sent from OneTap MITBBS Reader for Windows Phone";
        private static String _shareFromText = "--\n◆ Shared from OneTap MITBBS Reader for Windows Phone";

        private String _url;
        private bool _fullPage = false;
        private MitbbsPostEditBase _postEdit;
        private MitbbsFileUpload _fileUpload = new MitbbsFileUpload();
        private bool _appendSendFrom = false;
        private String _picFileId = null;
        private ImageLoader _imageLoader = new ImageLoader();
        
        private PhotoChooserTask _photoChooser = new PhotoChooserTask();
        private CameraCaptureTask _cameraTask = new CameraCaptureTask();
        private UploadFileList _uploadFileList = new UploadFileList();

        private PhotoResult _newChosenPhoto = null;
        
        public NewPostPage()
        {
            InitializeComponent();

#if DEBUG
#else
            ApplicationBar.MenuItems.RemoveAt(0); //remove the "open in browser" menu item
#endif

            App.Settings.ApplyPageSettings(this, LayoutRoot, false);

            _imageLoader.DisplayPanel = VerifyImagePanel;
            _imageLoader.ScrollPanel = null;
            _imageLoader.Page = this;
            _imageLoader.FontSize = (double)App.Current.Resources["MitbbsFontSizeText"];
            _imageLoader.ShowButtons = false;
            _imageLoader.CacheImage = false;

            App.Track("Navigation", "OpenPage", "NewPost");

            _photoChooser.Completed += PhotoChooserTask_Completed;
            _cameraTask.Completed += PhotoChooserTask_Completed;

            UploadListBox.ItemsSource = _uploadFileList;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            PageHelper.InitAdControl(AdGrid);

            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("FullPage"))
            {
                _fullPage = bool.Parse(parameters["FullPage"]);
            }
            else
            {
                _fullPage = false;
            }

            if (parameters.ContainsKey("Url"))
            {
                _url = parameters["Url"];
            }
            else
            {
                _url = null;
            }

            if (parameters.ContainsKey("PageTitle"))
            {
                ArticlePivotPage.Header = parameters["PageTitle"];
            }

            if (parameters.ContainsKey("SendFrom"))
            {
                _appendSendFrom = bool.Parse(parameters["SendFrom"]);
            }
            else
            {
                _appendSendFrom = false;
            }

            if (parameters.ContainsKey("Recipient"))
            {
                RecipientTextBox.Text = parameters["Recipient"];
            }

            if (parameters.ContainsKey("PostBody"))
            {
                BodyTextBox.Text = parameters["PostBody"];
            }

            if (parameters.ContainsKey("PostTitle"))
            {
                TitleTextBox.Text = parameters["PostTitle"];
            }

            if (parameters.ContainsKey("PostBody"))
            {
                BodyTextBox.Text = parameters["PostBody"];
            }

            if (parameters.ContainsKey("BlankText"))
            {
                bool useBlankText = bool.Parse(parameters["BlankText"]);
                if (useBlankText)
                {
                    BodyTextBox.Text = "\n\n";
                }
            }

            if (parameters.ContainsKey("PicFileId"))
            {
                _picFileId = parameters["PicFileId"];
            }

            if (State.ContainsKey("PostTitle"))
            {
                TitleTextBox.Text = (String)State["PostTitle"];
            }

            if (State.ContainsKey("PostBody"))
            {
                BodyTextBox.Text = (String)State["PostBody"];
            }

            if (State.ContainsKey("Recipient"))
            {
                RecipientTextBox.Text = (String)State["Recipient"];
            }

            if (_postEdit == null)
            {
                if (_fullPage)
                {
                    _postEdit = new MitbbsPostEdit();
                }
                else
                {
                    _postEdit = new MitbbsPostEditMobile();
                }

                _postEdit.EditPageLoaded += EditPage_Loaded;
                _postEdit.SendPostCompleted += SendPost_Completed;
            }

            _uploadFileList.RestoreState(State);

            if (_newChosenPhoto != null)
            {
                long totalSize = _uploadFileList.TotalUploadSize + _newChosenPhoto.ChosenPhoto.Length;

                if (totalSize > App.Settings.MaxUploadSize)
                {
                    MessageBox.Show("上载文件总长度不能超过" + App.Settings.MaxUploadSize + "字节", "无法添加此图片", MessageBoxButton.OK);
                }
                else
                {
                    String fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(_newChosenPhoto.OriginalFileName);
                    _uploadFileList.AddUpload(fileName, _newChosenPhoto.ChosenPhoto);

                    _newChosenPhoto.ChosenPhoto.Dispose();
                    
                    if (PivotControl.Items.Count > 1)
                    {
                        PivotControl.SelectedIndex = 1;
                    }
                }

                _newChosenPhoto = null;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            PageHelper.CleanupAdControl(AdGrid);

            State.Clear();

            State["Url"] = _url;
            State["PostTitle"] = TitleTextBox.Text;
            State["PostBody"] = BodyTextBox.Text;

            _uploadFileList.SaveState(State);

            if (RecipientPanel.Visibility == Visibility.Visible)
            {
                State["Recipient"] = RecipientTextBox.Text;
            }

            base.OnNavigatedFrom(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_postEdit.IsEditPageLoaded)
            {
                return;
            }

            if (!App.WebSession.IsLoggedIn)
            {
                if (App.WebSession.IsConnecting)
                {
                    (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //send button
                    (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //attach button
                    (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false; //account button

                    LoadProgressBar.Visibility = Visibility.Visible;
                    DisableRect.Visibility = Visibility.Visible;

                    App.WebSession.LogInCompleted += OnLogOnCompleted;
                    App.WebSession.LogOutCompleted += OnLogOnCompleted;
                }
                else
                {
                    MessageBox.Show("用户未登录！");

                    _uploadFileList.Clear();
                    _uploadFileList.ClearCachedUploads();

                    NavigationService.GoBack();
                }

                return;
            }

            if (!String.IsNullOrEmpty(_picFileId))
            {
                MediaLibrary library = new MediaLibrary();
                Picture photoFromLibrary = library.GetPictureFromToken(_picFileId);

                if (photoFromLibrary != null)
                {
                    //if (String.IsNullOrEmpty(TitleTextBox.Text))
                    //{
                    //    TitleTextBox.Text = "图片分享: " + photoFromLibrary.Name;
                    //}

                    String fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(photoFromLibrary.Name);
                    _uploadFileList.AddUpload(fileName, photoFromLibrary.GetImage());
                }
                else
                {
                    MessageBox.Show("图片分享失败:无法读取图片信息");
                    NavigationService.GoBack();
                }
            }


            PivotControl.Title = App.License.AppTitle + " (" + App.WebSession.Username + ")";

            LoadEditPage();
        }

        private bool ConfirmExit()
        {
            if (_postEdit.IsEditPageLoaded)
            {
                if ((TitleTextBox.Text.Trim() != "") || (BodyTextBox.Text.Trim('\n') != ""))
                {
                    MessageBoxResult result = MessageBox.Show("你将丢失未保存的文章内容。", "确认取消编辑文章？", MessageBoxButton.OKCancel);
                    return (result == MessageBoxResult.OK);
                }
            }

            return true;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!ConfirmExit())
            {
                e.Cancel = true;
            }
            else
            {
                _uploadFileList.Clear();
                _uploadFileList.ClearCachedUploads();
            }

            base.OnBackKeyPress(e);
        }

        private void OnLogOnCompleted(object sender, MitbbsWebSessionEventArgs args)
        {
            App.WebSession.LogInCompleted -= OnLogOnCompleted;
            App.WebSession.LogOutCompleted -= OnLogOnCompleted;

            PivotControl.Title = App.License.AppTitle + " (" + App.WebSession.Username + ")";

            if (App.WebSession.IsLoggedIn)
            {
                LoadEditPage();
            }
            else
            {
                MessageBox.Show("用户未登录！");
                
                _uploadFileList.Clear();
                _uploadFileList.ClearCachedUploads();

                NavigationService.GoBack();
            }
        }

        private void LoadEditPage()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //send button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //attach button
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false; //account button

            LoadProgressBar.Visibility = Visibility.Visible;
            DisableRect.Visibility = Visibility.Visible;

            if (_url != null)
            {
                _postEdit.LoadEditPage(App.WebSession.CreateWebClient(), _url);
            }
            else
            {
                ArticlePivotPage.Header = "参数错误!";
            }
        }

        private void EditPage_Loaded(object sender, DataLoadedEventArgs e)
        {
            LoadProgressBar.Visibility = Visibility.Collapsed;
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true; //account button

            if (_postEdit.IsEditPageLoaded)
            {
                CookieAwareClient web = new CookieAwareClient();
                web.Cookies = App.WebSession.Cookies;
                _imageLoader.Web = web;

                DisableRect.Visibility = Visibility.Collapsed;

                if (TitleTextBox.Text == "")
                {
                    TitleTextBox.Text = _postEdit.PostTitle;
                }

                if (BodyTextBox.Text == "")
                {
                    BodyTextBox.Text = _postEdit.PostBody;
                }

                _imageLoader.ClearImages();
                
                if (_postEdit.VerifyImageUrl != null)
                {
                    _imageLoader.LoadImage(
                                            _postEdit.VerifyImageUrl,
                                            _postEdit.EditPageUrl,
                                            null,
                                            false,
                                            false,
                                            "<正在打开验证码图片...>",
                                            "<无法开打验证码图片>"
                                            );

                    VerifyCodeTextBox.Text = "";
                    VerifyPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    VerifyPanel.Visibility = Visibility.Collapsed;
                }

                if (_postEdit.Recipient != null)
                {
                    if (RecipientTextBox.Text == "")
                    {
                        RecipientTextBox.Text = _postEdit.Recipient;
                    }

                    RecipientPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    RecipientPanel.Visibility = Visibility.Collapsed;
                }

                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //send button

                if (_postEdit.UploadFileUrl != null)
                {
                    (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //attach button
                }
                else
                {
                    if (PivotControl.Items.Count > 1)
                    {
                        PivotControl.Items.RemoveAt(1);
                    }
                }
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("是否要重试？如果选择取消，你的文章内容将会丢失。如果你已多次遇到此错误，请尝试重新登录。", "读取页面失败！", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    LoadEditPage();
                }
                else
                {
                    _uploadFileList.Clear();
                    _uploadFileList.ClearCachedUploads();

                    NavigationService.GoBack();
                }
            }
        }

        private void SendPost()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //send button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //attach button
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false; //account button

            LoadProgressBar.Visibility = Visibility.Visible;
            DisableRect.Visibility = Visibility.Visible;

            _postEdit.PostTitle = TitleTextBox.Text;
            _postEdit.PostBody = InsertSendFrom(BodyTextBox.Text).Replace('\r', '\n').Trim('\n');
            
            if (_postEdit.Recipient != null)
            {
                _postEdit.Recipient = RecipientTextBox.Text;
            }

            if (_postEdit.VerifyImageUrl != null)
            {
                _postEdit.VerifyCode = VerifyCodeTextBox.Text;
            }

            foreach (var ul in _postEdit.UploadFiles)
            {
                ul.FileStream.Dispose();
            }

            _postEdit.UploadFiles.Clear();

            foreach (var upload in _uploadFileList)
            {
                Stream fileStream = upload.FileStream;

                if (fileStream != null)
                {
                    _postEdit.UploadFiles.Add(
                        new FormUploadElement()
                        {
                            FileName = upload.FileName,
                            FileStream = fileStream,
                            ContentType = "image/pjpeg",
                        }
                        );
                }
            }

            _postEdit.SendPost();

            App.ForceRefreshContent = true;
        }

        public static String AddSendFrom(String text, bool fullPage, bool share = false)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(text.Replace('\r', '\n'));

            if (App.Settings.AppendSentFrom)
            {
                if (sb.Length < 1)
                {
                    sb.Append("\n\n");
                }
                else
                {
                    while (!sb.ToString().EndsWith("\n"))
                    {
                        sb.Append("\n");
                    }
                }

                sb.Append(share ? _shareFromText : _sendFromText);
            }
            
            return sb.ToString();
        }

        private String InsertSendFrom(String text)
        {
            if (!_appendSendFrom)
            {
                return text;
            }

            return AddSendFrom(text, _fullPage, !String.IsNullOrEmpty(_picFileId));
        }

        private void SendPost_Completed(object sender, DataLoadedEventArgs e)
        {
            LoadProgressBar.Visibility = Visibility.Collapsed;
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true; //account button

            if (_postEdit.IsPostSent)
            {
                if (_postEdit.MaxUploadSize < long.MaxValue)
                {
                    App.Settings.MaxUploadSize = _postEdit.MaxUploadSize;
                }

                _uploadFileList.Clear();
                _uploadFileList.ClearCachedUploads();

                if (!String.IsNullOrEmpty(_picFileId))
                {
                    MessageBox.Show("图片已成功分享");
                }

                NavigationService.GoBack();
            }
            else
            {
                if (_postEdit.IsUploadFailed)
                {
                    MessageBox.Show("无法上载图片。如果你已多次遇到此错误，请尝试重新登录。", "上载图片失败！", MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show("请确认你的验证码输入正确，标题和内容不为空。如果你已多次遇到此错误，请尝试重新登录。", "发送失败！", MessageBoxButton.OK);
                }
                LoadEditPage();

            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            if (ConfirmExit())
            {
                _uploadFileList.Clear();
                _uploadFileList.ClearCachedUploads();

                NavigationService.GoBack();
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            SendPost();
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_url);
        }

        private void PhotoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                _newChosenPhoto = e;
            }
        }

        private void DeleteUploadButton_Click(object sender, RoutedEventArgs e)
        {
            UploadFileEntry upload = (sender as Button).Tag as UploadFileEntry;

            if (upload != null)
            {
                _uploadFileList.Remove(upload);
            }
        }

        private void AddUploadButton_Click(object sender, RoutedEventArgs e)
        {
            App.Track("Navigation", "OpenPage", "OpenPicture");
            _photoChooser.Show();
        }

        private void CameraButton_Click(object sender, RoutedEventArgs e)
        {
            App.Track("Navigation", "OpenPage", "OpenCamera");
            _cameraTask.Show();
        }

        private void AttachButton_Click(object sender, EventArgs e)
        {
            if (PivotControl.Items.Count > 1)
            {
                PivotControl.SelectedIndex = 1;
            }
        }

        private void UserButton_Click(object sender, EventArgs e)
        {
            _postEdit.ClearContent();
            PageHelper.OpenSettingPage(NavigationService, false, true);
        }

        private void FeedbackMenu_Click(object sender, EventArgs e)
        {
            PageHelper.SendFeedbackAboutContent("", _url);
        }
    }

    public class UploadFileEntry : INotifyPropertyChanged
    {
        public String FileName;
        public Guid OfflineID;

        private long _fileSize;
        public long FileSize
        {
            get
            {
                return _fileSize;
            }

            set
            {
                _fileSize = value;
                NotifyPropertyChanged("DisplayText");
            }
        }
        
        private int _index;
        public int Index
        {
            get
            {
                return _index;
            }

            set
            {
                _index = value;
                NotifyPropertyChanged("DisplayText");
            }
        }

        public String DisplayText
        {
            get
            {
                return FileSize.ToString() + " 字节";
            }
        }

        public Stream FileStream
        {
            get
            {
                Stream fileStream = null;
                App.Settings.OfflineContentManager.TryLoadOfflineContent(OfflineID, FileName, out fileStream);

                return fileStream;
            }
        }

        public ImageSource Image
        {
            get
            {
                Stream fileStream = FileStream;

                if (fileStream != null)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.SetSource(fileStream);
                    fileStream.Close();

                    return bitmap;
                }

                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class UploadFileList : ObservableCollection<UploadFileEntry>
    {
        private Guid _offlineID = Guid.NewGuid();

        public void SaveState(IDictionary<string, object> state)
        {
            state["UploadFileList_OfflineID"] = _offlineID;

            int i = 0;
            foreach (var entry in this)
            {
                state["UploadFileList_FileName_" + i] = entry.FileName;
                state["UploadFileList_FileSize_" + i] = entry.FileSize;

                i++;
            }
        }

        public void RestoreState(IDictionary<string, object> state)
        {
            if (state.ContainsKey("UploadFileList_OfflineID"))
            {
                _offlineID = (Guid)state["UploadFileList_OfflineID"];

                Clear();

                int i = 0;
                while (state.ContainsKey("UploadFileList_FileName_" + i) && state.ContainsKey("UploadFileList_FileSize_" + i))
                {
                    UploadFileEntry entry = new UploadFileEntry()
                    {
                        FileName = (String)state["UploadFileList_FileName_" + i],
                        FileSize = (long)state["UploadFileList_FileSize_" + i],
                        OfflineID = _offlineID
                    };

                    if (App.Settings.OfflineContentManager.OfflineContentExists(_offlineID, entry.FileName))
                    {
                        this.Add(entry);
                        entry.Index = this.Count;
                    }

                    i++;
                }
            }
        }

        public new void Remove(UploadFileEntry upload)
        {
            int i;
            for (i = 0; i < this.Count; i++)
            {
                if (this[i] == upload)
                {
                    this.RemoveAt(i);
                    break;
                }
            }

            for (int j = i; j < this.Count; j++)
            {
                this[j].Index = j + 1;
            }
        }

        public void ClearCachedUploads()
        {
            App.Settings.OfflineContentManager.CleanupOfflineContent(_offlineID);
        }

        public bool AddUpload(String fileName, Stream fileStream)
        {
            UploadFileEntry entry = new UploadFileEntry()
            {
                FileName = fileName,
                FileSize = fileStream.Length,
                OfflineID = _offlineID
            };

            if (App.Settings.OfflineContentManager.SaveOfflineContent(_offlineID, entry.FileName, fileStream))
            {
                this.Add(entry);
                entry.Index = this.Count;
                return true;
            }

            return false;
        }

        public long TotalUploadSize
        {
            get
            {
                long size = 0;
                foreach (var upload in this)
                {
                    size += upload.FileSize;
                }

                return size;
            }
        }
    }
}