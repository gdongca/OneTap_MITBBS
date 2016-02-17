using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Windows.Threading;
using HtmlAgilityPack;
using System.Diagnostics;

namespace Naboo.MitbbsReader.Pages
{
    public abstract class TopicPageBase: PhoneApplicationPage
    {
        protected String _originalUrl;
        protected String _url;
        protected MitbbsTopicBase _topic { get; set; }
        protected bool _resetScrollPos = true;
        protected bool _openFromBoard = false;
        protected double _scrollOffset = -1;
        protected bool _forceRefresh = false;
        protected bool _showQuickReply = true;
        protected bool _showReplyToUser = true;
        protected bool _offline = false;
        protected Guid _offlineID = OfflineContentManager.CreateNewRootID();
        protected bool _preloaded = false;
        protected bool _club = false;
        
        protected StackPanel _topicBodyPanel { get; set; }
        
        private ScrollViewer __topicScrollViewer;
        protected ScrollViewer _topicScrollViewer
        {
            get
            {
                return __topicScrollViewer;
            }

            set
            {
                __topicScrollViewer = value;
                GestureListener gl = GestureService.GetGestureListener(__topicScrollViewer);
                gl.DragCompleted += new EventHandler<DragCompletedGestureEventArgs>(ScrollViewer_DragCompleted);
                gl.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(ScrollViewer_Tapped);
                gl.DoubleTap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(ScrollViewer_DoubleTapped);
                gl.PinchCompleted += new EventHandler<Microsoft.Phone.Controls.PinchGestureEventArgs>(ScrollViewer_PinchCompleted);
            }
        }

        private TextBlock __topicTitleTextBlock;
        protected TextBlock _topicTitleTextBlock
        {
            get
            {
                return __topicTitleTextBlock;
            }

            set
            {
                __topicTitleTextBlock = value;
            }
        }

        private Grid __rootGrid;
        protected Grid _rootGrid
        {
            get
            {
                return __rootGrid;
            }

            set
            {
                __rootGrid = value;

                if (__rootGrid != null)
                {
                    _disableRect = new Rectangle()
                    {
                        Margin = new Thickness(0),
                        Fill = new SolidColorBrush(Color.FromArgb(0xB0, 0, 0, 0)),
                        Visibility = System.Windows.Visibility.Collapsed
                    };

                    _disableRect.Tap += 
                        (s, e) =>
                        {
                            HideButtonPanel();
                        };

                    __rootGrid.Children.Add(_disableRect);
                }
            }
        }

        protected ImageLoader _imageLoader = new ImageLoader();
        protected List<UIElement> _firstPostControls = new List<UIElement>();

        protected UIElement _buttonPanel = null;
        protected UIElement _disableRect = null;

        protected virtual void ClearContent()
        {
            _topic.ClearContent();
            _topicBodyPanel.Children.Clear();
            _imageLoader.ClearImages();
            _firstPostControls.Clear();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("Url"))
            {
                _originalUrl = parameters["Url"];
                _url = _originalUrl;
            }
            else
            {
                _url = null;
            }

            if (State.ContainsKey("Url"))
            {
                _url = (String)State["Url"];
            }

            if ((!_topic.IsLoaded || _forceRefresh) && State.ContainsKey("ScrollOffset"))
            {
                _scrollOffset = (double)State["ScrollOffset"];
            }
            else
            {
                _scrollOffset = -1;
            }

            String offlineID = null;
            if (parameters.ContainsKey("OfflineID"))
            {
                offlineID = parameters["OfflineID"];
            }

            if (State.ContainsKey("OfflineID"))
            {
                offlineID = (String)State["OfflineID"];
            }

            if (!string.IsNullOrEmpty(offlineID))
            {
                Guid newOfflineID;
                _offline = HtmlAgilityPack.HtmlUtilities.TryParseGuid(offlineID, out newOfflineID);

                if (_offline)
                {
                    _offlineID = newOfflineID;
                }
            }
            
            if (parameters.ContainsKey("OpenFromBoard"))
            {
                _openFromBoard = bool.Parse(Uri.UnescapeDataString(parameters["OpenFromBoard"]));
            }
            else
            {
                _openFromBoard = false;
            }

            if (_topic != null)
            {
                if (parameters.ContainsKey("Name"))
                {
                    String title = parameters["Name"];
                    if (String.IsNullOrEmpty(_topic.Title) && !String.IsNullOrEmpty(title))
                    {
                        App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, title, "阅读文章");
                        _topic.Title = title + "...";
                    }
                    else
                    {
                        App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _topic.Title, "阅读文章");
                    }
                }
            }
            else
            {
                App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, "阅读文章");
            }

            if (!_topic.IsLoaded && App.Settings.RestoreLastVisit)
            {
                MitbbsLink historyLink = App.Settings.FindHistoryEntry(_originalUrl);
                if (_scrollOffset < 0 && historyLink != null && !String.IsNullOrEmpty(historyLink.LastVisitedUrl))
                {
                    _url = historyLink.LastVisitedUrl;
                    _scrollOffset = historyLink.LastVisitedScreenPos;
                }
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            State["Url"] = _url;

            if (_topic.IsLoaded && (_topicScrollViewer != null))
            {
                State["ScrollOffset"] = _topicScrollViewer.VerticalOffset;
            }

            if (_offline)
            {
                State["OfflineID"] = _offlineID.ToString();
            }

            MitbbsLink historyLink = App.Settings.FindHistoryEntry(_originalUrl);
            if (historyLink != null && _topicScrollViewer != null && _topic != null && _topic.IsLoaded)
            {
                if (_topic.PageIndex >= 1)
                {
                    historyLink.LastVisitedUrl = _topic.Url;
                    historyLink.LastVisitedPage = _topic.PageIndex;
                    historyLink.LastVisitedScreenPos = _topicScrollViewer.VerticalOffset;
                    historyLink.LastPage = _topic.LastPageIndex;

                    if (_topic.PageIndex == _topic.LastPageIndex)
                    {
                        historyLink.LastVisitedPageContentCount = _topic.Posts.Count;
                    }
                }
                else
                {
                    historyLink.LastVisitedUrl = null;
                    historyLink.LastVisitedPage = -1;
                    historyLink.LastVisitedScreenPos = -1;
                    historyLink.LastVisitedPageContentCount = -1;
                }
            }

            base.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (HideButtonPanel())
            {
                e.Cancel = true;
            }
            else
            {
                if (!_offline)
                {
                    App.Settings.OfflineContentManager.CleanupOfflineContent(_offlineID);
                }

                _imageLoader.StopDownload();
            }

            base.OnBackKeyPress(e);
        }

        protected abstract void LoadTopic(bool resetScrollPos, int pageToLoad = -1);

        protected bool HideButtonPanel()
        {
            if (_disableRect != null)
            {
                _disableRect.Visibility = System.Windows.Visibility.Collapsed;
            }

            if (_buttonPanel != null)
            {
                __rootGrid.Children.Remove(_buttonPanel);
                _buttonPanel = null;
                return true;
            }

            return false;
        }

        protected void ShowButtonPanel(MitbbsPostBase post)
        {
            if (__rootGrid == null)
            {
                return;
            }

            if (HideButtonPanel())
            {
                return;
            }

            StackPanel buttonPanel = new StackPanel()
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                VerticalAlignment = System.Windows.VerticalAlignment.Center                
            };

            String replyToText;
            if (post != null && post.AuthorId != null)
            {
                replyToText = "回信给" + post.AuthorId;
            }
            else
            {
                replyToText = "回信给作者";
            }

            if (!_offline && post.ReplyPostUrl != null)
            {
                buttonPanel.Children.Add(CreatePostButton("reply_roundbutton.png", "回复", post, ReplyButton_Click));
            }

            if (!_offline && _showQuickReply && (post.ReplyPostUrl != null))
            {
                buttonPanel.Children.Add(CreatePostButton("quicksend_roundbutton.png", "快速回复", post, QuickReplyButton_Click));
            }

            if (!_offline && post.ForwardUrl != null)
            {
                buttonPanel.Children.Add(CreatePostButton("lines_roundbutton.png", "转贴其它版面", post, ForwardButton_Click));
            }

            if (!_offline && _showReplyToUser && (post.AuthorId != null) && !(App.WebSession.IsLoggedIn && App.WebSession.Username.ToLower() == post.AuthorId.ToLower()))
            {
                buttonPanel.Children.Add(CreatePostButton("contact_roundbutton.png", replyToText, post, ReplyUserButton_Click));
            }

            if (!_offline && App.WebSession.IsLoggedIn && App.WebSession.Username.ToLower() == post.AuthorId.ToLower())
            {
                if (post.DeletePostUrl != null)
                {
                    buttonPanel.Children.Add(CreatePostButton("trash_roundbutton.png", "删除", post, DeleteButton_Click));
                }
            }

            if (!_offline && App.WebSession.IsLoggedIn && App.WebSession.Username.ToLower() == post.AuthorId.ToLower())
            {
                if (post.ModifyPostUrl != null)
                {
                    buttonPanel.Children.Add(CreatePostButton("edit_roundbutton.png", "编辑", post, ModifyButton_Click));
                }
            }

            buttonPanel.Children.Add(CreatePostButton("copy_roundbutton.png", "复制到剪切板", post, CopyButton_Click));
            buttonPanel.Children.Add(CreatePostButton("people_roundbutton.png", "社交网络分享", post, ShareButton_Click));
            buttonPanel.Children.Add(CreatePostButton("share_roundbutton.png", "邮件分享", post, MailButton_Click));

            Border buttonBorder = new Border()
            {
                BorderBrush = (Brush)App.Current.Resources["PhoneAccentBrush"],
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(3),
                Background = (Brush)App.Current.Resources["PhoneBackgroundBrush"],
                Child = buttonPanel
            };

            StackPanel rootPanel = new StackPanel()
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = System.Windows.VerticalAlignment.Center                
            };

            rootPanel.Children.Add(buttonBorder);

            if (_disableRect != null)
            {
                _disableRect.Visibility = System.Windows.Visibility.Visible;
            }

            _buttonPanel = rootPanel;
            __rootGrid.Children.Add(_buttonPanel);
        }

        protected Grid CreatePostButton(String image, String text, Object context, RoutedEventHandler onClick)
        {
            int buttonSize = 72;

            ImageBrush buttonImageBrush = new ImageBrush()
            {
                ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/" + image, UriKind.Relative)),
                Stretch = Stretch.None
            };

            Button button = new Button()
            {            
                Style = (Style)Application.Current.Resources["ImageButton"],
                Content = buttonImageBrush,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0),
                Width = buttonSize,
                Height = buttonSize,
                DataContext = context
            };

            button.Click +=
                (s, e) =>
                {
                    HideButtonPanel();
                    onClick(s, e);
                };
            
            GestureListener gl2 = GestureService.GetGestureListener(button);
            gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);
            
            TextBlock buttonText = new TextBlock()
            {
                Text = text
            };
            buttonText.SetValue(TiltEffect.IsTiltEnabledProperty, true);
            buttonText.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            buttonText.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            buttonText.DataContext = context;
            buttonText.Tap +=
                (s, e) =>
                {
                    HideButtonPanel();
                    onClick(button, e);
                };
            
            Grid buttonRow = new Grid();
            buttonRow.ColumnDefinitions.Add(
                new ColumnDefinition()
                {
                    Width = new GridLength(buttonSize, GridUnitType.Pixel),
                }
                );
            buttonRow.ColumnDefinitions.Add(
                new ColumnDefinition()
                {
                    Width = new GridLength(1, GridUnitType.Star),
                }
                );

            button.SetValue(Grid.ColumnProperty, 0);
            buttonRow.Children.Add(button);

            buttonText.SetValue(Grid.ColumnProperty, 1);
            buttonRow.Children.Add(buttonText);
            
            return buttonRow;
        }

        protected void RenderPost(MitbbsPostBase post)
        {
            // Header
            //
            Border headerLine = new Border();
            headerLine.BorderThickness = new Thickness(1);
            headerLine.Margin = new Thickness(0, 10, 0, 0);
            //headerLine.BorderBrush = (Brush)App.Current.Resources["PhoneAccentBrush"];
            headerLine.BorderBrush = new LinearGradientBrush()
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops = new GradientStopCollection()
                    {
                        new GradientStop()
                        {
                            Color = ((SolidColorBrush)App.Current.Resources["PhoneAccentBrush"]).Color,
                            Offset = 0
                        },

                        new GradientStop()
                        {
                            Color = ((SolidColorBrush)App.Current.Resources["PhoneSubtleBrush"]).Color,
                            Offset = 0.8
                        },
                        new GradientStop()
                        {
                            Color = ((SolidColorBrush)App.Current.Resources["PhoneBackgroundBrush"]).Color,
                            Offset = 1
                        },
                    }
            };

            //headerLine.BorderBrush = (Brush)App.Current.Resources["PhoneSubtleBrush"];
            //headerLine.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 112, 146, 190));
            headerLine.Height = .5;

            _firstPostControls.Add(headerLine);
            _topicBodyPanel.Children.Add(headerLine);

            StackPanel headerPanel = new StackPanel();
            headerPanel.Orientation = System.Windows.Controls.Orientation.Vertical;

            Grid headerPanel2 = new Grid();
            ColumnDefinition column1 = new ColumnDefinition();
            column1.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition column2 = new ColumnDefinition();
            column2.Width = new GridLength(1, GridUnitType.Auto);
            headerPanel2.ColumnDefinitions.Add(column1);
            headerPanel2.ColumnDefinitions.Add(column2);

            Grid expandButtonPanel = new Grid();
            expandButtonPanel.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            StackPanel headerPanel3 = new StackPanel();
            headerPanel3.Orientation = System.Windows.Controls.Orientation.Vertical;
            headerPanel3.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;

            Button expandButton = new Button();
            ImageBrush expandButtonImage = new ImageBrush();
            expandButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/overflowdots.png", UriKind.Relative));
            expandButtonImage.Stretch = Stretch.None;
            expandButton.Style = (Style)Application.Current.Resources["ImageButton"];
            expandButton.Content = expandButtonImage;
            expandButton.Padding = new Thickness(0);
            expandButton.BorderThickness = new Thickness(0);
            expandButton.Margin = new Thickness(0);
            expandButton.Width = 93;
            expandButton.Height = 54;

            GestureListener gl = GestureService.GetGestureListener(expandButton);
            gl.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            expandButton.Click += ExpandButton_Click;

            headerPanel3.SetValue(Grid.ColumnProperty, 0);
            expandButtonPanel.SetValue(Grid.ColumnProperty, 1);
            headerPanel2.Children.Add(headerPanel3);
            headerPanel2.Children.Add(expandButtonPanel);
            expandButtonPanel.Children.Add(expandButton);

            headerPanel.Children.Add(headerPanel2);

            TextBlock authorText = new TextBlock();
            authorText.Text = "作者: " + post.Author;
            authorText.FontWeight = FontWeights.ExtraBold;
            //authorText.Style = (Style)App.Current.Resources["PhoneTextAccentStyle"];
            authorText.FontSize = (double)App.Current.Resources["MitbbsFontSizeNormal"];
            authorText.Margin = new Thickness(0);
            authorText.TextWrapping = TextWrapping.Wrap;

            TextBlock sourceText = new TextBlock();
            sourceText.Text = "时间: " + post.IssueDate;
            sourceText.FontWeight = FontWeights.ExtraBold;
            //sourceText.Style = (Style)App.Current.Resources["PhoneTextAccentStyle"];
            sourceText.FontSize = (double)App.Current.Resources["MitbbsFontSizeNormal"];
            sourceText.Margin = new Thickness(0);
            sourceText.TextWrapping = TextWrapping.Wrap;

            headerPanel3.Children.Add(authorText);
            headerPanel3.Children.Add(sourceText);

            //Header buttons
            StackPanel buttonPanel = new StackPanel();
            buttonPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
            buttonPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;

            Border buttonBorder = new Border();
            buttonBorder.BorderBrush = (Brush)App.Current.Resources["PhoneAccentBrush"];
            buttonBorder.BorderThickness = new Thickness(2);
            buttonBorder.CornerRadius = new CornerRadius(3);
            buttonBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            buttonBorder.Visibility = Visibility.Collapsed;
            buttonBorder.Child = buttonPanel;

            //expandButton.DataContext = buttonBorder;
            expandButton.DataContext = post;

            int buttonSize = 72;

            //int buttonCount = 0;
            
            //if (!_offline && post.ReplyPostUrl != null)
            //{
            //    Button replyButton = new Button();
            //    ImageBrush replyButtonImage = new ImageBrush();
            //    replyButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/reply_roundbutton.png", UriKind.Relative));
            //    replyButtonImage.Stretch = Stretch.None;
            //    replyButton.Style = (Style)Application.Current.Resources["ImageButton"];
            //    replyButton.Content = replyButtonImage;
            //    replyButton.Padding = new Thickness(0);
            //    replyButton.BorderThickness = new Thickness(0);
            //    replyButton.Margin = new Thickness(0);
            //    replyButton.Width = buttonSize;
            //    replyButton.Height = buttonSize;

            //    GestureListener gl2 = GestureService.GetGestureListener(replyButton);
            //    gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            //    replyButton.Click += ReplyButton_Click;
            //    replyButton.DataContext = post;
            //    buttonPanel.Children.Add(replyButton);
            //    buttonCount++;
            //}

            //if (!_offline && _showQuickReply && (post.ReplyPostUrl != null))
            //{
            //    Button quickReplyButton = new Button();
            //    ImageBrush quickReplyButtonImage = new ImageBrush();
            //    quickReplyButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/quicksend_roundbutton.png", UriKind.Relative));
            //    quickReplyButtonImage.Stretch = Stretch.None;
            //    quickReplyButton.Style = (Style)Application.Current.Resources["ImageButton"];
            //    quickReplyButton.Content = quickReplyButtonImage;
            //    quickReplyButton.Padding = new Thickness(0);
            //    quickReplyButton.BorderThickness = new Thickness(0);
            //    quickReplyButton.Margin = new Thickness(0);
            //    quickReplyButton.Width = buttonSize;
            //    quickReplyButton.Height = buttonSize;

            //    GestureListener gl2 = GestureService.GetGestureListener(quickReplyButton);
            //    gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            //    quickReplyButton.Click += QuickReplyButton_Click;
            //    quickReplyButton.DataContext = post;
            //    buttonPanel.Children.Add(quickReplyButton);
            //    buttonCount++;
            //}

            //if (!_offline && post.ForwardUrl != null)
            //{
            //    Button forwardButton = new Button();
            //    ImageBrush forwardButtonImage = new ImageBrush();
            //    forwardButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/Clipboard-file_roundbutton.png", UriKind.Relative));
            //    forwardButtonImage.Stretch = Stretch.None;
            //    forwardButton.Style = (Style)Application.Current.Resources["ImageButton"];
            //    forwardButton.Content = forwardButtonImage;
            //    forwardButton.Padding = new Thickness(0);
            //    forwardButton.BorderThickness = new Thickness(0);
            //    forwardButton.Margin = new Thickness(0);
            //    forwardButton.Width = buttonSize;
            //    forwardButton.Height = buttonSize;

            //    GestureListener gl2 = GestureService.GetGestureListener(forwardButton);
            //    gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            //    forwardButton.Click += ForwardButton_Click;
            //    forwardButton.DataContext = post;
            //    buttonPanel.Children.Add(forwardButton);
            //    buttonCount++;
            //}

            //if (!_offline && _showReplyToUser && (post.AuthorId != null) && !(App.WebSession.IsLoggedIn && App.WebSession.Username.ToLower() == post.AuthorId.ToLower()))
            //{
            //    Button replyUserButton = new Button();
            //    ImageBrush replyUserButtonImage = new ImageBrush();
            //    replyUserButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/contact_roundbutton.png", UriKind.Relative));
            //    replyUserButtonImage.Stretch = Stretch.None;
            //    replyUserButton.Style = (Style)Application.Current.Resources["ImageButton"];
            //    replyUserButton.Content = replyUserButtonImage;
            //    replyUserButton.Padding = new Thickness(0);
            //    replyUserButton.BorderThickness = new Thickness(0);
            //    replyUserButton.Margin = new Thickness(0);
            //    replyUserButton.Width = buttonSize;
            //    replyUserButton.Height = buttonSize;

            //    GestureListener gl2 = GestureService.GetGestureListener(replyUserButton);
            //    gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            //    replyUserButton.Click += ReplyUserButton_Click;
            //    replyUserButton.DataContext = post;
            //    buttonPanel.Children.Add(replyUserButton);
            //    buttonCount++;
            //}

            //if (!_offline && App.WebSession.IsLoggedIn && App.WebSession.Username.ToLower() == post.AuthorId.ToLower())
            //{
            //    if (post.DeletePostUrl != null)
            //    {
            //        Button deleteButton = new Button();
            //        ImageBrush deleteButtonImage = new ImageBrush();
            //        deleteButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/trash_roundbutton.png", UriKind.Relative));
            //        deleteButtonImage.Stretch = Stretch.None;
            //        deleteButton.Style = (Style)Application.Current.Resources["ImageButton"];
            //        deleteButton.Content = deleteButtonImage;
            //        deleteButton.Padding = new Thickness(0);
            //        deleteButton.BorderThickness = new Thickness(0);
            //        deleteButton.Margin = new Thickness(0);
            //        deleteButton.Width = buttonSize;
            //        deleteButton.Height = buttonSize;

            //        GestureListener gl2 = GestureService.GetGestureListener(deleteButton);
            //        gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            //        deleteButton.Click += DeleteButton_Click;
            //        deleteButton.DataContext = post;
            //        buttonPanel.Children.Add(deleteButton);
            //        buttonCount++;
            //    }
            //}

            //if (!_offline && App.WebSession.IsLoggedIn && App.WebSession.Username.ToLower() == post.AuthorId.ToLower())
            //{
            //    if (post.ModifyPostUrl != null)
            //    {
            //        Button modifyButton = new Button();
            //        ImageBrush modifyButtonImage = new ImageBrush();
            //        modifyButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/edit_roundbutton.png", UriKind.Relative));
            //        modifyButtonImage.Stretch = Stretch.None;
            //        modifyButton.Style = (Style)Application.Current.Resources["ImageButton"];
            //        modifyButton.Content = modifyButtonImage;
            //        modifyButton.Padding = new Thickness(0);
            //        modifyButton.BorderThickness = new Thickness(0);
            //        modifyButton.Margin = new Thickness(0);
            //        modifyButton.Width = buttonSize;
            //        modifyButton.Height = buttonSize;

            //        GestureListener gl2 = GestureService.GetGestureListener(modifyButton);
            //        gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            //        modifyButton.Click += ModifyButton_Click;
            //        modifyButton.DataContext = post;
            //        buttonPanel.Children.Add(modifyButton);
            //        buttonCount++;
            //    }
            //}

            //{
            //    Button shareButton = new Button();
            //    ImageBrush shareButtonImage = new ImageBrush();
            //    shareButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/share_roundbutton.png", UriKind.Relative));
            //    shareButtonImage.Stretch = Stretch.None;
            //    shareButton.Style = (Style)Application.Current.Resources["ImageButton"];
            //    shareButton.Content = shareButtonImage;
            //    shareButton.Padding = new Thickness(0);
            //    shareButton.BorderThickness = new Thickness(0);
            //    shareButton.Margin = new Thickness(0);
            //    shareButton.Width = buttonSize;
            //    shareButton.Height = buttonSize;

            //    GestureListener gl2 = GestureService.GetGestureListener(shareButton);
            //    gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

            //    shareButton.Click += ShareButton_Click;
            //    shareButton.DataContext = post;
            //    buttonPanel.Children.Add(shareButton);
            //    buttonCount++;
            //}

            //if (buttonCount > 0)
            //{
            //    buttonBorder.Width = buttonCount * buttonSize;
            //    headerPanel.Children.Add(buttonBorder);
            //}
            //else
            //{
            //    expandButtonPanel.Visibility = Visibility.Collapsed;
            //}

            _topicBodyPanel.Children.Add(headerPanel);

            // Body
            //
            bool firstContent = true;
            for (int index = 0; index < post.Contents.Count; index++)
            {
                ContentBlock content = post.Contents[index];
                bool isSignature = (post.SignatureStart >= 0) && (index > post.SignatureStart);

                if (content is ImageBlock)
                {
                    String imageUrl = (content as ImageBlock).ImageUrl;

                    try
                    {
                        _imageLoader.LoadImage(
                                        imageUrl,
                                        HtmlUtilities.GetAbsoluteUrl(imageUrl, "/")
                                        );
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
                else if (content is LinkBlock)
                {
                    String url = (content as LinkBlock).Url;
                    if (MitbbsLinkConverter.IsMitbbsFullTopicLink(url))
                    {
                        Button articleButton = new Button();
                        ImageBrush articleButtonImage = new ImageBrush();
                        articleButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/right_roundbutton.png", UriKind.Relative));
                        articleButtonImage.Stretch = Stretch.Uniform;
                        articleButton.Style = (Style)Application.Current.Resources["ImageButton"];
                        articleButton.Content = articleButtonImage;
                        articleButton.Padding = new Thickness(0);
                        articleButton.BorderThickness = new Thickness(0);
                        articleButton.Margin = new Thickness(0);

                        GestureListener gl2 = GestureService.GetGestureListener(articleButton);
                        gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

                        articleButton.Click += ArticleButton_Click;
                        articleButton.DataContext = content;

                        HyperlinkButton link = new HyperlinkButton();
                        link.SetValue(TiltEffect.IsTiltEnabledProperty, true);
                        link.Click += GeneralLink_Click;
                        link.DataContext = content;
                        link.Content = "打开未名空间文章";
                        link.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        link.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;

                        Border articlePanelBorder = new Border();
                        articlePanelBorder.BorderBrush = (Brush)App.Current.Resources["PhoneForegroundBrush"];
                        articlePanelBorder.BorderThickness = new Thickness(2);

                        if (isSignature)
                        {
                            articleButton.Width = buttonSize * 2 / 3;
                            articleButton.Height = buttonSize * 2 / 3;
                            articleButton.Foreground = (Brush)App.Current.Resources["PhoneSubtleBrush"];
                            link.FontSize = (double)App.Current.Resources["PhoneFontSizeSmall"];
                            link.Foreground = (Brush)App.Current.Resources["PhoneSubtleBrush"];
                            articlePanelBorder.BorderBrush = (Brush)App.Current.Resources["PhoneSubtleBrush"];
                            articlePanelBorder.BorderThickness = new Thickness(1);
                            articlePanelBorder.Width = 300;
                            articlePanelBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        }
                        else
                        {
                            articleButton.Width = buttonSize;
                            articleButton.Height = buttonSize;
                            link.FontSize = (double)App.Current.Resources["MitbbsFontSizeText"];
                            articlePanelBorder.BorderBrush = (Brush)App.Current.Resources["PhoneForegroundBrush"];
                            articlePanelBorder.BorderThickness = new Thickness(2);
                            articlePanelBorder.Width = 350;
                        }

                        StackPanel articlePanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };

                        articlePanelBorder.Child = articlePanel;
                        articlePanel.Children.Add(articleButton);
                        articlePanel.Children.Add(link);

                        _topicBodyPanel.Children.Add(articlePanelBorder);
                    }
                    else
                    {
                        HyperlinkButton link = new HyperlinkButton();
                        link.SetValue(TiltEffect.IsTiltEnabledProperty, true);

                        if (isSignature)
                        {
                            link.FontSize = (double)App.Current.Resources["PhoneFontSizeSmall"];
                            link.Foreground = (Brush)App.Current.Resources["PhoneSubtleBrush"];
                        }
                        else
                        {
                            link.FontSize = (double)App.Current.Resources["MitbbsFontSizeText"];
                        }

                        //link.TargetName = "_new";
                        //link.NavigateUri = new Uri((content as LinkBlock).Url, UriKind.Absolute);
                        link.Click += GeneralLink_Click;
                        link.DataContext = content;
                        link.Content = (content as LinkBlock).Text;
                        link.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        link.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                        _topicBodyPanel.Children.Add(link);
                    }
                }
                else if (content is VideoBlock)
                {
                    Button playButton = new Button();
                    ImageBrush playButtonImage = new ImageBrush();
                    playButtonImage.ImageSource = new BitmapImage(new Uri("/MitbbsReader;component/Images/play_roundbutton.png", UriKind.Relative));
                    playButtonImage.Stretch = Stretch.Uniform;
                    playButton.Style = (Style)Application.Current.Resources["ImageButton"];
                    playButton.Content = playButtonImage;
                    playButton.Padding = new Thickness(0);
                    playButton.BorderThickness = new Thickness(0);
                    playButton.Margin = new Thickness(0);

                    GestureListener gl2 = GestureService.GetGestureListener(playButton);
                    gl2.Tap += new EventHandler<Microsoft.Phone.Controls.GestureEventArgs>(GeneralButton_Tapped);

                    playButton.Click += PlayButton_Click;
                    playButton.DataContext = content;

                    HyperlinkButton link = new HyperlinkButton();
                    link.SetValue(TiltEffect.IsTiltEnabledProperty, true);

                    Border playPanelBorder = new Border();

                    if (isSignature)
                    {
                        playButton.Width = buttonSize * 2 / 3;
                        playButton.Height = buttonSize * 2 / 3;
                        playButton.Foreground = (Brush)App.Current.Resources["PhoneSubtleBrush"];
                        link.FontSize = (double)App.Current.Resources["PhoneFontSizeSmall"];
                        link.Foreground = (Brush)App.Current.Resources["PhoneSubtleBrush"];
                        playPanelBorder.BorderBrush = (Brush)App.Current.Resources["PhoneSubtleBrush"];
                        playPanelBorder.BorderThickness = new Thickness(1);
                        playPanelBorder.Width = 200;
                        playPanelBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    }
                    else
                    {
                        playButton.Width = buttonSize;
                        playButton.Height = buttonSize;
                        link.FontSize = (double)App.Current.Resources["MitbbsFontSizeText"];
                        playPanelBorder.BorderBrush = (Brush)App.Current.Resources["PhoneForegroundBrush"];
                        playPanelBorder.BorderThickness = new Thickness(2);
                        playPanelBorder.Width = 300;
                    }

                    link.Content = "点击播放视频";
                    link.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    link.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                    link.DataContext = content;

                    link.Click += VideoLink_Click;

                    StackPanel playPanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };

                    playPanelBorder.Child = playPanel;
                    playPanel.Children.Add(playButton);
                    playPanel.Children.Add(link);

                    _topicBodyPanel.Children.Add(playPanelBorder);
                }
                else if (content is TextContentBlock)
                {
                    String contentText = (content as TextContentBlock).Text;

                    if (firstContent)
                    {
                        if (!contentText.StartsWith("\n"))
                        {
                            contentText = "\n" + contentText;
                        }

                        firstContent = false;
                    }

                    TextBlock tb = new TextBlock();
                    tb.Text = contentText;
                    tb.TextWrapping = TextWrapping.Wrap;

                    if (content is QuoteBlock)
                    {
                        tb.Style = (Style)App.Current.Resources["PhoneTextSubtleStyle"];
                        tb.FontSize = (double)App.Current.Resources["MitbbsFontSizeNormal"];
                        Border bd = new Border();
                        Thickness bdMgn = new Thickness(0);
                        bdMgn.Left = 20;

                        bd.Margin = bdMgn;
                        bd.BorderThickness = new Thickness(1);
                        bd.CornerRadius = new CornerRadius(8);
                        bd.BorderBrush = (Brush)App.Current.Resources["PhoneSubtleBrush"];

                        bd.Child = tb;
                        _topicBodyPanel.Children.Add(bd);
                    }
                    else
                    {
                        if (isSignature)
                        {
                            tb.Style = (Style)App.Current.Resources["PhoneTextSubtleStyle"];
                            tb.FontSize = (double)App.Current.Resources["MitbbsFontSizeNormal"];
                        }

                        _topicBodyPanel.Children.Add(tb);
                    }
                }
            }

            // Padding
            //
            Grid sep = new Grid();
            sep.Margin = new Thickness(10);

            _topicBodyPanel.Children.Add(sep);
        }

        protected void RenderTopic()
        {
            if ((_topic == null) || 
                !_topic.IsLoaded || 
                (_topicTitleTextBlock == null) || 
                (_topicBodyPanel == null) ||
                (_topicScrollViewer == null))
            {
                return;
            }

            MitbbsLink historyLink = App.Settings.FindHistoryEntry(_originalUrl);
            if (historyLink != null && _topicScrollViewer != null)
            {
                if (_topic != null && _topic.PageIndex >= 1)
                {
                    historyLink.LastVisitedUrl = _topic.Url;
                    historyLink.LastPage = _topic.LastPageIndex;

                    if (_topic.PageIndex == _topic.LastPageIndex)
                    {
                        historyLink.LastVisitedPageContentCount = _topic.Posts.Count;
                    }
                    
                    if (_topic.PageIndex == 1)
                    {
                        historyLink.Name = _topic.Title;
                    }
                }
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _topic.Title, "阅读文章");

            CookieAwareClient web = new CookieAwareClient();
            web.Cookies = App.WebSession.Cookies;

            _imageLoader.Web = web;
            _imageLoader.DisplayPanel = _topicBodyPanel;
            _imageLoader.ScrollPanel = _topicScrollViewer;
            _imageLoader.Page = this;
            _imageLoader.FontSize = (double)App.Current.Resources["MitbbsFontSizeText"];
            _imageLoader.IsOffline = _offline;
            _imageLoader.OfflineID = _offlineID;
            _imageLoader.IsClub = _club;

            _topicTitleTextBlock.Visibility = Visibility.Collapsed;
            
            TextBlock titleText = new TextBlock();
            //titleText.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            //titleText.TextAlignment = TextAlignment.Center;
            titleText.Style = (Style)App.Current.Resources["PhoneTextAccentStyle"];
            titleText.FontSize = (double)App.Current.Resources["MitbbsFontSizeText"];
            titleText.Margin = new Thickness(2);
            titleText.TextWrapping = TextWrapping.Wrap;
            titleText.Text = _topic.Title;

            _topicBodyPanel.Children.Add(titleText);

            if (!String.IsNullOrEmpty(_topic.BoardName))
            {
                TextBlock boardText = new TextBlock();
                //boardText.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                //boardText.TextAlignment = TextAlignment.Center;
                boardText.Style = (Style)App.Current.Resources["PhoneTextAccentStyle"];
                boardText.FontSize = (double)App.Current.Resources["MitbbsFontSizeNormal"];
                boardText.Margin = new Thickness(0, 3, 0, 3);
                boardText.TextWrapping = TextWrapping.Wrap;
                boardText.Text = "版面: " + _topic.BoardName;
                _topicBodyPanel.Children.Add(boardText);
            }

            foreach (MitbbsPostBase post in _topic.Posts)
            {
                RenderPost(post);
            }

            // Ending the page
            //
            Border bd2 = new Border();
            bd2.Margin = new Thickness(0);
            bd2.BorderThickness = new Thickness(1);
            bd2.CornerRadius = new CornerRadius(8);
            bd2.BorderBrush = (Brush)App.Current.Resources["PhoneAccentBrush"];
            bd2.HorizontalAlignment = HorizontalAlignment.Stretch;

            TextBlock tb2 = new TextBlock();
            tb2.HorizontalAlignment = HorizontalAlignment.Center;
            tb2.Style = (Style)App.Current.Resources["PhoneTextSubtleStyle"];
            tb2.FontSize = (double)App.Current.Resources["MitbbsFontSizeNormal"];
            if (_topic.NextPageUrl != null)
            {
                tb2.Text = "本页末尾";
            }
            else
            {
                tb2.Text = "文章末尾";
            }

            bd2.Child = tb2;
            _topicBodyPanel.Children.Add(bd2);

            _topicBodyPanel.Visibility = Visibility.Visible;

            if (_scrollOffset >= 0)
            {
                _topicScrollViewer.UpdateLayout();
                _topicScrollViewer.ScrollToVerticalOffset(_scrollOffset);
                _scrollOffset = -1;
            }
            else if (_resetScrollPos)
            {
                _topicScrollViewer.UpdateLayout();
                _topicScrollViewer.ScrollToVerticalOffset(0);
            }
        }

        protected void GeneralButton_Tapped(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            // This tap handler is created to stop propagating the tap event
            //
            e.Handled = true;
        }

        protected bool _handleTap = true;
        protected bool _ignoreDoubleTap = false;
        protected double? _offsetAtTap = null;
        protected void ScrollViewer_Tapped(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            _offsetAtTap = null;
            _ignoreDoubleTap = false;

            if (HideButtonPanel())
            {
                return;
            }
            
            if (App.Settings.IsTapIgnored)
            {
                return;
            }

            ScrollViewer scrollViewer = (sender as ScrollViewer);
            scrollViewer.UpdateLayout();
            _offsetAtTap = scrollViewer.VerticalOffset;

            if (scrollViewer != null)
            {
                _handleTap = true;
                Naboo.AppUtil.AsyncCallHelper.DelayCall(
                    () =>
                    {
                        _ignoreDoubleTap = true;
                        
                        if (!_handleTap)
                        {
                            return;
                        }

                        try
                        {
                            Point tapPos = e.GetPosition(scrollViewer);

                            if (tapPos.Y < (scrollViewer.ActualHeight / 6))
                            {
                                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (scrollViewer.ActualHeight - 60));
                            }
                            else if (tapPos.Y > (scrollViewer.ActualHeight * 5 / 6))
                            {
                                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + (scrollViewer.ActualHeight - 60));
                            }
                        }
                        catch (Exception)
                        {
                        }
                    },
                    300
                    );
            }
        }

        protected void ScrollViewer_DoubleTapped(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            double? offsetAtTap = _offsetAtTap;

            _handleTap = false;
            _offsetAtTap = null;
            if (App.Settings.IsTapIgnored)
            {
                return;
            }

            if (_ignoreDoubleTap)
            {
                return;
            }

            ScrollViewer scrollViewer = (sender as ScrollViewer);

            if (scrollViewer != null)
            {
                Point tapPos = e.GetPosition(scrollViewer);

                if (tapPos.Y < (scrollViewer.ActualHeight / 6))
                {
                    scrollViewer.ScrollToVerticalOffset(0);
                }
                else if (tapPos.Y > (scrollViewer.ActualHeight * 5 / 6))
                {
                    bool foundNextPost = false;
                    //scrollViewer.UpdateLayout();
                    //_topicBodyPanel.UpdateLayout();

                    //if (offsetAtTap.HasValue)
                    //{
                    //    offsetAtTap = offsetAtTap.Value + scrollViewer.ActualHeight;
                    //}
                    //else
                    //{
                    //    offsetAtTap = scrollViewer.VerticalOffset + scrollViewer.ActualHeight;
                    //}

                    //foreach (UIElement control in _firstPostControls)
                    //{
                    //    if (_topicBodyPanel.Children.Contains(control))
                    //    {
                    //        GeneralTransform transformToVisual = control.TransformToVisual(_topicBodyPanel);
                    //        Point controlPos = transformToVisual.Transform(new Point(0, 0));

                    //        if (controlPos.Y > offsetAtTap.Value)
                    //        {
                    //            scrollViewer.ScrollToVerticalOffset(controlPos.Y);
                    //            foundNextPost = true;
                    //            break;
                    //        }
                    //    }
                    //}

                    if (!foundNextPost)
                    {
                        scrollViewer.ScrollToVerticalOffset(double.MaxValue);
                    }
                }
            }
        }

        private void ScrollViewer_PinchCompleted(object sender, PinchGestureEventArgs e)
        {
            double angleDelta = e.TotalAngleDelta;

            if (angleDelta > 75)
            {
                if (this.Orientation == PageOrientation.PortraitUp)
                {
                    this.SupportedOrientations = SupportedPageOrientation.Landscape;
                    this.Orientation = PageOrientation.LandscapeLeft;
                }
                else if (this.Orientation == PageOrientation.LandscapeRight)
                {
                    this.SupportedOrientations = SupportedPageOrientation.Portrait;
                    this.Orientation = PageOrientation.Portrait;
                }
            }
            else if (angleDelta < -75)
            {
                if (this.Orientation == PageOrientation.PortraitUp)
                {
                    this.SupportedOrientations = SupportedPageOrientation.Landscape;
                    this.Orientation = PageOrientation.LandscapeRight;
                }
                else if (this.Orientation == PageOrientation.LandscapeLeft)
                {
                    this.SupportedOrientations = SupportedPageOrientation.Portrait;
                    this.Orientation = PageOrientation.Portrait;
                }
            }
        }

        protected void GeneralLink_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkButton linkButton = (sender as HyperlinkButton);
            if (linkButton != null)
            {
                LinkBlock linkBlock = (linkButton.DataContext as LinkBlock);
                PageHelper.OpenGeneralLink(linkBlock.Url, NavigationService);
            }

            App.Settings.SetIgnoreTap();
        }

        protected void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PageHelper.OpenVideoLink(((sender as Button).DataContext as VideoBlock).VideoLink);
        }

        protected void ArticleButton_Click(object sender, RoutedEventArgs e)
        {
            Button linkButton = (sender as Button);
            if (linkButton != null)
            {
                LinkBlock linkBlock = (linkButton.DataContext as LinkBlock);
                PageHelper.OpenGeneralLink(linkBlock.Url, NavigationService);
            }
        }

        protected void VideoLink_Click(object sender, RoutedEventArgs e)
        {
            PageHelper.OpenVideoLink(((sender as HyperlinkButton).DataContext as VideoBlock).VideoLink);
        }

        protected void ExpandButton_Click(object sender, EventArgs e)
        {
            Button expandButton = sender as Button;
            if (expandButton.DataContext != null)
            {
                MitbbsPostBase post = (expandButton.DataContext as MitbbsPostBase);
                ShowButtonPanel(post);

                //UIElement buttonPanel = ((sender as Button).DataContext as UIElement);

                //if (buttonPanel.Visibility == Visibility.Visible)
                //{
                //    buttonPanel.Visibility = Visibility.Collapsed;
                //}
                //else
                //{
                //    buttonPanel.Visibility = Visibility.Visible;
                //}
            }
        }

        protected void ScrollViewer_DragCompleted(object sender, DragCompletedGestureEventArgs e)
        {
            if (e.Direction == System.Windows.Controls.Orientation.Horizontal)
            {
                if ((e.VerticalChange < 50) && (e.VerticalChange > -50))
                {
                    if (e.HorizontalChange > 200)
                    {
                        if (_topic.PrevPageUrl != null)
                        {
                            _url = _topic.PrevPageUrl;
                            LoadTopic(true);
                        }
                        else
                        {
                            NavigationService.GoBack();
                        }
                    }
                    else if (e.HorizontalChange < -200)
                    {
                        if (_topic.NextPageUrl != null)
                        {
                            _url = _topic.NextPageUrl;
                            LoadTopic(true);
                        }
                    }
                }
            }
        }

        protected virtual void ShareButton_Click(object sender, EventArgs e)
        {
            MitbbsPostBase post = (sender as Button).DataContext as MitbbsPostBase;

            if(post != null)
            {
                String postUrl;
                if (post.PostUrl != null)
                {
                    postUrl = post.PostUrl;
                }
                else
                {
                    postUrl = _topic.Url;
                }

                ShareLinkTask shareTask = new ShareLinkTask()
                {
                    Title = post.Title,
                    Message = "Shared from OneTap MITBBS for Windows Phone",
                    LinkUri = new Uri(postUrl)
                };

                shareTask.Show();

                App.Track("Navigation", "OpenPage", "SocialShare");
            }
        }

        protected virtual void MailButton_Click(object sender, EventArgs e)
        {
            MitbbsPostBase post = (sender as Button).DataContext as MitbbsPostBase;

            if (post != null)
            {
                String postUrl;
                if (post.PostUrl != null)
                {
                    postUrl = post.PostUrl;
                }
                else
                {
                    postUrl = _topic.Url;
                }

                String text = "\n\n文章链接：" + postUrl + "\n\n";
                text += post.GetText();

                EmailComposeTask emailTask = new EmailComposeTask();

                emailTask.Subject = "FW: " + post.Title;
                emailTask.Body = text;
                emailTask.Show();

                App.Track("Navigation", "OpenPage", "Share");
            }
        }

        protected virtual void CopyButton_Click(object sender, EventArgs e)
        {
            MitbbsPostBase post = (sender as Button).DataContext as MitbbsPostBase;

            if (post != null)
            {
                String postUrl;
                if (post.PostUrl != null)
                {
                    postUrl = post.PostUrl;
                }
                else
                {
                    postUrl = _topic.Url;
                }

                String text = "\n\n文章链接：" + postUrl + "\n\n";
                text += post.GetText();

                Clipboard.SetText(text);

                App.Track("Navigation", "OpenPage", "CopyPost");
            }
        }

        protected virtual void ReplyUserButton_Click(object sender, EventArgs e)
        {
            MitbbsPostBase post = (sender as Button).DataContext as MitbbsPostBase;

            if ((post != null) && (post.AuthorId != null))
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String postTitle;
                    String postBody;

                    MitbbsPostEditBase.GenerateReplyTemplate(post, out postTitle, out postBody);

                    String pageUrl = String.Format(
                                                "/Pages/NewPostPage.xaml?Url={0}&PageTitle={1}&SendFrom=false&PostTitle={2}&PostBody={3}&Recipient={4}", 
                                                Uri.EscapeUriString(App.Settings.BuildUrl(MitbbsPostEditMobile.MailEditPageUrl)), 
                                                Uri.EscapeUriString("发送邮件"), 
                                                Uri.EscapeUriString(postTitle), 
                                                Uri.EscapeUriString(postBody), 
                                                Uri.EscapeUriString(post.AuthorId)
                                                );

                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录，无法回复邮件到用户。请到设置页面中设置账户信息并登录。", "无法回复到用户", MessageBoxButton.OK);
                }
            }
        }

        protected abstract void ReplyButton_Click(object sender, EventArgs e);

        protected abstract void ForwardButton_Click(object sender, EventArgs e);

        protected abstract void ModifyButton_Click(object sender, EventArgs e);

        protected abstract void DeleteButton_Click(object sender, EventArgs e);

        protected abstract void QuickReplyButton_Click(object sender, EventArgs e);
    }
}
