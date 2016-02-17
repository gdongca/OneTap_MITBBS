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
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader.Pages
{
    public partial class QuickReplyPage : PhoneApplicationPage
    {
        public class QuickReplyItem
        {
            public String Text { get; set; }
        };

        public ObservableCollection<QuickReplyItem> QuickReplies { get; private set; }
        
        private String _url;
        private bool _fullPage = false;
        private MitbbsPostEditBase _postEdit;
        private String _titleText = "";
        private String _bodyText = "";
        private String _replyText = "";
        
        public QuickReplyPage()
        {
            InitializeComponent();

            QuickReplies = new ObservableCollection<QuickReplyItem>();
            DataContext = this;

            QuickReplies.Add(new QuickReplyItem() { Text = "顶" });
            QuickReplies.Add(new QuickReplyItem() { Text = "吃包子" });
            QuickReplies.Add(new QuickReplyItem() { Text = "沙发" });
            QuickReplies.Add(new QuickReplyItem() { Text = "板凳" });
            QuickReplies.Add(new QuickReplyItem() { Text = "楼主威武" });
            QuickReplies.Add(new QuickReplyItem() { Text = "兰州烧饼" });
            QuickReplies.Add(new QuickReplyItem() { Text = "飘过" });
            QuickReplies.Add(new QuickReplyItem() { Text = "笑而不语" });
            QuickReplies.Add(new QuickReplyItem() { Text = "我就是来打酱油的" });
            QuickReplies.Add(new QuickReplyItem() { Text = "Peng" });
            QuickReplies.Add(new QuickReplyItem() { Text = "orz" });

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            App.Track("Navigation", "OpenPage", "QuickReply");
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
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

            if (parameters.ContainsKey("BlankText"))
            {
                bool useBlankText = bool.Parse(parameters["BlankText"]);
                if (useBlankText)
                {
                    _bodyText = NewPostPage.AddSendFrom("", _fullPage);
                    if (String.IsNullOrEmpty(_bodyText))
                    {
                        _bodyText = " ";
                    }
                }
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

            base.OnNavigatedTo(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void SendQuickReply()
        {
            if (App.WebSession.IsLoggedIn)
            {
                LoadProgressBar.Visibility = Visibility.Visible;
                DisableRect.Visibility = Visibility.Visible;

                if (_url != null)
                {
                    _postEdit.LoadEditPage(App.WebSession.CreateWebClient(), _url);
                }
                else
                {
                    PageTitle.Text = "参数错误!";
                }
            }
            else
            {
                MessageBox.Show("用户尚未登录");
            }
        }

        private void EditPage_Loaded(object sender, DataLoadedEventArgs e)
        {
            if (_postEdit.IsEditPageLoaded)
            {
                if (_titleText == "")
                {
                    _titleText = _postEdit.PostTitle;
                }

                if (_bodyText == "")
                {
                    _bodyText = NewPostPage.AddSendFrom(_postEdit.PostBody, _fullPage);
                }

                _bodyText = _replyText + "\n\n" + _bodyText.Trim('\n');

                if (_postEdit.VerifyImageUrl != null)
                {
                    MessageBox.Show("你的账号需要输入验证码，无法使用一键回复，请使用正式的回复功能来回复文章", "无法一键回复", MessageBoxButton.OK);
                    NavigationService.GoBack();
                    return;
                }

                _postEdit.PostTitle = _titleText;
                _postEdit.PostBody = _bodyText;

                _postEdit.SendPost();
                App.ForceRefreshContent = true;
            }
            else
            {
                MessageBox.Show("一键回复失败！如果你多次遇到此错误，请尝试重新登录。");
                NavigationService.GoBack();
            }
        }

        private void SendPost_Completed(object sender, DataLoadedEventArgs e)
        {
            if (_postEdit.IsPostSent)
            {
                NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("一键回复失败！如果你已多次遇到此错误，请尝试重新登录。", "发送失败！", MessageBoxButton.OK);
                NavigationService.GoBack();
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            QuickReplyItem item = ((sender as ListBox).SelectedItem as QuickReplyItem);
            if (item != null)
            {
                _replyText = item.Text;
                SendQuickReply();
            }

            (sender as ListBox).SelectedItem = null;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        
    }
}